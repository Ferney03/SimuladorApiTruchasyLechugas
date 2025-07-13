using System.ComponentModel.DataAnnotations;

namespace AquacultureAPI.Models
{
    public class TruchaData
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int TiempoSegundos { get; set; }
        public double LongitudCm { get; set; }
        public double TemperaturaC { get; set; }
        public double ConductividadUsCm { get; set; }
        public double pH { get; set; }
    }
}
