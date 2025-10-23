# 🌐 OpenRouter Integration Guide

## What is OpenRouter?

**OpenRouter** is a unified API gateway that provides access to **100+ AI models** from multiple providers:
- OpenAI (GPT-4, GPT-3.5, etc.)
- Anthropic (Claude 3.5, Claude 3, etc.)
- Meta (Llama 3, Llama 2, etc.)
- Google (Gemini Pro, PaLM, etc.)
- Mistral AI
- And many more!

**Benefits:**
- ✅ Single API key for all models
- ✅ Automatic fallbacks
- ✅ Competitive pricing
- ✅ No rate limits (pay-as-you-go)
- ✅ Access to latest models

**Website:** https://openrouter.ai

---

## 🔑 Getting Your API Key

### Step 1: Sign Up
1. Go to https://openrouter.ai
2. Click **"Sign Up"**
3. Register with email or GitHub

### Step 2: Get API Key
1. Login to your account
2. Go to **Keys** section
3. Click **"Create Key"**
4. Copy your API key (starts with `sk-or-v1-...`)

### Step 3: Add Credits (Optional)
- OpenRouter uses pay-as-you-go pricing
- Add credits in **Credits** section
- Most models are very affordable ($0.001 - $0.10 per 1K tokens)

---

## 🎯 Using OpenRouter in Unity

### 1. Open AI Code Actions
```
Window → AI Code Actions
```

### 2. Select OpenRouter Provider
- **Provider:** Select `OpenRouter`
- **API Key:** Paste your OpenRouter API key
- **Model:** Enter the full model name (see below)

### 3. Choose a Model

#### 🔥 Recommended Models

| Model | Name | Best For | Cost |
|-------|------|----------|------|
| **GPT-4o** | `openai/gpt-4o` | Best overall quality | $$$ |
| **GPT-4 Turbo** | `openai/gpt-4-turbo` | Fast + smart | $$$ |
| **Claude 3.5 Sonnet** | `anthropic/claude-3.5-sonnet` | Code generation | $$ |
| **Claude 3 Opus** | `anthropic/claude-3-opus` | Complex tasks | $$$ |
| **GPT-3.5 Turbo** | `openai/gpt-3.5-turbo` | Fast + cheap | $ |
| **Llama 3 70B** | `meta-llama/llama-3-70b-instruct` | Open source, free! | FREE |
| **Llama 3 8B** | `meta-llama/llama-3-8b-instruct` | Very fast, free! | FREE |
| **Gemini Pro** | `google/gemini-pro` | Free alternative | FREE |
| **Mistral Medium** | `mistralai/mistral-medium` | Balanced | $$ |

**💡 Tip:** Start with free models like Llama 3 or Gemini Pro for testing!

---

## 📖 Full Model List

See all available models at: **https://openrouter.ai/models**

### Popular Categories:

#### 🚀 Best for Code Generation
- `anthropic/claude-3.5-sonnet` - Excellent at Unity C# code
- `openai/gpt-4o` - Latest OpenAI model
- `meta-llama/llama-3-70b-instruct` - Free, very good

#### 💰 Best Free Models
- `meta-llama/llama-3-70b-instruct` - 70B parameters
- `meta-llama/llama-3-8b-instruct` - 8B parameters (faster)
- `google/gemini-pro` - Google's free model
- `mistralai/mistral-7b-instruct` - Mistral 7B

#### ⚡ Fastest Models
- `openai/gpt-3.5-turbo` - Very fast, cheap
- `meta-llama/llama-3-8b-instruct` - Free + fast
- `google/gemini-pro` - Fast responses

#### 🧠 Most Intelligent
- `openai/gpt-4o` - OpenAI's best
- `anthropic/claude-3-opus` - Anthropic's best
- `meta-llama/llama-3-70b-instruct` - Free alternative

---

## 🎨 Quick Setup Examples

### Example 1: GPT-4o (Premium)
```
Provider: OpenRouter
API Key: sk-or-v1-xxxxxxxxxxxxxxxx
Model: openai/gpt-4o
Temperature: 0.7
Max Tokens: 2048
```

### Example 2: Claude 3.5 Sonnet (Great for Code)
```
Provider: OpenRouter
API Key: sk-or-v1-xxxxxxxxxxxxxxxx
Model: anthropic/claude-3.5-sonnet
Temperature: 0.7
Max Tokens: 2048
```

