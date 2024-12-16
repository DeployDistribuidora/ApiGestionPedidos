using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class PedidosController : Controller
    {
        public IActionResult NuevoPedido()
        {
            var model = new PedidoViewModel
            {
                Clientes = new List<Cliente>
                {
                    new Cliente { Id = 1, Nombre = "Carlos Pérez" },
                    new Cliente { Id = 2, Nombre = "Ana López" }
                },
                Direcciones = new List<Direccion>
                {
                    new Direccion { Id = 1, Descripcion = "Calle Falsa 123" },
                    new Direccion { Id = 2, Descripcion = "Av. Siempre Viva 456" }
                },
                MediosPago = new List<MedioPago>
                {
                    new MedioPago { Id = 1, Nombre = "Efectivo" },
                    new MedioPago { Id = 2, Nombre = "Tarjeta de Crédito" }
                },
                Productos = new List<Producto>(),
                Comentarios = "",
                Total = 0
            };

            return View(model);
        }

        [HttpPost]
        public IActionResult RealizarPedido(PedidoViewModel pedido)
        {
            if (ModelState.IsValid)
            {
                // Procesar el pedido aquí
                return RedirectToAction("Confirmacion");
            }
            return View("NuevoPedido", pedido);
        }

        public IActionResult Confirmacion()
        {
            return View();
        }


        //public IActionResult BuscarPedidos(string cliente, string vendedor, DateTime? fechaInicio, DateTime? fechaFin)
        //{
        //    var pedidos = ObtenerPedidos();

        //    // Filtros
        //    if (!string.IsNullOrEmpty(cliente))
        //        pedidos = pedidos.Where(p => p.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();

        //    if (!string.IsNullOrEmpty(vendedor))
        //        pedidos = pedidos.Where(p => p.Vendedor.Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();

        //    if (fechaInicio.HasValue && fechaFin.HasValue)
        //        pedidos = pedidos.Where(p => p.Fecha >= fechaInicio && p.Fecha <= fechaFin).ToList();

        //    return View(pedidos);
        //}

        //public IActionResult Detalle(int id)
        //{
        //    // Obtener los detalles del pedido
        //    var pedido = ObtenerPedidos().FirstOrDefault(p => p.Id == id);

        //    if (pedido == null)
        //        return NotFound("Pedido no encontrado");

        //    return PartialView("_DetallePedido", pedido); // Renderiza una PartialView con los detalles
        //}


        //[HttpPost]
        //public IActionResult Aprobar(int id)
        //{
        //    // Lógica para aprobar el pedido
        //    TempData["Mensaje"] = $"Pedido #{id} aprobado exitosamente.";
        //    return RedirectToAction("BuscarPedidos");
        //}

        //[HttpPost]
        //public IActionResult Cancelar(int id)
        //{
        //    // Lógica para cancelar el pedido
        //    TempData["Mensaje"] = $"Pedido #{id} cancelado.";
        //    return RedirectToAction("BuscarPedidos");
        //}



        private List<Pedido> ObtenerPedidos()
        {
            return new List<Pedido>
    {
        new Pedido
        {
            Id = 1,
            Cliente = "Dorado",
            Vendedor = "Claudio",
            Estado = "Pendiente",
            Fecha = DateTime.Now.AddDays(-1),
            Productos = new List<Producto>
            {
                new Producto { Id = 1, Cantidad = 2, Precio = 50, Stock = 10 },
                new Producto { Id = 2, Cantidad = 1, Precio = 100, Stock = 5 }
            }
        },
        new Pedido
        {
            Id = 2,
            Cliente = "Juan",
            Vendedor = "Carlos",
            Estado = "Completado",
            Fecha = DateTime.Now.AddDays(-5),
            Productos = new List<Producto>
            {
                new Producto { Id = 3, Cantidad = 4, Precio = 25, Stock = 20 }
            }
        }
    };
        }


        public IActionResult SupervisarPedidos(string cliente, string vendedor, string estado)
        {
            var pedidos = ObtenerPedidos();

            // Aplicar filtros
            if (!string.IsNullOrEmpty(cliente))
                pedidos = pedidos.Where(p => p.Cliente.Contains(cliente, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrEmpty(vendedor))
                pedidos = pedidos.Where(p => p.Vendedor.Contains(vendedor, StringComparison.OrdinalIgnoreCase)).ToList();
            if (!string.IsNullOrEmpty(estado))
                pedidos = pedidos.Where(p => p.Estado == estado).ToList();

            return View(pedidos);
        }

        public IActionResult DetallePedido(int id)
        {
            var pedido = ObtenerPedidos().FirstOrDefault(p => p.Id == id);
            var productos = ObtenerProductosPorPedido(id);

            var viewModel = new PedidoDetalleViewModel
            {
                Pedido = pedido,
                Productos = productos,
                Comentarios = new List<string>() // Simulación de comentarios
            };

            return PartialView("_DetallePedido", viewModel);
        }

        [HttpPost]
        public IActionResult AprobarPedido(int id)
        {
            TempData["Mensaje"] = $"Pedido {id} aprobado.";
            return RedirectToAction("SupervisarPedidos");
        }

        [HttpPost]
        public IActionResult CancelarPedido(int id)
        {
            TempData["Mensaje"] = $"Pedido {id} cancelado.";
            return RedirectToAction("SupervisarPedidos");
        }

        private List<Producto> ObtenerProductosPorPedido(int pedidoId)
        {
            // Simulación de productos del pedido
            return new List<Producto>
    {
        new Producto { Id = 1, Cantidad = 2, Precio = 50, Stock = 10 },
        new Producto { Id = 2, Cantidad = 1, Precio = 30, Stock = 20 }
    };
        }

    }


        


    }
