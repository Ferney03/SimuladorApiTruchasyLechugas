using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Models;

namespace AquacultureAPI.Data
{
    public class AquacultureContext : DbContext
    {
        public AquacultureContext(DbContextOptions<AquacultureContext> options) : base(options)
        {
        }

        public DbSet<TruchaData> TruchasData { get; set; }
        public DbSet<LechugaData> LechugasData { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configuración de precisión para TruchaData
            modelBuilder.Entity<TruchaData>()
                .Property(t => t.LongitudCm)
                .HasPrecision(10, 4);

            modelBuilder.Entity<TruchaData>()
                .Property(t => t.TemperaturaC)
                .HasPrecision(10, 4);

            modelBuilder.Entity<TruchaData>()
                .Property(t => t.ConductividadUsCm)
                .HasPrecision(10, 4);

            modelBuilder.Entity<TruchaData>()
                .Property(t => t.pH)
                .HasPrecision(10, 4);

            // Configuración de precisión para LechugaData
            modelBuilder.Entity<LechugaData>()
                .Property(l => l.AlturaCm)
                .HasPrecision(10, 4);

            modelBuilder.Entity<LechugaData>()
                .Property(l => l.AreaFoliarCm2)
                .HasPrecision(10, 4);

            modelBuilder.Entity<LechugaData>()
                .Property(l => l.TemperaturaC)
                .HasPrecision(10, 4);

            modelBuilder.Entity<LechugaData>()
                .Property(l => l.HumedadPorcentaje)
                .HasPrecision(10, 4);

            modelBuilder.Entity<LechugaData>()
                .Property(l => l.pH)
                .HasPrecision(10, 4);

            // AGREGAR ÍNDICES PARA MEJORAR RENDIMIENTO
            modelBuilder.Entity<TruchaData>()
                .HasIndex(t => t.TiempoSegundos)
                .HasDatabaseName("IX_TruchasData_TiempoSegundos");

            modelBuilder.Entity<TruchaData>()
                .HasIndex(t => t.Timestamp)
                .HasDatabaseName("IX_TruchasData_Timestamp");

            modelBuilder.Entity<LechugaData>()
                .HasIndex(l => l.TiempoSegundos)
                .HasDatabaseName("IX_LechugasData_TiempoSegundos");

            modelBuilder.Entity<LechugaData>()
                .HasIndex(l => l.Timestamp)
                .HasDatabaseName("IX_LechugasData_Timestamp");
        }
    }
}