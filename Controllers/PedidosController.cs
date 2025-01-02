﻿using Front_End_Gestion_Pedidos.Models;
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

        // POST: Realizar Pedido
        /*
         [HttpPost]
         public async Task<IActionResult> RealizarPedido(PedidoDetalleViewModel model)
         {
             if (model == null || model.ClienteSeleccionado == null || model.Pedido == null || !model.Pedido.LineasPedido.Any())
             {
                 ModelState.AddModelError("", "Faltan datos para realizar el pedido.");
                 return RedirectToAction("NuevoPedido");
             }

             // Crear un objeto Pedido con los datos proporcionados
             var pedido = new Pedido
             {
                 IdCliente = model.Pedido.IdCliente,//ClienteSeleccionado?
                 MetodoPago = model.Pedido.MetodoPago,
                 Comentarios = model.Pedido.Comentarios,
                 Estado = "Pendiente", // Estado inicial
                 FechaCreacion = DateTime.Now,
                // Total = model.Productos.Where(p => p. > 0).Sum(p => p.Cantidad * (decimal)p.Precio),
                /* LineasPedido = model.Productos.Where(p => p.Cantidad > 0)
                     .Select(p => new LineaPedido
                     {
                         Codigo = p.Codigo,
                         Cantidad = p.Cantidad,
                         PrecioUnitario = (decimal)p.Precio
                     }).ToList()
    };

             // Enviar el pedido a la API
             var client = _httpClientFactory.CreateClient();
             var response = await client.PostAsJsonAsync("https://localhost:7078/api/Pedidos", pedido);

             if (response.IsSuccessStatusCode)
             {
                 TempData["Mensaje"] = "Pedido realizado con éxito.";
                 return RedirectToAction("NuevoPedido");
             }
             else
             {
                 TempData["Error"] = "Error al realizar el pedido.";
                 return RedirectToAction("NuevoPedido");
             }
         }*/

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

        // Obtiene los datos de contacto de un cliente específico
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

            return new List<DatosContacto>();
        }

        // Acción que carga los datos de contacto seleccionados en la vista de nuevo pedido
        [HttpPost]
        public async Task<IActionResult> CargarDatosContacto(long clienteId)
        {
            var datosContacto = await ObtenerDatosContactoPorClienteId(clienteId);
            var clientes = await ObtenerClientes();
            var productos = await ObtenerStock();

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

            var modelConDatos = new PedidoViewModel
            {
                Clientes = clientes,
                Productos = productos,
                ClienteSeleccionado = clientes.FirstOrDefault(c => c.NroCliente == clienteId),
                ContactosCliente = datosContacto
            };

            return View("NuevoPedido", modelConDatos);
        }

        // Acción para guardar la dirección seleccionada del cliente
        [HttpPost]
        public async Task<IActionResult> GuardarDireccionSeleccionada(long clienteId, string direccionSeleccionada)
        {
            var cliente = await ObtenerClientePorId(clienteId);
            if (cliente == null)
            {
                ModelState.AddModelError("", "El cliente no fue encontrado.");
                return RedirectToAction("NuevoPedido");
            }

            cliente.DirCliente = direccionSeleccionada;

            var model = new PedidoViewModel
            {
                Clientes = await ObtenerClientes(),
                Productos = await ObtenerStock(),
                ClienteSeleccionado = cliente
            };

            return View("NuevoPedido", model);
        }

        // --------------------------------------------------------------------------------------
        // SUPERVISAR PEDIDOS (Vista: SupervisarPedidos)
        // --------------------------------------------------------------------------------------

        // Vista principal para supervisar pedidos pendientes
        public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "")
        {
            var pedidos = await ObtenerPedidos();

            pedidos = pedidos.Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase)).ToList();

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
                Estado = "Pendiente"
            };

            return View(viewModel);
        }

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

        // Acción para aprobar un pedido
        [HttpPost]
        public IActionResult Aprobar(int id)
        {
            TempData["Mensaje"] = $"Pedido #{id} aprobado exitosamente.";
            return RedirectToAction("SupervisarPedidos");
        }

        // Acción para cancelar un pedido
        [HttpPost]
        public IActionResult Cancelar(int id)
        {
            TempData["Mensaje"] = $"Pedido #{id} cancelado.";
            return RedirectToAction("SupervisarPedidos");
        }

        // --------------------------------------------------------------------------------------
        // HISTORIAL DE PEDIDOS (Vista: BuscarPedidos)
        // --------------------------------------------------------------------------------------

        // Vista principal para mostrar el historial de pedidos
        public async Task<IActionResult> BuscarPedidos()
        {
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            var pedidos = await ObtenerPedidos();

            return View(pedidos);
        }

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
