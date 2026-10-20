using System;
using System.Text.Json;

// Talking to the local AI server (v5.18, untested in game).
// Api = "openai" (default): the OpenAI-style chat API that KoboldCpp, llama.cpp's llama-server, LM Studio and Ollama all offer
//                            at <Url>/v1/chat/completions. One code path for all of them.
// Api = "ollama":            Ollama's own /api/chat (only needed for its "think" switch).
// Used by the chat, the load greeting and the diary. No game types here.
public static class Llm
{
    public static bool OpenAi { get { return !string.Equals((Cfg.ApiType.Value ?? "").Trim(), "ollama", StringComparison.OrdinalIgnoreCase); } }

    public static string ChatUrl()
    {
        string u = (Cfg.OllamaUrl.Value ?? "").Trim().TrimEnd('/');
        if (!OpenAi) return u + "/api/chat";
        if (u.EndsWith("/chat/completions", StringComparison.OrdinalIgnoreCase)) return u;
        if (u.EndsWith("/v1", StringComparison.OrdinalIgnoreCase)) return u + "/chat/completions";
        return u + "/v1/chat/completions";
    }

    static string ModelName()
    {
        string m = (Cfg.Model.Value ?? "").Trim();
        return m.Length > 0 ? m : "local-model";     // KoboldCpp and llama-server ignore the name; Ollama and LM Studio need the real one
    }

    public static string Body(object messages, bool stream, int maxTokens)
    {
        if (OpenAi)
            return JsonSerializer.Serialize(new
            {
                model = ModelName(),
                messages = messages,
                stream = stream,
                max_tokens = maxTokens,
                temperature = Cfg.Temperature.Value
            });
        return JsonSerializer.Serialize(new
        {
            model = ModelName(),
            messages = messages,
            stream = stream,
            think = Cfg.Think.Value,
            options = new { num_predict = maxTokens, temperature = Cfg.Temperature.Value }
        });
    }

    // one line of a streamed answer: OpenAI style is "data: {...}" (server-sent events) ending with "data: [DONE]"; Ollama sends one JSON object per line
    public static bool StreamLine(string line, out string text, out bool done)
    {
        text = null; done = false;
        line = (line ?? "").Trim();
        if (line.Length == 0) return false;
        if (OpenAi)
        {
            if (!line.StartsWith("data:", StringComparison.Ordinal)) return false;     // "event:" lines, keep-alive comments
            line = line.Substring(5).Trim();
            if (line == "[DONE]") { done = true; return true; }
        }
        try
        {
            using (var doc = JsonDocument.Parse(line))
            {
                var root = doc.RootElement;
                if (OpenAi)
                {
                    if (root.TryGetProperty("choices", out var ch) && ch.ValueKind == JsonValueKind.Array && ch.GetArrayLength() > 0)
                    {
                        var c0 = ch[0];
                        if (c0.TryGetProperty("delta", out var d) && d.TryGetProperty("content", out var ct) && ct.ValueKind == JsonValueKind.String) text = ct.GetString();
                        if (c0.TryGetProperty("finish_reason", out var fr) && fr.ValueKind == JsonValueKind.String) done = true;
                    }
                }
                else
                {
                    if (root.TryGetProperty("message", out var m) && m.TryGetProperty("content", out var c) && c.ValueKind == JsonValueKind.String) text = c.GetString();
                    if (root.TryGetProperty("done", out var dn) && dn.ValueKind == JsonValueKind.True) done = true;
                }
            }
        }
        catch { return false; }
        return true;
    }

    // the whole answer of a request sent with stream = false
    public static string FullText(string json)
    {
        using (var doc = JsonDocument.Parse(json))
        {
            var root = doc.RootElement;
            if (root.TryGetProperty("choices", out var ch) && ch.ValueKind == JsonValueKind.Array && ch.GetArrayLength() > 0
                && ch[0].TryGetProperty("message", out var m1) && m1.TryGetProperty("content", out var c1) && c1.ValueKind == JsonValueKind.String)
                return c1.GetString();
            if (root.TryGetProperty("message", out var m2) && m2.TryGetProperty("content", out var c2) && c2.ValueKind == JsonValueKind.String)
                return c2.GetString();
        }
        return "";
    }

    public static string NotReachable { get { return "Can't reach the AI server at " + Cfg.OllamaUrl.Value + ". Is KoboldCpp / llama-server / Ollama running? (AI/Url in the settings)"; } }
}
