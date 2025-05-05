using System;
using System.ComponentModel.DataAnnotations;

namespace WebAppCA.Models
{
    // Modèle pour les informations d'un équipement (pour l'affichage dans l'UI)
    public class DeviceInfoModel
    {
        public int DeviceID { get; set; }

        [Display(Name = "Nom")]
        public string DeviceName { get; set; }
        
        [Display(Name = "Adresse IP")]
        public string IPAddress { get; set; }
        
        [Display(Name = "Port")]
        public int Port { get; set; }
        
        [Display(Name = "État")]
        public string Status { get; set; }
        
        [Display(Name = "Dernière connexion")]
        public DateTime? LastConnection { get; set; }
    }
}