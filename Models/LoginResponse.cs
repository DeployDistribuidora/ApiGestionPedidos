using Front_End_Gestion_Pedidos.Models;

namespace Front_End_Gestion_Pedidos.Models
{
    public class LoginResponse
    {
        public string Token { get; set; }
        public Usuario User { get; set; }
    }

}

