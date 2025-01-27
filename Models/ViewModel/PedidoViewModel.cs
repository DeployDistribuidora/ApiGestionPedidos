using System.Collections.Generic;


namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class PedidoViewModel
    {
        public List<Cliente> Clientes { get; set; } = new List<Cliente>();
        public List<string> Direcciones { get; set; } = new List<string>();
        public List<MedioPago> MediosPago { get; set; }
        public List<Producto> Productos { get; set; }
        public List<LineaPedido> ProductosSeleccionados { get; set; }
        public string Comentarios { get; set; }
        public decimal Total { get; set; }
        public Cliente ClienteSeleccionado { get; set; }
        public string MensajeError { get; set; } // Para errores
        
    }
}
