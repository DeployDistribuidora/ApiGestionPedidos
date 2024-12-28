namespace Front_End_Gestion_Pedidos.Models
{
    public class Producto
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public double Stock_Actual { get; set; }
        public double? Precio { get; set; }
    }
}