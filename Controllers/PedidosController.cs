using Front_End_Gestion_Pedidos.Models;
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

        public async Task<IActionResult> NuevoPedido(Cliente clienteSeleccionado, List<Producto> filtro)
        {
            var model = new PedidoViewModel
            {
                Clientes = new List<Cliente>(),    // Inicializa la lista de clientes
                Productos = new List<Producto>(),  // Inicializa la lista de productos
                Direcciones = new List<Direccion>(),
                ClienteSeleccionado = clienteSeleccionado
            };

            try
            {
                // Consumir la API para obtener clientes
                //var client = _httpClientFactory.CreateClient();
                //var response = await client.GetAsync("var url = "https://localhost:7078/api/Login/login";");

                //if (response.IsSuccessStatusCode)
                //{
                //    var jsonResponse = await response.Content.ReadAsStringAsync();
                //    var clientes = JsonSerializer.Deserialize<List<Cliente>>(jsonResponse, new JsonSerializerOptions
                //    {
                //        PropertyNameCaseInsensitive = true
                //    });

                //    model.Clientes = clientes ?? new List<Cliente>();
                //}
                //else
                //{
                //    // Manejar errores en la solicitud a la API
                //    ModelState.AddModelError(string.Empty, "Error al obtener los clientes.");
                //    model.Clientes = new List<Cliente>();


                var clientes = await ObtenerClientes();
                if (clientes != null)
                {
                    model.Clientes = clientes.ToList();
                }
                else
                {
                    model.Clientes = new List<Cliente>();
                }

                var productos = await ObtenerStock();

                if (filtro.Count() > 0)
                {
                    productos = filtro.ToList();
                }
                // Llamar al método para obtener stock


                if (productos != null)
                {
                    model.Productos = productos.ToList();
                }
                else
                {
                    model.Productos = new List<Producto>();
                }

            }
            catch (Exception ex)
            {
                // Manejar excepciones
                ModelState.AddModelError(string.Empty, $"Error inesperado: {ex.Message}");
                model.Clientes = new List<Cliente>();
            }

            return View(model);
        }

        private async Task<IEnumerable<Cliente>> ObtenerClientes()
        {
            var model = new PedidoViewModel();
            List<Cliente> clientes1 = new List<Cliente>();

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/v1/Clientes");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                model.Clientes = clientes ?? new List<Cliente>();
                clientes1 = model.Clientes;
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener los clientes.");
                return null;
            }

            return clientes1;
        }

        private async Task<IEnumerable<Producto>> ObtenerStock()
        {
            List<Producto> productos = new List<Producto>();

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/Stocks");

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

            return productos;
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


        [HttpPost]
        public async Task<IActionResult> ClienteSeleccionadoAsync(long clienteId)
        {
            Cliente? cliente = new Cliente();
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/v1/Clientes/" + clienteId);

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                cliente = JsonSerializer.Deserialize<Cliente>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener el stock.");
            }


            // Realiza cualquier lógica adicional aquí
            return RedirectToAction("NuevoPedido", cliente); // Vuelve a la vista de nuevo pedido
        }

        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            // Lógica para aprobar el pedido
            TempData["Mensaje"] = $"Pedido #{id} aprobado exitosamente.";
            return RedirectToAction("BuscarPedidos");
        }

        [HttpPost]
        public IActionResult Cancelar(int id)
        {
            // Lógica para cancelar el pedido
            TempData["Mensaje"] = $"Pedido #{id} cancelado.";
            return RedirectToAction("BuscarPedidos");
        }


    }
}