using Microsoft.AspNetCore.Mvc;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ClientesController : Controller
    {
        public IActionResult Clientes()
        {
            // Recuperar datos de la sesión y asignarlos al ViewBag
            ViewBag.IsLoggedIn = HttpContext.Session.GetString("IsLoggedIn") == "true";
            ViewBag.Username = HttpContext.Session.GetString("UsuarioLogueado");
            ViewBag.UserRole = HttpContext.Session.GetString("Role");

            // Verifica si la sesión está activa
            if (!ViewBag.IsLoggedIn)
            {
                return RedirectToAction("Login", "Account"); // Redirige al login si no hay sesión activa
            }

            return View();
        }
    }


}



