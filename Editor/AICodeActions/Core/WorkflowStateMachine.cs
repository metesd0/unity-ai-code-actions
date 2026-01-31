using System;
using System.Collections.Generic;
using UnityEngine;

namespace AICodeActions.Core
{
    /// <summary>
    /// State machine for workflow orchestration - prevents infinite loops
    /// </summary>
    public class WorkflowStateMachine
    {
        public enum State
        {
            Idle,           // No active workflow
            Planning,       // Creating task plan (expensive model)
            Executing,      // Executing current step (cheap model)
            Validating,     // Validating step result (cheap model)
            Healing,        // Deciding how to recover from error
            Replanning,     // Revising plan due to major issue
            Complete,       // Workflow successfully completed
            Failed          // Workflow failed (max retries exceeded)
        }
        
        public State CurrentState { get; private set; } = State.Idle;
        public int CurrentIteration { get; private set; } = 0;
        public int MaxIterations { get; set; } = 50;
        
        public bool IsActive => CurrentState != State.Idle && 
                                CurrentState != State.Complete && 
                                CurrentState != State.Failed;
        
        public bool CanContinue => IsActive && CurrentIteration < MaxIterations;
        
        // State history for debugging
        private List<StateTransition> history = new List<StateTransition>();
        
        public event Action<State, State> OnStateChanged;
        
        public WorkflowStateMachine()
        {
            Reset();
        }
        
        /// <summary>
        /// Transition to a new state with validation
        /// </summary>
        public bool TransitionTo(State newState, string reason = "")
        {
            // Validate transition is allowed
            if (!IsValidTransition(CurrentState, newState))
            {
                Debug.LogWarning($"[WorkflowStateMachine] Invalid transition: {CurrentState} → {newState}");
                return false;
            }
            
            // Check iteration limit
            if (CurrentIteration >= MaxIterations && newState != State.Failed)
            {
                Debug.LogError($"[WorkflowStateMachine] Max iterations ({MaxIterations}) reached! Forcing Failed state.");
                newState = State.Failed;
                reason = $"Max iterations exceeded ({MaxIterations})";
            }
            
            var oldState = CurrentState;
            CurrentState = newState;
            CurrentIteration++;
            
            // Record history
            history.Add(new StateTransition
            {
                FromState = oldState,
                ToState = newState,
                Iteration = CurrentIteration,
                Timestamp = DateTime.Now,
                Reason = reason
            });
            
            Debug.Log($"[WorkflowStateMachine] {oldState} → {newState} (iteration {CurrentIteration}/{MaxIterations}) {reason}");
            
            OnStateChanged?.Invoke(oldState, newState);
            
            return true;
        }
        
        /// <summary>
        /// Validates if a state transition is allowed
        /// </summary>
        private bool IsValidTransition(State from, State to)
        {
            // Always allow transition to Failed or Idle
            if (to == State.Failed || to == State.Idle) return true;
            
            // Define valid transitions
            switch (from)
            {
                case State.Idle:
                    return to == State.Planning;
                
                case State.Planning:
                    return to == State.Executing || to == State.Failed;
                
                case State.Executing:
                    return to == State.Validating || to == State.Healing || to == State.Complete;
                
                case State.Validating:
                    return to == State.Executing || to == State.Healing || to == State.Complete;
                
                case State.Healing:
                    return to == State.Executing || to == State.Replanning || to == State.Failed;
                
                case State.Replanning:
                    return to == State.Executing || to == State.Failed;
                
                case State.Complete:
                case State.Failed:
                    return to == State.Idle; // Can only reset
                
                default:
                    return false;
            }
        }
        
        /// <summary>
        /// Reset state machine to idle
        /// </summary>
        public void Reset()
        {
            CurrentState = State.Idle;
            CurrentIteration = 0;
            history.Clear();
            Debug.Log("[WorkflowStateMachine] Reset to Idle");
        }
        
        /// <summary>
        /// Get human-readable state description
        /// </summary>
        public string GetStateDescription()
        {
            return CurrentState switch
            {
                State.Idle => "Ready",
                State.Planning => "Creating task plan...",
                State.Executing => $"Executing step ({CurrentIteration}/{MaxIterations})",
                State.Validating => "Validating result...",
                State.Healing => "Recovering from error...",
                State.Replanning => "Revising plan...",
                State.Complete => "Task completed!",
                State.Failed => "Task failed (max retries exceeded)",
                _ => "Unknown"
            };
        }
        
        /// <summary>
        /// Get detailed state history for debugging
        /// </summary>
        public string GetStateHistory()
        {
            var summary = $"State History ({history.Count} transitions):\n";
            
            foreach (var transition in history)
            {
                summary += $"  [{transition.Iteration}] {transition.FromState} → {transition.ToState}";
                if (!string.IsNullOrEmpty(transition.Reason))
                {
                    summary += $" ({transition.Reason})";
                }
                summary += "\n";
            }
            
            return summary;
        }
        
        /// <summary>
        /// Get current progress percentage
        /// </summary>
        public float GetProgress()
        {
            if (!IsActive) return CurrentState == State.Complete ? 1f : 0f;
            return Math.Min(1f, (float)CurrentIteration / MaxIterations);
        }
        
        /// <summary>
        /// Check if we're close to iteration limit (warning threshold)
        /// </summary>
        public bool IsNearIterationLimit()
        {
            return CurrentIteration >= MaxIterations * 0.8f; // 80% threshold
        }
        
        private struct StateTransition
        {
            public State FromState;
            public State ToState;
            public int Iteration;
            public DateTime Timestamp;
            public string Reason;
        }
    }
}

