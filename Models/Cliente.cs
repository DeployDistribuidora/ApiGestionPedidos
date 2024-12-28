namespace Front_End_Gestion_Pedidos.Models
{
    public class Cliente
    {
        public long Nro_Cliente { get; set; }
        public string Nombre_Cliente { get; set; }
        public string? Dir_Cliente { get; set; }
        public string? Telef_Cliente { get; set; }
        public byte Estado { get; set; }
        public byte Intereses { get; set; }
        public byte Seguridad { get; set; }
        public string? Comentarios { get; set; }
        public string? E_mail { get; set; }
        public double? Tope_Credito { get; set; }
        public string? Cedula { get; set; }
        public string? RUT { get; set; }
        public string? Dir2_Cliente { get; set; }
        public string? Ocupacion { get; set; }
        public string? Conyugue { get; set; }
        public byte? E_Civil { get; set; }
        public string? Referencias { get; set; }
        public string? Cedula_Conyugue { get; set; }
        public string? Ciudad { get; set; }
        public DateTime? F_Nacim { get; set; }
        public byte? Por_Contado { get; set; }
        public byte? Por_Debito { get; set; }
        public byte? FacPa { get; set; }
        public byte? FacPa2 { get; set; }
        public string? Celular { get; set; }
        public string? Ciudad_T { get; set; }
        public double? Puntos { get; set; }
        public long? Cuenta_P { get; set; }
    }
}