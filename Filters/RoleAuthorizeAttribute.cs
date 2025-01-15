using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Front_End_Gestion_Pedidos.Filters
{
    public class RoleAuthorizeAttribute : ActionFilterAttribute
    {
        private readonly string[] _roles;

        public RoleAuthorizeAttribute(params string[] roles)
        {
            _roles = roles;
        }

        public override void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;

            // Verifica si el usuario tiene sesión activa
            var usuarioLogueado = httpContext.Session.GetString("UsuarioLogueado");
            var rolUsuario = httpContext.Session.GetString("Role");

            if (string.IsNullOrEmpty(usuarioLogueado) || string.IsNullOrEmpty(rolUsuario))
            {
                // Si no está logueado, redirige al Login
                context.Result = new RedirectToActionResult("Login", "Account", null);
                return;
            }

            // Verifica si el rol del usuario tiene acceso
            if (!_roles.Contains(rolUsuario))
            {
                // Si el rol no tiene permiso, redirige al Home
                context.Result = new RedirectToActionResult("Index", "Home", null);
                return;
            }

            base.OnActionExecuting(context);
        }
    }
}