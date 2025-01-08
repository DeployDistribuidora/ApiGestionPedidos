using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Net.Http;
using System.Reflection;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public PedidosController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }

        // --------------------------------------------------------------------------------------
        // NUEVO PEDIDO (Vista: NuevoPedido)
        // --------------------------------------------------------------------------------------

        // Vista principal para crear un nuevo pedido
        public async Task<IActionResult> NuevoPedido()
        {
            var clientes = await ObtenerClientes();
            var model = new PedidoViewModel
            {
                Clientes = clientes,
                Productos = await ObtenerStock(),
                ClienteSeleccionado = null
            };

            return View(model);
        }

        // --------------------------------------------------------------------------------------
        // SUPERVISAR PEDIDOS (Vista: SupervisarPedidos)
        // --------------------------------------------------------------------------------------

        public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "", string estado = "")
        {
            // Recuperar rol del usuario desde la sesión
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            if (string.IsNullOrEmpty(rolUsuario))
            {
                return RedirectToAction("Login", "Account");
            }

            // Obtener todos los pedidos desde la API
            var pedidos = await ObtenerPedidos();

            // Filtrar pedidos por rol y estado
            if (rolUsuario == "Administracion")
            {
                if (string.IsNullOrEmpty(estado)) estado = "Pendiente";
                pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (rolUsuario == "Supervisor de Carga")
            {
                estado = "Preparando"; // Supervisores solo ven pedidos "Preparando"
                pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Aplicar filtros opcionales de cliente y vendedor
            if (!string.IsNullOrEmpty(cliente))
            {
                pedidos = pedidos.Where(p => p.IdCliente.ToString().Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(vendedor))
            {
                pedidos = pedidos.Where(p => p.IdVendedor.ToString().Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Preparar el modelo para la vista
            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Cliente = cliente,
                Vendedor = vendedor,
                Estado = estado
            };

            return View(viewModel);
        }

        //public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "")
        //{
        //    var pedidos = await ObtenerPedidos();

        //    pedidos = pedidos.Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).ToList();

        //    if (!string.IsNullOrEmpty(cliente))
        //    {
        //        pedidos = pedidos.Where(p => p.IdCliente.ToString().Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
        //    }

        //    if (!string.IsNullOrEmpty(vendedor))
        //    {
        //        pedidos = pedidos.Where(p => p.IdVendedor.ToString().Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();
        //    }

        //    var viewModel = new SupervisarPedidosViewModel
        //    {
        //        Pedidos = pedidos,
        //        Cliente = cliente,
        //        Vendedor = vendedor,
        //        Estado = "Pendiente"
        //    };

        //    return View(viewModel);
        //}

        // Detalles del pedido en un modal (usado en la vista SupervisarPedidos)
        /* public async Task<IActionResult> DetallesPedido(int id)
         {
             var client = _httpClientFactory.CreateClient();
             var responsePedido = await client.GetAsync($"https://localhost:7078/api/Pedidos/Pedido/{id}");

             if (!responsePedido.IsSuccessStatusCode)
             {
                 TempData["Error"] = "Error al cargar los detalles del pedido.";
                 return RedirectToAction("SupervisarPedidos");
             }

             var jsonPedido = await responsePedido.Content.ReadAsStringAsync();
             var pedido = JsonSerializer.Deserialize<Pedido>(jsonPedido, new JsonSerializerOptions
             {
                 PropertyNameCaseInsensitive = true
             });

             var productos = new List<Producto>();

             if (pedido.LineasPedido != null)
             {
                 foreach (var linea in pedido.LineasPedido)
                 {
                     var productoResultado = await filtroStock($"?codigo={linea.Codigo}");
                     productos.AddRange(productoResultado);
                 }
             }

             var detallesViewModel = new PedidoDetalleViewModel
             {
                 Pedido = pedido,
                 Productos = productos
             };

             return PartialView("_DetallePedido", detallesViewModel);
         }*/



        // Acción para cambiar el estado de un pedido
        [HttpPost]
        public async Task<IActionResult> CambiarEstadoPedido(int id, string nuevoEstado)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Realiza una solicitud PUT al backend API
                var response = await client.PutAsJsonAsync($"https://localhost:7078/api/Pedidos/Estado/{id}", nuevoEstado);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Mensaje"] = $"El estado del pedido #{id} se actualizó correctamente a '{nuevoEstado}'.";
                    TempData["Exito"] = true; // Indicador de éxito
                }
                else
                {
                    TempData["Mensaje"] = $"Error al actualizar el estado del pedido #{id}: {response.ReasonPhrase}.";
                    TempData["Exito"] = false; // Indicador de error
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error al comunicarse con el backend: {ex.Message}";
                TempData["Exito"] = false;
            }

            return RedirectToAction("SupervisarPedidos");
        }



        // --------------------------------------------------------------------------------------
        // HISTORIAL DE PEDIDOS (Vista: BuscarPedidos)
        // --------------------------------------------------------------------------------------

        // Vista principal para mostrar el historial de pedidos
        public async Task<IActionResult> BuscarPedidos(string cliente, string vendedor, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Validar sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Obtener todos los pedidos
            var pedidos = await ObtenerPedidos();

            // Aplicar filtros si se proporcionan
            if (!string.IsNullOrEmpty(cliente))
            {
                pedidos = pedidos.Where(p => p.IdCliente != null && p.IdCliente.ToString().Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

            }

            if (!string.IsNullOrEmpty(vendedor))
            {
                pedidos = pedidos.Where(p => p.IdVendedor.HasValue && p.IdVendedor.Value.ToString().Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date >= fechaInicio.Value.Date && p.FechaCreacion.Date <= fechaFin.Value.Date).ToList();
            }
            else if (fechaInicio.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date >= fechaInicio.Value.Date).ToList();
            }
            else if (fechaFin.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date <= fechaFin.Value.Date).ToList();
            }

            return View(pedidos);
        }


        //public async Task<IActionResult> BuscarPedidos()
        //{
        //    var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
        //    var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

        //    if (string.IsNullOrEmpty(usuarioLogueado))
        //        return RedirectToAction("Login", "Account");

        //    var pedidos = await ObtenerPedidos();

        //    return View(pedidos);
        //}

        // --------------------------------------------------------------------------------------
        // MÉTODOS AUXILIARES
        // --------------------------------------------------------------------------------------

        // Obtener todos los pedidos
        private async Task<IEnumerable<Pedido>> ObtenerPedidos()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/Pedidos");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, "Error al obtener los pedidos.");
                return new List<Pedido>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Pedido>();
        }

        // Obtener la lista de clientes
        private async Task<List<Cliente>> ObtenerClientes()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/v1/Clientes");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Cliente>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Cliente>();
            }

            ModelState.AddModelError("", "Error al obtener los clientes.");
            return new List<Cliente>();
        }

        // Obtener la lista de productos
        private async Task<List<Producto>> ObtenerStock()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/Stocks");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Producto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Producto>();
            }

            ModelState.AddModelError("", "Error al obtener el stock.");
            return new List<Producto>();
        }

        // Obtener un cliente por ID
        private async Task<Cliente> ObtenerClientePorId(long clienteId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/v1/Clientes/{clienteId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<Cliente>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }

            return null;
        }

        // Filtrar stock por código
        private async Task<IEnumerable<Producto>> filtroStock(string filtro)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/Stocks{filtro}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Producto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Producto>();
            }

            return new List<Producto>();
        }

    }
}
