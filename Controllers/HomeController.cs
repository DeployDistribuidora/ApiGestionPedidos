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
            // Verifica si hay un token en la sesi�n
            var token = HttpContext.Session.GetString("Token");
            if (string.IsNullOrEmpty(token))
            {
                // Si no hay token, redirige al Login
                return RedirectToAction("Login", "Account");
            }

            // Configura datos de sesi�n en ViewBag
            ViewBag.IsLoggedIn = true; // Indica que el usuario est� logueado
            ViewBag.UserRole = HttpContext.Session.GetString("Role") ?? "Sin Rol";
            ViewBag.Username = HttpContext.Session.GetString("UsuarioLogueado") ?? "Invitado";

            return View();
        }


        //public IActionResult Privacy()
        //{
        //    // La p�gina Privacy no requiere autenticaci�n
        //    return View();
        //}

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
