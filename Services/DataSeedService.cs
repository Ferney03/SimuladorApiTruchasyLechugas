using AquacultureAPI.Data;
using AquacultureAPI.Models;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace AquacultureAPI.Services
{
    public class DataSeedService
    {
        private readonly AquacultureContext _context;
        private readonly ILogger<DataSeedService> _logger;

        public DataSeedService(AquacultureContext context, ILogger<DataSeedService> logger)
        {
            _context = context;
            _logger = logger;
        }

        public async Task SeedHistoricalDataAsync()
        {
            try
            {
                // Verificar si ya existen datos
                var existingTruchas = await _context.TruchasData.CountAsync();
                var existingLechugas = await _context.LechugasData.CountAsync();

                if (existingTruchas > 0 || existingLechugas > 0)
                {
                    _logger.LogInformation($"Datos existentes encontrados - Truchas: {existingTruchas}, Lechugas: {existingLechugas}");
                    return;
                }

                _logger.LogInformation("Generando datos históricos de 2 meses (60 días)...");

                // Generar datos en lotes más pequeños para evitar problemas de memoria
                await GenerateAndInsertTruchasDataAsync();
                await GenerateAndInsertLechugasDataAsync();

                _logger.LogInformation("Datos históricos generados completamente");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generando datos históricos");
                throw;
            }
        }

        private async Task GenerateAndInsertTruchasDataAsync()
        {
            var random = new Random(42);
            var baseTime = DateTime.Now.AddDays(-60);

            _context.Database.SetCommandTimeout(300); // 5 minutos de timeout

            double currentLength = 2.5; // Empezar con 2.5 cm

            // Diccionario de crecimiento por meses
            var crecimientoPorMes = new Dictionary<int, (double min, double max)>
            {
                { 0, (2.5, 2.5) },    // 0 meses: 2.5 cm
                { 1, (5.0, 5.0) },    // 1 mes: 5 cm
                { 2, (8.0, 8.0) },    // 2 meses: 8 cm
                { 3, (12.0, 12.0) },  // 3 meses: 12 cm
                { 4, (20.0, 20.0) },  // 4 meses: 20 cm
                { 5, (30.0, 35.0) },  // 5 meses: 30-35 cm
                { 6, (38.0, 42.0) },  // 6 meses: 38-42 cm
                { 7, (45.0, 48.0) },  // 7 meses: 45-48 cm
                { 8, (50.0, 55.0) }   // 8 meses: 50-55 cm
            };

            const int batchSize = 500;
            var batch = new List<TruchaData>();
            int totalRecords = 0;
            int dataCounter = 0;

            // Generar datos cada 15 segundos por 60 días
            int totalSeconds = 60 * 24 * 60 * 60;

            for (int segundos = 15; segundos <= totalSeconds; segundos += 15)
            {
                dataCounter++;
                double mesesReales = segundos / (30.0 * 24.0 * 60.0 * 60.0);

                // Determinar si generar anomalía (cada 500 datos)
                bool generarAnomalia = (dataCounter % 500) == 0;

                // Generar parámetros ambientales
                double temperatura = GenerarTemperaturaHistorica(random, generarAnomalia);
                double conductividad = GenerarConductividadHistorica(random, generarAnomalia);
                double pH = GenerarPHHistorico(random, generarAnomalia);

                // Calcular longitud basada en el mes
                double nuevaLongitud = CalcularLongitudPorMesHistorica(random, mesesReales, crecimientoPorMes);
                currentLength = Math.Max(currentLength, nuevaLongitud);

                batch.Add(new TruchaData
                {
                    Timestamp = baseTime.AddSeconds(segundos),
                    TiempoSegundos = segundos,
                    LongitudCm = Math.Round(currentLength, 4),
                    TemperaturaC = Math.Round(temperatura, 4),
                    ConductividadUsCm = Math.Round(conductividad, 4),
                    pH = Math.Round(pH, 4)
                });

                // Insertar cuando el lote esté lleno
                if (batch.Count >= batchSize)
                {
                    try
                    {
                        await _context.TruchasData.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        totalRecords += batch.Count;
                        _logger.LogInformation($"Insertados {totalRecords} registros de truchas...");
                        batch.Clear();
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error insertando lote de truchas en registro {totalRecords}");
                        throw;
                    }
                }
            }

            // Insertar el último lote si tiene datos
            if (batch.Count > 0)
            {
                try
                {
                    await _context.TruchasData.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    totalRecords += batch.Count;
                    _context.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error insertando último lote de truchas");
                    throw;
                }
            }

            _logger.LogInformation($"Completado: {totalRecords} registros de truchas insertados");
        }

        private double CalcularLongitudPorMesHistorica(Random random, double mesesReales, Dictionary<int, (double min, double max)> crecimientoPorMes)
        {
            const int MesesMaximoCrecimiento = 8;

            if (mesesReales >= MesesMaximoCrecimiento)
            {
                var rangoFinal = crecimientoPorMes[8];
                return random.NextDouble() * (rangoFinal.max - rangoFinal.min) + rangoFinal.min;
            }

            int mesActual = (int)Math.Floor(mesesReales);
            double fraccionMes = mesesReales - mesActual;

            if (mesActual <= 0)
            {
                return crecimientoPorMes[0].min + random.NextGaussian() * 0.1;
            }

            if (mesActual >= MesesMaximoCrecimiento)
            {
                var rango = crecimientoPorMes[MesesMaximoCrecimiento];
                return random.NextDouble() * (rango.max - rango.min) + rango.min;
            }

            var rangoActual = crecimientoPorMes[mesActual];
            var rangoSiguiente = crecimientoPorMes[Math.Min(mesActual + 1, MesesMaximoCrecimiento)];

            double longitudActual = (rangoActual.min + rangoActual.max) / 2.0;
            double longitudSiguiente = (rangoSiguiente.min + rangoSiguiente.max) / 2.0;

            double longitudInterpolada = longitudActual + (longitudSiguiente - longitudActual) * fraccionMes;

            return longitudInterpolada + random.NextGaussian() * 0.3;
        }

        private double GenerarTemperaturaHistorica(Random random, bool anomalia)
        {
            if (anomalia)
            {
                return random.NextDouble() < 0.5
                    ? Math.Max(8, 12 - random.NextDouble() * 3)
                    : Math.Min(22, 18 + random.NextDouble() * 4);
            }

            double temperaturaBase = 15.0;
            return Math.Max(12, Math.Min(18, temperaturaBase + random.NextGaussian() * 1.5));
        }

        private double GenerarConductividadHistorica(Random random, bool anomalia)
        {
            if (anomalia)
            {
                return random.NextDouble() < 0.5
                    ? Math.Max(200, 300 - random.NextDouble() * 80)
                    : Math.Min(900, 800 + random.NextDouble() * 100);
            }

            double conductividadBase = 550.0;
            return Math.Max(300, Math.Min(800, conductividadBase + random.NextGaussian() * 100));
        }

        private double GenerarPHHistorico(Random random, bool anomalia)
        {
            if (anomalia)
            {
                return random.NextDouble() < 0.5
                    ? Math.Max(6.2, 7.0 - random.NextDouble() * 0.6)
                    : Math.Min(9.0, 8.5 + random.NextDouble() * 0.5);
            }

            double pHBase = 7.75;
            return Math.Max(7.0, Math.Min(8.5, pHBase + random.NextGaussian() * 0.3));
        }

        private async Task GenerateAndInsertLechugasDataAsync()
        {
            var random = new Random(42);
            var baseTime = DateTime.Now.AddDays(-60);

            _context.Database.SetCommandTimeout(300); // 5 minutos de timeout

            double currentHeight = 1.0;
            double currentAreaFoliar = 10.0;
            const double alturaMaxima = 16.0;
            const double areaFoliarMaxima = 2000.0;
            const int diasMaximoCrecimiento = 90;

            const int batchSize = 500;
            var batch = new List<LechugaData>();
            int totalRecords = 0;

            int totalSeconds = 60 * 24 * 60 * 60;

            for (int segundos = 15; segundos <= totalSeconds; segundos += 15)
            {
                double diasReales = segundos / 86400.0;

                // Variables ambientales
                double temperatura = Math.Max(15, Math.Min(30,
                    22 + Math.Sin(2 * Math.PI * diasReales / 30) + random.NextGaussian() * 0.5));

                double humedad = Math.Max(50, Math.Min(90,
                    70 + 10 * Math.Sin(2 * Math.PI * diasReales / 25) + random.NextGaussian() * 2));

                double pH = Math.Max(5.5, Math.Min(7.5,
                    6.5 + 0.1 * Math.Sin(2 * Math.PI * diasReales / 40) + random.NextGaussian() * 0.05));

                // Cálculo de crecimiento
                double tasaCrecimiento = Math.Max(0.03, Math.Min(0.12,
                    0.08 - 0.003 * Math.Abs(temperatura - 22) -
                    0.002 * Math.Abs(humedad - 70) - 0.01 * Math.Abs(pH - 6.5)));

                double t0 = 30;

                double nuevaAltura;
                double nuevaAreaFoliar;

                if (diasReales >= diasMaximoCrecimiento)
                {
                    nuevaAltura = alturaMaxima;
                    nuevaAreaFoliar = areaFoliarMaxima;
                }
                else
                {
                    nuevaAltura = alturaMaxima / (1 + Math.Exp(-tasaCrecimiento * (diasReales - t0)));
                    nuevaAreaFoliar = Math.Pow(nuevaAltura / alturaMaxima, 2) * areaFoliarMaxima;
                }

                currentHeight = Math.Max(currentHeight, Math.Min(nuevaAltura, alturaMaxima));
                currentAreaFoliar = Math.Max(currentAreaFoliar, Math.Min(nuevaAreaFoliar, areaFoliarMaxima));

                batch.Add(new LechugaData
                {
                    Timestamp = baseTime.AddSeconds(segundos),
                    TiempoSegundos = segundos,
                    AlturaCm = Math.Round(currentHeight, 4),
                    AreaFoliarCm2 = Math.Round(currentAreaFoliar, 4),
                    TemperaturaC = Math.Round(temperatura, 4),
                    HumedadPorcentaje = Math.Round(humedad, 4),
                    pH = Math.Round(pH, 4)
                });

                // Insertar cuando el lote esté lleno
                if (batch.Count >= batchSize)
                {
                    try
                    {
                        await _context.LechugasData.AddRangeAsync(batch);
                        await _context.SaveChangesAsync();
                        totalRecords += batch.Count;
                        _logger.LogInformation($"Insertados {totalRecords} registros de lechugas...");
                        batch.Clear();

                        // Limpiar el contexto para liberar memoria
                        _context.ChangeTracker.Clear();
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex, $"Error insertando lote de lechugas en registro {totalRecords}");
                        throw;
                    }
                }
            }

            // Insertar el último lote si tiene datos
            if (batch.Count > 0)
            {
                try
                {
                    await _context.LechugasData.AddRangeAsync(batch);
                    await _context.SaveChangesAsync();
                    totalRecords += batch.Count;
                    _context.ChangeTracker.Clear();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error insertando último lote de lechugas");
                    throw;
                }
            }

            _logger.LogInformation($"Completado: {totalRecords} registros de lechugas insertados");
        }
    }

    public static class RandomExtensions
    {
        public static double NextGaussian(this Random random, double mean = 0, double stdDev = 1)
        {
            double u1 = 1.0 - random.NextDouble();
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            return mean + stdDev * randStdNormal;
        }
    }
}
