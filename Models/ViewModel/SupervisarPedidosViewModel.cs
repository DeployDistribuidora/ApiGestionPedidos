using Front_End_Gestion_Pedidos.Models;

namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class SupervisarPedidosViewModel
    {
        public IEnumerable<Pedido> Pedidos { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string Estado { get; set; }
        public List<LineaPedido> DetallePedido { get; set; } // Nueva propiedad

        public List<Cliente> Clientes { get; set; } // Lista de clientes para el desplegable
    }

}
