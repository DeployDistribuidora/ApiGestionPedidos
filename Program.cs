var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // Habilita IHttpClientFactory para el uso de HttpClient
builder.Services.AddHttpContextAccessor(); // Permite acceder al contexto HTTP, incluidas las sesiones

// Configura el manejo de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiraci�n de la sesi�n
    options.Cookie.HttpOnly = true; // Aumenta la seguridad
    options.Cookie.IsEssential = true; // Necesario para el funcionamiento incluso si el usuario rechaza cookies no esenciales
});

// Construye la aplicaci�n
var app = builder.Build();

// Configura el pipeline de solicitudes.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo de excepciones en producci�n
    app.UseHsts(); // Habilita HSTS en entornos no de desarrollo
}

app.UseHttpsRedirection(); // Redirige HTTP a HTTPS
app.UseStaticFiles(); // Habilita archivos est�ticos (wwwroot)

app.UseRouting(); // Habilita el enrutamiento de controladores

// Habilita el middleware de sesiones
app.UseSession();

// Habilita la autorizaci�n (si aplicas pol�ticas)
app.UseAuthorization();

// Configura el enrutamiento principal
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Establece el controlador y acci�n predeterminados al Login

// Ejecuta la aplicaci�n
app.Run();
