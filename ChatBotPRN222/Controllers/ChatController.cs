using System.Security.Claims;
using ChatBotPRN222.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ServiceLayer.Services;

namespace ChatBotPRN222.Controllers;

[Authorize]
public class ChatController : Controller
{
    private readonly IChatService _chat;
    private readonly ISubjectService _subject;
    public ChatController(IChatService chat, ISubjectService subject)
    {
        _chat = chat;
        _subject = subject;
    }

    private string UserId => User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public async Task<IActionResult> Index(string? id = null)
    {
        var sessions = await _chat.GetSessionsAsync(UserId);
        var subjects = await _subject.GetAllAsync();

        var vm = new ChatIndexViewModel
        {
            Sessions = sessions,
            Subjects = subjects
        };

        if (!string.IsNullOrEmpty(id))
        {
            var session = await _chat.GetSessionAsync(id);
            if (session is not null && session.UserId == UserId)
            {
                vm.CurrentSession = session;
                vm.Messages = await _chat.GetMessagesAsync(id);
            }
        }
        else if (sessions.Count > 0)
        {
            vm.CurrentSession = sessions[0];
            vm.Messages = await _chat.GetMessagesAsync(sessions[0].Id);
        }

        return View(vm);
    }

    [HttpPost]
    public async Task<IActionResult> NewSession([FromBody] CreateSessionRequest req)
    {
        var session = await _chat.CreateSessionAsync(UserId, req?.SubjectId);
        return Json(new { id = session.Id, title = session.Title });
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] AskRequest req)
    {
        if (req is null || string.IsNullOrWhiteSpace(req.Question) || string.IsNullOrEmpty(req.SessionId))
            return BadRequest(new { error = "Thiếu sessionId hoặc nội dung câu hỏi" });

        var session = await _chat.GetSessionAsync(req.SessionId);
        if (session is null || session.UserId != UserId)
            return Forbid();

        var answer = await _chat.AskAsync(req.SessionId, UserId, req.Question.Trim());
        return Json(new
        {
            answer = answer.Answer,
            sources = answer.Sources.Select(s => new
            {
                documentName = s.DocumentName,
                page = s.Page,
                chunkIndex = s.ChunkIndex,
                snippet = s.Snippet
            })
        });
    }

    [HttpPost]
    public async Task<IActionResult> Delete(string id)
    {
        var session = await _chat.GetSessionAsync(id);
        if (session is null || session.UserId != UserId) return Forbid();
        await _chat.DeleteSessionAsync(id);
        return RedirectToAction(nameof(Index));
    }
}
