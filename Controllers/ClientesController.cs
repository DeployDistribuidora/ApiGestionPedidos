using Microsoft.AspNetCore.Mvc;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ClientesController : Controller
    {
        public IActionResult Clientes()
        {
            return View();
        }
    }
}
