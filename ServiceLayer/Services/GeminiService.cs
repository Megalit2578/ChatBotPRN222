using System.Text;
using System.Text.Json;
using DataAccessLayer.Entities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ServiceLayer.Settings;

namespace ServiceLayer.Services;

public class GeminiService : IGeminiService
{
    private readonly HttpClient _http;
    private readonly GeminiSettings _settings;
    private readonly ILogger<GeminiService> _logger;

    public GeminiService(HttpClient http, IOptions<GeminiSettings> options, ILogger<GeminiService> logger)
    {
        _http = http;
        _settings = options.Value;
        _logger = logger;
    }

    public async Task<string> GenerateAnswerAsync(
        string question,
        IReadOnlyList<DocumentChunk> contextChunks,
        IReadOnlyList<ChatMessage> history,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(_settings.ApiKey))
            return BuildFallback(contextChunks);

        var systemPrompt = BuildSystemPrompt(contextChunks);
        var contents = new List<object>();

        // Add recent history (last 6 turns) as conversation context
        foreach (var msg in history.TakeLast(6))
        {
            contents.Add(new
            {
                role = msg.Role == "assistant" ? "model" : "user",
                parts = new[] { new { text = msg.Content } }
            });
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = question } }
        });

        var payload = new
        {
            systemInstruction = new { parts = new[] { new { text = systemPrompt } } },
            contents,
            generationConfig = new
            {
                temperature = 0.3,
                topP = 0.95,
                maxOutputTokens = 1024
            }
        };

        var url = $"{_settings.BaseUrl}/models/{_settings.Model}:generateContent?key={_settings.ApiKey}";
        var body = JsonSerializer.Serialize(payload);

        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Post, url)
            {
                Content = new StringContent(body, Encoding.UTF8, "application/json")
            };
            using var res = await _http.SendAsync(req, ct);
            var text = await res.Content.ReadAsStringAsync(ct);

            if (!res.IsSuccessStatusCode)
            {
                _logger.LogWarning("Gemini API error {Status}: {Body}", res.StatusCode, text);
                return BuildFallback(contextChunks) +
                       $"\n\n_(Gemini API trả về {(int)res.StatusCode}, đã dùng fallback.)_";
            }

            using var doc = JsonDocument.Parse(text);
            var root = doc.RootElement;
            if (root.TryGetProperty("candidates", out var candidates) && candidates.GetArrayLength() > 0)
            {
                var content = candidates[0].GetProperty("content");
                if (content.TryGetProperty("parts", out var parts) && parts.GetArrayLength() > 0)
                {
                    var sb = new StringBuilder();
                    foreach (var part in parts.EnumerateArray())
                    {
                        if (part.TryGetProperty("text", out var t))
                            sb.Append(t.GetString());
                    }
                    return sb.ToString().Trim();
                }
            }

            return BuildFallback(contextChunks);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Gemini call failed");
            return BuildFallback(contextChunks) + "\n\n_(Lỗi mạng khi gọi Gemini, đã dùng fallback.)_";
        }
    }

    private static string BuildSystemPrompt(IReadOnlyList<DocumentChunk> chunks)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Bạn là trợ lý học tập AI cho sinh viên Việt Nam, trả lời bằng tiếng Việt.");
        sb.AppendLine("Quy tắc bắt buộc:");
        sb.AppendLine("1. CHỈ trả lời dựa trên nội dung tài liệu được cung cấp bên dưới.");
        sb.AppendLine("2. Nếu tài liệu không đủ thông tin, nói rõ \"Mình chưa tìm thấy trong tài liệu\" — KHÔNG được bịa.");
        sb.AppendLine("3. Khi trích dẫn, ghi rõ nguồn dạng [Tên tài liệu - Trang X] sau câu liên quan.");
        sb.AppendLine("4. Giọng thân thiện, ngắn gọn, có thể dùng markdown.");
        sb.AppendLine();
        sb.AppendLine("=== NGỮ CẢNH TÀI LIỆU ===");
        if (chunks.Count == 0)
        {
            sb.AppendLine("(Không có ngữ cảnh — hãy lịch sự thông báo cho người dùng rằng chưa có tài liệu liên quan.)");
        }
        else
        {
            int i = 1;
            foreach (var c in chunks)
            {
                sb.AppendLine($"[{i}] Nguồn: {c.DocumentName} - Trang {c.Page}");
                sb.AppendLine(c.Content);
                sb.AppendLine();
                i++;
            }
        }
        sb.AppendLine("=== HẾT NGỮ CẢNH ===");
        return sb.ToString();
    }

    private static string BuildFallback(IReadOnlyList<DocumentChunk> chunks)
    {
        if (chunks.Count == 0)
            return "Mình chưa tìm thấy thông tin liên quan trong tài liệu môn học. Bạn thử upload thêm tài liệu hoặc đặt câu hỏi khác nhé.";

        var sb = new StringBuilder();
        sb.AppendLine("Dựa trên tài liệu môn học, mình tìm thấy các đoạn liên quan sau:");
        sb.AppendLine();
        int i = 1;
        foreach (var c in chunks)
        {
            var snippet = c.Content.Length > 400 ? c.Content.Substring(0, 400) + "..." : c.Content;
            sb.AppendLine($"[{i}] *{c.DocumentName} - Trang {c.Page}*");
            sb.AppendLine(snippet);
            sb.AppendLine();
            i++;
        }
        return sb.ToString().Trim();
    }
}
