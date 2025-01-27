namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class HistorialPedidosViewModel
    {
        public IEnumerable<Pedido> Pedidos { get; set; }
        public List<Cliente> Clientes { get; set; } // Lista de clientes para el desplegable
        public List<Vendedor> Vendedores { get; set; }
    }
}
