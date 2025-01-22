namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class ModificarPedidoModel
    {
        public Pedido Pedidos { get; set; }
        public Cliente ClienteSeleccionado { get; set; }
        public List<LineaPedido> ProductoSeleccionados { get; set; }
        public List<Producto> Productos { get; set; }
    }
}
