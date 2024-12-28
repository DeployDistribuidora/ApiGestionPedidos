namespace Front_End_Gestion_Pedidos.Models
{
    public class LineaPedido
    {
        public int IdPedido { get; set; }
        public int IdLineaPedido { get; set; }
        public string Codigo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioUnitario { get; set; }
        public decimal Subtotal { get; set; }
    }
}