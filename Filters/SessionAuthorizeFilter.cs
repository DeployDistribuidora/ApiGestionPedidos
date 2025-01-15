using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Front_End_Gestion_Pedidos.Filters
{
    public class SessionAuthorizeFilter : IActionFilter
    {
        public void OnActionExecuting(ActionExecutingContext context)
        {
            var httpContext = context.HttpContext;
            var session = httpContext.Session;

            // Excluir acciones específicas del filtro
            var excludedActions = new[] { "Login", "Logout" };
            var controllerName = context.RouteData.Values["controller"]?.ToString();
            var actionName = context.RouteData.Values["action"]?.ToString();

            if (excludedActions.Contains(actionName))
            {
                return;
            }

            // Verificar si el usuario está logueado
            if (string.IsNullOrEmpty(session.GetString("UsuarioLogueado")))
            {
                context.Result = new RedirectToActionResult("Login", "Account", null);
            }
        }

        public void OnActionExecuted(ActionExecutedContext context)
        {
            // No es necesario implementar nada aquí
        }
    }
}
