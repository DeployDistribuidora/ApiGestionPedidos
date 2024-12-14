var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // Habilita IHttpClientFactory para el uso de HttpClient
builder.Services.AddHttpContextAccessor(); // Permite acceder al contexto HTTP, incluidas las sesiones

// Configura el manejo de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
    options.Cookie.HttpOnly = true; // Aumenta la seguridad
    options.Cookie.IsEssential = true; // Necesario para el funcionamiento incluso si el usuario rechaza cookies no esenciales
});

// Construye la aplicación
var app = builder.Build();

// Configura el pipeline de solicitudes.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo de excepciones en producción
    app.UseHsts(); // Habilita HSTS en entornos no de desarrollo
}

app.UseHttpsRedirection(); // Redirige HTTP a HTTPS
app.UseStaticFiles(); // Habilita archivos estáticos (wwwroot)

app.UseRouting(); // Habilita el enrutamiento de controladores

// Habilita el middleware de sesiones
app.UseSession();

// Habilita la autorización (si aplicas políticas)
app.UseAuthorization();

// Configura el enrutamiento principal
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Establece el controlador y acción predeterminados al Login

// Ejecuta la aplicación
app.Run();
