using Front_End_Gestion_Pedidos.Filters;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
using Microsoft.AspNetCore.Mvc;
using System.Collections;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Reflection;
using System.Text;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class PedidosController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public PedidosController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClientFactory.CreateClient("PedidosClient");
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

        public async Task<IActionResult> RepetirPedido(int idPedido)
        {
            try
            {
                int clienteLogueadoId = (int)HttpContext.Session.GetInt32("IdUsuario");
                string rol = HttpContext.Session.GetString("Role");

               
                List<LineaPedido> productosSeleccionados = await ObtenerLineasPedidoBDModel(idPedido);
                Pedido p = await ObtenerPedidoPorId(idPedido);

              
                
                 if (p == null) return RedirectToAction("Index","Home");
               
                 if (rol == "Cliente" && clienteLogueadoId != p.IdCliente) return RedirectToAction("Index", "Home");

                Cliente cli = await ObtenerClientePorId(p.IdCliente);


                var model = new PedidoViewModel
                {
                    Productos = await ObtenerStock(),
                    ClienteSeleccionado = cli,
                    ProductosSeleccionados = productosSeleccionados,
                    Comentarios = p.Comentarios,
                    

                };

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index","Home");
            }
        }

        public async Task<IActionResult> ModificarPedido(int idPedido)
        {
            try
            {
                Pedido p = await ObtenerPedidoPorId(idPedido);
                List<LineaPedido> lista = await ObtenerLineasPedidoBDModel(idPedido);
                Cliente cli = await ObtenerClientePorId(p.IdCliente);

                var model = new ModificarPedidoModel
                {
                    Pedidos = p,
                    ClienteSeleccionado = cli,
                    ProductoSeleccionados=lista,
                    Productos = await ObtenerStock()
                };

                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
            
        }
        
        
        [RoleAuthorize("Cliente")]
        public async Task<IActionResult> ClienteNuevoPedido()
        {
            int clienteLogueadoId = (int)HttpContext.Session.GetInt32("IdUsuario");
            var cliente = await ObtenerClientePorId(clienteLogueadoId);
            var model = new PedidoViewModel
            {
                Productos = await ObtenerStock(),
                ClienteSeleccionado = cliente
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

            // Obtener el rol del usuario desde la sesión
             var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            // Filtrar pedidos según el rol del usuario
            if (rolUsuario == "Supervisor de Carga")
            {
                pedidos = pedidos.Where(p => p.Estado.Equals("Preparando", StringComparison.OrdinalIgnoreCase) ||
                                             p.Estado.Equals("En viaje", StringComparison.OrdinalIgnoreCase)).ToList();
            }
            else if (rolUsuario == "Administracion")
            {
                pedidos = pedidos.Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) ||
                                             p.Estado.Equals("En viaje", StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Aplicar filtros adicionales según los parámetros
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
                
                var response = await _httpClient.GetAsync($"/api/Pedidos/{idPedido}/lineas");

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

        // --------------------------------------------------------------------------------------
        // PEDIDOS EN CURSO (Vista: PedidosEnCurso)
        // --------------------------------------------------------------------------------------

        [RoleAuthorize("Cliente", "Vendedor")]
        public async Task<IActionResult> PedidosEnCurso()
        {
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var idUsuarioSesion = _httpContextAccessor.HttpContext.Session.GetString("ClienteId");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            List<Pedido> pedidos = new List<Pedido>();

            // Obtener pedidos según el rol del usuario
            if (rolUsuario == "Cliente" && !string.IsNullOrEmpty(idUsuarioSesion))
            {
                // Obtener pedidos del cliente y filtrar por estados correspondientes
                pedidos = (await ObtenerPedidosPorCliente(idUsuarioSesion))
                    .Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) ||
                                p.Estado.Equals("Preparando", StringComparison.OrdinalIgnoreCase) ||
                                p.Estado.Equals("En viaje", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (rolUsuario == "Vendedor")
            {
                // Obtener todos los pedidos y filtrar por estados correspondientes
                pedidos = (await ObtenerPedidos())
                    .Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) ||
                                p.Estado.Equals("Preparando", StringComparison.OrdinalIgnoreCase) ||
                                p.Estado.Equals("En viaje", StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }

            // Preparar el modelo para la vista
            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos
            };

            return View(viewModel);
        }


        //[RoleAuthorize("Cliente", "Vendedor")]
        //public async Task<IActionResult> PedidosEnCurso(string cliente = "", int? idPedido = null)
        //{
        //    var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
        //    var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
        //    var idUsuarioSesion = _httpContextAccessor.HttpContext.Session.GetString("ClienteId");

        //    if (string.IsNullOrEmpty(usuarioLogueado))
        //        return RedirectToAction("Login", "Account");

        //    // Inicializamos la lista de pedidos
        //    IEnumerable<Pedido> pedidos = new List<Pedido>();

        //    // Filtrar por rol
        //    if (rolUsuario == "Cliente" && !string.IsNullOrEmpty(idUsuarioSesion))
        //    {
        //        // Obtener pedidos para el cliente logueado
        //        pedidos = await ObtenerPedidosPorCliente(idUsuarioSesion);
        //        pedidos = pedidos.Where(p => p.Estado.Equals("Pendiente", StringComparison.OrdinalIgnoreCase) ||
        //                                     p.Estado.Equals("Preparando", StringComparison.OrdinalIgnoreCase) ||
        //                                     p.Estado.Equals("En viaje", StringComparison.OrdinalIgnoreCase)).ToList();
        //    }
        //    else if (rolUsuario == "Vendedor")
        //    {
        //        // Obtener pedidos para el vendedor según los estados relevantes
        //        pedidos = await ObtenerPedidosPorEstados("Pendiente", "Preparando", "En viaje");
        //    }

        //    // Cargar detalle de líneas de pedido si se especifica un idPedido
        //    List<LineaPedido> detallePedido = null;
        //    if (idPedido.HasValue)
        //    {
        //        var response = await _httpClient.GetAsync($"/api/Pedidos/{idPedido}/lineas");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            detallePedido = JsonSerializer.Deserialize<List<LineaPedido>>(jsonResponse, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });
        //        }
        //    }

        //    // Preparar el modelo de vista
        //    var viewModel = new SupervisarPedidosViewModel
        //    {
        //        Pedidos = pedidos,
        //        Cliente = cliente,
        //        DetallePedido = detallePedido
        //    };

        //    return View(viewModel);
        //}

        [HttpPost]
        [RoleAuthorize("Cliente", "Vendedor")]
        public async Task<IActionResult> AñadirComentario(int id, string comentario)
        {
            try
            {
                // Validar usuario logueado
                var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");
                if (string.IsNullOrEmpty(usuarioLogueado))
                {
                    TempData["Mensaje"] = "Error: Sesión de usuario no válida.";
                    TempData["Exito"] = false;
                    return RedirectToAction("PedidosEnCurso");
                }

                // Validar que el comentario no esté vacío
                if (string.IsNullOrWhiteSpace(comentario))
                {
                    TempData["Mensaje"] = "Error: El comentario no puede estar vacío.";
                    TempData["Exito"] = false;
                    return RedirectToAction("PedidosEnCurso");
                }

                // Obtener el pedido actual
                var pedido = await ObtenerPedidoPorId(id);
                if (pedido == null)
                {
                    TempData["Mensaje"] = $"Error: No se encontró el pedido con ID {id}.";
                    TempData["Exito"] = false;
                    return RedirectToAction("PedidosEnCurso");
                }

                // Concatenar el nuevo comentario con los existentes
                var nuevoComentario = $"[{usuarioLogueado} {DateTime.Now:dd/MM/yyyy HH:mm}]: {comentario}";
                pedido.Comentarios = string.IsNullOrWhiteSpace(pedido.Comentarios)
                    ? nuevoComentario
                    : nuevoComentario + Environment.NewLine + pedido.Comentarios;

                // Actualizar el pedido en el sistema
                var resultado = await ActualizarPedido(pedido);
                if (resultado)
                {
                    TempData["Mensaje"] = "Comentario añadido correctamente.";
                    TempData["Exito"] = true;
                }
                else
                {
                    TempData["Mensaje"] = "Error: No se pudo guardar el comentario.";
                    TempData["Exito"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error inesperado: {ex.Message}";
                TempData["Exito"] = false;
            }

            return RedirectToAction("PedidosEnCurso");
        }






        // --------------------------------------------------------------------------------------
        // HISTORIAL DE PEDIDOS (Vista: HistorialPedidos)
        // --------------------------------------------------------------------------------------

        // Vista principal para mostrar el historial de pedidos
        [RoleAuthorize("Administracion", "Supervisor de Carga", "Vendedor", "Cliente")]
        public async Task<IActionResult> HistorialPedidos(string cliente, string vendedor, DateTime? fechaInicio, DateTime? fechaFin)
        {
            // Validar sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var idUsuarioSesion = _httpContextAccessor.HttpContext.Session.GetString("ClienteId"); // Para clientes

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Obtener todos los pedidos
            var pedidos = await ObtenerPedidos();

            // Filtrar pedidos con estado "Entregado" o "Cancelado"
            pedidos = pedidos.Where(p => p.Estado == "Entregado" || p.Estado == "Cancelado").ToList();

            // Si el usuario es Cliente, filtrar por IdCliente correspondiente
            if (rolUsuario == "Cliente" && !string.IsNullOrEmpty(idUsuarioSesion))
            {
                var idUsuario = int.Parse(idUsuarioSesion); // Convertir idUsuario de la sesión a entero
                pedidos = pedidos.Where(p => p.IdCliente == idUsuario).ToList();
            }

            // Aplicar filtros adicionales si se proporcionan
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

            // Retornar la vista con los pedidos filtrados
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
            //var pedidos = await ObtenerPedidosPorEstado("Entregado");
            var pedidos = await ObtenerPedidosPorEstados("Entregado");

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
        private async Task<List<Pedido>> ObtenerPedidosConDetalles(string estado)
        {
            var pedidos = await ObtenerPedidosPorEstados(estado); // Reutiliza el método simple
            
            var codigosConsultados = new Dictionary<string, string>();

            foreach (var pedido in pedidos)
            {
                // Obtén las líneas del pedido
                var lineasResponse = await _httpClient.GetAsync($"/api/Pedidos/{pedido.IdPedido}/lineas");

                if (lineasResponse.IsSuccessStatusCode)
                {
                    var lineasJson = await lineasResponse.Content.ReadAsStringAsync();
                    var lineas = JsonSerializer.Deserialize<List<LineaPedido>>(lineasJson);

                    if (lineas != null)
                    {
                        foreach (var linea in lineas)
                        {
                            // Evitar solicitudes duplicadas al endpoint de stocks
                            if (!codigosConsultados.ContainsKey(linea.Codigo))
                            {
                                var stockResponse = await _httpClient.GetAsync($"/api/Stocks/{linea.Codigo}");
                                if (stockResponse.IsSuccessStatusCode)
                                {
                                    var stockJson = await stockResponse.Content.ReadAsStringAsync();
                                    var stock = JsonSerializer.Deserialize<Producto>(stockJson);

                                    if (stock != null)
                                    {
                                        codigosConsultados[linea.Codigo] = stock.Descripcion;
                                    }
                                }
                            }

                            // Asigna la descripción del producto si está disponible
                            linea.Codigo = codigosConsultados.GetValueOrDefault(linea.Codigo, linea.Codigo);
                        }

                        pedido.LineasPedido = lineas;
                    }
                }
            }

            return pedidos;
        }


        //private async Task<List<Pedido>> ObtenerPedidosPorEstado(string estado)
        //{
        //    var client = _httpClientFactory.CreateClient();

        //    // Obtener pedidos con el estado especificado
        //    var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/Estado/{estado}");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var jsonResponse = await response.Content.ReadAsStringAsync();

        //        // Deserializar los pedidos
        //        var pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        });

        //        if (pedidos != null)
        //        {
        //            var codigosConsultados = new Dictionary<string, string>();

        //            foreach (var pedido in pedidos)
        //            {
        //                // Obtener las líneas de pedido
        //                var lineasResponse = await client.GetAsync($"https://localhost:7078/api/Pedidos/{pedido.IdPedido}/lineas");

        //                if (lineasResponse.IsSuccessStatusCode)
        //                {
        //                    var lineasJson = await lineasResponse.Content.ReadAsStringAsync();
        //                    var lineas = JsonSerializer.Deserialize<List<LineaPedido>>(lineasJson, new JsonSerializerOptions
        //                    {
        //                        PropertyNameCaseInsensitive = true
        //                    });

        //                    if (lineas != null)
        //                    {
        //                        foreach (var linea in lineas)
        //                        {
        //                            // Evitar solicitudes duplicadas para el mismo código
        //                            if (!codigosConsultados.ContainsKey(linea.Codigo))
        //                            {
        //                                var stockResponse = await client.GetAsync($"https://localhost:7078/api/Stocks/{linea.Codigo}");
        //                                if (stockResponse.IsSuccessStatusCode)
        //                                {
        //                                    var stockJson = await stockResponse.Content.ReadAsStringAsync();
        //                                    var stock = JsonSerializer.Deserialize<Producto>(stockJson, new JsonSerializerOptions
        //                                    {
        //                                        PropertyNameCaseInsensitive = true
        //                                    });

        //                                    if (stock != null)
        //                                    {
        //                                        codigosConsultados[linea.Codigo] = stock.Descripcion; // Guardamos la descripción
        //                                    }
        //                                }
        //                            }

        //                            // Asignar la descripción al producto de la línea
        //                            linea.Codigo = codigosConsultados.ContainsKey(linea.Codigo)
        //                                ? codigosConsultados[linea.Codigo]
        //                                : linea.Codigo; // Si no se encuentra, usar el código original

        //                        }

        //                        pedido.LineasPedido = lineas;
        //                    }
        //                }
        //            }

        //            return pedidos;
        //        }
        //    }

        //    return new List<Pedido>();
        //}






        // --------------------------------------------------------------------------------------
        // MÉTODOS AUXILIARES
        // --------------------------------------------------------------------------------------


        // Obtener todos los pedidos
        private async Task<IEnumerable<Pedido>> ObtenerPedidos()
        {
            var response = await _httpClient.GetAsync($"/api/Pedidos");

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

        private async Task<List<Pedido>> ObtenerPedidosPorEstados(params string[] estados)
        {
            var pedidos = new List<Pedido>();

            // Ejecuta las solicitudes en paralelo
            var tasks = estados.Select(async estado =>
            {
                var response = await _httpClient.GetAsync($"/api/Pedidos/Estado/{estado}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse) ?? new List<Pedido>();
                }
                return new List<Pedido>();
            });

            // Combina los resultados
            var resultados = await Task.WhenAll(tasks);
            foreach (var resultado in resultados)
            {
                pedidos.AddRange(resultado);
            }

            return pedidos;
        }

        //private async Task<IEnumerable<Pedido>> ObtenerPedidosPorEstado(params string[] estados)
        //{
        //    var client = _httpClientFactory.CreateClient();
        //    var pedidos = new List<Pedido>();

        //    foreach (var estado in estados)
        //    {
        //        var response = await client.GetAsync($"https://localhost:7078/api/Pedidos/Estado/{estado}");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            var pedidosPorEstado = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });

        //            if (pedidosPorEstado != null)
        //            {
        //                pedidos.AddRange(pedidosPorEstado);
        //            }
        //        }
        //    }

        //    return pedidos;
        //}

        private async Task<List<LineaPedido>> ObtenerLineasPedidoBDModel(int idPedido)
        {
            var response = await _httpClient.GetAsync($"/api/Pedidos/{idPedido}/lineas");

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

            return lineasPedido;

        }


        private async Task<List<LineaPedido>> ObtenerLineasPedido(int idPedido)
        {
            var response = await _httpClient.GetAsync($"/api/Pedidos/{idPedido}/lineas");

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
                var productoResponse = await _httpClient.GetAsync($"/api/Stocks/{linea.Codigo}");
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

        private async Task<Pedido> ObtenerPedidoPorId(int idPedido)
        {
            var response = await _httpClient.GetAsync($"/api/Pedidos/Pedido/{idPedido}");

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<Pedido>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }

        //[HttpPost]
        //public async Task<IActionResult> CambiarEstadoPedido(int id, string nuevoEstado)
        //{
        //    try
        //    {
        //        // Realiza una solicitud PUT al backend API
        //        var response = await _httpClient.PutAsJsonAsync($"/api/Pedidos/Estado/{id}", nuevoEstado);



        //        if (response.IsSuccessStatusCode)
        //        {
        //            TempData["Mensaje"] = $"El estado del pedido #{id} se actualizó correctamente a '{nuevoEstado}'.";
        //            TempData["Exito"] = true; // Indicador de éxito
        //        }
        //        else
        //        {
        //            TempData["Mensaje"] = $"Error al actualizar el estado del pedido #{id}: {response.ReasonPhrase}.";
        //            TempData["Exito"] = false; // Indicador de error
        //        }
        //    }
        //    catch (Exception ex)
        //    {
        //        TempData["Mensaje"] = $"Error al comunicarse con el backend: {ex.Message}";
        //        TempData["Exito"] = false;
        //    }

        //    return RedirectToAction("SupervisarPedidos");
        //}


        // Acción para cambiar el estado de un pedido

        [HttpPost]
        public async Task<IActionResult> CambiarEstadoPedido(int id, string nuevoEstado)
        {
            try
            {
                var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");
                //var idUsuario = HttpContext.Session.GetString("IdUsuario");
                var idUsuario = HttpContext.Session.GetInt32("IdUsuario");
                var rolUsuario = HttpContext.Session.GetString("Role");

                if (string.IsNullOrEmpty(usuarioLogueado))
                {
                    TempData["Mensaje"] = "Error: Sesión de usuario no válida.";
                    TempData["Exito"] = false;
                    return RedirectToAction("SupervisarPedidos");
                }

                // Obtener el pedido actual
                var pedido = await ObtenerPedidoPorId(id);
                if (pedido == null)
                {
                    TempData["Mensaje"] = $"Error: No se encontró el pedido con ID {id}.";
                    TempData["Exito"] = false;
                    return RedirectToAction("SupervisarPedidos");
                }

                // Actualizar campos específicos según el nuevo estado
                pedido.Estado = nuevoEstado;
                pedido.Comentarios = $"[{usuarioLogueado} {DateTime.Now:dd/MM/yyyy HH:mm}]: {nuevoEstado}" +
                                     (string.IsNullOrWhiteSpace(pedido.Comentarios) ? "" : Environment.NewLine + pedido.Comentarios);

                if (rolUsuario == "Supervisor de Carga")
                {
                    pedido.IdSupervisor = idUsuario.Value;
                }

                // Si el estado es "Entregado", actualizar fechaEntregado
                if (nuevoEstado.Equals("Entregado", StringComparison.OrdinalIgnoreCase))
                {
                    pedido.FechaEntregado = DateTime.Now;
                }

                // Delegar la actualización al método ActualizarPedido
                var resultado = await ActualizarPedido(pedido);
                if (resultado)
                {
                    TempData["Mensaje"] = "El estado del pedido se actualizó correctamente.";
                    TempData["Exito"] = true;
                }
                else
                {
                    TempData["Mensaje"] = "Error: No se pudo actualizar el estado del pedido.";
                    TempData["Exito"] = false;
                }
            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error inesperado: {ex.Message}";
                TempData["Exito"] = false;
            }

            return RedirectToAction("SupervisarPedidos");
        }

        private async Task<IEnumerable<Pedido>> ObtenerPedidosPorCliente(string idCliente)
        {
           var response = await _httpClient.GetAsync($"/api/Pedidos/Cliente/{idCliente}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Error al obtener los pedidos del cliente.");
                return new List<Pedido>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Pedido>();
        }


        private async Task<bool> ActualizarPedido(Pedido pedido)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(pedido);

                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Realizar la solicitud PUT
                var response = await _httpClient.PutAsync($"/api/Pedidos/{pedido.IdPedido}", content);


                if (!response.IsSuccessStatusCode)
                {
                    var errorDetails = await response.Content.ReadAsStringAsync();
                    TempData["Mensaje"] = $"Error al actualizar el pedido: {errorDetails}";
                    return false;
                }

                TempData["Mensaje"] = "Pedido actualizado correctamente.";
                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
                TempData["Mensaje"] = $"Error inesperado: {ex.Message}";
                return false;
            }
        }






        // Obtener la lista de CLIENTES
        private async Task<List<Cliente>> ObtenerClientes()
        {
           var response = await _httpClient.GetAsync("/api/v1/Clientes");

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

        // Obtener un cliente por ID
        private async Task<Cliente> ObtenerClientePorId(long clienteId)
        {
            var response = await _httpClient.GetAsync($"/api/v1/Clientes/{clienteId}");

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




        // Obtener la lista de PRODUCTOS
        private async Task<List<Producto>> ObtenerStock()
        {
            var response = await _httpClient.GetAsync("/api/Stocks");

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





        [HttpGet]
        public async Task<IActionResult> DetallePedido(int idPedido)
        {
            try
            {

                // Llamar al endpoint correcto en el Backend
                var pedidoResponse = await _httpClient.GetAsync($"https://localhost:7078/api/Pedidos/Pedido/{idPedido}");

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
                var lineasResponse = await _httpClient.GetAsync($"https://localhost:7078/api/Pedidos/{idPedido}/lineas");

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
        //private async Task<IEnumerable<Producto>> filtroStock(string filtro)
        //{
        //    var client = _httpClientFactory.CreateClient();
        //    var response = await client.GetAsync($"https://localhost:7078/api/Stocks{filtro}");

        //    if (response.IsSuccessStatusCode)
        //    {
        //        var jsonResponse = await response.Content.ReadAsStringAsync();
        //        return JsonSerializer.Deserialize<List<Producto>>(jsonResponse, new JsonSerializerOptions
        //        {
        //            PropertyNameCaseInsensitive = true
        //        }) ?? new List<Producto>();
        //    }

        //    return new List<Producto>();
        //}






        //private async Task<bool> ActualizarPedidoEnBaseDeDatos(Pedido pedido)
        //{
        //    var client = _httpClientFactory.CreateClient();
        //    var jsonContent = JsonSerializer.Serialize(pedido);
        //    var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");
        //    var response = await client.PutAsync($"https://localhost:7078/api/Pedidos/{pedido.IdPedido}", content);
        //    return response.IsSuccessStatusCode;
        //}



    }

}
