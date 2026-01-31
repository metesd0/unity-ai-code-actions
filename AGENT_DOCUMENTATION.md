# 🤖 Unity AI Agent - Complete Documentation

**Version:** 2.0  
**Last Updated:** October 24, 2025  
**Total Tools:** 41

---

## 📋 Table of Contents

1. [Overview](#overview)
2. [Key Features](#key-features)
3. [Tool Categories](#tool-categories)
4. [Detailed Tool Reference](#detailed-tool-reference)
5. [Usage Examples](#usage-examples)
6. [Advanced Features](#advanced-features)
7. [Tips & Best Practices](#tips--best-practices)

---

## 🌟 Overview

The **Unity AI Agent** is an intelligent assistant that can create, modify, and manage Unity projects through natural language commands. It uses a powerful tool system with **41 specialized tools** across 6 categories to perform complex Unity development tasks automatically.

### What Can It Do?

✅ **Scene Setup:** Create complete game scenes with GameObjects, lights, cameras  
✅ **Script Generation:** Write, modify, and attach C# scripts with full Unity API knowledge  
✅ **Component Management:** Add, configure, and link Unity components  
✅ **Material & Visual:** Create materials, lights, cameras with customization  
✅ **Project Management:** Scene saving, project statistics, file operations  
✅ **Auto-Complete:** Automatically continues complex tasks until completion  

---

## 🎯 Key Features

### 1️⃣ **AUTO-CONTINUE MECHANISM** ⭐⭐⭐⭐⭐

The AI automatically detects incomplete tasks and continues execution for up to 2 additional turns!

**How It Works:**
- Detects missing tool calls (e.g., script creation promised but not executed)
- Identifies incomplete responses (abrupt endings, missing closing tags)
- Sends continuation prompts automatically
- Ensures complex tasks are completed fully

**Example:**
```
User: "Create first person controller"
Turn 1: Player + Camera + CharacterController (7 tools)
Turn 2 (AUTO): Full movement script created! (1 tool)
Turn 3 (AUTO): Positioned + Ground + Light (5 tools)
✅ Task completed successfully!
```

### 2️⃣ **LIVE PROGRESS UPDATES** ⚡

Watch AI execute tools **ONE BY ONE** in real-time!

**Before:**
```
User sends message → ... wait ... → All results appear at once
```

**Now:**
```
User sends message → 
  '🤖 Processing...'
  '⏳ Executing create_gameobject...'
  '✅ Created GameObject Player'
  '⏳ Executing add_component...'
  '✅ Added CharacterController'
  (SEE EACH TOOL AS IT EXECUTES!)
```

### 3️⃣ **CONTEXT AWARENESS** 🧠

The agent tracks recent operations to understand references like "this script" or "that object":

- **LastCreatedScript:** Most recently created script name
- **LastCreatedGameObject:** Most recently created GameObject
- **LastModifiedGameObject:** Most recently modified GameObject
- **RecentScripts:** Last 10 scripts created/modified
- **RecentGameObjects:** Last 10 GameObjects created/modified

### 4️⃣ **TOKEN BOOST** 📈

Agent mode now uses **6144 tokens** (up from 3072) for:
- Longer script generation (200+ lines)
- More tool calls per response (8-12 tools)
- Better handling of complex tasks

---

## 🛠️ Tool Categories

| Category | Tools | Description |
|----------|-------|-------------|
| **File Operations** | 3 | Read scripts, files, list assets |
| **GameObject Operations** | 15 | Create, modify, transform GameObjects |
| **Component Operations** | 4 | Add, configure, attach components |
| **Script Manipulation** | 11 | Create, modify, analyze scripts |
| **Visual Operations** | 4 | Materials, lights, cameras |
| **Scene Management** | 4 | Scene info, save, project stats |

**Total:** 41 Tools

---

## 📚 Detailed Tool Reference

### 📁 FILE OPERATIONS (3 Tools)

#### 1. `read_script`
**Description:** Read a C# script file content  
**Parameters:**
- `script_name` (string): Name of the script (with or without .cs)

**Example:**
```
[TOOL:read_script]
script_name: PlayerController
[/TOOL]
```

**Returns:** Script content in markdown code block

---

#### 2. `read_file`
**Description:** Read any file content from Assets folder  
**Parameters:**
- `file_path` (string): Path to file (relative to Assets/)

**Example:**
```
[TOOL:read_file]
file_path: Config/GameSettings.json
[/TOOL]
```

**Returns:** File content with appropriate syntax highlighting

---

#### 3. `list_scripts`
**Description:** List all C# scripts in the project  
**Parameters:**
- `filter` (string, optional): Filter scripts by name

**Example:**
```
[TOOL:list_scripts]
filter: Controller
[/TOOL]
```

**Returns:** List of matching scripts with paths

---

### 🎮 GAMEOBJECT OPERATIONS (15 Tools)

#### 1. `get_scene_info`
**Description:** Get current Unity scene hierarchy and all GameObjects  
**Parameters:** None

**Example:**
```
[TOOL:get_scene_info]
[/TOOL]
```

**Returns:** Complete scene hierarchy with components

---

#### 2. `get_gameobject_info`
**Description:** Get detailed information about a specific GameObject  
**Parameters:**
- `name` (string): GameObject name

**Example:**
```
[TOOL:get_gameobject_info]
name: Player
[/TOOL]
```

**Returns:** Position, rotation, scale, components, children, scripts

---

#### 3. `find_gameobjects`
**Description:** Find GameObjects by name or tag  
**Parameters:**
- `search_term` (string): Name to search or tag
- `by_tag` (bool, optional): Search by tag instead of name (default: false)

**Example:**
```
[TOOL:find_gameobjects]
search_term: Enemy
by_tag: false
[/TOOL]
```

**Returns:** List of matching GameObjects with positions

---

#### 4. `create_gameobject`
**Description:** Create a new empty GameObject  
**Parameters:**
- `name` (string): GameObject name
- `parent` (string, optional): Parent GameObject name

**Example:**
```
[TOOL:create_gameobject]
name: Player
parent: GameManager
[/TOOL]
```

**Returns:** Confirmation with creation details

---

#### 5. `create_primitive`
**Description:** Create a primitive GameObject (Cube, Sphere, Capsule, Cylinder, Plane, Quad)  
**Parameters:**
- `primitive_type` (string): Type (Cube, Sphere, Capsule, Cylinder, Plane, Quad)
- `name` (string, optional): Custom name
- `x, y, z` (float, optional): Position (default: 0,0,0)

**Example:**
```
[TOOL:create_primitive]
primitive_type: Plane
name: Ground
x: 0
y: 0
z: 0
[/TOOL]
```

**Returns:** Confirmation with primitive type and position

---

#### 6. `set_position`
**Description:** Set GameObject position in world space  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `x, y, z` (float): World position coordinates

**Example:**
```
[TOOL:set_position]
gameobject_name: Player
x: 0
y: 1
z: -5
[/TOOL]
```

**Returns:** Confirmation with new position

---

#### 7. `set_rotation`
**Description:** Set GameObject rotation using Euler angles (degrees)  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `x, y, z` (float): Euler angles in degrees

**Example:**
```
[TOOL:set_rotation]
gameobject_name: Directional Light
x: 50
y: -30
z: 0
[/TOOL]
```

**Returns:** Confirmation with new rotation

---

#### 8. `set_scale`
**Description:** Set GameObject local scale  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `x, y, z` (float): Scale values

**Example:**
```
[TOOL:set_scale]
gameobject_name: Ground
x: 10
y: 1
z: 10
[/TOOL]
```

**Returns:** Confirmation with new scale

---

#### 9. `delete_gameobject`
**Description:** Delete a GameObject from the scene (supports undo)  
**Parameters:**
- `gameobject_name` (string): GameObject to delete

**Example:**
```
[TOOL:delete_gameobject]
gameobject_name: OldPlayer
[/TOOL]
```

**Returns:** Confirmation with component count

---

#### 10. `set_parent`
**Description:** Set the parent of a GameObject (hierarchy organization)  
**Parameters:**
- `child_name` (string): Child GameObject
- `parent_name` (string): Parent GameObject (use "null" for root)

**Example:**
```
[TOOL:set_parent]
child_name: PlayerCamera
parent_name: Player
[/TOOL]
```

**Returns:** Confirmation with parent relationship

---

#### 11. `set_active`
**Description:** Enable or disable a GameObject  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `active` (bool): Active state

**Example:**
```
[TOOL:set_active]
gameobject_name: DebugUI
active: false
[/TOOL]
```

**Returns:** Confirmation with new active state

---

#### 12. `rename_gameobject`
**Description:** Rename a GameObject  
**Parameters:**
- `old_name` (string): Current name
- `new_name` (string): New name

**Example:**
```
[TOOL:rename_gameobject]
old_name: GameObject
new_name: Player
[/TOOL]
```

**Returns:** Confirmation with name change

---

#### 13. `duplicate_gameobject`
**Description:** Duplicate a GameObject with all components and children  
**Parameters:**
- `name` (string): GameObject to duplicate
- `new_name` (string, optional): Name for duplicate

**Example:**
```
[TOOL:duplicate_gameobject]
name: Enemy
new_name: Enemy2
[/TOOL]
```

**Returns:** Confirmation with duplicate name

---

#### 14. `set_tag`
**Description:** Set GameObject tag  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `tag` (string): Tag name (e.g., "Player", "Enemy", "Untagged")

**Example:**
```
[TOOL:set_tag]
gameobject_name: Player
tag: Player
[/TOOL]
```

**Returns:** Confirmation with tag assignment

---

#### 15. `set_layer`
**Description:** Set GameObject layer  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `layer_name` (string): Layer name (e.g., "Default", "UI", "Water")

**Example:**
```
[TOOL:set_layer]
gameobject_name: Player
layer_name: Player
[/TOOL]
```

**Returns:** Confirmation with layer assignment

---

### 🔧 COMPONENT OPERATIONS (4 Tools)

#### 1. `add_component`
**Description:** Add a Unity component to a GameObject (built-in or compiled custom)  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `component_type` (string): Component type (e.g., "Rigidbody", "CharacterController")

**Example:**
```
[TOOL:add_component]
gameobject_name: Player
component_type: CharacterController
[/TOOL]
```

**Returns:** Confirmation or "already exists" message

---

#### 2. `attach_script`
**Description:** Attach an existing compiled script to a GameObject  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `script_name` (string): Script name (with or without .cs)

**Example:**
```
[TOOL:attach_script]
gameobject_name: Player
script_name: PlayerMovement
[/TOOL]
```

**Returns:** Confirmation with script path

---

#### 3. `create_and_attach_script`
**Description:** Create a new C# script and attach it to a GameObject  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `script_name` (string): Script name (without .cs)
- `script_content` (string): Full C# script content

**Example:**
```
[TOOL:create_and_attach_script]
gameobject_name: Player
script_name: PlayerController
script_content:
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    void Update()
    {
        // Movement logic
    }
}
[/TOOL]
```

**Returns:** Confirmation with file size and auto-attach status

---

#### 4. `set_component_property`
**Description:** Set a property/field value on a component  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `component_type` (string): Component type
- `property_name` (string): Property/field name
- `value` (string): New value

**Supported Types:**
- **Transform/GameObject:** Reference by name
- **float, int, bool, string:** Primitive values
- **Vector3:** Format: "x,y,z" or "(x,y,z)"
- **Color:** Name (red, green, blue, etc.) or hex (#RRGGBB)

**Example:**
```
[TOOL:set_component_property]
gameobject_name: Player
component_type: CharacterController
property_name: height
value: 2.0
[/TOOL]
```

**Returns:** Confirmation with property value

---

### 📝 SCRIPT MANIPULATION (11 Tools)

#### 1. `modify_script`
**Description:** Modify an existing script by appending code  
**Parameters:**
- `script_name` (string): Script to modify
- `modifications` (string): Code to append

**Example:**
```
[TOOL:modify_script]
script_name: PlayerController
modifications: 
    public void Jump() {
        // Jump logic
    }
[/TOOL]
```

**Returns:** Confirmation with auto-recompile message

---

#### 2. `add_method_to_script`
**Description:** Add a method to an existing script (inserts before last brace)  
**Parameters:**
- `script_name` (string): Script to modify
- `method_code` (string): Complete method code

**Example:**
```
[TOOL:add_method_to_script]
script_name: PlayerController
method_code:
public void Shoot()
{
    Debug.Log("Shooting!");
}
[/TOOL]
```

**Returns:** Confirmation with auto-recompile message

---

#### 3. `add_field_to_script`
**Description:** Add a field/property to an existing script (inserts after class opening brace)  
**Parameters:**
- `script_name` (string): Script to modify
- `field_code` (string): Field declaration

**Example:**
```
[TOOL:add_field_to_script]
script_name: PlayerController
field_code: public float speed = 5f;
[/TOOL]
```

**Returns:** Confirmation with auto-recompile message

---

#### 4. `delete_script`
**Description:** Delete a script file from the project  
**Parameters:**
- `script_name` (string): Script to delete

**Example:**
```
[TOOL:delete_script]
script_name: OldController
[/TOOL]
```

**Returns:** Confirmation with deleted path

---

#### 5. `find_in_script`
**Description:** Search for text in a script  
**Parameters:**
- `script_name` (string): Script to search
- `search_text` (string): Text to find

**Example:**
```
[TOOL:find_in_script]
script_name: PlayerController
search_text: Jump
[/TOOL]
```

**Returns:** List of matching lines with line numbers

---

#### 6. `replace_in_script`
**Description:** Replace text in a script (all occurrences)  
**Parameters:**
- `script_name` (string): Script to modify
- `find_text` (string): Text to find
- `replace_text` (string): Replacement text

**Example:**
```
[TOOL:replace_in_script]
script_name: PlayerController
find_text: speed = 5f
replace_text: speed = 10f
[/TOOL]
```

**Returns:** Confirmation with occurrence count

---

#### 7. `validate_script`
**Description:** Basic syntax validation for a script  
**Parameters:**
- `script_name` (string): Script to validate

**Example:**
```
[TOOL:validate_script]
script_name: PlayerController
[/TOOL]
```

**Returns:** Validation results (brace matching, class name check, using statements)

---

#### 8. `create_from_template`
**Description:** Create a script from a template (Singleton, StateMachine, ObjectPool, ScriptableObject)  
**Parameters:**
- `script_name` (string): New script name
- `template_type` (string): Template (singleton, statemachine, objectpool, scriptableobject)
- `gameobject_name` (string, optional): GameObject to attach to

**Example:**
```
[TOOL:create_from_template]
script_name: GameManager
template_type: singleton
gameobject_name: GameManager
[/TOOL]
```

**Returns:** Confirmation with template type and path

---

#### 9. `add_comments_to_script`
**Description:** Add header comments/documentation to a script  
**Parameters:**
- `script_name` (string): Script to modify
- `comments` (string): Comment text

**Example:**
```
[TOOL:add_comments_to_script]
script_name: PlayerController
comments: Handles player movement, jumping, and input
[/TOOL]
```

**Returns:** Confirmation with timestamp

---

#### 10. `create_multiple_scripts`
**Description:** Create multiple empty scripts at once  
**Parameters:**
- `script_names` (string): Comma/semicolon/newline separated names
- `base_namespace` (string, optional): Namespace for scripts

**Example:**
```
[TOOL:create_multiple_scripts]
script_names: EnemyAI, HealthSystem, ScoreManager
base_namespace: GameSystems
[/TOOL]
```

**Returns:** List of created scripts

---

#### 11. `add_namespace_to_script`
**Description:** Wrap script content in a namespace  
**Parameters:**
- `script_name` (string): Script to modify
- `namespace_name` (string): Namespace name

**Example:**
```
[TOOL:add_namespace_to_script]
script_name: PlayerController
namespace_name: PlayerSystems
[/TOOL]
```

**Returns:** Confirmation with namespace

---

### 🎨 VISUAL OPERATIONS (4 Tools)

#### 1. `create_material`
**Description:** Create a new material asset  
**Parameters:**
- `name` (string): Material name
- `color` (string, optional): Color (name or hex)

**Example:**
```
[TOOL:create_material]
name: PlayerMaterial
color: blue
[/TOOL]
```

**Returns:** Confirmation with asset path

---

#### 2. `assign_material`
**Description:** Assign a material to a GameObject's Renderer  
**Parameters:**
- `gameobject_name` (string): Target GameObject
- `material_name` (string): Material to assign

**Example:**
```
[TOOL:assign_material]
gameobject_name: Player
material_name: PlayerMaterial
[/TOOL]
```

**Returns:** Confirmation with material assignment

---

#### 3. `create_light`
**Description:** Create a light GameObject  
**Parameters:**
- `name` (string): Light name
- `light_type` (string): Type (directional, point, spot, area)
- `color` (string, optional): Light color (default: white)
- `intensity` (float, optional): Intensity (default: 1.0)

**Example:**
```
[TOOL:create_light]
name: SunLight
light_type: directional
color: white
intensity: 1.2
[/TOOL]
```

**Returns:** Confirmation with light type

---

#### 4. `create_camera`
**Description:** Create a camera GameObject  
**Parameters:**
- `name` (string): Camera name
- `field_of_view` (float, optional): FOV in degrees (default: 60)

**Example:**
```
[TOOL:create_camera]
name: MainCamera
field_of_view: 75
[/TOOL]
```

**Returns:** Confirmation with FOV

---

### 🎬 SCENE MANAGEMENT (4 Tools)

#### 1. `get_scene_info`
**Description:** Get current scene information and hierarchy  
**Parameters:** None

**Example:**
```
[TOOL:get_scene_info]
[/TOOL]
```

**Returns:** Scene name, path, root GameObjects, complete hierarchy

---

#### 2. `save_scene`
**Description:** Save the current scene  
**Parameters:** None

**Example:**
```
[TOOL:save_scene]
[/TOOL]
```

**Returns:** Confirmation with scene path

---

#### 3. `save_scene_as`
**Description:** Save scene with a new name/path  
**Parameters:**
- `scene_name` (string): New scene name (without .unity)

**Example:**
```
[TOOL:save_scene_as]
scene_name: Level1
[/TOOL]
```

**Returns:** Confirmation with new path

---

#### 4. `get_project_stats`
**Description:** Get project statistics (scripts, scenes, prefabs, materials, textures)  
**Parameters:** None

**Example:**
```
[TOOL:get_project_stats]
[/TOOL]
```

**Returns:** Complete project statistics

---

## 💡 Usage Examples

### Example 1: Create a Complete First Person Controller

**User Request:**
```
Create a first person controller with movement, jumping, and mouse look
```

**AI Execution:**

**Turn 1:**
```
[TOOL:create_gameobject]
name: Player
[/TOOL]

[TOOL:add_component]
gameobject_name: Player
component_type: CharacterController
[/TOOL]

[TOOL:set_component_property]
gameobject_name: Player
component_type: CharacterController
property_name: height
value: 2
[/TOOL]

[TOOL:create_camera]
name: PlayerCamera
field_of_view: 70
[/TOOL]

[TOOL:set_parent]
child_name: PlayerCamera
parent_name: Player
[/TOOL]

[TOOL:set_position]
gameobject_name: PlayerCamera
x: 0
y: 1.6
z: 0
[/TOOL]
```

**Turn 2 (AUTO-CONTINUE):**
```
[TOOL:create_and_attach_script]
gameobject_name: Player
script_name: FirstPersonController
script_content:
using UnityEngine;

public class FirstPersonController : MonoBehaviour
{
    [Header("Movement")]
    public float walkSpeed = 5f;
    public float runSpeed = 8f;
    public float jumpHeight = 2f;
    public float gravity = -15f;
    
    [Header("Mouse Look")]
    public float mouseSensitivity = 2f;
    public float lookXLimit = 85f;
    
    public Camera playerCamera;
    private CharacterController controller;
    private Vector3 velocity;
    private float rotationX = 0f;
    
    void Start()
    {
        controller = GetComponent<CharacterController>();
        if (playerCamera == null)
            playerCamera = GetComponentInChildren<Camera>();
        Cursor.lockState = CursorLockMode.Locked;
    }
    
    void Update()
    {
        // Mouse look
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
        
        rotationX -= mouseY;
        rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
        playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
        transform.Rotate(Vector3.up * mouseX);
        
        // Movement
        float x = Input.GetAxis("Horizontal");
        float z = Input.GetAxis("Vertical");
        Vector3 move = transform.right * x + transform.forward * z;
        
        bool isRunning = Input.GetKey(KeyCode.LeftShift);
        float speed = isRunning ? runSpeed : walkSpeed;
        controller.Move(move * speed * Time.deltaTime);
        
        // Gravity and jumping
        if (controller.isGrounded)
        {
            velocity.y = -2f;
            if (Input.GetButtonDown("Jump"))
                velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }
}
[/TOOL]

[TOOL:set_component_property]
gameobject_name: Player
component_type: FirstPersonController
property_name: playerCamera
value: PlayerCamera
[/TOOL]
```

**Turn 3 (AUTO-CONTINUE):**
```
[TOOL:create_primitive]
primitive_type: Plane
name: Ground
x: 0
y: 0
z: 0
[/TOOL]

[TOOL:set_scale]
gameobject_name: Ground
x: 20
y: 1
z: 20
[/TOOL]

[TOOL:set_position]
gameobject_name: Player
x: 0
y: 1
z: 0
[/TOOL]

[TOOL:create_light]
name: Sun
light_type: directional
intensity: 1.0
[/TOOL]

[TOOL:set_rotation]
gameobject_name: Sun
x: 50
y: -30
z: 0
[/TOOL]

[TOOL:save_scene_as]
scene_name: FPSDemo
[/TOOL]
```

**Result:** ✅ Fully functional first person controller ready to test!

---

### Example 2: Create a Simple Enemy AI

**User Request:**
```
Create an enemy that patrols and chases the player
```

**AI Execution:**
```
[TOOL:create_primitive]
primitive_type: Capsule
name: Enemy
x: 5
y: 1
z: 0
[/TOOL]

[TOOL:set_tag]
gameobject_name: Enemy
tag: Enemy
[/TOOL]

[TOOL:create_material]
name: EnemyMaterial
color: red
[/TOOL]

[TOOL:assign_material]
gameobject_name: Enemy
material_name: EnemyMaterial
[/TOOL]

[TOOL:create_and_attach_script]
gameobject_name: Enemy
script_name: EnemyAI
script_content:
using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public Transform[] patrolPoints;
    public float detectionRange = 10f;
    public float chaseSpeed = 6f;
    public float patrolSpeed = 2f;
    
    private Transform player;
    private NavMeshAgent agent;
    private int currentPatrolIndex = 0;
    private bool isChasing = false;
    
    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        player = GameObject.FindGameObjectWithTag("Player").transform;
        agent.speed = patrolSpeed;
        
        if (patrolPoints.Length > 0)
            agent.SetDestination(patrolPoints[0].position);
    }
    
    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);
        
        if (distanceToPlayer <= detectionRange)
        {
            // Chase player
            if (!isChasing)
            {
                isChasing = true;
                agent.speed = chaseSpeed;
            }
            agent.SetDestination(player.position);
        }
        else
        {
            // Patrol
            if (isChasing)
            {
                isChasing = false;
                agent.speed = patrolSpeed;
            }
            
            if (patrolPoints.Length > 0 && !agent.pathPending && agent.remainingDistance < 0.5f)
            {
                currentPatrolIndex = (currentPatrolIndex + 1) % patrolPoints.Length;
                agent.SetDestination(patrolPoints[currentPatrolIndex].position);
            }
        }
    }
    
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);
    }
}
[/TOOL]

[TOOL:add_component]
gameobject_name: Enemy
component_type: NavMeshAgent
[/TOOL]
```

**Result:** ✅ Enemy AI with patrol and chase behavior!

---

### Example 3: Setup a Complete Scene

**User Request:**
```
Create a platformer scene with ground, platforms, player spawn, and lighting
```

**AI Execution:**
```
[TOOL:create_primitive]
primitive_type: Plane
name: Ground
x: 0
y: 0
z: 0
[/TOOL]

[TOOL:set_scale]
gameobject_name: Ground
x: 50
y: 1
z: 50
[/TOOL]

[TOOL:create_material]
name: GroundMat
color: gray
[/TOOL]

[TOOL:assign_material]
gameobject_name: Ground
material_name: GroundMat
[/TOOL]

[TOOL:create_primitive]
primitive_type: Cube
name: Platform1
x: 5
y: 2
z: 0
[/TOOL]

[TOOL:set_scale]
gameobject_name: Platform1
x: 3
y: 0.3
z: 3
[/TOOL]

[TOOL:create_primitive]
primitive_type: Cube
name: Platform2
x: 10
y: 4
z: 3
[/TOOL]

[TOOL:set_scale]
gameobject_name: Platform2
x: 3
y: 0.3
z: 3
[/TOOL]

[TOOL:create_gameobject]
name: PlayerSpawn
[/TOOL]

[TOOL:set_position]
gameobject_name: PlayerSpawn
x: 0
y: 1
z: -5
[/TOOL]

[TOOL:create_light]
name: Sun
light_type: directional
color: white
intensity: 1.2
[/TOOL]

[TOOL:set_rotation]
gameobject_name: Sun
x: 50
y: -30
z: 0
[/TOOL]

[TOOL:create_light]
name: FillLight
light_type: point
color: blue
intensity: 0.5
[/TOOL]

[TOOL:set_position]
gameobject_name: FillLight
x: -10
y: 5
z: 0
[/TOOL]

[TOOL:save_scene_as]
scene_name: PlatformerLevel1
[/TOOL]
```

**Result:** ✅ Complete platformer scene ready for player implementation!

---

## 🚀 Advanced Features

### Auto-Continue Detection Logic

The agent detects incomplete responses using 4 checks:

1. **Unclosed Tool Tags:**
   ```
   toolOpenCount > toolCloseCount
   ```

2. **Insufficient Tools for Promises:**
   ```
   Response mentions "create" but toolCount < 3
   ```

3. **Missing Critical Tools:**
   ```
   Mentions "script" but no create_and_attach_script call
   ```

4. **Abrupt Ending:**
   ```
   Response doesn't end with proper punctuation (. ! ] })
   ```

### Continuation Prompt

When incomplete task is detected:
```
⚡ CONTINUATION REQUIRED - Complete remaining tasks!

You started this task but didn't finish. Continue now:

1. Create scripts using create_and_attach_script
2. Position objects with set_position
3. Add lights/cameras if needed
4. Use 3-8 tools to complete the task

Execute remaining work immediately!
```

### Context Injection

The agent's system prompt includes:
```
RECENT CONTEXT:
📝 Last Created Script: FirstPersonController
🎮 Last Created GameObject: Player
🔧 Last Modified: Player
📚 Recent Scripts: FirstPersonController, EnemyAI, HealthSystem
🎯 Recent GameObjects: Player, Enemy, Platform1, Ground
```

This helps the AI understand references like:
- "Add a health variable to that script" → Knows to modify `FirstPersonController`
- "Move it up 5 units" → Knows to move `Player`

---

## 💎 Tips & Best Practices

### For Users:

1. **Be Specific:**
   - ❌ "Make a player"
   - ✅ "Create a first person player with WASD movement, jumping, and mouse look"

2. **Complex Tasks Are OK:**
   - The agent handles multi-step tasks automatically
   - Example: "Create a complete inventory system with UI and drag-drop"

3. **Reference Previous Work:**
   - "Add a shoot method to that controller"
   - "Move the player to the spawn point"

4. **Trust Auto-Continue:**
   - If the agent seems to stop mid-task, wait a moment
   - It will automatically continue for up to 2 more turns

### For AI:

1. **Always Complete Tasks:**
   - Don't stop after partial implementation
   - Use 5-15 tools per complex request

2. **Create Complete Scripts:**
   - Don't create stub scripts with `// TODO`
   - Implement full functionality

3. **Position Everything:**
   - Always set positions for spawned objects
   - Add ground, lights, cameras for new scenes

4. **Validate References:**
   - Use `find_gameobjects` if GameObject name is uncertain
   - Use `get_gameobject_info` to check existing components

5. **Follow Execution Pattern:**
   ```
   1. Create GameObjects
   2. Add components
   3. Configure properties
   4. Create and attach scripts
   5. Position everything
   6. Add visuals (materials, lights)
   7. Save scene
   ```

---

## 📊 Performance Metrics

| Metric | Value |
|--------|-------|
| Total Tools | 41 |
| Max Tools per Response | 20 |
| Max Token Output | 6144 |
| Auto-Continue Turns | 2 |
| Script Compilation Wait | 30 seconds |
| Compilation Check Attempts | 30 |

---

## 🔄 Version History

### v2.0 (Current)
- ✨ Added Auto-Continue Mechanism
- ✨ Added Live Progress Updates
- ✨ Boosted token limit to 6144
- ✨ Improved context awareness
- ✨ Enhanced system prompt

### v1.0
- 🎉 Initial release with 41 tools
- 📁 File operations (3 tools)
- 🎮 GameObject operations (15 tools)
- 🔧 Component operations (4 tools)
- 📝 Script manipulation (11 tools)
- 🎨 Visual operations (4 tools)
- 🎬 Scene management (4 tools)

---

## 📞 Support & Feedback

For issues, suggestions, or questions:
- Open an issue on GitHub
- Check Unity Console for detailed error logs
- Use `validate_script` tool for script debugging

---

**Made with ❤️ for Unity Developers**

*"From idea to implementation in seconds!"* 🚀

