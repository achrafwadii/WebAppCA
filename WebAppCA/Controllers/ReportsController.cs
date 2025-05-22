using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using WebAppCA.Data;
using WebAppCA.Models;

namespace WebAppCA.Controllers
{
    public class ReportsController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ReportsController(ApplicationDbContext context)
        {
            _context = context;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Reports/Reports.cshtml");
        }

        #region CSV Reports

        [HttpPost]
        public async Task<IActionResult> DownloadUsersReport()
        {
            var users = await _context.Utilisateurs.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Nom,Prenom,Email,Departement,Position,DateCreation");

            foreach (var user in users)
            {
                csv.AppendLine($"{user.Id},{EscapeCsv(user.Nom)},{EscapeCsv(user.Prenom)},{EscapeCsv(user.Email)},{EscapeCsv(user.Departement)},{EscapeCsv(user.Position)},{user.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rapport_utilisateurs.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadDevicesReport()
        {
            var devices = await _context.Devices.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,NomDispositif,AdresseIP,Port,Description,Statut,DerniereConnexion");

            foreach (var device in devices)
            {
                csv.AppendLine($"{device.DeviceID},{EscapeCsv(device.DeviceName)},{EscapeCsv(device.IPAddress)},{device.Port},{EscapeCsv(device.Description)},{EscapeCsv(device.Status)},{device.LastConnectionTime:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rapport_dispositifs.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAccessPointsReport()
        {
            var points = await _context.PointsAcces.ToListAsync();

            var csv = new StringBuilder();
            csv.AppendLine("ID,Nom,Description,Etat,DateCreation");

            foreach (var point in points)
            {
                var etat = point.EstVerrouille ? "Verrouille" : "Deverrouille";
                csv.AppendLine($"{point.Id},{EscapeCsv(point.Nom)},{EscapeCsv(point.Description)},{etat},{point.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rapport_points_acces.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAccessReport(string dateRange = "last7days", DateTime? startDate = null, DateTime? endDate = null)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate);

            var csv = new StringBuilder();
            csv.AppendLine("ID,Utilisateur,PointAcces,Date,HeureEntree,HeureSortie,Duree");

            foreach (var pointage in pointages)
            {
                var duree = pointage.Duree?.TotalHours.ToString("F2") ?? "";
                csv.AppendLine($"{pointage.Id},{EscapeCsv(pointage.Utilisateur.FullName)},{EscapeCsv(pointage.PointAcces.Nom)},{pointage.Date:yyyy-MM-dd},{pointage.HeureEntree:HH:mm},{pointage.HeureSortie?.ToString("HH:mm") ?? ""},{duree}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rapport_acces.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPresenceReport(string dateRange = "last7days", DateTime? startDate = null, DateTime? endDate = null, int? utilisateurId = null)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate, utilisateurId);

            // Regrouper par utilisateur et par date
            var presenceData = pointages
                .GroupBy(p => new { p.UtilisateurId, p.Date })
                .Select(g => new
                {
                    UtilisateurNom = g.First().Utilisateur.FullName,
                    Date = g.Key.Date,
                    HeureEntree = g.Min(p => p.HeureEntree),
                    HeureSortie = g.Max(p => p.HeureSortie),
                    TotalDuree = g.Sum(p => p.Duree?.TotalHours ?? 0)
                })
                .OrderBy(p => p.UtilisateurNom)
                .ThenBy(p => p.Date)
                .ToList();

            var csv = new StringBuilder();
            csv.AppendLine("Utilisateur,Date,HeureEntree,HeureSortie,DureeTotale");

            foreach (var item in presenceData)
            {
                csv.AppendLine($"{EscapeCsv(item.UtilisateurNom)},{item.Date:yyyy-MM-dd},{item.HeureEntree:HH:mm},{item.HeureSortie?.ToString("HH:mm") ?? ""},{item.TotalDuree:F2}");
            }

            return File(Encoding.UTF8.GetBytes(csv.ToString()), "text/csv", "rapport_presence.csv");
        }

        #endregion

        #region JSON Reports

        [HttpPost]
        public async Task<IActionResult> GetStatisticsJson()
        {
            var stats = await GetStatisticsAsync();
            var json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
            return File(Encoding.UTF8.GetBytes(json), "application/json", "statistiques.json");
        }

        [HttpPost]
        public async Task<IActionResult> GetUsersJson()
        {
            var users = await _context.Utilisateurs
                .Select(u => new
                {
                    u.Id,
                    u.Nom,
                    u.Prenom,
                    u.Email,
                    u.Departement,
                    u.Position,
                    CreatedAt = u.CreatedAt.ToString("yyyy-MM-dd HH:mm")
                })
                .ToListAsync();

            var json = JsonSerializer.Serialize(users, new JsonSerializerOptions { WriteIndented = true });
            return File(Encoding.UTF8.GetBytes(json), "application/json", "utilisateurs.json");
        }

        #endregion

        #region HTML Reports

        [HttpPost]
        public async Task<IActionResult> GenerateHtmlReport(string reportType = "summary")
        {
            var html = new StringBuilder();
            html.AppendLine("<!DOCTYPE html>");
            html.AppendLine("<html lang='fr'>");
            html.AppendLine("<head>");
            html.AppendLine("<meta charset='UTF-8'>");
            html.AppendLine("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
            html.AppendLine("<title>Rapport TimeTrack</title>");
            html.AppendLine("<style>");
            html.AppendLine("body { font-family: Arial, sans-serif; margin: 20px; }");
            html.AppendLine("table { width: 100%; border-collapse: collapse; margin: 20px 0; }");
            html.AppendLine("th, td { border: 1px solid #ddd; padding: 8px; text-align: left; }");
            html.AppendLine("th { background-color: #f2f2f2; }");
            html.AppendLine(".header { text-align: center; margin-bottom: 30px; }");
            html.AppendLine(".stats { display: flex; justify-content: space-around; margin: 20px 0; }");
            html.AppendLine(".stat-card { text-align: center; padding: 15px; border: 1px solid #ddd; border-radius: 5px; }");
            html.AppendLine("</style>");
            html.AppendLine("</head>");
            html.AppendLine("<body>");

            html.AppendLine("<div class='header'>");
            html.AppendLine("<h1>Rapport TimeTrack</h1>");
            html.AppendLine($"<p>Généré le {DateTime.Now:dd/MM/yyyy à HH:mm}</p>");
            html.AppendLine("</div>");

            if (reportType == "summary" || reportType == "all")
            {
                var stats = await GetStatisticsAsync();
                html.AppendLine("<div class='stats'>");
                html.AppendLine($"<div class='stat-card'><h3>{stats.UserCount}</h3><p>Utilisateurs</p></div>");
                html.AppendLine($"<div class='stat-card'><h3>{stats.DeviceCount}</h3><p>Dispositifs</p></div>");
                html.AppendLine($"<div class='stat-card'><h3>{stats.DoorCount}</h3><p>Points d'accès</p></div>");
                html.AppendLine($"<div class='stat-card'><h3>{stats.TodayAccessCount}</h3><p>Accès aujourd'hui</p></div>");
                html.AppendLine("</div>");
            }

            if (reportType == "users" || reportType == "all")
            {
                var users = await _context.Utilisateurs.ToListAsync();
                html.AppendLine("<h2>Utilisateurs</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>ID</th><th>Nom</th><th>Prénom</th><th>Email</th><th>Département</th></tr>");

                foreach (var user in users)
                {
                    html.AppendLine($"<tr><td>{user.Id}</td><td>{user.Nom}</td><td>{user.Prenom}</td><td>{user.Email}</td><td>{user.Departement}</td></tr>");
                }
                html.AppendLine("</table>");
            }

            if (reportType == "access" || reportType == "all")
            {
                var pointages = await _context.Pointages
                    .Include(p => p.Utilisateur)
                    .Include(p => p.PointAcces)
                    .Where(p => p.Date >= DateTime.Today.AddDays(-7))
                    .OrderByDescending(p => p.DateHeure)
                    .Take(50)
                    .ToListAsync();

                html.AppendLine("<h2>Derniers Accès (7 derniers jours)</h2>");
                html.AppendLine("<table>");
                html.AppendLine("<tr><th>Utilisateur</th><th>Point d'Accès</th><th>Date</th><th>Heure</th><th>Durée</th></tr>");

                foreach (var pointage in pointages)
                {
                    var duree = pointage.Duree?.TotalHours.ToString("F1") + "h" ?? "-";
                    html.AppendLine($"<tr><td>{pointage.Utilisateur.FullName}</td><td>{pointage.PointAcces.Nom}</td><td>{pointage.Date:dd/MM/yyyy}</td><td>{pointage.HeureEntree:HH:mm}</td><td>{duree}</td></tr>");
                }
                html.AppendLine("</table>");
            }

            html.AppendLine("</body>");
            html.AppendLine("</html>");

            return File(Encoding.UTF8.GetBytes(html.ToString()), "text/html", $"rapport_{reportType}_{DateTime.Now:yyyyMMdd}.html");
        }

        #endregion

        #region API Endpoints pour AJAX

        [HttpGet]
        public async Task<JsonResult> GetDashboardStats()
        {
            var stats = await GetStatisticsAsync();
            return Json(stats);
        }

        [HttpGet]
        public async Task<JsonResult> GetRecentAccess(int count = 10)
        {
            var pointages = await _context.Pointages
                .Include(p => p.Utilisateur)
                .Include(p => p.PointAcces)
                .OrderByDescending(p => p.DateHeure)
                .Take(count)
                .Select(p => new
                {
                    p.Id,
                    Utilisateur = p.Utilisateur.FullName,
                    PointAcces = p.PointAcces.Nom,
                    Date = p.Date.ToString("dd/MM/yyyy"),
                    Heure = p.HeureEntree.ToString("HH:mm"),
                    Duree = p.Duree.HasValue ? $"{p.Duree.Value.TotalHours:F1}h" : "-"
                })
                .ToListAsync();

            return Json(pointages);
        }

        #endregion

        #region Helper Methods

        private string EscapeCsv(string field)
        {
            if (string.IsNullOrEmpty(field))
                return "";

                using (var range = wsStats.Cells[1, 1, 1, 2])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                var stats = await GetStatisticsAsync();

                row = 2;
                wsStats.Cells[row, 1].Value = "Nombre total d'utilisateurs";
                wsStats.Cells[row, 2].Value = stats.UserCount;
                row++;

                wsStats.Cells[row, 1].Value = "Nombre total de dispositifs";
                wsStats.Cells[row, 2].Value = stats.DeviceCount;
                row++;

                wsStats.Cells[row, 1].Value = "Nombre total de portes";
                wsStats.Cells[row, 2].Value = stats.DoorCount;
                row++;

                wsStats.Cells[row, 1].Value = "Nombre d'accès aujourd'hui";
                wsStats.Cells[row, 2].Value = stats.TodayAccessCount;

                wsStats.Cells[row, 1].Value = "Nombre d'accès aujourd'hui";
                wsStats.Cells[row, 2].Value = stats.TodayAccessCount;
                row++;

                wsStats.Cells[row, 1].Value = "Utilisateurs actifs (7 jours)";
                wsStats.Cells[row, 2].Value = stats.ActiveUsersLast7Days;
                row++;

                wsStats.Cells[row, 1].Value = "Durée moyenne par accès";
                wsStats.Cells[row, 2].Value = stats.AverageAccessDuration?.TotalHours ?? 0;
                wsStats.Cells[row, 2].Style.Numberformat.Format = "0.00\" h\"";
                row++;

                wsStats.Cells[row, 1].Value = "Point d'accès le plus utilisé";
                wsStats.Cells[row, 2].Value = stats.MostUsedAccessPoint;
                row++;

// Formatage automatique des colonnes
wsStats.Cells.AutoFitColumns();

// Générer le fichier
var stream = new MemoryStream();
package.SaveAs(stream);
stream.Position = 0;

return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rapports_Complets.xlsx");
            }
        }

        private async Task<dynamic> GetStatisticsAsync()
{
    return new
    {
        UserCount = await _context.Utilisateurs.CountAsync(),
        DeviceCount = await _context.Devices.CountAsync(),
        DoorCount = await _context.PointsAcces.CountAsync(),
        TodayAccessCount = await _context.Pointages.CountAsync(p => p.Date == DateTime.Today),
        ActiveUsersLast7Days = await _context.Pointages
            .Where(p => p.Date >= DateTime.Today.AddDays(-7))
            .Select(p => p.UtilisateurId)
            .Distinct()
            .CountAsync(),
        AverageAccessDuration = await _context.Pointages
            .AverageAsync(p => p.Duree.HasValue ? p.Duree.Value.TotalHours : 0),
        MostUsedAccessPoint = await _context.Pointages
            .GroupBy(p => p.PointAccesId)
            .OrderByDescending(g => g.Count())
            .Select(g => g.First().PointAcces.Nom)
            .FirstOrDefaultAsync()
    };
}

#endregion

#region Helpers

private async Task<List<Pointage>> GetPointagesWithFilters(string dateRange, DateTime? startDate, DateTime? endDate, int? utilisateurId = null)
{
    IQueryable<Pointage> query = _context.Pointages
        .Include(p => p.Utilisateur)
        .Include(p => p.PointAcces);

            if (utilisateurId.HasValue)
            {
                query = query.Where(p => p.UtilisateurId == utilisateurId.Value);
            }

    switch (dateRange)
    {
        case "today":
            query = query.Where(p => p.Date == DateTime.Today);
            break;
        case "yesterday":
            query = query.Where(p => p.Date == DateTime.Today.AddDays(-1));
            break;
        case "last7days":
            query = query.Where(p => p.Date >= DateTime.Today.AddDays(-7));
            break;
        case "custom" when startDate.HasValue && endDate.HasValue:
            query = query.Where(p => p.Date >= startDate.Value && p.Date <= endDate.Value);
            break;
    }

    return await query.OrderByDescending(p => p.DateHeure).ToListAsync();
}


        #endregion
    }
}