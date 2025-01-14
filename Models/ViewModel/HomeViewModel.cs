using System.Collections.Generic;

namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class HomeViewModel
    {
        public string Username { get; set; }
        public string UserRole { get; set; }
        public List<AlertMessage> Messages { get; set; }
    }

    public class AlertMessage
    {
        public string Type { get; set; } // Tipo de alerta (success, info, danger, etc.)
        public string Icon { get; set; } // Clase del ícono de Bootstrap
        public string Content { get; set; } // Contenido del mensaje
    }
}
