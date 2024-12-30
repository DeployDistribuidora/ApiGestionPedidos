using System.ComponentModel.DataAnnotations;

namespace Front_End_Gestion_Pedidos.Models.ViewModel
{
    public class LoginDataModel
    {

        [Required(ErrorMessage = "El nombre de usuario es obligatorio")]
        [Display(Name = "Usuario")]
        public string Username { get; set; }

        [Required(ErrorMessage = "La contraseña es obligatoria")]
        [DataType(DataType.Password)]
        [Display(Name = "Contraseña")]
        public string Password { get; set; }


    }



}
