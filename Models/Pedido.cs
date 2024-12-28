namespace Front_End_Gestion_Pedidos.Models
{
    public class Pedido
    {
        public int IdPedido { get; set; }
        public long? IdVendedor { get; set; }
        public long IdCliente { get; set; }
        public long IdAdministracion { get; set; }
        public long? IdSupervisor { get; set; }
        public DateTime FechaCreacion { get; set; }
        public DateTime? FechaEntrega { get; set; }
        public DateTime? FechaEntregado { get; set; }
        public string Estado { get; set; }
        public int? IdContacto { get; set; }
        public string MetodoPago { get; set; }
        public decimal Total { get; set; }
        public string Comentarios { get; set; }
        public long? TarjetaID { get; set; }
        public List<LineaPedido> LineasPedido { get; set; }
    }
}