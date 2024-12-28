namespace Front_End_Gestion_Pedidos.Models
{
    public class Pedido
    {
        public int Id { get; set; }
        public string Cliente { get; set; }
        public string Vendedor { get; set; }
        public string Estado { get; set; }
        public DateTime Fecha { get; set; }
        public List<Producto> Productos { get; set; } // Asegúrate de incluir esto
    }
}
