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

        private async Task<List<DatosContacto>> ObtenerDatosContactoPorClienteId(long clienteId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/v1/Clientes/{clienteId}/DatosContacto");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<DatosContacto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<DatosContacto>();
            }

            return new List<DatosContacto>(); // Devuelve lista vacía en caso de error
        }
        [HttpPost]
        public async Task<IActionResult> CargarDatosContacto(long clienteId)
        {
            // Obtener datos contacto del cliente
            var datosContacto = await ObtenerDatosContactoPorClienteId(clienteId);

            // Obtener los clientes y productos nuevamente para reconstruir el modelo
            var clientes = await ObtenerClientes();
            var productos = await ObtenerStock();

            // Si no hay datos contacto, mostrar mensaje de error en la vista
            if (datosContacto == null || !datosContacto.Any())
            {
                var modelConError = new PedidoViewModel
                {
                    Clientes = clientes,
                    Productos = productos,
                    ClienteSeleccionado = clientes.FirstOrDefault(c => c.NroCliente == clienteId),
                    MensajeError = "No hay datos contacto disponibles para este cliente."
                };

                return View("NuevoPedido", modelConError);
            }

            // Construir el modelo con los datos contacto cargados
            var modelConDatos = new PedidoViewModel
            {
                Clientes = clientes,
                Productos = productos,
                ClienteSeleccionado = clientes.FirstOrDefault(c => c.NroCliente == clienteId),
                ContactosCliente = datosContacto
            };

            return View("NuevoPedido", modelConDatos);
        }


        [HttpPost]
        public async Task<IActionResult> GuardarDireccionSeleccionada(long clienteId, string direccionSeleccionada)
        {
            var cliente = await ObtenerClientePorId(clienteId);
            if (cliente == null)
            {
                ModelState.AddModelError("", "El cliente no fue encontrado.");
                return RedirectToAction("NuevoPedido");
            }

            // Actualizar la dirección seleccionada
            cliente.DirCliente = direccionSeleccionada;

            // Reconstruir el modelo principal con el cliente actualizado
            var model = new PedidoViewModel
            {
                Clientes = await ObtenerClientes(),
                Productos = await ObtenerStock(),
                ClienteSeleccionado = cliente
            };

            return View("NuevoPedido", model);
        }


        public async Task<IActionResult> BuscarPedidos()
        {
            IEnumerable<Pedido> pedidos = new List<Pedido>();

            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Llamar al método asincrónico para obtener los pedidos
            pedidos = await ObtenerPedidos();

            return View(pedidos);
        }

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
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError("", "Error al obtener los clientes.");
                return new List<Cliente>(); // Devuelve lista vacía en caso de error
            }
        }

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
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError("", "Error al obtener el stock.");
                return new List<Producto>(); // Devuelve lista vacía en caso de error
            }
        }

        [HttpPost]
        private async Task<IEnumerable<Producto>> filtroStock(string filtro)
        {
            List<Producto> productos = new List<Producto>();

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/Stocks" + filtro);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                productos = JsonSerializer.Deserialize<List<Producto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Producto>();
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener el stock.");
            }

            return (IEnumerable<Producto>)RedirectToAction("NuevoPedido", productos);
        }

        private async Task<Cliente> ObtenerClientePorId(long clienteId)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/v1/Clientes/{clienteId}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var cliente = JsonSerializer.Deserialize<Cliente>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Validar que el cliente tenga datos contacto
                if (cliente != null && cliente.ContactosCliente == null)
                {
                    cliente.ContactosCliente = new List<DatosContacto>(); // Inicializar si está vacío
                }

                return cliente;
            }

            return null;
        }
        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            // Lógica para aprobar el pedido
            TempData["Mensaje"] = $"Pedido #{id} aprobado exitosamente.";
            return RedirectToAction("SupervisarPedidos");
        }

        [HttpPost]
        public IActionResult Cancelar(int id)
        {
            // Lógica para cancelar el pedido
            TempData["Mensaje"] = $"Pedido #{id} cancelado.";
            return RedirectToAction("SupervisarPedidos");
        }
        [HttpPost]
        public IActionResult AgregarComentario(int id, string comentario)
        {
            if (string.IsNullOrEmpty(comentario))
            {
                TempData["Error"] = "El comentario no puede estar vacío.";
                return RedirectToAction("SupervisarPedidos");
            }

            // Aquí iría la lógica para guardar el comentario, como una llamada a la API
            TempData["Mensaje"] = $"Comentario agregado al Pedido #{id}.";
            return RedirectToAction("SupervisarPedidos");
        }

        public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "")
        {
            var pedidos = await ObtenerPedidos();

            // Filtrar por estado Pendiente
            pedidos = pedidos.Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).ToList();

            // Aplicar filtros adicionales si se proporcionan
            if (!string.IsNullOrEmpty(cliente))
            {
                pedidos = pedidos.Where(p => p.IdCliente.ToString().Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            if (!string.IsNullOrEmpty(vendedor))
            {
                pedidos = pedidos.Where(p => p.IdVendedor.ToString().Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Cliente = cliente,
                Vendedor = vendedor,
                Estado = "Pendiente" // Fijamos el estado aquí
            };

            return View(viewModel); // Usamos una vista diferente para SupervisarPedidos
        }



        public async Task<IActionResult> DetallesPedido(int id)
        {
            // Obtener el pedido por ID desde la API
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

            // Obtener los productos asociados a las líneas del pedido
            var productos = new List<Producto>();

            if (pedido.LineasPedido != null)
            {
                foreach (var linea in pedido.LineasPedido)
                {
                    // Buscar producto por código usando filtroStock
                    var productoResultado = await filtroStock($"?codigo={linea.Codigo}");
                    productos.AddRange(productoResultado);
                }
            }

            // Preparar el modelo para el PartialView
            var detallesViewModel = new PedidoDetalleViewModel
            {
                Pedido = pedido,
                Productos = productos
            };

            return PartialView("_DetallePedido", detallesViewModel);
        }
        private async Task<IEnumerable<Pedido>> ObtenerPedidos()
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/Pedidos");

            if (!response.IsSuccessStatusCode)
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener los pedidos.");
                return new List<Pedido>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            return pedidos ?? new List<Pedido>();
        }



        //[HttpPost]
        //public IActionResult RealizarPedido([FromBody] List<LineaPedido> lineasPedido)
        //{


        //    if (lineasPedido == null || !lineasPedido.Any())
        //    {
        //        return BadRequest("No se seleccionaron productos.");
        //    }

        //    // Lógica para procesar el pedido aquí
        //    foreach (var linea in lineasPedido)
        //    {
        //        Console.WriteLine($"Producto: {linea.Codigo}, Cantidad: {linea.Cantidad}");
        //    }

        //    return Ok("Pedido realizado con éxito.");
        //}
    }
}