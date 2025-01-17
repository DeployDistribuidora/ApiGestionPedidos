namespace Front_End_Gestion_Pedidos.Models
{
    public class PedidoUpdateModel
    {
        public int? IdAdministracion { get; set; }
        public int? IdSupervisor { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaEntregado { get; set; }
        public string Comentarios { get; set; }

        public string Estado { get; set; }

        // Campos no editables que pueden ser usados para validaciones
        public int? IdCliente { get; } // Solo lectura
        public DateTime? FechaCreacion { get; } // Solo lectura
    }
}
