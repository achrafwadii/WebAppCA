using System;

namespace WebAppCA.Data
{
    // Énumérations extraites du fichier door.proto

    /// <summary>
    /// Représente les différents types de drapeaux de porte
    /// </summary>
    public enum DoorFlag
    {
        /// <summary>Aucun drapeau</summary>
        NONE = 0x00,

        /// <summary>Porte contrôlée par un planning</summary>
        SCHEDULED = 0x01,

        /// <summary>Contrôle d'urgence</summary>
        EMERGENCY = 0x02,

        /// <summary>Contrôle par un opérateur</summary>
        OPERATOR = 0x04
    }

    /// <summary>
    /// Représente les différents types d'alarmes pouvant être déclenchées
    /// </summary>
    public enum AlarmFlag
    {
        /// <summary>Aucune alarme</summary>
        NO_ALARM = 0x00,

        /// <summary>Ouverture forcée</summary>
        FORCED_OPEN = 0x01,

        /// <summary>Porte maintenue ouverte</summary>
        HELD_OPEN = 0x02,

        /// <summary>Violation APB (Anti-PassBack)</summary>
        APB_VIOLATION = 0x04
    }

    /// <summary>
    /// Options pour l'authentification double sur les appareils
    /// </summary>
    public enum DualAuthDevice
    {
        /// <summary>Aucun appareil pour l'auth double</summary>
        DUAL_AUTH_NO_DEVICE = 0x00,

        /// <summary>Auth double sur l'appareil d'entrée seulement</summary>
        DUAL_AUTH_ENTRY_DEVICE_ONLY = 0x01,

        /// <summary>Auth double sur l'appareil de sortie seulement</summary>
        DUAL_AUTH_EXIT_DEVICE_ONLY = 0x02,

        /// <summary>Auth double sur les deux appareils</summary>
        DUAL_AUTH_BOTH_DEVICE = 0x03
    }

    /// <summary>
    /// Types d'authentification double
    /// </summary>
    public enum DualAuthType
    {
        /// <summary>Pas d'authentification double</summary>
        DUAL_AUTH_NONE = 0x00,

        /// <summary>Authentification double avec le dernier utilisateur</summary>
        DUAL_AUTH_LAST = 0x01
    }
}