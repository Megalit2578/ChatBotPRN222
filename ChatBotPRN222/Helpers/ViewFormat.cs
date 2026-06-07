namespace ChatBotPRN222.Helpers;

public static class ViewFormat
{
    /// <summary>Định dạng kích thước tệp gọn gàng: B / KB / MB / GB (tránh hiển thị "0,00 MB" với file nhỏ).</summary>
    public static string FileSize(long bytes)
    {
        if (bytes <= 0) return "0 KB";
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double size = bytes;
        int u = 0;
        while (size >= 1024 && u < units.Length - 1)
        {
            size /= 1024;
            u++;
        }
        // Byte hiển thị số nguyên; KB trở lên hiển thị tối đa 2 chữ số thập phân (bỏ số 0 thừa).
        return u == 0 ? $"{size:0} {units[u]}" : $"{size:0.##} {units[u]}";
    }

    /// <summary>Lớp CSS Bootstrap, icon và nhãn tiếng Việt cho một trạng thái tài liệu (status pipeline).</summary>
    public static (string Css, string Icon, string Label) StatusBadge(string? status) => status switch
    {
        "Indexed" => ("bg-success-subtle text-success border border-success-subtle", "bi-check-circle-fill", "Đã index"),
        "Processing" => ("bg-info-subtle text-info border border-info-subtle", "bi-arrow-repeat", "Đang xử lý"),
        "Pending" => ("bg-secondary-subtle text-secondary border border-secondary-subtle", "bi-hourglass-split", "Chờ xử lý"),
        "Failed" => ("bg-danger-subtle text-danger border border-danger-subtle", "bi-x-circle-fill", "Thất bại"),
        "Empty" => ("bg-warning-subtle text-warning border border-warning-subtle", "bi-slash-circle", "Rỗng"),
        _ => ("bg-light text-dark border", "bi-question-circle", status ?? "?")
    };
}
