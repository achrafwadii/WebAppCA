using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    public class AddDoorModel
    {
        [Required(ErrorMessage = "Le nom de la porte est requis")]
        [Display(Name = "Nom du point d'accès")]
        [StringLength(48, ErrorMessage = "Le nom ne peut pas dépasser 48 caractères")]
        public string DoorName { get; set; }

        [Required(ErrorMessage = "L'équipement est requis")]
        [Display(Name = "Équipement")]
        public int? DeviceID { get; set; }

        [Required(ErrorMessage = "Le numéro de port est requis")]
        [Display(Name = "Port")]
        [Range(0, 255, ErrorMessage = "Le port doit être entre 0 et 255")]
        public int? PortNumber { get; set; }

        [Display(Name = "Description")]
        [StringLength(255, ErrorMessage = "La description ne peut pas dépasser 255 caractères")]
        public string Description { get; set; }
    }
}