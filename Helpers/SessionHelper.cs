using Front_End_Gestion_Pedidos.Models;
using Microsoft.AspNetCore.Http;
using System.Text.Json;

namespace Front_End_Gestion_Pedidos.Helpers
{
    public static class SessionHelper
    {
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
