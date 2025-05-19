using Microsoft.AspNetCore.Mvc;
using System.Text;
using WebAppCA.Data;
using System.Linq;

namespace WebAppCA.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpPost]
        public IActionResult DownloadUsersReport()
        {
            var users = _context.Utilisateurs.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom;Prénom;Email;Date de création");

            foreach (var user in users)
            {
                sb.AppendLine($"{user.Id};{user.Nom};{user.Prenom};{user.Email};{user.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Utilisateurs.csv");
        }

        [HttpPost]
        public IActionResult DownloadDevicesReport()
        {
            var devices = _context.Devices.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom du Dispositif;Description;Statut;Dernière Connexion");

            foreach (var device in devices)
            {
                sb.AppendLine($"{device.DeviceID};{device.DeviceName};{device.Description};{device.Status};{device.LastConnectionTime:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Dispositifs.csv");
        }

        [HttpPost]
        public IActionResult DownloadAccessPointsReport()
        {
            var points = _context.PointsAcces.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom du Point d’Accès;Date de Création");

            foreach (var point in points)
            {
                sb.AppendLine($"{point.Id};{point.Nom};{point.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_PointsAcces.csv");
        }
    }
}
