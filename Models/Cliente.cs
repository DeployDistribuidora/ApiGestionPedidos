﻿namespace Front_End_Gestion_Pedidos.Models
{
    public class Cliente
    {
        public long NroCliente { get; set; }
        public string NombreCliente { get; set; } // Máximo de 255 caracteres
        public string DirCliente { get; set; } // Campo nullable
        public string TelefCliente { get; set; } // Campo nullable
        public List<DatosContacto> ContactosCliente { get; set; }
    }
}