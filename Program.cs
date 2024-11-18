var builder = WebApplication.CreateBuilder(args);

// Agrega servicios al contenedor.
builder.Services.AddControllersWithViews();
builder.Services.AddHttpClient(); // Habilita IHttpClientFactory para el uso de HttpClient
builder.Services.AddSession(); // Habilita el manejo de sesiones

var app = builder.Build();

// Configura el pipeline de solicitudes.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts(); // Habilita HSTS en entornos no de desarrollo
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// Habilita el middleware de sesiones
app.UseSession();

app.UseAuthorization();

// Redirige la raíz al Login
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}"); // Establece el controlador y acción predeterminados al Login

app.Run();
