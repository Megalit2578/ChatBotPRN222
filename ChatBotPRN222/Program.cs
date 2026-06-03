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
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();
            builder.Services.AddScoped<IFeedbackRepository, FeedbackRepository>();
            builder.Services.AddScoped<IFeedbackReplyRepository, FeedbackReplyRepository>();

            // === Services ===
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
            builder.Services.AddSingleton<IChunker, SlidingWindowChunker>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IFeedbackService, FeedbackService>();
            builder.Services.AddScoped<IDashboardService, DashboardService>();
            builder.Services.AddScoped<IChatService, ChatService>();
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

                    // EnsureCreated does not migrate existing databases, so patch new
                    // schema (Document.Title + Feedbacks table) idempotently for older DBs.
                    db.Database.ExecuteSqlRaw(@"
IF COL_LENGTH('Documents', 'Title') IS NULL
    ALTER TABLE [Documents] ADD [Title] nvarchar(500) NOT NULL DEFAULT '';

IF COL_LENGTH('Documents', 'ContentHash') IS NULL
    ALTER TABLE [Documents] ADD [ContentHash] nvarchar(64) NOT NULL DEFAULT '';

IF OBJECT_ID('Feedbacks', 'U') IS NULL
CREATE TABLE [Feedbacks] (
    [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_Feedbacks] PRIMARY KEY,
    [UserId] nvarchar(36) NOT NULL,
    [UserName] nvarchar(200) NOT NULL,
    [UserAvatar] nvarchar(500) NULL,
    [Rating] int NOT NULL,
    [Content] nvarchar(max) NOT NULL,
    [CreatedAt] datetime2 NOT NULL,
    [AdminReply] nvarchar(max) NULL,
    [RepliedBy] nvarchar(200) NULL,
    [RepliedByAvatar] nvarchar(500) NULL,
    [RepliedAt] datetime2 NULL
);

IF COL_LENGTH('Feedbacks', 'UserAvatar') IS NULL
    ALTER TABLE [Feedbacks] ADD [UserAvatar] nvarchar(500) NULL;

IF COL_LENGTH('Feedbacks', 'RepliedByAvatar') IS NULL
    ALTER TABLE [Feedbacks] ADD [RepliedByAvatar] nvarchar(500) NULL;

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_Feedbacks_UserId' AND object_id = OBJECT_ID('Feedbacks'))
    CREATE INDEX [IX_Feedbacks_UserId] ON [Feedbacks] ([UserId]);

IF OBJECT_ID('FeedbackReplies', 'U') IS NULL
CREATE TABLE [FeedbackReplies] (
    [Id] nvarchar(36) NOT NULL CONSTRAINT [PK_FeedbackReplies] PRIMARY KEY,
    [FeedbackId] nvarchar(36) NOT NULL,
    [UserId] nvarchar(36) NOT NULL,
    [UserName] nvarchar(200) NOT NULL,
    [UserAvatar] nvarchar(500) NULL,
    [Content] nvarchar(max) NOT NULL,
    [IsAdmin] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL
);

IF NOT EXISTS (SELECT 1 FROM sys.indexes WHERE name = 'IX_FeedbackReplies_FeedbackId' AND object_id = OBJECT_ID('FeedbackReplies'))
    CREATE INDEX [IX_FeedbackReplies_FeedbackId] ON [FeedbackReplies] ([FeedbackId]);

-- Migrate legacy single admin reply into the new thread table (one-time, idempotent).
INSERT INTO [FeedbackReplies] ([Id], [FeedbackId], [UserId], [UserName], [UserAvatar], [Content], [IsAdmin], [CreatedAt])
SELECT NEWID(), f.[Id], '', ISNULL(f.[RepliedBy], 'Admin'), f.[RepliedByAvatar], f.[AdminReply], 1, ISNULL(f.[RepliedAt], f.[CreatedAt])
FROM [Feedbacks] f
WHERE f.[AdminReply] IS NOT NULL AND LTRIM(RTRIM(f.[AdminReply])) <> ''
  AND NOT EXISTS (SELECT 1 FROM [FeedbackReplies] r WHERE r.[FeedbackId] = f.[Id]);
");

                    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    var subjects = scope.ServiceProvider.GetRequiredService<ISubjectService>();
                    await auth.EnsureSeedUsersAsync();
                    await subjects.EnsureSeedAsync();
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
