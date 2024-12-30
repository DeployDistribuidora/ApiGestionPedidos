namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class ClienteViewModel
    {
        public string SearchTerm { get; set; } // Filtro de búsqueda
        public long ClienteSeleccionadoId { get; set; } // ID del cliente seleccionado
        public List<Cliente> Clientes { get; set; } = new List<Cliente>(); // Lista de clientes
        public List<DatosContacto> DatosContacto { get; set; } = new List<DatosContacto>(); // Datos de contacto del cliente seleccionado
    }


}
