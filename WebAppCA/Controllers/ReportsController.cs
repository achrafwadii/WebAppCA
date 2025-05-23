using CsvHelper;
using iTextSharp.text;
using iTextSharp.text.pdf;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
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
        }

        public IActionResult Index()
        {
            return View();
        }

        #region User Report
        public async Task<IActionResult> UserReport(string format = "csv")
        {
            var users = await _context.Utilisateurs.ToListAsync();
            return format.ToLower() == "pdf"
                ? GenerateUserPdfReport(users)
                : GenerateUserCsvReport(users);
        }

        private FileResult GenerateUserCsvReport(List<Utilisateur> users)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(users);
            writer.Flush();
            return File(stream.ToArray(), "text/csv", "users_report.csv");
        }

        private FileResult GenerateUserPdfReport(List<Utilisateur> users)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 10, 10, 30, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.PageEvent = new PdfReportHeaderFooter();

            document.Open();
            AddReportTitle(document, "USER MANAGEMENT REPORT", users.Count);

            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new[] { 1, 3, 3, 2, 2 });

            AddTableHeader(table, "ID");
            AddTableHeader(table, "Full Name");
            AddTableHeader(table, "Email");
            AddTableHeader(table, "Department");
            AddTableHeader(table, "Position");

            foreach (var user in users)
            {
                AddTableRow(table, user.Id.ToString());
                AddTableRow(table, user.FullName);
                AddTableRow(table, user.Email);
                AddTableRow(table, user.Departement);
                AddTableRow(table, user.Position);
            }

            document.Add(table);
            document.Close();
            return File(stream.ToArray(), "application/pdf", "users_report.pdf");
        }
        #endregion
        #region Monthly Hours Report
        public async Task<IActionResult> MonthlyHoursReport(string month, string format = "csv")
        {
            if (!DateTime.TryParseExact(month, "yyyy-MM", CultureInfo.InvariantCulture, DateTimeStyles.None, out var selectedMonth))
            {
                return BadRequest("Invalid month format. Use YYYY-MM");
            }

            var startDate = new DateTime(selectedMonth.Year, selectedMonth.Month, 1);
            var endDate = startDate.AddMonths(1).AddDays(-1);

            var query = _context.Pointages
                .Where(p => p.Date >= startDate && p.Date <= endDate && p.HeureSortie.HasValue)
                .Join(_context.Utilisateurs,
                    p => p.UtilisateurId,
                    u => u.Id,
                    (p, u) => new { Pointage = p, Utilisateur = u })
                .GroupBy(x => x.Utilisateur.Id)
                .Select(g => new {
                    UserId = g.Key,
                    FirstRecord = g.First(),
                    TotalSeconds = g.Sum(x =>
                        EF.Functions.DateDiffSecond(
                            x.Pointage.HeureEntree,
                            x.Pointage.HeureSortie.Value
                        )
                    )
                });

            var dbResults = await query.ToListAsync();

            var monthlyData = dbResults.Select(r => new MonthlyHoursEntry
            {
                UserName = $"{r.FirstRecord.Utilisateur.Prenom} {r.FirstRecord.Utilisateur.Nom}",
                MonthYear = startDate.ToString("MMMM yyyy"),
                TotalHours = FormatTotalHours(r.TotalSeconds)
            }).ToList();

            return format.ToLower() == "pdf"
                ? GenerateMonthlyHoursPdfReport(monthlyData, startDate.ToString("MMMM yyyy"))
                : GenerateMonthlyHoursCsvReport(monthlyData);
        }

        private string FormatTotalHours(int totalSeconds)
        {
            var totalHours = totalSeconds / 3600.0;
            var hours = (int)totalHours;
            var minutes = (int)((totalHours - hours) * 60);
            return $"{hours}h {minutes:00}m";
        }
  
        private FileResult GenerateMonthlyHoursCsvReport(List<MonthlyHoursEntry> data)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            return File(stream.ToArray(), "text/csv", "monthly_hours_report.csv");
        }

        private FileResult GenerateMonthlyHoursPdfReport(List<MonthlyHoursEntry> data, string monthYear)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 10, 10, 30, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.PageEvent = new PdfReportHeaderFooter();

            document.Open();
            AddReportTitle(document, $"MONTHLY WORK HOURS REPORT - {monthYear}", data.Count);

            var table = new PdfPTable(3) { WidthPercentage = 100 };
            table.SetWidths(new[] { 3, 2, 3 });

            AddTableHeader(table, "User");
            AddTableHeader(table, "Month");
            AddTableHeader(table, "Total Hours");

            foreach (var entry in data)
            {
                AddTableRow(table, entry.UserName);
                AddTableRow(table, entry.MonthYear);
                AddTableRow(table, entry.TotalHours);
            }

            document.Add(table);
            document.Close();
            return File(stream.ToArray(), "application/pdf", $"monthly_hours_report_{monthYear.Replace(" ", "_")}.pdf");
        }
        #endregion
        #region Presence Report
        public async Task<IActionResult> PresenceReport(DateTime startDate, DateTime endDate, string format = "csv")
{
    var presenceData = await _context.Pointages
        .Include(p => p.Utilisateur)
        .Where(p => p.Date >= startDate && p.Date <= endDate)
        .Select(p => new PresenceReportEntry
        {
            Id = p.Id,
            UserName = p.Utilisateur.FullName,
            Date = p.Date,
            HeureEntree = p.HeureEntree,
            HeureSortie = p.HeureSortie,
            Duration = p.HeureSortie.HasValue
                ? $"{(int)(p.HeureSortie.Value - p.HeureEntree).TotalHours}h {(p.HeureSortie.Value - p.HeureEntree).Minutes}m"
                : "N/A"
        })
        .ToListAsync();

    return format.ToLower() == "pdf"
        ? GeneratePresencePdfReport(presenceData)
        : GeneratePresenceCsvReport(presenceData);
}

        private FileResult GeneratePresenceCsvReport(List<PresenceReportEntry> data)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(data);
            writer.Flush();
            return File(stream.ToArray(), "text/csv", "presence_report.csv");
        }

        private FileResult GeneratePresencePdfReport(List<PresenceReportEntry> data)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 10, 10, 30, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.PageEvent = new PdfReportHeaderFooter();

            document.Open();
            AddReportTitle(document, "PRESENCE TRACKING REPORT", data.Count);

            var table = new PdfPTable(6) { WidthPercentage = 100 };
            table.SetWidths(new[] { 1, 3, 2, 2, 2, 3 });

            AddTableHeader(table, "ID");
            AddTableHeader(table, "User");
            AddTableHeader(table, "Date");
            AddTableHeader(table, "Entry Time");
            AddTableHeader(table, "Exit Time");
            AddTableHeader(table, "Duration"); // New column

            foreach (var entry in data)
            {
                AddTableRow(table, entry.Id.ToString());
                AddTableRow(table, entry.UserName);
                AddTableRow(table, entry.Date.ToString("dd MMM yyyy"));
                AddTableRow(table, entry.HeureEntree.ToString("HH:mm"));
                AddTableRow(table, entry.HeureSortie?.ToString("HH:mm") ?? "N/A");
                AddTableRow(table, entry.Duration);
            }

            document.Add(table);
            document.Close();
            return File(stream.ToArray(), "application/pdf", "presence_report.pdf");
        }
        #endregion

        #region Device Report
        public async Task<IActionResult> DeviceReport(string format = "csv")
        {
            var devices = await _context.Devices
                .Select(d => new DeviceInfoModel
                {
                    DeviceID = d.DeviceID,
                    DeviceName = d.DeviceName,
                    IPAddress = d.IPAddress,
                    Port = d.Port,
                    Status = d.Status,
                    LastConnection = d.LastConnectionTime
                })
                .ToListAsync();

            return format.ToLower() == "pdf"
                ? GenerateDevicePdfReport(devices)
                : GenerateDeviceCsvReport(devices);
        }

        private FileResult GenerateDeviceCsvReport(List<DeviceInfoModel> devices)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(devices);
            writer.Flush();
            return File(stream.ToArray(), "text/csv", "devices_report.csv");
        }

        private FileResult GenerateDevicePdfReport(List<DeviceInfoModel> devices)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 10, 10, 30, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.PageEvent = new PdfReportHeaderFooter();

            document.Open();
            AddReportTitle(document, "DEVICE MANAGEMENT REPORT", devices.Count);

            var table = new PdfPTable(5) { WidthPercentage = 100 };
            table.SetWidths(new[] { 1, 3, 2, 2, 3 });

            AddTableHeader(table, "ID");
            AddTableHeader(table, "Device Name");
            AddTableHeader(table, "IP Address");
            AddTableHeader(table, "Port");
            AddTableHeader(table, "Last Connection");

            foreach (var device in devices)
            {
                AddTableRow(table, device.DeviceID.ToString());
                AddTableRow(table, device.DeviceName);
                AddTableRow(table, device.IPAddress);
                AddTableRow(table, device.Port.ToString());
                AddTableRow(table, device.LastConnection?.ToString("g") ?? "Never");
            }

            document.Add(table);
            document.Close();
            return File(stream.ToArray(), "application/pdf", "devices_report.pdf");
        }
        #endregion
        #region Door Report
        public async Task<IActionResult> PorteReport(string format = "csv")
        {
            var doors = await _context.Doors
                .Select(d => new DoorInfoModel
                {
                    DoorID = (Int32)d.DoorID, // Conversion explicite si nécessaire
                    Name = d.Name,
                    Description = d.Description,
                    Status = d.EstOuverte ? "Open" : "Closed"
                })
                .ToListAsync();

            return format.ToLower() == "pdf"
                ? GeneratePortePdfReport(doors)
                : GeneratePorteCsvReport(doors);
        }

        private FileResult GeneratePorteCsvReport(List<DoorInfoModel> doors)
        {
            using var stream = new MemoryStream();
            using var writer = new StreamWriter(stream);
            using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
            csv.WriteRecords(doors);
            writer.Flush();
            return File(stream.ToArray(), "text/csv", "doors_report.csv");
        }

        private FileResult GeneratePortePdfReport(List<DoorInfoModel> doors)
        {
            using var stream = new MemoryStream();
            var document = new Document(PageSize.A4.Rotate(), 10, 10, 30, 20);
            var writer = PdfWriter.GetInstance(document, stream);
            writer.PageEvent = new PdfReportHeaderFooter();

            document.Open();
            AddReportTitle(document, "DOOR ACCESS REPORT", doors.Count);

            var table = new PdfPTable(4) { WidthPercentage = 100 };
            table.SetWidths(new[] { 1, 3, 3, 2 });

            AddTableHeader(table, "ID");
            AddTableHeader(table, "Door Name");
            AddTableHeader(table, "Location");
            AddTableHeader(table, "Status");

            foreach (var door in doors)
            {
                AddTableRow(table, door.DoorID.ToString());
                AddTableRow(table, door.Name);
                AddTableRow(table, door.Description);
                AddTableRow(table, door.Status);
            }

            document.Add(table);
            document.Close();
            return File(stream.ToArray(), "application/pdf", "doors_report.pdf");
        }
        #endregion

        #region PDF Helpers
        private void AddReportTitle(Document document, string titleText, int recordCount)
        {
            var title = new Paragraph(titleText,
                new Font(Font.FontFamily.HELVETICA, 18, Font.BOLD, BaseColor.DARK_GRAY))
            {
                SpacingAfter = 20f,
                Alignment = Element.ALIGN_CENTER
            };
            document.Add(title);

            var infoTable = new PdfPTable(3)
            {
                WidthPercentage = 70,
                SpacingAfter = 15f
            };
            infoTable.SetWidths(new[] { 2, 3, 2 });

            AddInfoCell(infoTable, "Report Date:", DateTime.Now.ToString("f"));
            AddInfoCell(infoTable, "Total Records:", recordCount.ToString());
            AddInfoCell(infoTable, "Generated By:", User.Identity?.Name ?? "System");

            document.Add(infoTable);
        }

        private void AddInfoCell(PdfPTable table, string label, string value)
        {
            table.AddCell(new Phrase(label,
                new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD)));
            table.AddCell(new Phrase(value,
                new Font(Font.FontFamily.HELVETICA, 10)));
        }

        private void AddTableHeader(PdfPTable table, string header)
        {
            var cell = new PdfPCell(new Phrase(header,
                new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.WHITE)))
            {
                BackgroundColor = new BaseColor(59, 89, 152),
                Padding = 5,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        private void AddTableRow(PdfPTable table, string text, BaseColor color = null)
        {
            var font = color == null
                ? new Font(Font.FontFamily.HELVETICA, 10)
                : new Font(Font.FontFamily.HELVETICA, 10, Font.NORMAL, color);

            var cell = new PdfPCell(new Phrase(text, font))
            {
                Padding = 5,
                HorizontalAlignment = Element.ALIGN_CENTER
            };
            table.AddCell(cell);
        }

        public class PdfReportHeaderFooter : PdfPageEventHelper
        {
            public override void OnEndPage(PdfWriter writer, Document document)
            {
                // Header
                var header = new Phrase("Secure Facility Management System - Confidential",
                    new Font(Font.FontFamily.HELVETICA, 10, Font.BOLD, BaseColor.DARK_GRAY));
                ColumnText.ShowTextAligned(
                    writer.DirectContent,
                    Element.ALIGN_LEFT,
                    header,
                    document.LeftMargin,
                    document.Top + 30,
                    0);

                // Footer
                var footer = new Phrase($"Page {writer.PageNumber} • {DateTime.Now:yyyy-MM-dd HH:mm:ss}",
                    new Font(Font.FontFamily.HELVETICA, 9, Font.ITALIC));
                ColumnText.ShowTextAligned(
                    writer.DirectContent,
                    Element.ALIGN_CENTER,
                    footer,
                    (document.Right - document.Left) / 2 + document.LeftMargin,
                    document.Bottom - 20,
                    0);
            }
        }
        #endregion
    }
}