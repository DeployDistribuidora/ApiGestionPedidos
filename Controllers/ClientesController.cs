using Front_End_Gestion_Pedidos.Helpers;
using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ClientesController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;

        public ClientesController(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }
        public IActionResult Index()
        {

            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            //ViewBag.Username = usuario.Username;
            //ViewBag.Role = usuario.Role;

            //Llamar a simulación de datos
            var clientes = ObtenerClientes();

            return View(clientes);
        }


        private List<Cliente> ObtenerClientes()
        {
            // Simulación de datos
            return new List<Cliente>
            {
                new Cliente { Id = 1, Nombre = "Carlos Pérez", Email = "carlos@example.com", Telefono = "123456789" },
                new Cliente { Id = 2, Nombre = "Ana López", Email = "ana@example.com", Telefono = "987654321" },
                new Cliente { Id = 3, Nombre = "Juan García", Email = "juan@example.com", Telefono = "456789123" },
                new Cliente { Id = 4, Nombre = "Marta Díaz", Email = "marta@example.com", Telefono = "654987321" },
                new Cliente { Id = 5, Nombre = "Luis Gómez", Email = "luis@example.com", Telefono = "789123654" },
                new Cliente { Id = 6, Nombre = "Elena Ruiz", Email = "elena@example.com", Telefono = "321456987" },
            };
        }

        public IActionResult Clientes(string searchTerm, int page = 1)
        {
            int pageSize = 5;
            var clientes = ObtenerClientes();

            // Filtrado
            if (!string.IsNullOrEmpty(searchTerm))
            {
                clientes = clientes.Where(c =>
                    c.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    c.Telefono.Contains(searchTerm)).ToList();
            }

            // Paginación
            var pagedClientes = clientes
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList();

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.TotalPages = (int)Math.Ceiling(clientes.Count / (double)pageSize);

            return View(pagedClientes);
        }
    }


}



