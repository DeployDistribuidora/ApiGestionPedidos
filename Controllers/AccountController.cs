using Microsoft.AspNetCore.Mvc;
using Front_End_Gestion_Pedidos.Models;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using Microsoft.AspNetCore.Http;

namespace Front_End_Gestion_Pedidos.Controllers
{
    public class AccountController : Controller
    {
        private readonly IHttpClientFactory _httpClientFactory;

        public AccountController(IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
        }

        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Login(LoginDataModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model); // Devuelve la vista si el modelo no es válido.
            }

            var client = _httpClientFactory.CreateClient();

            // URL del Backend-API

            var url = "https://localhost:7078/api/Login/login";
            //var url = "https://apigestionpedidos-fxbafbb8b0htapdr.canadacentral-01.azurewebsites.net/api/Login/login";
                       
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
                var response = await client.PostAsync(url, content);

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

                    //if (jsonResponse?.Message == "OK")
                    if (!string.IsNullOrEmpty(jsonResponse?.Token))
                    {
                        Console.WriteLine("TOKEN");
                        // Guardar en sesión y redirigir
                        //HttpContext.Session.SetString("UsuarioLogueado", model.Username);
                        HttpContext.Session.SetString("Token", jsonResponse.Token);
                        return RedirectToAction("Index", "Home");
                    }
                    else
                    {
                        Console.WriteLine("Credenciales inválidas.");
                        ModelState.AddModelError(string.Empty, "Credenciales inválidas.");
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
            HttpContext.Session.Clear(); // Limpia la sesión
            return RedirectToAction("Login");
        }
    }


}
