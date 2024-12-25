var builder = WebApplication.CreateBuilder(args);

// 1. Agregar Servicios al Contenedor
builder.Services.AddControllersWithViews(); // Habilita controladores y vistas para MVC
builder.Services.AddHttpClient(); // Habilita IHttpClientFactory para el uso de HttpClient
builder.Services.AddHttpContextAccessor(); // Permite acceso al contexto HTTP (incluidas sesiones)
builder.Services.AddDistributedMemoryCache(); // Agrega soporte para sesiones en memoria

// Configura el manejo de sesiones
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30); // Tiempo de expiración de la sesión
    options.Cookie.HttpOnly = true; // Mejora la seguridad del manejo de cookies
    options.Cookie.IsEssential = true; // Necesario para funcionar incluso si el usuario rechaza cookies no esenciales
});

// 2. Construcción de la Aplicación
var app = builder.Build();

// 3. Configuración del Middleware (Pipeline de Solicitudes)
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error"); // Manejo de excepciones en producción
    app.UseHsts(); // Habilita HSTS para mayor seguridad
}

app.UseHttpsRedirection(); // Redirige automáticamente HTTP a HTTPS
app.UseStaticFiles(); // Habilita archivos estáticos en wwwroot

app.UseRouting(); // Habilita el enrutamiento de controladores
app.UseSession(); // Habilita el middleware de sesiones
app.UseAuthorization(); // Manejo de autorización si aplicas políticas

// Configura el enrutamiento principal (Controlador y Acción predeterminados)
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Redirige al Login como vista predeterminada

//PRUEBAS
//app.MapControllerRoute(
//    name: "default",
//    pattern: "{controller=Pedidos}/{action=NuevoPedido}/{id?}"); 

// Ejecuta la aplicación
app.Run();
