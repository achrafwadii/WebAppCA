namespace WebAppCA.Models
{
    // Models/User.cs
    public class Useer
    {
        public string Username { get; set; }
        public string Email { get; set; }
        public string PasswordHash { get; set; } // Toujours stocker des mots de passe hashés
    }
}
