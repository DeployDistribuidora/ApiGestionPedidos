namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class PedidoParaMetricasViewModel
    {
        public Pedido Pedido { get; set; }
        public List<LineaPedidoConProductoViewModel> lineaPedidoConProducto { get; set; }
    }
}
