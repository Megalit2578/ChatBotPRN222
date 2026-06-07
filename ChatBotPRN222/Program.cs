using DataAccessLayer.Context;
using DataAccessLayer.Repositories;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using ServiceLayer.Services;
using ServiceLayer.Settings;

namespace ChatBotPRN222
{
    public class Program
    {
        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // === Config ===
            builder.Services.Configure<GroqSettings>(builder.Configuration.GetSection("Groq"));
            builder.Services.Configure<EmailSettings>(builder.Configuration.GetSection("Email"));

            // === Upload size limits (2GB) ===
            const long maxUploadBytes = 2048L * 1024 * 1024;
            builder.Services.Configure<FormOptions>(o =>
            {
                o.MultipartBodyLengthLimit = maxUploadBytes;
                o.ValueLengthLimit = int.MaxValue;
                o.MemoryBufferThreshold = int.MaxValue;
            });
            builder.WebHost.ConfigureKestrel(opt =>
            {
                opt.Limits.MaxRequestBodySize = maxUploadBytes;
            });
            builder.Services.Configure<Microsoft.AspNetCore.Builder.IISServerOptions>(o =>
            {
                o.MaxRequestBodySize = maxUploadBytes;
            });

            // === Database ===
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

            // === DAL ===
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
            builder.Services.AddScoped<IChapterRepository, ChapterRepository>();
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            builder.Services.AddScoped<IFeedbackReplyRepository, FeedbackReplyRepository>();
            builder.Services.AddScoped<IAllowedEmailRepository, AllowedEmailRepository>();

            // === Services ===
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IEmailService, EmailService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddScoped<IChapterService, ChapterService>();
            builder.Services.AddScoped<IAllowedEmailService, AllowedEmailService>();
            builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
            // Chunking bằng Microsoft Semantic Kernel TextChunker (đo theo ký tự) — giống Assignment_1.
            builder.Services.AddSingleton<IChunker, TextChunkerChunker>();
            builder.Services.AddSingleton<IDocumentFileStore>(_ =>
                new LocalDocumentFileStore(Path.Combine(builder.Environment.ContentRootPath, "App_Data", "uploads")));
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddScoped<Seeders.DocumentSeeder>();
            builder.Services.AddHttpClient<IGroqService, GroqService>(c =>
            {
                c.Timeout = TimeSpan.FromSeconds(60);
            });

            // === Auth ===
            builder.Services
                .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.LoginPath = "/Auth/Login";
                    options.LogoutPath = "/Auth/Logout";
                    options.AccessDeniedPath = "/Auth/AccessDenied";
                    options.ExpireTimeSpan = TimeSpan.FromHours(8);
                    options.SlidingExpiration = true;
                });

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("LecturerOrAdmin", p => p.RequireRole("Lecturer", "Admin"));
                options.AddPolicy("AdminOnly", p => p.RequireRole("Admin"));
                // Admins always; lecturers only if granted the upload permission (CanUpload claim).
                options.AddPolicy("CanUploadDocuments", p => p.RequireAssertion(ctx =>
                    ctx.User.IsInRole("Admin") || ctx.User.HasClaim("CanUpload", "true")));
            });

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddSession(o =>
            {
                o.IdleTimeout = TimeSpan.FromHours(2);
                o.Cookie.HttpOnly = true;
                o.Cookie.IsEssential = true;
            });

            builder.Services.AddControllersWithViews();

            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Home/Error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");

            // === DB Init & Seed ===
            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    db.Database.EnsureCreated();

                    // EnsureCreated() does not migrate existing databases, so add newer columns/tables
                    // by hand if they're missing (idempotent — safe to run on every startup).
                    db.Database.ExecuteSqlRaw(@"
                        IF COL_LENGTH('Users', 'CanUploadDocuments') IS NULL
                            ALTER TABLE Users ADD CanUploadDocuments bit NOT NULL DEFAULT 0;
                        IF COL_LENGTH('Users', 'AssignedSubjectId') IS NULL
                            ALTER TABLE Users ADD AssignedSubjectId nvarchar(36) NULL;

                        -- Chapters (Subject → Chapter → Document). Index tạo cùng khối với bảng
                        -- (OBJECT_ID không nhận diện index nên không thể dùng để kiểm tra tồn tại).
                        IF OBJECT_ID('Chapters') IS NULL
                        BEGIN
                            CREATE TABLE Chapters (
                                Id          nvarchar(36)  NOT NULL PRIMARY KEY,
                                SubjectId   nvarchar(36)  NOT NULL DEFAULT '',
                                Title       nvarchar(300) NOT NULL DEFAULT '',
                                Description nvarchar(max) NOT NULL DEFAULT '',
                                OrderIndex  int           NOT NULL DEFAULT 0,
                                CreatedAt   datetime2     NOT NULL DEFAULT GETUTCDATE()
                            );
                            CREATE INDEX IX_Chapters_SubjectId ON Chapters (SubjectId);
                        END;

                        -- Optional chapter link on documents
                        IF COL_LENGTH('Documents', 'ChapterId') IS NULL
                            ALTER TABLE Documents ADD ChapterId nvarchar(36) NULL;

                        -- Email registration whitelist
                        IF OBJECT_ID('AllowedEmails') IS NULL
                        BEGIN
                            CREATE TABLE AllowedEmails (
                                Id        nvarchar(36)  NOT NULL PRIMARY KEY,
                                Email     nvarchar(200) NOT NULL DEFAULT '',
                                Note      nvarchar(300) NOT NULL DEFAULT '',
                                AddedBy   nvarchar(200) NOT NULL DEFAULT '',
                                CreatedAt datetime2     NOT NULL DEFAULT GETUTCDATE()
                            );
                            CREATE UNIQUE INDEX UX_AllowedEmails_Email ON AllowedEmails (Email);
                        END;");

                    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    var subjects = scope.ServiceProvider.GetRequiredService<ISubjectService>();
                    await auth.EnsureSeedUsersAsync();
                    await subjects.EnsureSeedAsync();

                    // Tự động index bộ tài liệu mẫu (SeedData/) cho lần chạy đầu — để test set dùng được ngay.
                    var docSeeder = scope.ServiceProvider.GetRequiredService<Seeders.DocumentSeeder>();
                    await docSeeder.SeedAsync(Path.Combine(app.Environment.ContentRootPath, "SeedData"));
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "DB init or seeding failed — check connection string in appsettings.json");
                }
            }

            app.Run();
        }
    }
}
