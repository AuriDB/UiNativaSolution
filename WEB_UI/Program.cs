// ============================================================
// Program.cs — Punto de entrada y configuración de la aplicación
// WEB_UI es SOLO la capa de presentación (MVC + Razor Views).
// Toda la lógica de negocio vive en la API externa.
// ============================================================

var builder = WebApplication.CreateBuilder(args);

// ----------------------------------------------------------
// SERVICIOS
// ----------------------------------------------------------

// Registra los controllers MVC y el motor de vistas Razor.
// Permite usar Controllers con sus Views correspondientes.
builder.Services.AddControllersWithViews();

// Habilita el proveedor de caché en memoria necesario para Session.
// Sin esto, AddSession() falla en tiempo de ejecución.
builder.Services.AddDistributedMemoryCache();

// Configura la cookie de sesión del lado del servidor.
// Se usa para guardar temporalmente datos del usuario autenticado
// (nombre, rol) mientras no esté conectada la API.
builder.Services.AddSession(options =>
{
    // La sesión expira si el usuario está inactivo por 60 minutos.
    options.IdleTimeout        = TimeSpan.FromMinutes(60);

    // HttpOnly: la cookie no es accesible desde JavaScript (seguridad XSS).
    options.Cookie.HttpOnly    = true;

    // IsEssential: la cookie se crea aunque el usuario no haya aceptado cookies
    // (requerido por GDPR para cookies funcionales).
    options.Cookie.IsEssential = true;

    // Nombre personalizado de la cookie de sesión en el navegador.
    options.Cookie.Name        = ".Nativa.Session";
});

// Registra IHttpContextAccessor en el contenedor DI.
// Permite acceder al HttpContext (y por ende a la sesión)
// desde lugares que no son controllers, como _Layout.cshtml.
builder.Services.AddHttpContextAccessor();

// ----------------------------------------------------------
// PIPELINE DE MIDDLEWARE
// ----------------------------------------------------------

var app = builder.Build();

// En producción: redirige al handler de errores y activa HSTS
// (fuerza HTTPS por un período de tiempo en el navegador).
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

// Redirige automáticamente las peticiones HTTP a HTTPS.
app.UseHttpsRedirection();

// Habilita el enrutamiento de requests a controllers.
app.UseRouting();

// Activa el middleware de Session. Debe ir DESPUÉS de UseRouting
// y ANTES de UseAuthorization para que los controllers puedan leer la sesión.
app.UseSession();

// Habilita la autorización (aunque actualmente no se usa [Authorize],
// es buena práctica dejarlo registrado para cuando se integre la API).
app.UseAuthorization();

// Sirve archivos estáticos (CSS, JS, imágenes) desde wwwroot/
// con soporte de fingerprinting para cache busting.
app.MapStaticAssets();

// Ruta MVC por defecto: si no se especifica controller ni action,
// va a LandingController.Index() → muestra la página pública del proyecto.
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Landing}/{action=Index}/{id?}")
    .WithStaticAssets();

app.Run();
