using Microsoft.EntityFrameworkCore;
using Nativa.Infrastructure;
using WEB_UI.Services;

var builder = WebApplication.CreateBuilder(args);

// MVC
builder.Services.AddControllersWithViews();

// EF Core → SQL Server
builder.Services.AddDbContext<NativaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Cookie Auth — nativa_auth, 8h, HttpOnly, SameSite=Strict
builder.Services.AddAuthentication("nativa_auth")
    .AddCookie("nativa_auth", o =>
    {
        o.LoginPath           = "/Auth/Login";
        o.LogoutPath          = "/Auth/Logout";
        o.ExpireTimeSpan      = TimeSpan.FromHours(
            builder.Configuration.GetValue<int>("Auth:ExpireHours"));
        o.SlidingExpiration   = false;
        o.Cookie.HttpOnly     = true;
        o.Cookie.SameSite     = SameSiteMode.Strict;
        o.Cookie.Name         = builder.Configuration["Auth:CookieName"]!;
        o.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    });

// Servicios de aplicación
builder.Services.AddScoped<EmailService>();
builder.Services.AddScoped<OtpService>();
builder.Services.AddScoped<AuthService>();
builder.Services.AddScoped<BlobService>();

// P5 — Cuentas bancarias, cálculo PSA, activación de planes
builder.Services.AddScoped<BankAccountService>();
builder.Services.AddScoped<CalculatorService>();
builder.Services.AddScoped<PlanActivationService>();

// P6 — Ejecución automática de pagos mensuales (background)
builder.Services.AddHostedService<PaymentHostedService>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();
app.UseAuthentication();   // DEBE ir antes de UseAuthorization
app.UseAuthorization();
app.MapStaticAssets();

// Ruta por defecto
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
