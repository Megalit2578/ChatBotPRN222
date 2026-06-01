# ChatBotPRN222 - Hệ Thống Hỏi Đáp Tài Liệu Slide PowerPoint 🚀

Dự án **ChatBotPRN222** là một ứng dụng Web ASP.NET Core MVC (sử dụng .NET 8.0 và cơ sở dữ liệu SQL Server qua Entity Framework Core) kết hợp sức mạnh của mô hình ngôn ngữ lớn **Groq (Llama 3.3)** để trích xuất nội dung từ slide PowerPoint bài giảng và hỗ trợ sinh viên học tập, đặt câu hỏi trực tiếp dựa trên ngữ cảnh tài liệu học trình.

---

## 📌 1. Yêu cầu chuẩn bị (Prerequisites)
Để chạy được ứng dụng, máy tính của bạn cần cài đặt:

1. **.NET 8.0 SDK** (bắt buộc):
   - Tải về và cài đặt bản chính thức từ Microsoft tại đây: [Tải .NET 8.0 SDK](https://dotnet.microsoft.com/en-us/download/dotnet/8.0) (Chọn bản **SDK** tương ứng với hệ điều hành của bạn).
2. **Visual Studio 2022** (khuyên dùng trên Windows) hoặc **Visual Studio Code**:
   - Nếu dùng VS Code, bạn hãy cài thêm extension **C# Dev Kit**.
3. **Mạng Internet**:
   - Cần thiết để gọi Groq AI API được cấu hình sẵn.
4. **SQL Server** (LocalDB, Express hoặc bản đầy đủ):
   - Dùng làm cơ sở dữ liệu. Cập nhật chuỗi kết nối `DefaultConnection` trong `appsettings.json` cho khớp với SQL Server trên máy bạn. Lần chạy đầu, EF Core sẽ tự tạo schema và seed dữ liệu mẫu.

---

## 🛠️ 2. Cấu hình ứng dụng (Configuration)
Tất cả cấu hình quan trọng nằm trong file **`appsettings.json`** tại thư mục `ChatBotPRN222/ChatBotPRN222/appsettings.json`:
* **SQL Server**: Chỉnh `ConnectionStrings:DefaultConnection` trỏ tới SQL Server của bạn. Database và bảng sẽ được EF Core tự tạo (`EnsureCreated`) ở lần chạy đầu.
* **Groq API**: Đã cấu hình sẵn API Key và model `llama-3.3-70b-versatile` để Chatbot hoạt động ngay lập tức.

---

## 🏃‍♂️ 3. Cách chạy ứng dụng (How to Run)

Bạn có thể chạy dự án bằng **1 trong 2 cách** siêu đơn giản dưới đây:

### Cách 1: Chạy bằng Dòng lệnh (Command Line - Nhanh nhất)
1. Mở ứng dụng **Terminal**, **Command Prompt (CMD)** hoặc **PowerShell** trên máy tính.
2. Di chuyển (`cd`) vào thư mục **chứa dự án web** (thư mục có file `Program.cs`):
   ```bash
   cd ChatBotPRN222
   ```
3. Chạy lệnh khởi động:
   ```bash
   dotnet run
   ```
4. Đợi vài giây, Terminal sẽ hiển thị các đường dẫn chạy web. Hãy mở trình duyệt và truy cập:
   - **`http://localhost:5216`** hoặc **`https://localhost:7216`** (hoặc cổng bất kỳ hiển thị trên Terminal).

---

### Cách 2: Chạy bằng Visual Studio 2022
1. Nhấp đúp chuột vào file **`ChatBotPRN222.sln`** ở thư mục gốc để mở toàn bộ dự án bằng Visual Studio.
2. Đợi Visual Studio load các dự án con hoàn tất.
3. Nhấp nút **Play (Kestrel / ChatBotPRN222)** trên thanh công cụ, hoặc nhấn tổ hợp phím **`Ctrl + F5`** (chạy không debug) hoặc **`F5`** (chạy kèm debug).
4. Trình duyệt web sẽ tự động mở trang chủ của ứng dụng.

---

## 🔑 4. Tài khoản đăng nhập thử nghiệm (Test Accounts)
Sau khi ứng dụng khởi chạy thành công lần đầu tiên, hệ thống sẽ tự động khởi tạo (seed) cơ sở dữ liệu mẫu bao gồm các môn học mẫu (`PRN222`, `DBI202`) và **3 tài khoản thử nghiệm** sau để bạn trải nghiệm các vai trò khác nhau:

| Vai trò (Role) | Tên đăng nhập (Username) | Mật khẩu (Password) | Chức năng chính |
| :--- | :--- | :--- | :--- |
| **Quản trị viên (Admin)** | `admin` | `admin123` | Quản lý người dùng, cấu hình hệ thống |
| **Giảng viên (Lecturer)** | `lecturer` | `lecturer123` | Tải lên slide tài liệu (.pptx), tạo phòng chat |
| **Sinh viên (Student)** | `student` | `student123` | Vào phòng chat, hỏi đáp chatbot dựa trên tài liệu |

---

💡 **Mẹo sử dụng:** 
1. Đăng nhập tài khoản **`lecturer`** để tải tài liệu bài giảng PowerPoint lên môn học.
2. Đăng nhập tài khoản **`student`** để bắt đầu chat và đặt câu hỏi cho Bot dựa trên chính tài liệu vừa được giảng viên tải lên.
