# AI Code Actions for Unity

**Context-aware AI code generation with offline support**

[![Unity](https://img.shields.io/badge/Unity-2021.3+-black.svg)](https://unity.com)
[![License](https://img.shields.io/badge/License-Unity%20Asset%20Store%20EULA-blue.svg)](LICENSE.md)
[![Version](https://img.shields.io/badge/Version-1.0.0-green.svg)](CHANGELOG.md)

Transform your Unity workflow with AI-powered code assistance that understands your project context.

---

## ✨ Features

- 🎯 **Project-Aware**: Analyzes your C# scripts, scenes, and prefabs for relevant context
- 🌐 **Multiple Providers**: OpenAI (GPT-4), Google Gemini, or Ollama (offline)
- 🛡️ **Safe Modifications**: Visual diff preview + automatic backups
- ⚡ **Unity-Optimized**: Built-in best practices and performance patterns
- 💰 **Cost Control**: Token budgeting and completely free offline mode

---

## 📦 Installation

### Method 1: Unity Package Manager (Git URL)

1. Open Unity Package Manager: `Window > Package Manager`
2. Click `+` button → `Add package from git URL...`
3. Enter:
   ```
   https://github.com/metesd0/unity-ai-code-actions.git
   ```
4. Wait for import to complete

### Method 2: Manual Installation

1. Download the [latest release](https://github.com/metesd0/unity-ai-code-actions/releases)
2. Extract to your project's `Packages` folder
3. Unity will auto-import

---

## 🚀 Quick Start

### 1. Configure Provider

Open the window: `Window > AI Code Actions`

#### Option A: Ollama (Free & Offline) ⭐
```bash
# Install Ollama: https://ollama.ai
ollama run mistral
```

In Unity:
- Provider: **Ollama (Local)**
- Model: `mistral`
- Endpoint: `http://localhost:11434/api/generate`
- No API key needed!

#### Option B: OpenAI
- Get API key: [platform.openai.com](https://platform.openai.com)
- Model: `gpt-4` or `gpt-3.5-turbo`

#### Option C: Google Gemini
- Get API key: [Google AI Studio](https://makersuite.google.com/app/apikey)
- Model: `gemini-pro`

### 2. Generate Your First Script

```
Action: Generate Script
Specification: "Create a 2D character controller with jump and double jump"
→ Execute → Save
```

---

## 🎮 Actions

### Generate Script
Create Unity MonoBehaviours from natural language specifications.

```
"Create an object pooling system for bullets"
→ Complete C# class with proper Unity patterns
```

### Explain Code
Get detailed explanations with Unity-specific insights.

```
Copy code → Explain → Learn Unity patterns
```

### Refactor Code
Improve code quality and performance.

```
"Convert Update() to event-driven pattern"
"Add object pooling"
"Reduce GC allocations"
```

---

## 📚 Documentation

- [Quick Start Guide](QUICKSTART.md)
- [Testing Guide](TESTING_GUIDE.md)
- [Changelog](CHANGELOG.md)
- [License](LICENSE.md)

---

## 🎯 Why AI Code Actions?

| Feature | AI Code Actions | Generic AI | Other Tools |
|---------|----------------|------------|-------------|
| Unity Context | ✅ Full AST | ❌ | ⚠️ Limited |
| Offline Mode | ✅ Ollama | ❌ | ❌ |
| Safe Edits | ✅ Diff + Backup | ❌ | ⚠️ Varies |
| Unity Best Practices | ✅ Built-in | ❌ | ⚠️ Basic |

---

## 💡 Examples

### Character Controller
```
"2D platformer controller with jump, coyote time, and jump buffering"
```

### Object Pool
```
"Generic object pool with warm-up and auto-expand"
```

### Save System
```
"JSON save system with multiple save slots"
```

---

## 🛠️ Requirements

- Unity 2021.3 or later
- All render pipelines supported (Built-in, URP, HDRP)
- .NET Framework 4.x equivalent

---

## 🤝 Support

- **Issues**: [GitHub Issues](https://github.com/metesd0/unity-ai-code-actions/issues)
- **Discussions**: [GitHub Discussions](https://github.com/metesd0/unity-ai-code-actions/discussions)
- **Email**: support@aicodeactions.dev

---

## 📄 License

This package is licensed under the Unity Asset Store EULA.
See [LICENSE.md](LICENSE.md) for details.

---

## 🎉 Getting Started

1. Install the package
2. Open `Window > AI Code Actions`
3. Configure your AI provider
4. Generate your first script!

**Transform your Unity workflow today.** 🚀

---

Made with ❤️ for Unity developers
