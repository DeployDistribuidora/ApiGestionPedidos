namespace Front_End_Gestion_Pedidos.Models
{
    public class Ciudad
    {
        public int Id { get; set; }
        public string Nombre { get; set; }
        public int OrdenPrioridadReparto { get; set; }

        // Relación con Departamento (N:1)
        public int DepartamentoId { get; set; }
        //public Departamento Departamento { get; set; }
    }
}
