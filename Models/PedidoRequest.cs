namespace Front_End_Gestion_Pedidos.Models
{
    public class PedidoRequest
    {
        public Pedido Pedido { get; set; }
        public List<LineaPedido> LineasPedido { get; set; }
    }
}
