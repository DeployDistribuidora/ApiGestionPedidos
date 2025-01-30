using Front_End_Gestion_Pedidos.Handlers;
using Front_End_Gestion_Pedidos.Helpers;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddTransient<AuthenticatedHttpClientHandler>();
// Configurar servicios
builder.Services.AddControllersWithViews(options =>
{
    options.Filters.Add<Front_End_Gestion_Pedidos.Filters.SessionAuthorizeFilter>(); // Filtro global
});

builder.Services.AddHttpClient("NoAuthClient", client =>
{
    client.BaseAddress = new Uri("https://gestionpedidosapi-dfcbf9hrchbthraz.brazilsouth-01.azurewebsites.net/api/v1/");
   
});

builder.Services.AddHttpClient("PedidosClient", client =>
{
    client.BaseAddress = new Uri("https://gestionpedidosapi-dfcbf9hrchbthraz.brazilsouth-01.azurewebsites.net/api/v1/");
   
}).AddHttpMessageHandler<AuthenticatedHttpClientHandler>();

builder.Services.AddHttpContextAccessor();
builder.Services.AddDistributedMemoryCache();
builder.Services.AddScoped<SessionHelper>();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Configuración del middleware
var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Login}/{id?}");

app.Run();
