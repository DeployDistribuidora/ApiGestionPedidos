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
            ViewData["Token"] = HttpContext.Session.GetString("Token");
            ViewData["Role"] = HttpContext.Session.GetString("Role");
            ViewData["IdUsuario"] = HttpContext.Session.GetInt32("IdUsuario");
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



                if (p == null) return RedirectToAction("Index", "Home");

                if (rol == "Cliente" && clienteLogueadoId != p.IdCliente) return RedirectToAction("Index", "Home");

                Cliente cli = await ObtenerClientePorId(p.IdCliente);


                var model = new PedidoViewModel
                {
                    Productos = await ObtenerStock(),
                    ClienteSeleccionado = cli,
                    ProductosSeleccionados = productosSeleccionados,
                    Comentarios = p.Comentarios,


                };
                ViewData["Role"] = HttpContext.Session.GetString("Role");
                ViewData["Token"] = HttpContext.Session.GetString("Token");
                return View(model);
            }
            catch (Exception ex)
            {
                return RedirectToAction("Index", "Home");
            }
        }

        public async Task<IActionResult> ModificarPedido(int idPedido)
        {
            try
            {
                Pedido p = await ObtenerPedidoPorId(idPedido);

                if (p.Estado == "Entregado" || p.Estado == "Cancelado")
                {
                    return RedirectToAction("Index", "Home");
                }

                List<LineaPedido> lista = await ObtenerLineasPedidoBDModel(idPedido);
                Cliente cli = await ObtenerClientePorId(p.IdCliente);

                var model = new ModificarPedidoModel
                {
                    Pedidos = p,
                    ClienteSeleccionado = cli,
                    ProductoSeleccionados = lista,
                    Productos = await ObtenerStock()
                };
                ViewData["Token"] = HttpContext.Session.GetString("Token");
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


           
            ViewData["Token"] = HttpContext.Session.GetString("Token");
            return View(model);
        }
        // --------------------------------------------------------------------------------------
        // SUPERVISAR PEDIDOS (Vista: SupervisarPedidos)
        // --------------------------------------------------------------------------------------

        [RoleAuthorize("Administracion", "Supervisor de Carga")]
        public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "", string estado = "")
        {
            // Obtener el rol del usuario desde la sesión
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            // Determinar los estados permitidos según el rol del usuario
            var estadosPermitidos = rolUsuario switch
            {
                "Supervisor de Carga" => new[] { "Preparando", "En viaje" },
                "Administracion" => new[] { "Pendiente", "En viaje" },
                _ => Array.Empty<string>()
            };

            List<Pedido> pedidos = new List<Pedido>();

            // Obtener la lista de clientes para el filtro en memoria
            var clientes = await ObtenerClientes();

            // Obtener la lista de vendedores para el filtro en memoria
            var vendedores = await ObtenerVendedores();

            // Caso 1: Si se filtra por cliente
            if (!string.IsNullOrEmpty(cliente))
            {
                var response = await _httpClient.GetAsync($"Pedidos/Cliente/{cliente}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }).Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase)).ToList();
                }
            }
            // Caso 2: Si se filtra por vendedor
            else if (!string.IsNullOrEmpty(vendedor))
            {
                var response = await _httpClient.GetAsync($"Pedidos/Vendedor/{vendedor}");
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }).Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase)).ToList();
                }
            }
            // Caso 3: Sin filtro de cliente ni vendedor
            else
            {
                foreach (var estadoPermitido in estadosPermitidos)
                {
                    var responseEstado = await _httpClient.GetAsync($"Pedidos/Estado/{estadoPermitido}");
                    if (responseEstado.IsSuccessStatusCode)
                    {
                        var jsonResponse = await responseEstado.Content.ReadAsStringAsync();
                        var pedidosPorEstado = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        pedidos.AddRange(pedidosPorEstado);
                    }
                }
            }

            // Filtrar por estado si se especifica explícitamente
            if (!string.IsNullOrEmpty(estado))
            {
                pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Preparar el modelo para la vista
            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Clientes = clientes,
                Vendedores = vendedores, // Se añade la lista de vendedores
                Cliente = cliente,
                Vendedor = vendedor,
                Estado = estado
            };

            return View(viewModel);
        }



        //[RoleAuthorize("Administracion", "Supervisor de Carga")]
        //public async Task<IActionResult> SupervisarPedidos(string cliente = "", string vendedor = "", string estado = "")
        //{
        //    // Obtener el rol del usuario desde la sesión
        //    var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

        //    // Determinar los estados permitidos según el rol del usuario
        //    var estadosPermitidos = rolUsuario switch
        //    {
        //        "Supervisor de Carga" => new[] { "Preparando", "En viaje" },
        //        "Administracion" => new[] { "Pendiente", "En viaje" },
        //        _ => Array.Empty<string>()
        //    };

        //    List<Pedido> pedidos = new List<Pedido>();

        //    // Obtener la lista de clientes para el filtro en memoria
        //    var clientes = await ObtenerClientes();

        //    // Caso 1: Si se filtra por cliente
        //    if (!string.IsNullOrEmpty(cliente))
        //    {
        //        var response = await _httpClient.GetAsync($"Pedidos/Cliente/{cliente}");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            }).Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase)).ToList();
        //        }
        //    }
        //    // Caso 2: Si se filtra por vendedor
        //    else if (!string.IsNullOrEmpty(vendedor))
        //    {
        //        var response = await _httpClient.GetAsync($"Pedidos/Vendedor/{vendedor}");
        //        if (response.IsSuccessStatusCode)
        //        {
        //            var jsonResponse = await response.Content.ReadAsStringAsync();
        //            pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            }).Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase)).ToList();
        //        }
        //    }
        //    // Caso 3: Sin filtro de cliente ni vendedor
        //    else
        //    {
        //        foreach (var estadoPermitido in estadosPermitidos)
        //        {
        //            var responseEstado = await _httpClient.GetAsync($"Pedidos/Estado/{estadoPermitido}");
        //            if (responseEstado.IsSuccessStatusCode)
        //            {
        //                var jsonResponse = await responseEstado.Content.ReadAsStringAsync();
        //                var pedidosPorEstado = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
        //                {
        //                    PropertyNameCaseInsensitive = true
        //                });
        //                pedidos.AddRange(pedidosPorEstado);
        //            }
        //        }
        //    }

        //    // Filtrar por estado si se especifica explícitamente
        //    if (!string.IsNullOrEmpty(estado))
        //    {
        //        pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
        //    }

        //    // Preparar el modelo para la vista
        //    var viewModel = new SupervisarPedidosViewModel
        //    {
        //        Pedidos = pedidos,
        //        Clientes = clientes,
        //        Cliente = cliente,
        //        Vendedor = vendedor,
        //        Estado = estado
        //    };

        //    return View(viewModel);
        //}

        // Comparador para realizar intersecciones entre listas de pedidos
        public class PedidoComparer : IEqualityComparer<Pedido>
        {
            public bool Equals(Pedido? x, Pedido? y)
            {
                // Si ambos son null, son iguales
                if (x == null && y == null) return true;

                // Si uno es null y el otro no, no son iguales
                if (x == null || y == null) return false;

                // Comparar los IdPedido
                return x.IdPedido == y.IdPedido;
            }

            public int GetHashCode(Pedido obj)
            {
                // Verifica si obj es null para evitar errores de referencia nula
                if (obj == null) throw new ArgumentNullException(nameof(obj));

                return obj.IdPedido.GetHashCode();
            }
        }




        // --------------------------------------------------------------------------------------
        // PEDIDOS EN CURSO (Vista: PedidosEnCurso)
        // --------------------------------------------------------------------------------------
        /*
        [RoleAuthorize("Cliente", "Vendedor")]
        public async Task<IActionResult> PedidosEnCurso(string cliente)
        {
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var idUsuario = _httpContextAccessor.HttpContext.Session.GetInt32("IdUsuario");

            if (string.IsNullOrEmpty(usuarioLogueado) || !idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            List<Pedido> pedidos = new List<Pedido>();

            if (rolUsuario == "Cliente")
            {
                // Obtener pedidos del cliente logueado
                var estadosPermitidos = new[] { "Pendiente", "Preparando", "En viaje" };
                pedidos = (await ObtenerPedidosPorCliente(idUsuario.Value.ToString()))
                    .Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (rolUsuario == "Vendedor")
            {
                // Obtener todos los pedidos del vendedor con estados específicos
                var estadosPermitidos = new[] { "Pendiente", "Preparando", "En viaje" };
                foreach (var estado in estadosPermitidos)
                {
                    var response = await _httpClient.GetAsync($"Pedidos/Estado/{estado}");
                    if (response.IsSuccessStatusCode)
                    {
                        var jsonResponse = await response.Content.ReadAsStringAsync();
                        var pedidosPorEstado = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });
                        pedidos.AddRange(pedidosPorEstado);
                    }
                }
            }
            else if (!string.IsNullOrEmpty(cliente))
            {
                // Obtener pedidos por cliente desde el filtro
                pedidos = (await ObtenerPedidosPorCliente(cliente)).ToList();
            }

            var clientes = await ObtenerClientes();

            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Clientes = clientes
            };

            return View(viewModel);
        }*/

        // --------------------------------------------------------------------------------------
        // PEDIDOS EN CURSO (Vista: PedidosEnCurso)  praa ver es el de gabi nuevo
        // --------------------------------------------------------------------------------------
        [RoleAuthorize("Cliente", "Vendedor")]
        public async Task<IActionResult> PedidosEnCurso(string cliente = "", string vendedor = "", string estado = "")
        {
            // Obtener datos de la sesión del usuario
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var idUsuario = _httpContextAccessor.HttpContext.Session.GetInt32("IdUsuario");

            if (string.IsNullOrEmpty(usuarioLogueado) || !idUsuario.HasValue)
                return RedirectToAction("Login", "Account");

            List<Pedido> pedidos = new List<Pedido>();

            // Obtener los estados permitidos según el rol
            var estadosPermitidos = rolUsuario switch
            {
                "Cliente" => new[] { "Pendiente", "Preparando", "En viaje" },
                "Vendedor" => new[] { "Pendiente", "Preparando", "En viaje" },
                _ => Array.Empty<string>()
            };

            // Filtrar pedidos según el rol del usuario
            if (rolUsuario == "Cliente")
            {
                pedidos = (await ObtenerPedidosPorCliente(idUsuario.Value.ToString()))
                    .Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase))
                    .ToList();
            }
            else if (rolUsuario == "Vendedor")
            {
                pedidos = (await ObtenerPedidosPorEstados(estadosPermitidos));
            }
            //else if (rolUsuario == "Vendedor")
            //{
            //    pedidos = (await ObtenerPedidosPorVendedor(idUsuario.Value.ToString()))
            //        .Where(p => estadosPermitidos.Contains(p.Estado, StringComparer.OrdinalIgnoreCase))
            //        .ToList();
            //}

          

            // Filtros adicionales para cliente, vendedor y estado
            if (!string.IsNullOrEmpty(cliente))
            {
                pedidos = pedidos.Where(p => p.IdCliente.ToString() == cliente).ToList();
            }

            if (!string.IsNullOrEmpty(vendedor))
            {
                pedidos = pedidos.Where(p => p.IdVendedor.ToString() == vendedor).ToList();
            }

            if (!string.IsNullOrEmpty(estado))
            {
                pedidos = pedidos.Where(p => p.Estado.Equals(estado, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            // Obtener listas de clientes y vendedores para los filtros
            var clientes = await ObtenerClientes();
            var vendedores = await ObtenerVendedores();

            // Preparar el modelo para la vista
            var viewModel = new SupervisarPedidosViewModel
            {
                Pedidos = pedidos,
                Clientes = clientes,
                Vendedores = vendedores,
                Cliente = cliente,
                Vendedor = vendedor,
                Estado = estado
            };

            return View(viewModel);
        }


        [HttpPost]
        [RoleAuthorize("Cliente", "Vendedor", "Supervisor de Carga", "Administracion")]
        public async Task<IActionResult> AñadirComentario(int id, string comentario)
        {
            // Obtener el rol del usuario
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            // Validar usuario logueado
            var usuarioLogueado = HttpContext.Session.GetString("UsuarioLogueado");
         
            try
            {
               

                // Validar que el comentario no esté vacío
                if (string.IsNullOrWhiteSpace(comentario))
                {
                    TempData["Mensaje"] = "Error: El comentario no puede estar vacío.";
                    TempData["Exito"] = false;
                    return rolUsuario == "Cliente" || rolUsuario == "Vendedor"
                        ? RedirectToAction("PedidosEnCurso")
                        : RedirectToAction("SupervisarPedidos");
                }

                // Obtener el pedido actual
                var pedido = await ObtenerPedidoPorId(id);
                if (pedido == null)
                {
                    TempData["Mensaje"] = $"Error: No se encontró el pedido con ID {id}.";
                    TempData["Exito"] = false;
                    return rolUsuario == "Cliente" || rolUsuario == "Vendedor"
                        ? RedirectToAction("PedidosEnCurso")
                        : RedirectToAction("SupervisarPedidos");
                }

                // Concatenar el nuevo comentario con los existentes
                var nuevoComentario = $"[{usuarioLogueado} {DateTime.Now:dd/MM/yyyy HH:mm}]: {comentario}";
                pedido.Comentarios = string.IsNullOrWhiteSpace(pedido.Comentarios)
                    ? nuevoComentario
                    : nuevoComentario + Environment.NewLine + pedido.Comentarios;

                // Actualizar el pedido en el sistema
                var resultado = await ActualizarPedido(pedido);
                TempData["Mensaje"] = resultado.Mensaje; // Ahora usa el mensaje del resultado
                TempData["Exito"] = resultado.Exito;
                TempData["Advertencia"] = resultado.Advertencia;

            }
            catch (Exception ex)
            {
                TempData["Mensaje"] = $"Error inesperado: {ex.Message}";
                TempData["Exito"] = false;
            }

            // Redirigir según el rol del usuario
            return rolUsuario == "Cliente" || rolUsuario == "Vendedor"
                ? RedirectToAction("PedidosEnCurso")
                : RedirectToAction("SupervisarPedidos");
        }


        // --------------------------------------------------------------------------------------
        // HISTORIAL DE PEDIDOS (Vista: HistorialPedidos)
        // --------------------------------------------------------------------------------------

        // Vista principal para mostrar el historial de pedidos
        [RoleAuthorize("Administracion", "Supervisor de Carga", "Vendedor", "Cliente")]
        public async Task<IActionResult> HistorialPedidos(int? cliente, int? idPedido, int? vendedor, DateTime? fechaInicio, DateTime? fechaFin)
        {
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");
            var idUsuarioSesion = _httpContextAccessor.HttpContext.Session.GetString("ClienteId");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            var pedidos = await ObtenerPedidos();
            var vendedores = await ObtenerVendedores();

            // Filtrar por estados entregado o cancelado
            pedidos = pedidos.Where(p => p.Estado == "Entregado" || p.Estado == "Cancelado").ToList();

            // Filtrar por cliente si es un cliente logueado
            if (rolUsuario == "Cliente" && !string.IsNullOrEmpty(idUsuarioSesion))
            {
                var idUsuario = int.Parse(idUsuarioSesion);
                pedidos = pedidos.Where(p => p.IdCliente == idUsuario).ToList();
            }

            // Aplicar filtro por cliente
            if (cliente.HasValue)
            {
                pedidos = pedidos.Where(p => p.IdCliente == cliente.Value).ToList();
            }

            // Aplicar filtro por vendedor
            if (vendedor.HasValue)
            {
                pedidos = pedidos.Where(p => p.IdVendedor == vendedor.Value).ToList();
            }

            // Aplicar filtro por idPedido
            if (idPedido.HasValue)
            {
                pedidos = pedidos.Where(p => p.IdPedido == idPedido.Value).ToList();
            }

            // Aplicar filtro por rango de fechas
            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date >= fechaInicio.Value.Date &&
                                             p.FechaCreacion.Date <= fechaFin.Value.Date).ToList();
            }
            else if (fechaInicio.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date >= fechaInicio.Value.Date).ToList();
            }
            else if (fechaFin.HasValue)
            {
                pedidos = pedidos.Where(p => p.FechaCreacion.Date <= fechaFin.Value.Date).ToList();
            }

            var viewModel = new HistorialPedidosViewModel
            {
                Pedidos = pedidos,
                Clientes = await ObtenerClientes(),
                Vendedores = vendedores
            };

            return View(viewModel);
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

            // Obtener pedidos con estado "Entregado"
            List<Pedido> pedidos = await ObtenerPedidosPorEstados("Entregado");

            if (fechaInicio.HasValue && fechaFin.HasValue)
            {
                var fechaInicioFormateada = fechaInicio.Value.Date;
                var fechaFinFormateada = fechaFin.Value.Date.AddDays(1); // Inicio del día siguiente

                pedidos = pedidos
                    .Where(p => p.FechaCreacion != DateTime.MinValue) // Ignorar fechas inválidas
                    .Where(p => p.FechaCreacion >= fechaInicioFormateada && p.FechaCreacion < fechaFinFormateada)
                    .ToList();
            }

            List<PedidoParaMetricasViewModel> pedidosViewModel = pedidos.Select(p => new PedidoParaMetricasViewModel
            {
                Pedido = p,
                lineaPedidoConProducto = new List<LineaPedidoConProductoViewModel>(),
            }).ToList();

            // Llamar al método que llena la info de las líneas en el ViewModel
            await LlenarLineasPedido(pedidosViewModel);

            var productosMasVendidos = pedidosViewModel
                .SelectMany(vm => vm.lineaPedidoConProducto)
                .GroupBy(lp => lp.lineaPedido.Codigo)
                .Select(g => new
                {
                    Codigo = g.Key,
                    // Tomamos la descripción del primer elemento del grupo 
                    // (asumiendo que la descripción es la misma para todos con el mismo Código)
                    DescripcionProducto = g.First().DescripcionProducto,
                    CantidadTotal = g.Sum(lp => lp.lineaPedido.Cantidad)
                })
                .OrderByDescending(p => p.CantidadTotal)
                .Take(10)
                .ToList();
            
            // Serializar datos para gráficas
            ViewBag.ProductosLabels = productosMasVendidos.Select(p => p.DescripcionProducto).ToList();
            ViewBag.ProductosCantidades = productosMasVendidos.Select(p => p.CantidadTotal).ToList();

            return View("Metricas");
        }


        /// <summary>
        /// Llama al endpoint /Pedidos/{idPedido}/lineas para cada pedido de la lista,
        /// y asigna la propiedad LineasPedido en cada uno.
        /// </summary>
        private async Task LlenarLineasPedido(List<PedidoParaMetricasViewModel> pedidos)
        {
            if (pedidos == null || pedidos.Count == 0)
                return;

            foreach (PedidoParaMetricasViewModel pedido in pedidos)
            {
                var lineasResponse = await _httpClient.GetAsync($"Pedidos/{pedido.Pedido.IdPedido}/lineas/DescProducto");
                if (!lineasResponse.IsSuccessStatusCode)
                {
                    // Si no fue exitosa la respuesta, seguimos con el siguiente pedido
                    continue;
                }

                var lineasJson = await lineasResponse.Content.ReadAsStringAsync();

                // Deserializa la lista de líneas (configurada para ignorar mayúsculas en el JSON)
                var lineas = JsonSerializer.Deserialize<List<LineaPedidoConProductoViewModel>>(
                    lineasJson,
                    new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true
                    }
                );
                // Asigna la lista de líneas al pedido (si es null, asigna lista vacía)
                pedido.lineaPedidoConProducto = lineas ?? new List<LineaPedidoConProductoViewModel>();
            }
        }

        // --------------------------------------------------------------------------------------
        // MÉTODOS AUXILIARES
        // --------------------------------------------------------------------------------------


        // Obtener todos los pedidos
        private async Task<IEnumerable<Pedido>> ObtenerPedidos()
        {
            var response = await _httpClient.GetAsync($"Pedidos");

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
            // Lanza las solicitudes en paralelo para cada estado
            var tasks = estados.Select(estado => ObtenerPedidosEnEstadoAsync(estado));

            // Espera a que todas las tareas se completen
            var resultados = await Task.WhenAll(tasks);

            // Combina todos los resultados en una sola lista
            return resultados.SelectMany(pedidos => pedidos).ToList();
        }

        private async Task<List<Pedido>> ObtenerPedidosEnEstadoAsync(string estado)
        {
            try
            {
                var response = await _httpClient.GetAsync($"Pedidos/Estado/{estado}");
                if (!response.IsSuccessStatusCode)
                {
                    return new List<Pedido>();
                }

                var jsonResponse = await response.Content.ReadAsStringAsync();

                // Opciones para ignorar diferencia mayúsculas/minúsculas en los nombres
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                };

                // Deserializa; si es null, devolvemos una lista vacía
                var pedidos = JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, options);
                return pedidos ?? new List<Pedido>();
            }
            catch (Exception ex)
            {
                // Manejo de errores si algo falla (opcional)
                Console.WriteLine($"Error al obtener pedidos para el estado {estado}: {ex.Message}");
                return new List<Pedido>();
            }
        }

        private async Task<List<LineaPedido>> ObtenerLineasPedidoBDModel(int idPedido)
        {
            var response = await _httpClient.GetAsync($"Pedidos/{idPedido}/lineas");

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

        //private async Task<List<LineaPedido>> ObtenerLineasPedido(int idPedido)
        //{
        //    var response = await _httpClient.GetAsync($"/api/Pedidos/{idPedido}/lineas");

        //    if (!response.IsSuccessStatusCode)
        //    {
        //        ModelState.AddModelError(string.Empty, $"Error al obtener las líneas del pedido {idPedido}.");
        //        return new List<LineaPedido>();
        //    }

        //    var jsonResponse = await response.Content.ReadAsStringAsync();
        //    var lineasPedido = JsonSerializer.Deserialize<List<LineaPedido>>(jsonResponse, new JsonSerializerOptions
        //    {
        //        PropertyNameCaseInsensitive = true
        //    }) ?? new List<LineaPedido>();

        //    // Obtener los nombres de los productos
        //    foreach (var linea in lineasPedido)
        //    {
        //        var productoResponse = await _httpClient.GetAsync($"/api/Stocks/{linea.Codigo}");
        //        if (productoResponse.IsSuccessStatusCode)
        //        {
        //            var productoJson = await productoResponse.Content.ReadAsStringAsync();
        //            var producto = JsonSerializer.Deserialize<Producto>(productoJson, new JsonSerializerOptions
        //            {
        //                PropertyNameCaseInsensitive = true
        //            });

        //            if (producto != null)
        //            {
        //                linea.Codigo = producto.Descripcion; // Añadir el nombre al modelo
        //            }
        //        }
        //    }

        //    return lineasPedido;
        //}

        // Método actualizado para obtener pedidos con líneas de pedido
        private async Task<List<Pedido>> ObtenerPedidosConLineas()
        {
            var pedidos = (await ObtenerPedidos()).ToList(); // Convertir a lista

            foreach (var pedido in pedidos)
            {
                pedido.LineasPedido = await ObtenerLineasPedidoBDModel(pedido.IdPedido);
            }

            return pedidos;
        }

        private async Task<Pedido> ObtenerPedidoPorId(int idPedido)
        {
            var response = await _httpClient.GetAsync($"Pedidos/Pedido/{idPedido}");

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
                TempData["Mensaje"] = resultado.Mensaje;
                TempData["Exito"] = resultado.Exito;
                TempData["Advertencia"] = resultado.Advertencia;

                if (rolUsuario == "Cliente" || rolUsuario == "Vendedor")
                {
                    return RedirectToAction("PedidosEnCurso");
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
            var response = await _httpClient.GetAsync($"Pedidos/Cliente/{idCliente}");

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

        private async Task<IEnumerable<Pedido>> ObtenerPedidosPorVendedor(string idVendedor)
        {
            var response = await _httpClient.GetAsync($"Pedidos/Vendedor/{idVendedor}");

            if (!response.IsSuccessStatusCode)
            {
                ModelState.AddModelError("", "Error al obtener los pedidos del vendedor.");
                return new List<Pedido>();
            }

            var jsonResponse = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<List<Pedido>>(jsonResponse, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            }) ?? new List<Pedido>();
        }



        // Clase auxiliar para manejar el resultado de la actualización
        private class ResultadoActualizacion
        {
            public bool Exito { get; set; }
            public string Mensaje { get; set; } 
            public bool Advertencia { get; set; } 
        }

        private async Task<ResultadoActualizacion> ActualizarPedido(Pedido pedido)
        {
            try
            {
                var jsonContent = JsonSerializer.Serialize(pedido);

                using var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // Realizar la solicitud PUT
                var response = await _httpClient.PutAsync($"Pedidos/sin-lineas/{pedido.IdPedido}", content);

                var responseContent = await response.Content.ReadAsStringAsync();

                if (response.IsSuccessStatusCode)
                {
                    // Manejar respuesta "Accepted" con advertencia
                    if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
                    {
                        var jsonResponse = JsonSerializer.Deserialize<BackendResponse>(responseContent, new JsonSerializerOptions
                        {
                            PropertyNameCaseInsensitive = true
                        });

                        return new ResultadoActualizacion 
                        {
                            Exito = true,
                            Mensaje = jsonResponse?.Mensaje ?? "Pedido actualizado con advertencias.", 
                            Advertencia = true 
                        };
                    }
                    else
                    {
                        return new ResultadoActualizacion
                        {
                            Exito = true,
                            Mensaje = "Pedido actualizado correctamente.",
                            Advertencia = false
                        };
                    }
                }
                else
                {
                    var mensajeError = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.NotFound => "No se encontró el pedido especificado.",
                        System.Net.HttpStatusCode.BadRequest => $"Datos inválidos: {responseContent}", 
                        _ => $"Error al actualizar el pedido: {responseContent}"
                    };

                    return new ResultadoActualizacion 
                    {
                        Exito = false,
                        Mensaje = mensajeError,
                        Advertencia = false 
                    };
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error inesperado: {ex.Message}");
                return new ResultadoActualizacion 
                {
                    Exito = false, //******************
                    Mensaje = $"Error inesperado: {ex.Message}",
                    Advertencia = false
                };
            }
        }

        // Clase auxiliar para manejar la respuesta del backend con advertencia
        private class BackendResponse
        {
            public string Mensaje { get; set; }
            public bool Advertencia { get; set; }
        }

        // Obtener la lista de CLIENTES
        private async Task<List<Cliente>> ObtenerClientes()
        {
            var response = await _httpClient.GetAsync("Clientes");

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

        // Método auxiliar para obtener vendedores
        private async Task<List<Vendedor>> ObtenerVendedores()
        {
            var response = await _httpClient.GetAsync("Pedidos/Vendedores");
            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                return JsonSerializer.Deserialize<List<Vendedor>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                }) ?? new List<Vendedor>();
            }

            return new List<Vendedor>();
        }

        // Obtener un cliente por ID
        private async Task<Cliente> ObtenerClientePorId(long clienteId)
        {
            var response = await _httpClient.GetAsync($"Clientes/{clienteId}");

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
            var response = await _httpClient.GetAsync("Stocks");

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
                var pedidoResponse = await _httpClient.GetAsync($"Pedidos/Pedido/{idPedido}");

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
                var lineasResponse = await _httpClient.GetAsync($"Pedidos/{idPedido}/lineas/DescProducto");

                if (!lineasResponse.IsSuccessStatusCode)
                {
                    return PartialView("_Error", $"Error al cargar las líneas del pedido: {lineasResponse.ReasonPhrase}");
                }

                var lineasJson = await lineasResponse.Content.ReadAsStringAsync();
                var lineasPedido = JsonSerializer.Deserialize<List<LineaPedidoConProductoViewModel>>(lineasJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                Cliente cli = await ObtenerClientePorId(pedido.IdCliente);

                // Crear un modelo combinado con los detalles del pedido y las líneas
                var detallePedidoViewModel = new DetallePedidoViewModel
                {
                    Pedido = pedido,
                    LineasPedido = lineasPedido,
                    Cliente = cli,
                };

                return PartialView("_DetallePedido", detallePedidoViewModel);
            }
            catch (Exception ex)
            {
                return PartialView("_Error", $"Ocurrió un error al cargar el detalle del pedido: {ex.Message}");
            }
        }





    }

}
