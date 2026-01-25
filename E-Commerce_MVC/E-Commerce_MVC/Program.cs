using BLL.Helper;
using BLL.IService;
using BLL.Service;
using DAL.Entities;
using DAL.IRepository;
using DAL.Repository;
using DotNetEnv;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using System.Collections;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

Env.Load();

builder.Configuration.AddEnvironmentVariables();

// DEBUG: Kiểm tra xem đã nhận được chưa (Chỉ chạy khi Dev)
if (builder.Environment.IsDevelopment())
{
    Console.WriteLine("=== ENV LOAD CHECK ===");
    Console.WriteLine($"DB Connected: {!string.IsNullOrEmpty(builder.Configuration.GetConnectionString("DefaultConnection"))}");
    Console.WriteLine($"JWT Token Loaded: {!string.IsNullOrEmpty(builder.Configuration["Jwt:Token"])}");
    Console.WriteLine($"Gemini Key Loaded: {!string.IsNullOrEmpty(builder.Configuration["Gemini:ApiKey"])}");
    Console.WriteLine($"Email Configured: {!string.IsNullOrEmpty(builder.Configuration["EmailSettings:SmtpPass"])}");
    Console.WriteLine("======================");
}

var envPath = Path.Combine(Directory.GetCurrentDirectory(), "SMTP.env");
if (File.Exists(envPath))
{
    Env.Load(envPath);
    Console.WriteLine("✅ SMTP.env loaded");

    // ✅ INJECT VÀO CONFIGURATION (QUAN TRỌNG!)
    var envVars = new Dictionary<string, string>
    {
        ["EmailSettings:FromEmail"] = Environment.GetEnvironmentVariable("EmailSettings__FromEmail") ?? "",
        ["EmailSettings:SmtpHost"] = Environment.GetEnvironmentVariable("EmailSettings__SmtpHost") ?? "",
        ["EmailSettings:SmtpPort"] = Environment.GetEnvironmentVariable("EmailSettings__SmtpPort") ?? "",
        ["EmailSettings:SmtpUser"] = Environment.GetEnvironmentVariable("EmailSettings__SmtpUser") ?? "",
        ["EmailSettings:SmtpPass"] = Environment.GetEnvironmentVariable("EmailSettings__SmtpPass") ?? ""
    };

    foreach (var kvp in envVars)
    {
        if (!string.IsNullOrEmpty(kvp.Value))
        {
            builder.Configuration[kvp.Key] = kvp.Value;
        }
    }
}
else
{
    Console.WriteLine($"⚠️ SMTP.env not found at: {envPath}");
}

 
Console.WriteLine("=== CONFIG CHECK ===");
Console.WriteLine($"FromEmail: {builder.Configuration["EmailSettings:FromEmail"]}");
Console.WriteLine($"SmtpHost: {builder.Configuration["EmailSettings:SmtpHost"]}");
Console.WriteLine($"SmtpUser: {builder.Configuration["EmailSettings:SmtpUser"]}");
Console.WriteLine($"SmtpPass: {(builder.Configuration["EmailSettings:SmtpPass"]?.Length > 0 ? "***SET***" : "❌ EMPTY")}");
Console.WriteLine("====================");


builder.Services.AddHttpClient();

builder.Services.AddScoped<GeminiHelper>();
// Add services to the container.
builder.Services.AddControllersWithViews();
//NewtonsoftJson cho GenericResult<T>
builder.Services.AddControllers()
    .AddNewtonsoftJson(options =>
    {
        options.SerializerSettings.NullValueHandling = NullValueHandling.Ignore;
    });

// DbContext
builder.Services.AddDbContext<ShopDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Repositories
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<IRoleRepository, RoleRepository>();
builder.Services.AddScoped<ICartRepository, CartRepository>();
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IOrderItemRepository, OrderItemRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<ICategoryRepository, CategoryRepository>();
builder.Services.AddScoped<IInventoryRepository, InventoryRepository>();
builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
builder.Services.AddScoped<IEmailVerificationTokenRepository, EmailVerificationTokenRepository>();

// Services
builder.Services.AddScoped<IJwtService, JwtService>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IWishlistService, WishlistService>();
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IRoleService, RoleService>();
builder.Services.AddScoped<ICartService, CartService>();
builder.Services.AddScoped<IOrderService, OrderService>();
builder.Services.AddScoped<IProductService, ProductService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IInventoryService, InventoryService>();
builder.Services.AddScoped<IGoogleAuthService, GoogleAuthService>();
builder.Services.AddScoped<IEmailVerificationService, EmailVerificationService>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();


// JWT Authentication
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Token"])),
        ClockSkew = TimeSpan.Zero
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            // Nếu có cookie "jwt" → dùng làm Bearer token
            if (context.Request.Cookies.TryGetValue("jwt", out var token))
                context.Token = token;

            return Task.CompletedTask;
        }
    };
});
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new() { Title = "E-Commerce API", Version = "v1" });

    // ← THÊM JWT cho Swagger: nút Authorize
    options.AddSecurityDefinition("Bearer", new()
    {
        Description = "Nhập 'Bearer {token}' để authorize",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new() { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            Array.Empty<string>()
        }
    });
});

// Trong app pipeline (sau app.UseRouting())

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseHttpsRedirection();
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseStaticFiles();

app.UseRouting();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "E-Commerce API v1");
    });
}

app.UseAuthentication();  // ← Quan trọng: trước UseAuthorization
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
