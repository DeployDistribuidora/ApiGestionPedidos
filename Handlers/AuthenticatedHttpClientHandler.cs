using Front_End_Gestion_Pedidos.Helpers;
using System.Net.Http.Headers;

namespace Front_End_Gestion_Pedidos.Handlers
{
    public class AuthenticatedHttpClientHandler : DelegatingHandler
    {
        private readonly SessionHelper _sessionHelper;

        public AuthenticatedHttpClientHandler(SessionHelper sessionHelper)
        {
            _sessionHelper = sessionHelper;
        }

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string token = await _sessionHelper.ObtenerTokenJwt();

            if (!string.IsNullOrEmpty(token))
            {
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            }

            return await base.SendAsync(request, cancellationToken);
        }
    }

}
