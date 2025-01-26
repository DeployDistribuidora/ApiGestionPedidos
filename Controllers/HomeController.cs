using System.Net.Http;
using System.Text.Json;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;

public class HomeController : Controller
{
    private readonly HttpClient _httpClient;

    public HomeController(IHttpClientFactory httpClientFactory)
    {
        _httpClient = httpClientFactory.CreateClient("PedidosClient");
    }

    public async Task<IActionResult> Index()
    {
        try
        {
            // Obtener datos de sesión
            var userRole = HttpContext.Session.GetString("Role");
            var userId = HttpContext.Session.GetString("UsuarioLogueado") ?? "Invitado";
            var messages = new List<AlertMessage>();

            // Generar mensajes según el rol
            await GenerarMensajesPorRol(userRole, messages);

            // Crear el modelo de vista
            var model = new HomeViewModel
            {
                Username = userId,
                UserRole = userRole ?? "Sin Rol",
                Messages = messages
            };

            return View(model);
        }
        catch (Exception ex)
        {
            // En caso de error crítico, muestra un mensaje global
            var model = new HomeViewModel
            {
                Username = "Invitado",
                UserRole = "Sin Rol",
                Messages = new List<AlertMessage>
                {
                    new AlertMessage
                    {
                        Type = "danger",
                        Icon = "bi-exclamation-octagon",
                        Content = $"Error inesperado: {ex.Message}"
                    }
                }
            };

            return View(model);
        }
    }

    private async Task GenerarMensajesPorRol(string userRole, List<AlertMessage> messages)
    {
        if (string.IsNullOrEmpty(userRole))
        {
            messages.Add(new AlertMessage
            {
                Type = "danger",
                Icon = "bi-exclamation-octagon",
                Content = "Rol no reconocido o no asignado."
            });
            return;
        }

        switch (userRole)
        {
            case "Cliente":
                var clienteIdString = HttpContext.Session.GetString("ClienteId");
                if (int.TryParse(clienteIdString, out var clienteId))
                {
                    await GenerarMensajesDePedidos($"Pedidos/Cliente/{clienteId}", messages);
                }
                break;

            case "Vendedor":
                var vendedorIdString = HttpContext.Session.GetString("VendedorId");
                if (int.TryParse(vendedorIdString, out var vendedorId))
                {
                    await GenerarMensajesDePedidos($"Pedidos/Vendedor/{vendedorId}", messages);
                }
                break;

            case "Administracion":
                await GenerarMensajesDePedidos("Pedidos", messages, estadosPermitidos: new[] { "Pendiente", "En viaje" });
                break;

            case "Supervisor de Carga":
                await GenerarMensajesDePedidos("Pedidos", messages, estadosPermitidos: new[] { "Preparando" });
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

    private async Task GenerarMensajesDePedidos(string endpoint, List<AlertMessage> messages, string[] estadosPermitidos = null)
    {
        try
        {
            var pedidos = await ObtenerPedidos(endpoint);

            if (estadosPermitidos != null)
            {
                pedidos = pedidos.Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase)).ToList();
            }

            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "dark", "bi-hourglass-split", "Cantidad de pedidos pendientes: {0}");
            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "success", "bi-box", "Cantidad de pedidos en preparación: {0}");
            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de pedidos en viaje: {0}");
        }
        catch (Exception ex)
        {
            messages.Add(new AlertMessage
            {
                Type = "danger",
                Icon = "bi-exclamation-octagon",
                Content = $"Error al obtener los pedidos: {ex.Message}"
            });
        }
    }

    private async Task<List<Pedido>> ObtenerPedidos(string endpoint)
    {
        var response = await _httpClient.GetAsync(endpoint);

        if (!response.IsSuccessStatusCode)
        {
            throw new Exception($"Error en la solicitud al endpoint '{endpoint}': {response.ReasonPhrase}");
        }

        var jsonResponse = await response.Content.ReadAsStringAsync();
        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        }) ?? new List<Pedido>();
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
}


//using System.Net.Http;
//using System.Text.Json;
//using Front_End_Gestion_Pedidos.Models;
//using Front_End_Gestion_Pedidos.Models.ViewModel;
//using Microsoft.AspNetCore.Mvc;

//public class HomeController : Controller
//{
//    private readonly HttpClient _httpClient;

//    public HomeController(IHttpClientFactory httpClientFactory)
//    {
//        _httpClient = httpClientFactory.CreateClient("PedidosClient");
//    }

//    public async Task<IActionResult> Index()
//    {
//        var userRole = HttpContext.Session.GetString("Role");
//        var userIdString = HttpContext.Session.GetString("UsuarioLogueado");
//        var messages = new List<AlertMessage>();

