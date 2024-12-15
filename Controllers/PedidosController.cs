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
    }
}
