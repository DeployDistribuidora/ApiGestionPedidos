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

            // Obtén el rol del usuario y el nombre de la sesión
            var userRole = HttpContext.Session.GetString("Role");
            var username = HttpContext.Session.GetString("Username");

            // Envía los datos necesarios a la vista usando ViewBag
            ViewBag.UserRole = userRole;
            ViewBag.Username = username;

            return View();
        }

        public IActionResult Privacy()
        {
            // La página Privacy no requiere autenticación
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
