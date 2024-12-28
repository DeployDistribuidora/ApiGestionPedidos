namespace Front_End_Gestion_Pedidos.Models
{
    public class Producto
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int Cantidad { get; set; }
        public decimal Precio { get; set; }
        public int Stock { get; set; }
        public decimal Total => Precio * Cantidad;
    }
}
