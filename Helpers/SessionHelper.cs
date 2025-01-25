using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Helpers
{
    public class SessionHelper
    {
       
        private readonly IHttpContextAccessor _httpContextAccessor;
        public SessionHelper(IHttpContextAccessor httpContextAccessor)
        {
            _httpContextAccessor = httpContextAccessor;
        }

        public async Task<string> ObtenerTokenJwt()
        {
            if (_httpContextAccessor.HttpContext == null)
            {
                throw new Exception("HttpContext no está disponible.");
            }

            string token = _httpContextAccessor.HttpContext.Session.GetString("Token");

            if (string.IsNullOrEmpty(token))
            {
                throw new UnauthorizedAccessException("No se ha encontrado un token válido en la sesión.");
            }

            return await Task.FromResult(token);
        }
    

        private const string SessionKey = "UsuarioSesion";

        public static void SetUsuario(HttpContext httpContext, Usuario usuario)
        {
            var usuarioJson = JsonSerializer.Serialize(usuario);
            httpContext.Session.SetString(SessionKey, usuarioJson);
        }

        public static Usuario GetUsuario(HttpContext httpContext)
        {
            var usuarioJson = httpContext.Session.GetString(SessionKey);
            return string.IsNullOrEmpty(usuarioJson)
                ? null
                : JsonSerializer.Deserialize<Usuario>(usuarioJson);
        }

        public static void ClearUsuario(HttpContext httpContext)
        {
            httpContext.Session.Remove(SessionKey);
        }

       
    }
}
