namespace Front_End_Gestion_Pedidos.Models
{
    public class DatosContacto
    {
        public int Id { get; set; }
        public string Alias { get; set; }
        public string Direccion { get; set; }
        public string NumeroTelefono { get; set; }
        public string Comentario { get; set; }

        // Relación con Ciudad (N:1)
        public int CiudadId { get; set; }
        //public Ciudad Ciudad { get; set; }

        // Relación con Cliente (N:1)
        public long ClienteId { get; set; }
        //public ClienteGP Cliente { get; set; }
    }
}
