using DataAccessLayer.Context;
using DataAccessLayer.Repositories;
using DataAccessLayer.Settings;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
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
            builder.Services.Configure<MongoDbSettings>(builder.Configuration.GetSection("MongoDb"));
            builder.Services.Configure<GeminiSettings>(builder.Configuration.GetSection("Gemini"));
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

            // === DAL ===
            builder.Services.AddSingleton<MongoDbContext>();
            builder.Services.AddScoped<IUserRepository, UserRepository>();
            builder.Services.AddScoped<ISubjectRepository, SubjectRepository>();
            builder.Services.AddScoped<IDocumentRepository, DocumentRepository>();
            builder.Services.AddScoped<IDocumentChunkRepository, DocumentChunkRepository>();
            builder.Services.AddScoped<IChatRepository, ChatRepository>();

            // === Services ===
            builder.Services.AddScoped<IAuthService, AuthService>();
            builder.Services.AddScoped<IUserService, UserService>();
            builder.Services.AddScoped<ISubjectService, SubjectService>();
            builder.Services.AddSingleton<ITextExtractor, TextExtractor>();
            builder.Services.AddSingleton<IChunker, SlidingWindowChunker>();
            builder.Services.AddScoped<IDocumentService, DocumentService>();
            builder.Services.AddScoped<IChatService, ChatService>();
            builder.Services.AddHttpClient<IGeminiService, GeminiService>(c =>
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

            using (var scope = app.Services.CreateScope())
            {
                try
                {
                    var auth = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    var subjects = scope.ServiceProvider.GetRequiredService<ISubjectService>();
                    await auth.EnsureSeedUsersAsync();
                    await subjects.EnsureSeedAsync();
                }
                catch (Exception ex)
                {
                    var logger = app.Services.GetRequiredService<ILogger<Program>>();
                    logger.LogWarning(ex, "Seeding skipped — check MongoDB connection in appsettings.json");
                }
            }

            app.Run();
        }
    }
}
