using AquacultureAPI.Models;
using AquacultureAPI.Data;
using Microsoft.EntityFrameworkCore;

namespace AquacultureAPI.Services
{
    public class LechugaSimulationService
    {
        private readonly Random _random;
        private readonly IServiceProvider _serviceProvider;
        private int _currentTime = 0;
        private double _currentHeight = 1.0;
        private double _currentAreaFoliar = 10.0;
        private bool _initialized = false;

        // Constantes de crecimiento
        private const double AlturaMaxima = 16.0; // 16 cm máximo
        private const double AreaFoliarMaxima = 2000.0; // 2000 cm² máximo
        private const int DiasMaximoCrecimiento = 90; // 3 meses = 90 días

        public LechugaSimulationService(IServiceProvider serviceProvider)
        {
            _random = new Random(42);
            _serviceProvider = serviceProvider;
        }

        public async Task InitializeFromDatabaseAsync()
        {
            if (_initialized) return;

            try
            {
                using var scope = _serviceProvider.CreateScope();
                var context = scope.ServiceProvider.GetRequiredService<AquacultureContext>();

                // Usar una consulta más eficiente con timeout extendido
                context.Database.SetCommandTimeout(120); // 2 minutos de timeout

                var lastRecord = await context.LechugasData
                    .AsNoTracking()
                    .OrderByDescending(l => l.Id) // Usar Id en lugar de TiempoSegundos para mejor rendimiento
                    .Take(1)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                {
                    _currentTime = lastRecord.TiempoSegundos;
                    _currentHeight = lastRecord.AlturaCm;
                    _currentAreaFoliar = lastRecord.AreaFoliarCm2;
                }
                else
                {
                    // Si no hay datos, empezar desde cero
                    _currentTime = 0;
                    _currentHeight = 1.0;
                    _currentAreaFoliar = 10.0;
                }

                _initialized = true;
            }
            catch (Exception ex)
            {
                // Si hay error, inicializar con valores por defecto
                _currentTime = 0;
                _currentHeight = 1.0;
                _currentAreaFoliar = 10.0;
                _initialized = true;

                var logger = _serviceProvider.GetService<ILogger<LechugaSimulationService>>();
                logger?.LogWarning(ex, "Error inicializando desde base de datos, usando valores por defecto");
            }
        }

        public async Task<LechugaData> GenerateNextDataAsync()
        {
            if (!_initialized)
            {
                await InitializeFromDatabaseAsync();
            }

            _currentTime += 15; // CAMBIADO: Cada 15 segundos en lugar de 5

            // Convertir segundos a días reales
            double diasReales = _currentTime / 86400.0;

            // Variables ambientales con fluctuaciones
            double temperatura = Math.Max(15, Math.Min(30,
                22 + Math.Sin(2 * Math.PI * diasReales / 30) + _random.NextGaussian() * 0.5));

            double humedad = Math.Max(50, Math.Min(90,
                70 + 10 * Math.Sin(2 * Math.PI * diasReales / 25) + _random.NextGaussian() * 2));

            double pH = Math.Max(5.5, Math.Min(7.5,
                6.5 + 0.1 * Math.Sin(2 * Math.PI * diasReales / 40) + _random.NextGaussian() * 0.05));

            // Calcular nuevos valores
            double nuevaAltura;
            double nuevaAreaFoliar;

            if (diasReales >= DiasMaximoCrecimiento)
            {
                // Después de 3 meses, mantener valores máximos
                nuevaAltura = AlturaMaxima;
                nuevaAreaFoliar = AreaFoliarMaxima;
            }
            else
            {
                // Cálculo de crecimiento logístico
                double tasaCrecimiento = Math.Max(0.03, Math.Min(0.12,
                    0.08 - 0.003 * Math.Abs(temperatura - 22) -
                    0.002 * Math.Abs(humedad - 70) - 0.01 * Math.Abs(pH - 6.5)));

                double t0 = 30; // Punto de inflexión
                nuevaAltura = AlturaMaxima / (1 + Math.Exp(-tasaCrecimiento * (diasReales - t0)));
                nuevaAreaFoliar = Math.Pow(nuevaAltura / AlturaMaxima, 2) * AreaFoliarMaxima;
            }

            // Asegurar crecimiento progresivo y límites
            _currentHeight = Math.Max(_currentHeight, Math.Min(nuevaAltura, AlturaMaxima));
            _currentAreaFoliar = Math.Max(_currentAreaFoliar, Math.Min(nuevaAreaFoliar, AreaFoliarMaxima));

            return new LechugaData
            {
                Timestamp = DateTime.Now,
                TiempoSegundos = _currentTime,
                AlturaCm = Math.Round(_currentHeight, 4),
                AreaFoliarCm2 = Math.Round(_currentAreaFoliar, 4),
                TemperaturaC = Math.Round(temperatura, 4),
                HumedadPorcentaje = Math.Round(humedad, 4),
                pH = Math.Round(pH, 4)
            };
        }

        public void Reset()
        {
            _currentTime = 0;
            _currentHeight = 1.0;
            _currentAreaFoliar = 10.0;
            _initialized = false;
        }
    }
}
