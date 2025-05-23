namespace WebAppCA.Models
{
    public class PresenceReportEntry
    {
        public int Id { get; set; }
        public string UserName { get; set; }
        public DateTime Date { get; set; }
        public DateTime HeureEntree { get; set; }
        public DateTime? HeureSortie { get; set; }
        public string Duration { get; set; }
    }
}
