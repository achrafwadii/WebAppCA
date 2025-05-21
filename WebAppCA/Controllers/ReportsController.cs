using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
            // Configure EPPlus pour l'utilisation non-commerciale
            ExcelPackage.LicenseContext = OfficeOpenXml.LicenseContext.NonCommercial;
        }

        [HttpGet]
        public IActionResult Index()
        {
            return View("~/Views/Home/Reports.cshtml");
        }

        #region CSV Reports

        [HttpPost]
        public IActionResult DownloadUsersReport()
        {
            var users = _context.Utilisateurs.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom;Prénom;Email;Département;Position;Date de création");

            foreach (var user in users)
            {
                sb.AppendLine($"{user.Id};{user.Nom};{user.Prenom};{user.Email};{user.Departement};{user.Position};{user.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Utilisateurs.csv");
        }

        [HttpPost]
        public IActionResult DownloadDevicesReport()
        {
            var devices = _context.Devices.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom du Dispositif;IP;Port;Description;Statut;Dernière Connexion");

            foreach (var device in devices)
            {
                sb.AppendLine($"{device.DeviceID};{device.DeviceName};{device.IPAddress};{device.Port};{device.Description};{device.Status};{device.LastConnectionTime:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Dispositifs.csv");
        }

        [HttpPost]
        public IActionResult DownloadAccessPointsReport()
        {
            var points = _context.PointsAcces.ToList();

            var sb = new StringBuilder();
            sb.AppendLine("ID;Nom du Point d'Accès;Description;État;Date de Création");

            foreach (var point in points)
            {
                var etat = point.EstVerrouille ? "Verrouillé" : "Déverrouillé";
                sb.AppendLine($"{point.Id};{point.Nom};{point.Description};{etat};{point.CreatedAt:yyyy-MM-dd HH:mm}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_PointsAcces.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAccessReport(string dateRange, DateTime? startDate, DateTime? endDate)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate);

            var sb = new StringBuilder();
            sb.AppendLine("ID;Utilisateur;Point d'Accès;Date;Heure Entrée;Heure Sortie;Durée");

            foreach (var pointage in pointages)
            {
                string duree = pointage.Duree.HasValue ?
                    String.Format("{0}h{1:00}", (int)pointage.Duree.Value.TotalHours, pointage.Duree.Value.Minutes) :
                    "";

                sb.AppendLine($"{pointage.Id};{pointage.Utilisateur.FullName};{pointage.PointAcces.Nom};" +
                    $"{pointage.Date:yyyy-MM-dd};{pointage.HeureEntree:HH:mm};" +
                    $"{(pointage.HeureSortie.HasValue ? pointage.HeureSortie.Value.ToString("HH:mm") : "")};{duree}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Acces.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPresenceReport(string dateRange, DateTime? startDate, DateTime? endDate, int? utilisateurId)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate, utilisateurId);

            // Regrouper par utilisateur et par date
            var presenceData = pointages
                .GroupBy(p => new { p.UtilisateurId, p.Date })
                .Select(g => new
                {
                    UtilisateurId = g.Key.UtilisateurId,
                    UtilisateurNom = g.First().Utilisateur.FullName,
                    Date = g.Key.Date,
                    HeureEntree = g.Min(p => p.HeureEntree),
                    HeureSortie = g.Max(p => p.HeureSortie),
                    TotalDuree = g.Sum(p => p.Duree.HasValue ? p.Duree.Value.TotalMinutes : 0)
                })
                .ToList();

            var sb = new StringBuilder();
            sb.AppendLine("Utilisateur;Date;Heure Entrée;Heure Sortie;Durée Totale");

            foreach (var item in presenceData)
            {
                TimeSpan totalDuree = TimeSpan.FromMinutes(item.TotalDuree);
                string dureeFormatted = String.Format("{0}h{1:00}", (int)totalDuree.TotalHours, totalDuree.Minutes);

                sb.AppendLine($"{item.UtilisateurNom};{item.Date:yyyy-MM-dd};{item.HeureEntree:HH:mm};" +
                    $"{(item.HeureSortie.HasValue ? item.HeureSortie.Value.ToString("HH:mm") : "")};{dureeFormatted}");
            }

            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/csv", "Rapport_Presence.csv");
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllCsvReports()
        {
            // Créer un fichier ZIP contenant tous les rapports CSV
            using (var memoryStream = new MemoryStream())
            {
                using (var archive = new System.IO.Compression.ZipArchive(memoryStream, System.IO.Compression.ZipArchiveMode.Create, true))
                {
                    // Rapport utilisateurs
                    var users = _context.Utilisateurs.ToList();
                    var usersEntry = archive.CreateEntry("Rapport_Utilisateurs.csv");
                    using (var entryStream = usersEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        writer.WriteLine("ID;Nom;Prénom;Email;Département;Position;Date de création");
                        foreach (var user in users)
                        {
                            writer.WriteLine($"{user.Id};{user.Nom};{user.Prenom};{user.Email};{user.Departement};{user.Position};{user.CreatedAt:yyyy-MM-dd HH:mm}");
                        }
                    }

                    // Rapport dispositifs
                    var devices = _context.Devices.ToList();
                    var devicesEntry = archive.CreateEntry("Rapport_Dispositifs.csv");
                    using (var entryStream = devicesEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        writer.WriteLine("ID;Nom du Dispositif;IP;Port;Description;Statut;Dernière Connexion");
                        foreach (var device in devices)
                        {
                            writer.WriteLine($"{device.DeviceID};{device.DeviceName};{device.IPAddress};{device.Port};{device.Description};{device.Status};{device.LastConnectionTime:yyyy-MM-dd HH:mm}");
                        }
                    }

                    // Rapport points d'accès
                    var points = _context.PointsAcces.ToList();
                    var pointsEntry = archive.CreateEntry("Rapport_PointsAcces.csv");
                    using (var entryStream = pointsEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        writer.WriteLine("ID;Nom du Point d'Accès;Description;État;Date de Création");
                        foreach (var point in points)
                        {
                            var etat = point.EstVerrouille ? "Verrouillé" : "Déverrouillé";
                            writer.WriteLine($"{point.Id};{point.Nom};{point.Description};{etat};{point.CreatedAt:yyyy-MM-dd HH:mm}");
                        }
                    }

                    // Rapport des pointages
                    var pointages = await _context.Pointages
                        .Include(p => p.Utilisateur)
                        .Include(p => p.PointAcces)
                        .ToListAsync();

                    var pointagesEntry = archive.CreateEntry("Rapport_Acces.csv");
                    using (var entryStream = pointagesEntry.Open())
                    using (var writer = new StreamWriter(entryStream, Encoding.UTF8))
                    {
                        writer.WriteLine("ID;Utilisateur;Point d'Accès;Date;Heure Entrée;Heure Sortie;Durée");
                        foreach (var pointage in pointages)
                        {
                            string duree = pointage.Duree.HasValue ?
                                String.Format("{0}h{1:00}", (int)pointage.Duree.Value.TotalHours, pointage.Duree.Value.Minutes) :
                                "";

                            writer.WriteLine($"{pointage.Id};{pointage.Utilisateur.FullName};{pointage.PointAcces.Nom};" +
                                $"{pointage.Date:yyyy-MM-dd};{pointage.HeureEntree:HH:mm};" +
                                $"{(pointage.HeureSortie.HasValue ? pointage.HeureSortie.Value.ToString("HH:mm") : "")};{duree}");
                        }
                    }
                }

                memoryStream.Position = 0;
                return File(memoryStream.ToArray(), "application/zip", "Rapports_TimeTrack.zip");
            }
        }

        #endregion

        #region Excel Reports

        [HttpPost]
        public IActionResult DownloadUsersExcel()
        {
            var users = _context.Utilisateurs.ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Utilisateurs");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Nom";
                worksheet.Cells[1, 3].Value = "Prénom";
                worksheet.Cells[1, 4].Value = "Email";
                worksheet.Cells[1, 5].Value = "Département";
                worksheet.Cells[1, 6].Value = "Position";
                worksheet.Cells[1, 7].Value = "Date de création";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                int row = 2;
                foreach (var user in users)
                {
                    worksheet.Cells[row, 1].Value = user.Id;
                    worksheet.Cells[row, 2].Value = user.Nom;
                    worksheet.Cells[row, 3].Value = user.Prenom;
                    worksheet.Cells[row, 4].Value = user.Email;
                    worksheet.Cells[row, 5].Value = user.Departement;
                    worksheet.Cells[row, 6].Value = user.Position;
                    worksheet.Cells[row, 7].Value = user.CreatedAt;
                    worksheet.Cells[row, 7].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                    row++;
                }

                // Auto-ajuster les colonnes
                worksheet.Cells.AutoFitColumns();

                // Générer le fichier
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rapport_Utilisateurs.xlsx");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAccessExcel(string dateRange, DateTime? startDate, DateTime? endDate)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate);

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Accès");

                // En-têtes
                worksheet.Cells[1, 1].Value = "ID";
                worksheet.Cells[1, 2].Value = "Utilisateur";
                worksheet.Cells[1, 3].Value = "Point d'Accès";
                worksheet.Cells[1, 4].Value = "Date";
                worksheet.Cells[1, 5].Value = "Heure Entrée";
                worksheet.Cells[1, 6].Value = "Heure Sortie";
                worksheet.Cells[1, 7].Value = "Durée";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                int row = 2;
                foreach (var pointage in pointages)
                {
                    worksheet.Cells[row, 1].Value = pointage.Id;
                    worksheet.Cells[row, 2].Value = pointage.Utilisateur.FullName;
                    worksheet.Cells[row, 3].Value = pointage.PointAcces.Nom;
                    worksheet.Cells[row, 4].Value = pointage.Date;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[row, 5].Value = pointage.HeureEntree;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "hh:mm";

                    if (pointage.HeureSortie.HasValue)
                    {
                        worksheet.Cells[row, 6].Value = pointage.HeureSortie.Value;
                        worksheet.Cells[row, 6].Style.Numberformat.Format = "hh:mm";
                    }

                    if (pointage.Duree.HasValue)
                    {
                        worksheet.Cells[row, 7].Value = pointage.Duree.Value.TotalHours;
                        worksheet.Cells[row, 7].Style.Numberformat.Format = "[h]:mm";
                    }

                    row++;
                }

                // Auto-ajuster les colonnes
                worksheet.Cells.AutoFitColumns();

                // Générer le fichier
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rapport_Acces.xlsx");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadPresenceExcel(string dateRange, DateTime? startDate, DateTime? endDate, int? utilisateurId)
        {
            var pointages = await GetPointagesWithFilters(dateRange, startDate, endDate, utilisateurId);

            // Regrouper par utilisateur et par date
            var presenceData = pointages
                .GroupBy(p => new { p.UtilisateurId, p.Date })
                .Select(g => new
                {
                    UtilisateurId = g.Key.UtilisateurId,
                    UtilisateurNom = g.First().Utilisateur.FullName,
                    Date = g.Key.Date,
                    HeureEntree = g.Min(p => p.HeureEntree),
                    HeureSortie = g.Max(p => p.HeureSortie),
                    TotalDuree = g.Sum(p => p.Duree.HasValue ? p.Duree.Value.TotalMinutes : 0)
                })
                .ToList();

            using (var package = new ExcelPackage())
            {
                var worksheet = package.Workbook.Worksheets.Add("Présence");

                // En-têtes
                worksheet.Cells[1, 1].Value = "Utilisateur";
                worksheet.Cells[1, 2].Value = "Date";
                worksheet.Cells[1, 3].Value = "Heure Entrée";
                worksheet.Cells[1, 4].Value = "Heure Sortie";
                worksheet.Cells[1, 5].Value = "Durée Totale";

                // Style des en-têtes
                using (var range = worksheet.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                int row = 2;
                foreach (var item in presenceData)
                {
                    worksheet.Cells[row, 1].Value = item.UtilisateurNom;
                    worksheet.Cells[row, 2].Value = item.Date;
                    worksheet.Cells[row, 2].Style.Numberformat.Format = "yyyy-mm-dd";
                    worksheet.Cells[row, 3].Value = item.HeureEntree;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "hh:mm";

                    if (item.HeureSortie.HasValue)
                    {
                        worksheet.Cells[row, 4].Value = item.HeureSortie.Value;
                        worksheet.Cells[row, 4].Style.Numberformat.Format = "hh:mm";
                    }

                    worksheet.Cells[row, 5].Value = TimeSpan.FromMinutes(item.TotalDuree).TotalHours;
                    worksheet.Cells[row, 5].Style.Numberformat.Format = "[h]:mm";

                    row++;
                }

                // Récapitulatif par utilisateur
                worksheet = package.Workbook.Worksheets.Add("Récapitulatif");

                worksheet.Cells[1, 1].Value = "Utilisateur";
                worksheet.Cells[1, 2].Value = "Jours de présence";
                worksheet.Cells[1, 3].Value = "Heures totales";
                worksheet.Cells[1, 4].Value = "Moyenne quotidienne";

                using (var range = worksheet.Cells[1, 1, 1, 4])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                var recap = presenceData
                    .GroupBy(p => p.UtilisateurId)
                    .Select(g => new
                    {
                        UtilisateurNom = g.First().UtilisateurNom,
                        JoursPresence = g.Count(),
                        HeuresTotal = g.Sum(p => p.TotalDuree),
                        MoyenneQuotidienne = g.Average(p => p.TotalDuree)
                    })
                    .OrderBy(r => r.UtilisateurNom)
                    .ToList();

                row = 2;
                foreach (var item in recap)
                {
                    worksheet.Cells[row, 1].Value = item.UtilisateurNom;
                    worksheet.Cells[row, 2].Value = item.JoursPresence;
                    worksheet.Cells[row, 3].Value = TimeSpan.FromMinutes(item.HeuresTotal).TotalHours;
                    worksheet.Cells[row, 3].Style.Numberformat.Format = "[h]:mm";
                    worksheet.Cells[row, 4].Value = TimeSpan.FromMinutes(item.MoyenneQuotidienne).TotalHours;
                    worksheet.Cells[row, 4].Style.Numberformat.Format = "[h]:mm";
                    row++;
                }

                // Auto-ajuster les colonnes
                worksheet.Cells.AutoFitColumns();

                // Générer le fichier
                var stream = new MemoryStream();
                package.SaveAs(stream);
                stream.Position = 0;

                return File(stream, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "Rapport_Presence.xlsx");
            }
        }

        [HttpPost]
        public async Task<IActionResult> DownloadAllExcelReports()
        {
            using (var package = new ExcelPackage())
            {
                // Feuille Utilisateurs
                var users = _context.Utilisateurs.ToList();
                var wsUsers = package.Workbook.Worksheets.Add("Utilisateurs");

                // En-têtes
                wsUsers.Cells[1, 1].Value = "ID";
                wsUsers.Cells[1, 2].Value = "Nom";
                wsUsers.Cells[1, 3].Value = "Prénom";
                wsUsers.Cells[1, 4].Value = "Email";
                wsUsers.Cells[1, 5].Value = "Département";
                wsUsers.Cells[1, 6].Value = "Date de création";

                // Style des en-têtes
                using (var range = wsUsers.Cells[1, 1, 1, 6])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                // Données
                int row = 2;
                foreach (var user in users)
                {
                    wsUsers.Cells[row, 1].Value = user.Id;
                    wsUsers.Cells[row, 2].Value = user.Nom;
                    wsUsers.Cells[row, 3].Value = user.Prenom;
                    wsUsers.Cells[row, 4].Value = user.Email;
                    wsUsers.Cells[row, 5].Value = user.Departement;
                    wsUsers.Cells[row, 6].Value = user.CreatedAt;
                    wsUsers.Cells[row, 6].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                    row++;
                }
                wsUsers.Cells.AutoFitColumns();

                // Feuille Points d'Accès
                var points = _context.PointsAcces.ToList();
                var wsPoints = package.Workbook.Worksheets.Add("Points d'Accès");

                wsPoints.Cells[1, 1].Value = "ID";
                wsPoints.Cells[1, 2].Value = "Nom";
                wsPoints.Cells[1, 3].Value = "Description";
                wsPoints.Cells[1, 4].Value = "État";
                wsPoints.Cells[1, 5].Value = "Date de création";

                using (var range = wsPoints.Cells[1, 1, 1, 5])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                row = 2;
                foreach (var point in points)
                {
                    wsPoints.Cells[row, 1].Value = point.Id;
                    wsPoints.Cells[row, 2].Value = point.Nom;
                    wsPoints.Cells[row, 3].Value = point.Description;
                    wsPoints.Cells[row, 4].Value = point.EstVerrouille ? "Verrouillé" : "Déverrouillé";
                    wsPoints.Cells[row, 5].Value = point.CreatedAt;
                    wsPoints.Cells[row, 5].Style.Numberformat.Format = "yyyy-mm-dd hh:mm";
                    row++;
                }
                wsPoints.Cells.AutoFitColumns();

                // Feuille Accès (derniers 100 pointages)
                var pointages = await _context.Pointages
                    .Include(p => p.Utilisateur)
                    .Include(p => p.PointAcces)
                    .OrderByDescending(p => p.DateHeure)
                    .Take(100)
                    .ToListAsync();

                var wsAcces = package.Workbook.Worksheets.Add("Derniers Accès");

                wsAcces.Cells[1, 1].Value = "ID";
                wsAcces.Cells[1, 2].Value = "Utilisateur";
                wsAcces.Cells[1, 3].Value = "Point d'Accès";
                wsAcces.Cells[1, 4].Value = "Date";
                wsAcces.Cells[1, 5].Value = "Heure Entrée";
                wsAcces.Cells[1, 6].Value = "Heure Sortie";
                wsAcces.Cells[1, 7].Value = "Durée";

                using (var range = wsAcces.Cells[1, 1, 1, 7])
                {
                    range.Style.Font.Bold = true;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);
                }

                row = 2;
                foreach (var pointage in pointages)
                {
                    wsAcces.Cells[row, 1].Value = pointage.Id;
                    wsAcces.Cells[row, 2].Value = pointage.Utilisateur.FullName;
                    wsAcces.Cells[row, 3].Value = pointage.PointAcces.Nom;
                    wsAcces.Cells[row, 4].Value = pointage.Date;
                    wsAcces.Cells[row, 4].Style.Numberformat.Format = "yyyy-mm-dd";
                    wsAcces.Cells[row, 5].Value = pointage.HeureEntree;
                    wsAcces.Cells[row, 5].Style.Numberformat.Format = "hh:mm";

                    if (pointage.HeureSortie.HasValue)
                    {
                        wsAcces.Cells[row, 6].Value = pointage.HeureSortie.Value;
                        wsAcces.Cells[row, 6].Style.Numberformat.Format = "hh:mm";
                    }

                    if (pointage.Duree.HasValue)
                    {
                        wsAcces.Cells[row, 7].Value = pointage.Duree.Value.TotalHours;
                        wsAcces.Cells[row, 7].Style.Numberformat.Format = "[h]:mm";
                    }

                    row++;
                }
                wsAcces.Cells.AutoFitColumns();

                // Feuille de statistiques
                var wsStats = package.Workbook.Worksheets.Add("Statistiques");

                wsStats.Cells[1, 1].Value = "Métrique";
                wsStats.Cells[1, 2].Value = "Valeur";

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