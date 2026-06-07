using DataAccessLayer.Entities;

namespace ChatBotPRN222.Models;

public class AllowedEmailIndexViewModel
{
    public List<AllowedEmail> Emails { get; set; } = new();
    // True khi danh sách có ít nhất 1 email → đăng ký bị giới hạn theo whitelist.
    public bool WhitelistEnabled => Emails.Count > 0;
}
