using Front_End_Gestion_Pedidos.Filters;
using Front_End_Gestion_Pedidos.Helpers;
using Front_End_Gestion_Pedidos.Models;
using Front_End_Gestion_Pedidos.Models.ViewModel;
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

        [RoleAuthorize("Administracion", "Vendedor")]
        public async Task<IActionResult> Index()
        {
            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");

            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            var viewModel = new ClienteViewModel();
            // Recuperar clientes desde la API
            viewModel.Clientes = (await ObtenerClientes())?.ToList() ?? new List<Cliente>();

            return View(viewModel); // Pasar la lista de clientes a la vista
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

        [HttpGet]
        public async Task<IActionResult> BuscarClientes(string searchTerm)
        {
            var viewModel = new ClienteViewModel();

            try
            {
                // Obtener todos los clientes desde la API
                var clientes = await ObtenerClientes();

                // Aplicar el filtro de búsqueda
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var clientesFiltrados = clientes
                        .Where(c => c.NombreCliente.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (clientesFiltrados.Count > 0)
                    {
                        viewModel.Clientes = clientesFiltrados;
                    }
                    else
                    {
                        ViewBag.Alerta = "No se encontraron clientes que coincidan con el término de búsqueda.";
                        viewModel.Clientes = clientes.ToList(); // Mantén la lista completa
                    }
                }
                else
                {
                    viewModel.Clientes = clientes.ToList(); // Si no hay término, muestra todos
                }

                ViewBag.SearchTerm = searchTerm;
            }
            catch (Exception ex)
            {
                viewModel.Clientes = new List<Cliente>();
                ViewBag.Error = $"Error al buscar los clientes: {ex.Message}";
            }

            return View("Index", viewModel);
        }


    }


}



