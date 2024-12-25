var builder = WebApplication.CreateBuilder(args);

// 1. Agregar Servicios al Contenedor
builder.Services.AddControllersWithViews(); // Habilita controladores y vistas para MVC
builder.Services.AddHttpClient(); // Habilita IHttpClientFactory para el uso de HttpClient
builder.Services.AddHttpContextAccessor(); // Permite acceso al contexto HTTP (incluidas sesiones)
builder.Services.AddDistributedMemoryCache(); // Agrega soporte para sesiones en memoria

// Configura el manejo de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiraci�n de la sesi�n
    options.Cookie.HttpOnly = true; // Mejora la seguridad del manejo de cookies
    options.Cookie.IsEssential = true; // Necesario para funcionar incluso si el usuario rechaza cookies no esenciales
});

// 2. Construcci�n de la Aplicaci�n
var app = builder.Build();

// 3. Configuraci�n del Middleware (Pipeline de Solicitudes)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo de excepciones en producci�n
    app.UseHsts(); // Habilita HSTS para mayor seguridad
}

app.UseHttpsRedirection(); // Redirige autom�ticamente HTTP a HTTPS
app.UseStaticFiles(); // Habilita archivos est�ticos en wwwroot

app.UseRouting(); // Habilita el enrutamiento de controladores
app.UseSession(); // Habilita el middleware de sesiones
app.UseAuthorization(); // Manejo de autorizaci�n si aplicas pol�ticas

// Configura el enrutamiento principal (Controlador y Acci�n predeterminados)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Redirige al Login como vista predeterminada

//PRUEBAS
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Pedidos}/{action=NuevoPedido}/{id?}"); 

// Ejecuta la aplicaci�n
app.Run();
