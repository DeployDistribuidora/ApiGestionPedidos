namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class DetallePedidoViewModel
    {
        public Pedido Pedido { get; set; }
        public List<LineaPedido> LineasPedido { get; set; }
    }
}
