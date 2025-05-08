namespace WebAppCA.Models
{
    // Modèle pour le statut d'une porte
    public class DoorStatusModel
    {
        public uint DoorID { get; set; }
        public bool IsOpen { get; set; }
        public bool IsUnlocked { get; set; }
        public bool HeldOpen { get; set; }
        public uint AlarmFlags { get; set; }

        public string StatusText => IsUnlocked ? "Déverrouillée" : "Verrouillée";
        public string StateText => IsOpen ? "Ouverte" : "Fermée";
        public string AlarmText
        {
            get
            {
                if (AlarmFlags == 0) return "Aucune";
                var result = string.Empty;

                if ((AlarmFlags & 0x01) != 0) result += "Ouverture forcée, ";
                if ((AlarmFlags & 0x02) != 0) result += "Maintenue ouverte, ";
                if ((AlarmFlags & 0x04) != 0) result += "Violation APB, ";

                return result.TrimEnd(' ', ',');
            }
        }
    }
}
