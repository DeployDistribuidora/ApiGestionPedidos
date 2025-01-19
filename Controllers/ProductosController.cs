using Front_End_Gestion_Pedidos.Filters;
using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ProductosController : Controller
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly HttpClient _httpClient;

        public ProductosController(IHttpContextAccessor httpContextAccessor, IHttpClientFactory httpClientFactory)
        {
            _httpContextAccessor = httpContextAccessor;
            _httpClient = httpClientFactory.CreateClient("PedidosClient");
        }

        [RoleAuthorize("Administracion", "Vendedor", "Cliente")]
        public async Task<IActionResult> Index()
        {

            IEnumerable<Producto> productos = new List<Producto>();
            // Recuperar usuario desde la sesión
            var usuarioLogueado = _httpContextAccessor.HttpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = _httpContextAccessor.HttpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado))
                return RedirectToAction("Login", "Account");

            // Obtener todos los productos
            productos = await ObtenerProductos();

            // Retornar la vista con todos los productos
            return View(productos);
        }

        [HttpGet]
        public async Task<IActionResult> BuscarProductos(string searchTerm)
        {
            try
            {
                // Obtener todos los productos desde la API
                var productos = await ObtenerProductos();

                // Filtro de búsqueda
                if (!string.IsNullOrEmpty(searchTerm))
                {
                    var productosFiltrados = productos
                        .Where(p => p.Descripcion.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    if (productosFiltrados.Count > 0)
                    {
                        return View("Index", productosFiltrados);
                    }
                    else
                    {
                        ViewBag.Alerta = "No se encontraron productos que coincidan con el término de búsqueda.";
                        return View("Index", productos);
                    }
                }

                return View("Index", productos);
                 
            }
            catch (Exception ex)
            {
                ViewBag.Error = $"Error al buscar productos: {ex.Message}";
                return View("Index", new List<Producto>());
            }
        }


        private async Task<IEnumerable<Producto>> ObtenerProductos()
        {
            //var model = new PedidoViewModel();
            IEnumerable<Producto> productosResponse = new List<Producto>();

            var response = await _httpClient.GetAsync($"/api/Stocks");

            if (response.IsSuccessStatusCode)
            {
                var jsonResponse = await response.Content.ReadAsStringAsync();
                productosResponse = JsonSerializer.Deserialize<List<Producto>>(jsonResponse, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                //model.Pedidos = pedidosResponse ?? new List<Pedido>();
                //pedidos = model.Pedidos;
            }
            else
            {
                // Manejar errores en la solicitud a la API
                ModelState.AddModelError(string.Empty, "Error al obtener los productos.");
                return null;
            }

            return productosResponse;
        }


    }
}
