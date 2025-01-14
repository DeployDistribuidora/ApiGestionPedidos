using System.Text.Json;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly IHttpClientFactory _httpClientFactory;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    public async Task<IActionResult> Index()
    {
        var userRole = HttpContext.Session.GetString("Role");
        var userIdString = HttpContext.Session.GetString("UsuarioLogueado");
        var messages = new List<AlertMessage>();

        if (!string.IsNullOrEmpty(userRole))
        {
            switch (userRole)
            {
                case "Cliente":
                    await HandleClienteMessages(messages);
                    break;

                case "Vendedor":
                    await HandleVendedorMessages(messages);
                    break;

                case "Administracion":
                    await HandleAdministracionMessages(messages);
                    break;

                case "Supervisor de Carga":
                    await HandleSupervisorMessages(messages);
                    break;

                default:
                    messages.Add(new AlertMessage
                    {
                        Type = "danger",
                        Icon = "bi-exclamation-octagon",
                        Content = "Rol no reconocido."
                    });
                    break;
            }
        }

        var model = new HomeViewModel
        {
            Username = userIdString ?? "Invitado",
            UserRole = userRole ?? "Sin Rol",
            Messages = messages
        };

        return View(model);
    }

    private async Task HandleClienteMessages(List<AlertMessage> messages)
    {
        var clienteIdString = HttpContext.Session.GetString("ClienteId");
        if (!string.IsNullOrEmpty(clienteIdString) && int.TryParse(clienteIdString, out var clienteId))
        {
            var pedidos = await ObtenerPedidosCliente(clienteId);

            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "dark", "bi-hourglass-split", "Cantidad de sus pedidos pendientes: {0}");
            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de sus pedidos en viaje: {0}");
            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "success", "bi-box", "Cantidad de sus pedidos en preparación: {0}");
        }
    }

    private async Task HandleVendedorMessages(List<AlertMessage> messages)
    {
        var vendedorIdString = HttpContext.Session.GetString("VendedorId");
        if (!string.IsNullOrEmpty(vendedorIdString) && int.TryParse(vendedorIdString, out var vendedorId))
        {
            var pedidos = await ObtenerPedidosVendedor(vendedorId);

            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "dark", "bi-hourglass-split", "Cantidad de sus pedidos pendientes: {0}");
            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "success", "bi-box", "Cantidad de sus pedidos en preparación: {0}");
        }
    }


    private async Task HandleAdministracionMessages(List<AlertMessage> messages)
    {
        var pedidos = await ObtenerPedidos();

        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "warning", "bi-exclamation-octagon", "Cantidad de pedidos pendientes: {0}");
        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de pedidos en viaje: {0}");
    }

    private async Task HandleSupervisorMessages(List<AlertMessage> messages)
    {
        var pedidos = await ObtenerPedidos();

        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "warning", "bi-box", "Cantidad de pedidos aprobados para preparar: {0}");
    }

    private void AddMessageIfNotZero(List<AlertMessage> messages, int count, string type, string icon, string template)
    {
        if (count > 0)
        {
            messages.Add(new AlertMessage
            {
                Type = type,
                Icon = icon,
                Content = string.Format(template, count)
            });
        }
    }


    private async Task<List<Pedido>> ObtenerPedidos()
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync("https://localhost:7078/api/Pedidos");

        if (!response.IsSuccessStatusCode)
            return new List<Pedido>();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Pedido>();
    }

    private async Task<List<Pedido>> ObtenerPedidosCliente(int clienteId)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/Cliente/{clienteId}");

        if (!response.IsSuccessStatusCode)
            return new List<Pedido>();

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Pedido>();
    }

    private async Task<List<Pedido>> ObtenerPedidosVendedor(int vendedorId)
    {
        var client = _httpClientFactory.CreateClient();
        var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/Vendedor/{vendedorId}");

        if (!response.IsSuccessStatusCode)
        {
            return new List<Pedido>();
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Pedido>();
    }


}



//using Front_End_Gestion_Pedidos.Models;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Http;
//using System.Diagnostics;
//using Front_End_Gestion_Pedidos.Models.ViewModel;
//using System.Text.Json;

//namespace Front_End_Gestion_Pedidos.Controllers
//{
//    public class HomeController : Controller
//    {


//        private readonly ILogger<HomeController> _logger;

//        public HomeController(ILogger<HomeController> logger)
//        {
//            _logger = logger;
//        }

//        public IActionResult Index()
//        {
//            // Verifica si hay un token en la sesión
//            var token = HttpContext.Session.GetString("Token");
//            if (string.IsNullOrEmpty(token))
//            {
//                // Si no hay token, redirige al Login
//                return RedirectToAction("Login", "Account");
//            }

//            // Configura datos de sesión en ViewBag
//            ViewBag.IsLoggedIn = true; // Indica que el usuario está logueado
//            ViewBag.UserRole = HttpContext.Session.GetString("Role") ?? "Sin Rol";
//            ViewBag.Username = HttpContext.Session.GetString("UsuarioLogueado") ?? "Invitado";

//            return View();
//        }


//        //public IActionResult Privacy()
//        //{
//        //    // La página Privacy no requiere autenticación
//        //    return View();
//        //}

//        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
//        public IActionResult Error()
//        {
//            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
//        }
//    }
//}
