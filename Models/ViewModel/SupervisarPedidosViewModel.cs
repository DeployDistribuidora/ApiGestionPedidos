using Front_End_Gestion_Pedidos.Models;

namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class SupervisarPedidosViewModel
    {
        public IEnumerable<Pedido> Pedidos { get; set; } = Enumerable.Empty<Pedido>();
        public string Cliente { get; set; } = string.Empty;
        public string Vendedor { get; set; } = string.Empty;
        public string Estado { get; set; } = string.Empty;
        public List<LineaPedido> DetallePedido { get; set; } = new List<LineaPedido>();
        public List<Cliente> Clientes { get; set; } = new List<Cliente>();
    }

}