//        if (!string.IsNullOrEmpty(userRole))
//        {
//            switch (userRole)
//            {
//                case "Cliente":
//                    await HandleClienteMessages(messages);
//                    break;

//                case "Vendedor":
//                    await HandleVendedorMessages(messages);
//                    break;

//                case "Administracion":
//                    await HandleAdministracionMessages(messages);
//                    break;

//                case "Supervisor de Carga":
//                    await HandleSupervisorMessages(messages);
//                    break;

//                default:
//                    messages.Add(new AlertMessage
//                    {
//                        Type = "danger",
//                        Icon = "bi-exclamation-octagon",
//                        Content = "Rol no reconocido."
//                    });
//                    break;
//            }
//        }

//        var model = new HomeViewModel
//        {
//            Username = userIdString ?? "Invitado",
//            UserRole = userRole ?? "Sin Rol",
//            Messages = messages
//        };

//        return View(model);
//    }

//    private async Task HandleClienteMessages(List<AlertMessage> messages)
//    {
//        var clienteIdString = HttpContext.Session.GetString("ClienteId");
//        if (!string.IsNullOrEmpty(clienteIdString) && int.TryParse(clienteIdString, out var clienteId))
//        {
//            var pedidos = await ObtenerPedidosCliente(clienteId);

//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "dark", "bi-hourglass-split", "Cantidad de sus pedidos pendientes: {0}");
//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "success", "bi-box", "Cantidad de sus pedidos en preparación: {0}");
//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de sus pedidos en viaje: {0}");

//        }
//    }

//    private async Task HandleVendedorMessages(List<AlertMessage> messages)
//    {
//        var vendedorIdString = HttpContext.Session.GetString("VendedorId");
//        if (!string.IsNullOrEmpty(vendedorIdString) && int.TryParse(vendedorIdString, out var vendedorId))
//        {
//            var pedidos = await ObtenerPedidosVendedor(vendedorId);

//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "dark", "bi-hourglass-split", "Cantidad de sus pedidos pendientes: {0}");
//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "success", "bi-box", "Cantidad de sus pedidos en preparación: {0}");
//            AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de sus pedidos en viaje: {0}");
//        }
//    }


//    private async Task HandleAdministracionMessages(List<AlertMessage> messages)
//    {
//        var pedidos = await ObtenerPedidos();

//        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Pendiente"), "warning", "bi-exclamation-octagon", "Cantidad de pedidos pendientes: {0}");
//        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "En viaje"), "info", "bi-truck", "Cantidad de pedidos en viaje: {0}");
//    }

//    private async Task HandleSupervisorMessages(List<AlertMessage> messages)
//    {
//        var pedidos = await ObtenerPedidos();

//        AddMessageIfNotZero(messages, pedidos.Count(p => p.Estado == "Preparando"), "warning", "bi-box", "Cantidad de pedidos aprobados para preparar: {0}");
//    }

//    private void AddMessageIfNotZero(List<AlertMessage> messages, int count, string type, string icon, string template)
//    {
//        if (count > 0)
//        {
//            messages.Add(new AlertMessage
//            {
//                Type = type,
//                Icon = icon,
//                Content = string.Format(template, count)
//            });
//        }
//    }


//    private async Task<List<Pedido>> ObtenerPedidos()
//    {
//       var response = await _httpClient.GetAsync($"/api/Pedidos");

//        if (!response.IsSuccessStatusCode)
//            return new List<Pedido>();

//        var jsonResponse = await response.Content.ReadAsStringAsync();
//        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
//        {
//            PropertyNameCaseInsensitive = true
//        }) ?? new List<Pedido>();
//    }

//    private async Task<List<Pedido>> ObtenerPedidosCliente(int clienteId)
//    {
//       var response = await _httpClient.GetAsync($"/api/Pedidos/Cliente/{clienteId}");

//        if (!response.IsSuccessStatusCode)
//            return new List<Pedido>();

//        var jsonResponse = await response.Content.ReadAsStringAsync();
//        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
//        {
//            PropertyNameCaseInsensitive = true
//        }) ?? new List<Pedido>();
//    }

//    private async Task<List<Pedido>> ObtenerPedidosVendedor(int vendedorId)
//    {
//       var response = await _httpClient.GetAsync($"/api/Pedidos/Vendedor/{vendedorId}");

//        if (!response.IsSuccessStatusCode)
//        {
//            return new List<Pedido>();
//        }

//        var jsonResponse = await response.Content.ReadAsStringAsync();
//        return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
//        {
//            PropertyNameCaseInsensitive = true
//        }) ?? new List<Pedido>();
//    }


//}

