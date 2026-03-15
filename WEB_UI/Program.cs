using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.EntityFrameworkCore;
using WEB_UI.Data;

var builder = WebApplication.CreateBuilder(args);

// ── MVC ───────────────────────────────────────────────────────────────────────
builder.Services.AddControllersWithViews();

// ── DbContext ─────────────────────────────────────────────────────────────────
builder.Services.AddDbContext<NativaDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// ── Cookie Authentication ─────────────────────────────────────────────────────
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.Cookie.Name     = builder.Configuration["Auth:CookieName"] ?? "nativa_auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan  = TimeSpan.FromHours(
            int.Parse(builder.Configuration["Auth:ExpireHours"] ?? "8"));
        options.SlidingExpiration = false;
        options.LoginPath         = "/Auth/Login";
        options.AccessDeniedPath  = "/Auth/AccesoDenegado";
    });

// ── Session (mantener para compatibilidad con UI existente) ───────────────────
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout        = TimeSpan.FromMinutes(60);
    options.Cookie.HttpOnly    = true;
    options.Cookie.IsEssential = true;
    options.Cookie.Name        = ".Nativa.Session";
});

// ── HttpClient para APIs externas ─────────────────────────────────────────────
builder.Services.AddHttpClient();

// ── IHttpContextAccessor ──────────────────────────────────────────────────────
builder.Services.AddHttpContextAccessor();

// ── Servicios de Auth ─────────────────────────────────────────────────────────
builder.Services.AddScoped<WEB_UI.Services.OtpService>();
builder.Services.AddScoped<WEB_UI.Services.EmailService>();
builder.Services.AddScoped<WEB_UI.Services.AuthService>();

var app = builder.Build();

// ── Pipeline ──────────────────────────────────────────────────────────────────
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
app.MapStaticAssets();

// Ruta por defecto: Landing/Project
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
