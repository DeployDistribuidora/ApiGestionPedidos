using Microsoft.AspNetCore.Mvc;
using Front_End_Gestion_Pedidos.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using Front_End_Gestion_Pedidos.Models.ViewModel;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class AccountController : Controller
    {
        private readonly HttpClient _httpClient;
        private readonly HttpClient _noAuthClient;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("PedidosClient");
            _noAuthClient = httpClientFactory.CreateClient("NoAuthClient");
        }

        public IActionResult Login()
        {
            // Si el usuario ya está autenticado, redirigir al Home
            if (HttpContext.Session.GetString("UsuarioLogueado") != null)
            {
                return RedirectToAction("Index", "Home");
            }

            // Limpiar sesión al iniciar login
            HttpContext.Session.Clear();
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDataModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Devuelve la vista si el modelo no es válido.
            }
           
            // Crea el objeto de solicitud
            var loginRequest = new LoginRequest
            {
                Username = model.Username,
                Password = model.Password
            };

            // Serializa el objeto en formato JSON
            var content = new StringContent(
                JsonSerializer.Serialize(loginRequest),
                Encoding.UTF8,
                "application/json"
            );

            try
            {
                // Envía la solicitud POST al backend
                //var response = await _noAuthClient.PostAsync($"/Login/login", content);
                var response = await _noAuthClient.PostAsync("Login/login", content);

                if (response.IsSuccessStatusCode)
                {
                    // Lee y procesa la respuesta
                    var responseContent = await response.Content.ReadAsStringAsync();

                    // Configura las opciones para deserializar ignorando mayúsculas/minúsculas
                    var options = new JsonSerializerOptions
                    {
                        PropertyNameCaseInsensitive = true // Ignorar sensibilidad a mayúsculas/minúsculas
                    };

                    // Deserializa la respuesta en el objeto LoginResponse
                    var jsonResponse = JsonSerializer.Deserialize<LoginResponse>(responseContent, options);

                    if (jsonResponse != null)
                    {
                        HttpContext.Session.SetString("UsuarioLogueado", jsonResponse.User.NombreUsuario);
                        HttpContext.Session.SetString("Role", jsonResponse.User.Rol);
                        HttpContext.Session.SetString("Token", jsonResponse.Token);
                        //HttpContext.Session.SetString("IdUsuario", jsonResponse.User.IdUsuario.ToString());
                        HttpContext.Session.SetInt32("IdUsuario", (int)jsonResponse.User.IdUsuario);


                        // Si el usuario es Cliente, configura ClienteId en la sesión
                        if (jsonResponse.User.Rol == "Cliente")
                        {
                            HttpContext.Session.SetString("ClienteId", jsonResponse.User.IdUsuario.ToString());
                        }
                        if (jsonResponse.User.Rol == "Vendedor")
                        {
                            HttpContext.Session.SetString("VendedorId", jsonResponse.User.IdUsuario.ToString());
                        }


                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        Console.WriteLine("Credenciales inválidas.");
                        ModelState.AddModelError(string.Empty, "Credenciales inválidas. Intente nuevamente.");
                    }
                }
                else
                {
                    // Manejo de errores HTTP
                    var errorContent = await response.Content.ReadAsStringAsync();
                    ModelState.AddModelError(string.Empty, $"Error del servidor: {errorContent}");
                }
            }
            catch (Exception ex)
            {
                // Manejo de excepciones
                ModelState.AddModelError(string.Empty, $"Error inesperado: {ex.Message}");
            }

            return View(model); // Regresa a la vista de login si algo falla.
        }

        public IActionResult Logout()
        {
            // Limpiar sesión y redirigir al Login
            HttpContext.Session.Clear();
            return RedirectToAction("Login");
        }
    }


}
