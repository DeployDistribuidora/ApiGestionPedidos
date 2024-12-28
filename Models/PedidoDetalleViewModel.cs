using Front_End_Gestion_Pedidos.Models;

namespace Front_End_Gestion_Pedidos.Models
{
    public class PedidoDetalleViewModel
    {
        public Pedido Pedido { get; set; }
        public List<Producto> Productos { get; set; }
        public List<string> Comentarios { get; set; }
    }
}


