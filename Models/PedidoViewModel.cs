using System.Collections.Generic;


namespace Front_End_Gestion_Pedidos.Models
{
    public class PedidoViewModel
    {
        public List<Cliente> Clientes { get; set; }
        public List<Direccion> Direcciones { get; set; }
        public List<MedioPago> MediosPago { get; set; }
        public List<Producto> Productos { get; set; }
        public string Comentarios { get; set; }
        public decimal Total { get; set; }
        public Cliente ClienteSeleccionado { get; set; }

    }
}
