using System.ComponentModel.DataAnnotations.Schema;

namespace Front_End_Gestion_Pedidos.Models
{
    public class Producto
    {
        public string Codigo { get; set; }
        public string Descripcion { get; set; }
        public string Marca { get; set; }
        public double Stock_Actual { get; set; }
        public double Stock_Minimo { get; set; }
        public string Estante { get; set; }
        public string Procedencia { get; set; }
        public byte Clase { get; set; }
        public byte Moneda { get; set; }
        public byte MonLista { get; set; }
        public byte Tasa { get; set; }
        public double Stock_2 { get; set; }
        public double Stock_3 { get; set; }
        public double Stock_4 { get; set; }
        public double Stock_5 { get; set; }
        public double? Precio { get; set; }
        public double? Costo { get; set; }
        public string? Detalle { get; set; }
        public string? CodBarra { get; set; }
        public double? Porcentaje { get; set; }
        public long? Similar { get; set; }
        public DateTime? F_Compra { get; set; }
        public byte? Estado { get; set; }
    }
}