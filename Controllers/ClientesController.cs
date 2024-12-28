using Front_End_Gestion_Pedidos.Helpers;
using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ClientesController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IHttpClientFactory _httpClientFactory;

        public ClientesController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClientFactory = httpClientFactory;
        }
             
        public async Task<IActionResult> Index()
        {
            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Llamar al método asincrónico para obtener clientes
            var clientes = await ObtenerClientes(); // Asegúrate de usar await para resolver la tarea

            return View(clientes); // Pasar la lista de clientes a la vista
        }

        public async Task<IActionResult> Clientes()
        {
            var model = new PedidoViewModel();
            // Recuperar datos de la sesión y asignarlos al ViewBag
            //string? token = HttpContext.Session.GetString("token");

            // Verifica si la sesión está activa
            //if (token == null)
            //{
            //    return RedirectToAction("login", "account"); // redirige al login si no hay sesión activa
            //}

            var clientes = await ObtenerClientes();
            if (clientes != null)
            {
                model.Clientes = clientes.ToList();
            }
            else
            {
                model.Clientes = new List<Cliente>();
            }

            return View(model);
        }

        private async Task<IEnumerable<Cliente>> ObtenerClientes()
        {
            var model = new PedidoViewModel();
            List<Cliente> clientes1 = new List<Cliente>();

            var client = _httpClientFactory.CreateClient();
            var response = await client.GetAsync("https://localhost:7078/api/v1/Clientes");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                var clientes = JsonSerializer.Deserialize<List<Cliente>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                model.Clientes = clientes ?? new List<Cliente>();

                clientes1 = model.Clientes;
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener los clientes.");
                return null;
            }

            return clientes1;
        }



        //private List<Cliente> ObtenerClientes()
        //{
        //    // Simulación de datos
        //    return new List<Cliente>
        //    {
        //        new Cliente { Id = 1, Nombre = "Carlos Pérez", Email = "carlos@example.com", Telefono = "123456789" },
        //        new Cliente { Id = 2, Nombre = "Ana López", Email = "ana@example.com", Telefono = "987654321" },
        //        new Cliente { Id = 3, Nombre = "Juan García", Email = "juan@example.com", Telefono = "456789123" },
        //        new Cliente { Id = 4, Nombre = "Marta Díaz", Email = "marta@example.com", Telefono = "654987321" },
        //        new Cliente { Id = 5, Nombre = "Luis Gómez", Email = "luis@example.com", Telefono = "789123654" },
        //        new Cliente { Id = 6, Nombre = "Elena Ruiz", Email = "elena@example.com", Telefono = "321456987" },
        //    };
        //}

        //[HttpGet]
        //public IActionResult BuscarClientes(string searchTerm)
        //{
        //    var clientes = ObtenerClientes();

        //    // Filtrado
        //    if (!string.IsNullOrEmpty(searchTerm))
        //    {
        //        clientes = clientes.Where(c =>
        //            c.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            c.Telefono.Contains(searchTerm)).ToList();
        //    }

        //    ViewBag.SearchTerm = searchTerm; // Para mantener el término de búsqueda en la vista

        //    return View("Index", clientes);
        //}


        //public IActionResult Clientes(string searchTerm, int page = 1)
        //{
        //    int pageSize = 5;
        //    var clientes = ObtenerClientes();

        //    // Filtrado
        //    if (!string.IsNullOrEmpty(searchTerm))
        //    {
        //        clientes = clientes.Where(c =>
        //            c.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            c.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
        //            c.Telefono.Contains(searchTerm)).ToList();
        //    }

        //    // Paginación
        //    var pagedClientes = clientes
        //        .Skip((page - 1) * pageSize)
        //        .Take(pageSize)
        //        .ToList();

        //    ViewBag.SearchTerm = searchTerm;
        //    ViewBag.CurrentPage = page;
        //    ViewBag.TotalPages = (int)Math.Ceiling(clientes.Count / (double)pageSize);

        //    return View(pagedClientes);
        //}


    }


}



