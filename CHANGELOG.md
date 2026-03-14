# Changelog

All notable changes to AI Code Actions will be documented in this file.

## [1.0.0] - 2024-XX-XX (Launch)

### Added
- ✨ Initial release
- 🎯 Project-aware code indexing with AST parsing
- 🌐 Three AI provider integrations:
  - OpenAI (GPT-4, GPT-3.5)
  - Google Gemini (Gemini Pro)
  - Ollama (Local/Offline)
- 🔧 Three core code actions:
  - Generate Script from specification
  - Explain Code with Unity insights
  - Refactor Code for quality and performance
- 🛡️ Safe modification system:
  - Visual diff preview
  - Automatic backup (.bak files)
  - Non-destructive workflow
- ⚡ Unity-optimized prompt templates
- 💰 Token usage control and cost estimation
- 📊 Project context building with:
  - C# script analysis
  - Scene inventory
  - Prefab component tracking
- 🎨 Clean, dockable Editor window UI
- 📚 Comprehensive documentation
- ⚙️ Configurable parameters:
  - Temperature control
  - Max token limits
  - Model selection per provider

### Features
- Incremental project indexing (only re-scans changed files)
- Clipboard integration for quick code transfer
- EditorPrefs persistence for settings
- Status indicators and progress feedback
- Error handling with user-friendly messages

### Compatibility
- Unity 2021.3 LTS and later
- All render pipelines (Built-in, URP, HDRP)
- .NET Framework and Mono compatible
- Windows, macOS, Linux

---

## [Upcoming in 1.1.0] - Sprint 2

### Planned Features
- 🧪 Generate Unit Tests with NUnit
- ♻️ "Add Object Pool" automated action
- 📡 "Convert Update to Events" automated action
- 📊 Usage analytics and cost tracking panel
- 🎨 Custom prompt template system
- 🔄 Batch file processing
- 📈 Performance profiling hints
- 🎯 Quick actions from context menu

### Improvements
- Better diff algorithm (Myers)
- Roslyn-based AST parsing (replacing regex)
- Multi-file context awareness
- Smarter token budgeting

---

## Future Roadmap

### 1.2.0 - Enhanced Actions
- Speech-to-code (STT integration)
- Code-to-speech explanations (TTS)
- Visual node-based prompt builder
- Generate ScriptableObject templates
- Shader generation support

### 1.3.0 - Team Features
- Shared prompt library
- Team settings synchronization
- Code style enforcement
- Review and approval workflow

### 2.0.0 - Runtime AI (Separate Module)
- NPC dialog generation
- Quest system AI
- Procedural content tools
- Runtime agent behaviors

---

## Notes

### Maintenance
- Repository history metadata was cleaned up to keep contributor attribution current.

### Why These Features?
Based on our competitive analysis and Unity developer needs:
1. **Offline mode** - differentiates from competitors (AI Toolbox, U Coder)
2. **Project context** - makes suggestions actually useful vs. generic AI
3. **Safe modifications** - critical for professional workflows
4. **Unity-specific** - optimized for game dev patterns, not generic code

### Development Philosophy
- Editor productivity first (runtime features later)
- Non-destructive workflow (always preview, never force)
- Privacy-conscious (offline option)
- Cost-aware (token budgeting, local alternatives)

---

## Support

For bug reports and feature requests:
- Email: support@yourdomain.com
- Unity Forum: [link]
- Asset Store Q&A

---

*AI Code Actions is committed to regular updates and responsive support.*

