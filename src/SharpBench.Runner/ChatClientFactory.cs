using System.ClientModel;
using Anthropic;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;
using Mscc.GenerativeAI.Microsoft;
using OllamaSharp;
using OpenAI;

namespace SharpBench.Runner;

/// <summary>
/// Resolves a model label to an <see cref="IChatClient"/> for one provider. Frontier
/// model IDs are self-identifying, so bare labels work (<c>claude-sonnet-5</c>,
/// <c>gpt-4o</c>, <c>gemini-2.5-pro</c>); Ollama keeps an explicit prefix because its
/// model names are arbitrary. An explicit <c>provider:model</c> prefix always wins.
///
/// Keys come from the environment: <c>ANTHROPIC_API_KEY</c>, <c>OPENAI_API_KEY</c>,
/// <c>GEMINI_API_KEY</c>/<c>GOOGLE_API_KEY</c>. Ollama uses <c>OLLAMA_HOST</c>
/// (default <c>http://localhost:11434</c>).
/// </summary>
public static class ChatClientFactory
{
    /// <summary>Headroom for a whole C# file; contestants answer with one complete source file.</summary>
    public const int MaxOutputTokens = 8192;

    public static IChatClient Create(string label)
    {
        var (provider, model) = Resolve(label);
        return provider switch
        {
            // Reuses NetEval's ClaudeJudge pattern: the Anthropic client reads
            // ANTHROPIC_API_KEY itself and adapts to IChatClient with a token cap.
            Provider.Claude => new AnthropicClient().AsIChatClient(model, MaxOutputTokens),

            Provider.OpenAI => new OpenAIClient(new ApiKeyCredential(RequireKey(label, "OPENAI_API_KEY")))
                .GetChatClient(model)
                .AsIChatClient(),

            // Cast the null logger to disambiguate from the multi-arg constructor overload.
            Provider.Gemini => new GeminiChatClient(
                RequireKey(label, "GEMINI_API_KEY", "GOOGLE_API_KEY"), model, (ILogger?)null),

            Provider.Ollama => new OllamaApiClient(
                new Uri(Environment.GetEnvironmentVariable("OLLAMA_HOST") ?? "http://localhost:11434"),
                defaultModel: model),

            _ => throw new ArgumentOutOfRangeException(nameof(label), provider, "Unhandled provider."),
        };
    }

    private enum Provider { Claude, OpenAI, Gemini, Ollama }

    private static (Provider Provider, string Model) Resolve(string label)
    {
        if (string.IsNullOrWhiteSpace(label))
            throw new ArgumentException("Model label must not be empty.", nameof(label));

        // Explicit provider:model prefix always wins.
        var colon = label.IndexOf(':');
        if (colon > 0)
        {
            var scheme = label[..colon];
            var rest = label[(colon + 1)..];
            var provider = scheme.ToLowerInvariant() switch
            {
                "claude" or "anthropic" => (Provider?)Provider.Claude,
                "openai" or "gpt" => Provider.OpenAI,
                "gemini" or "google" => Provider.Gemini,
                "ollama" => Provider.Ollama,
                _ => null,
            };
            if (provider is { } p)
                return (p, rest);
            // Not a known scheme (e.g. a stray colon in an Ollama tag like "qwen:7b" without prefix)
            // — fall through to bare-label heuristics on the whole string.
        }

        // Bare frontier model IDs are self-identifying.
        if (label.StartsWith("claude-", StringComparison.OrdinalIgnoreCase))
            return (Provider.Claude, label);
        if (label.StartsWith("gpt-", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("o1", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("o3", StringComparison.OrdinalIgnoreCase)
            || label.StartsWith("o4", StringComparison.OrdinalIgnoreCase))
            return (Provider.OpenAI, label);
        if (label.StartsWith("gemini-", StringComparison.OrdinalIgnoreCase))
            return (Provider.Gemini, label);

        throw new NotSupportedException(
            $"Could not resolve a provider for '{label}'. Use a bare frontier model ID (claude-*, gpt-*, o1/o3/o4-*, " +
            "gemini-*), or an explicit 'provider:model' prefix: claude:, openai:, gemini:, ollama:.");
    }

    private static string RequireKey(string label, params string[] envVarNames)
    {
        foreach (var name in envVarNames)
        {
            var value = Environment.GetEnvironmentVariable(name);
            if (!string.IsNullOrWhiteSpace(value))
                return value;
        }

        throw new InvalidOperationException(
            $"'{label}' needs an API key: set {string.Join(" or ", envVarNames)}.");
    }
}
