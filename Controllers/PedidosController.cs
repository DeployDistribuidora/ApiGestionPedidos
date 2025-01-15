using Front_End_Gestion_Pedidos.Filters;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
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
        [RoleAuthorize("Administracion", "Vendedor", "Cliente")]
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
        [RoleAuthorize("Administracion", "Supervisor de Carga")]
        public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "", string estado = "", int? idPedido = null)
        {
            var pedidos = await ObtenerPedidos();

            // Filtrar pedidos
            if (!string.IsNullOrEmpty(cliente))
                pedidos = pedidos.Where(p => p.IdCliente.ToString().Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(vendedor))
                pedidos = pedidos.Where(p => p.IdVendedor.ToString().Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();

            if (!string.IsNullOrEmpty(estado))
                pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();

            // Cargar detalle de líneas de pedido si idPedido está presente
            List<LineaPedido> detallePedido = null;
            if (idPedido.HasValue)
            {
                var client = _httpClientFactory.CreateClient();
                var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/{idPedido}/lineas");

                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    detallePedido = JsonSerializer.Deserialize<List<LineaPedido>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });
                }
            }

            // Preparar el modelo
            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Cliente = cliente,
                Vendedor = vendedor,
                Estado = estado,
                DetallePedido = detallePedido
            };

            return View(viewModel);
        }


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
        [RoleAuthorize("Administracion", "Supervisor de Carga", "Vendedor", "Cliente")]
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


        // --------------------------------------------------------------------------------------
        // MÉTRICAS (Vista: Metricas)
        // --------------------------------------------------------------------------------------
        // Vista principal para mostrar las gráficas
        [RoleAuthorize("Administracion", "Supervisor de Carga", "Vendedor")]
        public async Task<IActionResult> Metricas()
        {
            var fechaFin = DateTime.Now;
            var fechaInicio = fechaFin.AddMonths(-1); // Último mes

            // Pasar fechas seleccionadas a la vista
            ViewBag.FechaInicio = fechaInicio.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin.ToString("yyyy-MM-dd");

            // Obtener pedidos y generar datos de las gráficas
            return await ProductosMasVendidos(fechaInicio, fechaFin);
        }



        // Acción para calcular los productos más vendidos y redirigir a Métricas
        [RoleAuthorize("Administracion", "Supervisor de Carga", "Vendedor", "Cliente")]
        public async Task<IActionResult> ProductosMasVendidos(DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Validar sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Pasar fechas seleccionadas a la vista
            ViewBag.FechaInicio = fechaInicio?.ToString("yyyy-MM-dd");
            ViewBag.FechaFin = fechaFin?.ToString("yyyy-MM-dd");

            // Obtener pedidos con estado "Entregado" desde la API
            var pedidos = await ObtenerPedidosPorEstado("Entregado");

            // Filtrar por rango de fechas
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                // Ajustar rango de fechas para incluir todos los pedidos creados en esas fechas
                var fechaInicioFormateada = fechaInicio.Value.Date; // Inicio del día
                var fechaFinFormateada = fechaFin.Value.Date.AddDays(1).AddTicks(-1); // Fin del día

                pedidos = pedidos.Where(p => p.FechaCreacion >= fechaInicioFormateada && p.FechaCreacion <= fechaFinFormateada).ToList();
            }


            // Calcular los productos más vendidos con la descripción como clave
            var productosMasVendidos = pedidos
                .SelectMany(p => p.LineasPedido)
                .GroupBy(lp => lp.Codigo) // Aquí "Codigo" ya contiene la descripción del producto
                .Select(g => new
                {
                    Producto = g.Key, // Este será el nombre (descripción)
                    CantidadTotal = g.Sum(lp => lp.Cantidad)
                })
                .OrderByDescending(p => p.CantidadTotal)
                .Take(10)
                .ToList();

            // Serializar datos para las gráficas
            ViewBag.ProductosLabels = productosMasVendidos.Select(p => p.Producto).ToList(); // Ahora son descripciones
            ViewBag.ProductosCantidades = productosMasVendidos.Select(p => p.CantidadTotal).ToList();


            // Renderizar la vista Métricas con los datos actualizados
            return View("Metricas");
        }

        // Método para obtener pedidos filtrados por estado
        private async Task<List<Pedido>> ObtenerPedidosPorEstado(string estado)
        {
            var client = _httpClientFactory.CreateClient();

            // Obtener pedidos con el estado especificado
            var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/Estado/{estado}");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Deserializar los pedidos
                var pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (pedidos != null)
                {
                    var codigosConsultados = new Dictionary<string, string>();

                    foreach (var pedido in pedidos)
                    {
                        // Obtener las líneas de pedido
                        var lineasResponse = await client.GetAsync($"https://localhost:7078/api/Pedidos/{pedido.IdPedido}/lineas");

                        if (lineasResponse.IsSuccessStatusCode)
                        {
                            var lineasJson = await lineasResponse.Content.ReadAsStringAsync();
                            var lineas = JsonSerializer.Deserialize<List<LineaPedido>>(lineasJson, new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            });

                            if (lineas != null)
                            {
                                foreach (var linea in lineas)
                                {
                                    // Evitar solicitudes duplicadas para el mismo código
                                    if (!codigosConsultados.ContainsKey(linea.Codigo))
                                    {
                                        var stockResponse = await client.GetAsync($"https://localhost:7078/api/Stocks/{linea.Codigo}");
                                        if (stockResponse.IsSuccessStatusCode)
                                        {
                                            var stockJson = await stockResponse.Content.ReadAsStringAsync();
                                            var stock = JsonSerializer.Deserialize<Producto>(stockJson, new JsonSerializerOptions
                                            {
                                                PropertyNameCaseInsensitive = true
                                            });

                                            if (stock != null)
                                            {
                                                codigosConsultados[linea.Codigo] = stock.Descripcion; // Guardamos la descripción
                                            }
                                        }
                                    }

                                    // Asignar la descripción al producto de la línea
                                    linea.Codigo = codigosConsultados.ContainsKey(linea.Codigo)
                                        ? codigosConsultados[linea.Codigo]
                                        : linea.Codigo; // Si no se encuentra, usar el código original

                                }

                                pedido.LineasPedido = lineas;
                            }
                        }
                    }

                    return pedidos;
                }
            }

            return new List<Pedido>();
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

        private async Task<List<LineaPedido>> ObtenerLineasPedido(int idPedido)
        {
            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/{idPedido}/lineas");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError(string.Empty, $"Error al obtener las líneas del pedido {idPedido}.");
                return new List<LineaPedido>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            var lineasPedido = JsonSerializer.Deserialize<List<LineaPedido>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<LineaPedido>();

            // Obtener los nombres de los productos
            foreach (var linea in lineasPedido)
            {
                var productoResponse = await client.GetAsync($"https://localhost:7078/api/Stocks/{linea.Codigo}");
                if (productoResponse.IsSuccessStatusCode)
                {
                    var productoJson = await productoResponse.Content.ReadAsStringAsync();
                    var producto = JsonSerializer.Deserialize<Producto>(productoJson, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    });

                    if (producto != null)
                    {
                        linea.Codigo = producto.Descripcion; // Añadir el nombre al modelo
                    }
                }
            }

            return lineasPedido;
        }

        // Método actualizado para obtener pedidos con líneas de pedido
        private async Task<List<Pedido>> ObtenerPedidosConLineas()
        {
            var pedidos = (await ObtenerPedidos()).ToList(); // Convertir a lista

            foreach (var pedido in pedidos)
            {
                pedido.LineasPedido = await ObtenerLineasPedido(pedido.IdPedido);
            }

            return pedidos;
        }


        [HttpGet]
        public async Task<IActionResult> DetallePedido(int idPedido)
        {
            try
            {
                var client = _httpClientFactory.CreateClient();

                // Llamar al endpoint correcto en el Backend
                var pedidoResponse = await client.GetAsync($"https://localhost:7078/api/Pedidos/Pedido/{idPedido}");

                if (!pedidoResponse.IsSuccessStatusCode)
                {
                    return PartialView("_Error", $"Error al cargar los detalles del pedido: {pedidoResponse.ReasonPhrase}");
                }

                // Parsear la respuesta JSON del pedido
                var pedidoJson = await pedidoResponse.Content.ReadAsStringAsync();
                var pedido = JsonSerializer.Deserialize<Pedido>(pedidoJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Cargar las líneas del pedido
                var lineasResponse = await client.GetAsync($"https://localhost:7078/api/Pedidos/{idPedido}/lineas");

                if (!lineasResponse.IsSuccessStatusCode)
                {
                    return PartialView("_Error", $"Error al cargar las líneas del pedido: {lineasResponse.ReasonPhrase}");
                }

                var lineasJson = await lineasResponse.Content.ReadAsStringAsync();
                var lineasPedido = JsonSerializer.Deserialize<List<LineaPedido>>(lineasJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                // Crear un modelo combinado con los detalles del pedido y las líneas
                var detallePedidoViewModel = new DetallePedidoViewModel
                {
                    Pedido = pedido,
                    LineasPedido = lineasPedido
                };

                return PartialView("_DetallePedido", detallePedidoViewModel);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", $"Ocurrió un error al cargar el detalle del pedido: {ex.Message}");
            }
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
