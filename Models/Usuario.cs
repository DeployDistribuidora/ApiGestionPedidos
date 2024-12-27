using System.ComponentModel.DataAnnotations;

namespace Front_End_Gestion_Pedidos.Models
{
    public class Usuario
    {
        //public long Id { get; set; }
        //public string Username { get; set; }
        //public string Role { get; set; }

        public long IdUsuario { get; set; }
        public string Rol { get; set; }
        public string NombreUsuario { get; set; }
        public string Password { get; set; }

        //public string Token { get; set; }
        //public bool IsLoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Token);
    }
}
