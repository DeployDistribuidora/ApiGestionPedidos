using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using System.Diagnostics;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            // Verifica si hay un token en la sesión
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                // Si no hay token, redirige al Login
                return RedirectToAction("Login", "Account");
            }

            // Configura datos de sesión en ViewBag
            ViewBag.IsLoggedIn = true; // Indica que el usuario está logueado
            ViewBag.UserRole = HttpContext.Session.GetString("Role") ?? "Sin Rol";
            ViewBag.Username = HttpContext.Session.GetString("UsuarioLogueado") ?? "Invitado";

            return View();
        }


        //public IActionResult Privacy()
        //{
        //    // La página Privacy no requiere autenticación
        //    return View();
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
