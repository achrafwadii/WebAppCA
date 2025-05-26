using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace WebAppCA.Models
{
    public class DoorStatusModel
    {
        [Key]
        public int DoorID { get; set; }

        [Required]
        public bool IsOpen { get; set; }

        [Required]
        public bool IsUnlocked { get; set; }

        [Required]
        public bool HeldOpen { get; set; }

        public int AlarmFlags { get; set; }

        // Navigation property vers DoorInfoModel si nécessaire
        [ForeignKey("DoorID")]
        public virtual DoorInfoModel? Door { get; set; }

        // Propriétés calculées pour compatibilité avec votre contrôleur existant
        [NotMapped]
        public bool IsLocked
        {
            get => !IsUnlocked;
            set => IsUnlocked = !value;
        }

        [NotMapped]
        public DateTime LastModified { get; set; } = DateTime.Now;

        [NotMapped]
        public string? ChangeReason { get; set; }

        // Propriétés utilitaires pour l'affichage
        [NotMapped]
        public string StatusText => IsLocked ? "Verrouillée" : "Déverrouillée";

        [NotMapped]
        public string StatusColor => IsLocked ? "danger" : "success";

        [NotMapped]
        public string DoorStateText
        {
            get
            {
                if (HeldOpen) return "Maintenue ouverte";
                if (IsOpen) return "Ouverte";
                return "Fermée";
            }
        }

        [NotMapped]
        public string AlarmStatusText
        {
            get
            {
                if (AlarmFlags == 0) return "Aucune alarme";
                return $"Alarme active (Code: {AlarmFlags})";
            }
        }

        [NotMapped]
        public bool HasAlarm => AlarmFlags > 0;
    }
}