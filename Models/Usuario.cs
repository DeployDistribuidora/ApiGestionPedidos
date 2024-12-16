namespace Front_End_Gestion_Pedidos.Models
{
    public class Usuario
    {
        public string Username { get; set; }
        public string Role { get; set; }
        public string Token { get; set; }
        public bool IsLoggedIn => !string.IsNullOrEmpty(Username) && !string.IsNullOrEmpty(Token);
    }
}