### Example 3: Llama 3 70B (FREE!)
```
Provider: OpenRouter
API Key: sk-or-v1-xxxxxxxxxxxxxxxx
Model: meta-llama/llama-3-70b-instruct
Temperature: 0.7
Max Tokens: 2048
```

---

## 💡 Tips & Best Practices

### 1. Start with Free Models
- Test with `meta-llama/llama-3-8b-instruct` first
- Upgrade to paid models only if needed

### 2. Use Quick Model Buttons
In the UI, click these buttons for instant model selection:
- **GPT-4o** - Best overall
- **GPT-4** - Good balance
- **Claude-3.5** - Great for code
- **Llama-3** - Free option

### 3. Pricing
- Check current pricing: https://openrouter.ai/models
- Most models: $0.001 - $0.10 per 1K tokens
- Free models: $0.00 (community-supported)

### 4. Temperature Settings
- **0.2-0.5:** More focused, deterministic (good for code)
- **0.7-0.9:** Balanced creativity
- **1.0-1.5:** More creative, varied outputs

### 5. Max Tokens
- **512:** Quick responses, simple code
- **1024:** Standard Unity scripts
- **2048:** Complex systems, multiple files
- **4096:** Large codebases (costs more)

---

## 🔧 Troubleshooting

### Error: "Invalid API key"
- ✅ Check your API key starts with `sk-or-v1-`
- ✅ Make sure you copied the full key
- ✅ Generate a new key at https://openrouter.ai

### Error: "Insufficient credits"
- ✅ Add credits to your OpenRouter account
- ✅ Or use free models (Llama 3, Gemini Pro)

### Error: "Model not found"
- ✅ Check the exact model name at https://openrouter.ai/models
- ✅ Model names are case-sensitive
- ✅ Use format: `provider/model-name`

### Slow Responses
- ✅ Try smaller models (8B instead of 70B)
- ✅ Use `openai/gpt-3.5-turbo` for speed
- ✅ Reduce `Max Tokens` setting

---

## 📊 Pricing Comparison

| Model | Provider | Cost per 1K tokens | Speed | Quality |
|-------|----------|-------------------|-------|---------|
| GPT-4o | OpenRouter | $5.00 | ⚡⚡⚡ | 🌟🌟🌟🌟🌟 |
| Claude 3.5 | OpenRouter | $3.00 | ⚡⚡⚡ | 🌟🌟🌟🌟🌟 |
| GPT-3.5 Turbo | OpenRouter | $0.50 | ⚡⚡⚡⚡⚡ | 🌟🌟🌟🌟 |
| Llama 3 70B | OpenRouter | FREE | ⚡⚡⚡ | 🌟🌟🌟🌟 |
| Gemini Pro | OpenRouter | FREE | ⚡⚡⚡⚡ | 🌟🌟🌟 |

---

## 🎓 Advanced Usage

### Custom Model Parameters
Some models support additional parameters:
- `top_p` - Nucleus sampling (0.0-1.0)
- `frequency_penalty` - Reduce repetition
- `presence_penalty` - Encourage new topics

### Model Routing
OpenRouter can automatically route to the best available model:
- Use `auto` as model name
- OpenRouter picks best model based on your prompt

### Fallback Models
If your primary model fails, OpenRouter can auto-fallback:
- Enable in OpenRouter dashboard
- Set fallback chain (e.g., GPT-4 → GPT-3.5 → Llama)

---

## 🔗 Useful Links

- 📚 **All Models:** https://openrouter.ai/models
- 💳 **Pricing:** https://openrouter.ai/models (click any model)
- 📖 **Documentation:** https://openrouter.ai/docs
- 🔑 **API Keys:** https://openrouter.ai/keys
- 💰 **Credits:** https://openrouter.ai/credits
- 📊 **Usage Stats:** https://openrouter.ai/activity

---

## 🎯 Quick Start Checklist

- [ ] Create OpenRouter account
- [ ] Get API key
- [ ] Add credits (or use free models)
- [ ] Open Unity → Window → AI Code Actions
- [ ] Select "OpenRouter" provider
- [ ] Paste API key
- [ ] Choose a model (start with `meta-llama/llama-3-8b-instruct`)
- [ ] Click "Save Settings"
- [ ] Test with "Generate Script" action
- [ ] Try AI Chat window for agent mode

---

## 💬 Support

Having issues?
1. Check Console logs in Unity (Ctrl + Shift + C)
2. Verify model name at https://openrouter.ai/models
3. Try a different model
4. Open GitHub issue with error details

---

**Happy Coding with 100+ AI Models! 🚀**

Made with ❤️ for Unity Developers

