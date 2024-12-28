using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Mvc;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class ProductosController : Controller
    {
        public IActionResult Index()
        {
            // Obtener todos los productos
            var productos = ObtenerProductos();

            // Retornar la vista con todos los productos
            return View(productos);
        }

        [HttpGet]
        public IActionResult BuscarProductos(string searchTerm)
        {
            // Obtener todos los productos
            var productos = ObtenerProductos();

            // Filtrar los productos según el término de búsqueda
            if (!string.IsNullOrEmpty(searchTerm))
            {
                productos = productos.Where(p =>
                    p.Nombre.Contains(searchTerm, StringComparison.OrdinalIgnoreCase)).ToList();
            }

            ViewBag.SearchTerm = searchTerm; // Mantener el término de búsqueda en la vista

            // Retornar la vista con los productos filtrados
            return View("Index", productos);
        }

        private List<Producto> ObtenerProductos()
        {
            // Simulación de datos
            return new List<Producto>
            {
                new Producto { Id = 1, Nombre = "Papel higénico", Stock = 100, Precio = 25.50m },
                new Producto { Id = 2, Nombre = "Jabón", Stock = 50, Precio = 15.00m },
                new Producto { Id = 3, Nombre = "Pata dental", Stock = 0, Precio = 30.75m },
                new Producto { Id = 4, Nombre = "Esponjas", Stock = 200, Precio = 10.00m },
                new Producto { Id = 5, Nombre = "Escobas", Stock = 300, Precio = 50.00m }
            };
        }
    }
}
