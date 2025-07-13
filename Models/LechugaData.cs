using System.ComponentModel.DataAnnotations;

namespace AquacultureAPI.Models
{
    public class LechugaData
    {
        [Key]
        public int Id { get; set; }
        public DateTime Timestamp { get; set; }
        public int TiempoSegundos { get; set; }
        public double AlturaCm { get; set; }
        public double AreaFoliarCm2 { get; set; }
        public double TemperaturaC { get; set; }
        public double HumedadPorcentaje { get; set; }
        public double pH { get; set; }
    }
}
