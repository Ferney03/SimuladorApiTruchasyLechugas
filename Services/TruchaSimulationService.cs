using AquacultureAPI.Models;
using AquacultureAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace AquacultureAPI.Services
{
    public class TruchaSimulationService
    {
        private readonly Random _random;
        private readonly IServiceProvider _serviceProvider;
        private int _currentTime = 0;
        private double _currentLength = 2.5;
        private bool _initialized = false;
        private int _dataCounter = 0; // Contador para generar anomalías cada 500 datos

        // Constantes de crecimiento por meses
        private readonly Dictionary<int, (double min, double max)> _crecimientoPorMes = new()
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

        private const int MesesMaximoCrecimiento = 8;

        public TruchaSimulationService(IServiceProvider serviceProvider)
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

                var lastRecord = await context.TruchasData
                    .AsNoTracking()
                    .OrderByDescending(t => t.Id) // Usar Id en lugar de TiempoSegundos para mejor rendimiento
                    .Take(1)
                    .FirstOrDefaultAsync();

                if (lastRecord != null)
                {
                    _currentTime = lastRecord.TiempoSegundos;
                    _currentLength = lastRecord.LongitudCm;
                    _dataCounter = await context.TruchasData.AsNoTracking().CountAsync();
                }
                else
                {
                    // Si no hay datos, empezar desde cero
                    _currentTime = 0;
                    _currentLength = 2.5;
                    _dataCounter = 0;
                }

                _initialized = true;
            }
            catch (Exception ex)
            {
                // Si hay error, inicializar con valores por defecto
                _currentTime = 0;
                _currentLength = 2.5;
                _dataCounter = 0;
                _initialized = true;

                var logger = _serviceProvider.GetService<ILogger<TruchaSimulationService>>();
                logger?.LogWarning(ex, "Error inicializando desde base de datos, usando valores por defecto");
            }
        }

        public async Task<TruchaData> GenerateNextDataAsync()
        {
            if (!_initialized)
            {
                await InitializeFromDatabaseAsync();
            }

            _currentTime += 15; // Cada 15 segundos
            _dataCounter++;

            // Convertir segundos a meses reales
            double mesesReales = _currentTime / (30.0 * 24.0 * 60.0 * 60.0); // 30 días por mes

            // Determinar si generar anomalía (cada 500 datos)
            bool generarAnomalia = (_dataCounter % 500) == 0;

            // Parámetros ambientales óptimos
            double temperatura = GenerarTemperatura(generarAnomalia);
            double conductividad = GenerarConductividad(generarAnomalia);
            double pH = GenerarPH(generarAnomalia);

            // Calcular nueva longitud basada en el mes actual
            double nuevaLongitud = CalcularLongitudPorMes(mesesReales);

            // Asegurar crecimiento progresivo
            _currentLength = Math.Max(_currentLength, nuevaLongitud);

            return new TruchaData
            {
                Timestamp = DateTime.Now,
                TiempoSegundos = _currentTime,
                LongitudCm = Math.Round(_currentLength, 4),
                TemperaturaC = Math.Round(temperatura, 4),
                ConductividadUsCm = Math.Round(conductividad, 4),
                pH = Math.Round(pH, 4)
            };
        }

        private double CalcularLongitudPorMes(double mesesReales)
        {
            if (mesesReales >= MesesMaximoCrecimiento)
            {
                // Después de 8 meses, mantener rango final
                var rangoFinal = _crecimientoPorMes[8];
                return _random.NextDouble() * (rangoFinal.max - rangoFinal.min) + rangoFinal.min;
            }

            int mesActual = (int)Math.Floor(mesesReales);
            double fraccionMes = mesesReales - mesActual;

            // Si estamos en el primer mes o antes
            if (mesActual <= 0)
            {
                return _crecimientoPorMes[0].min + _random.NextGaussian() * 0.1;
            }

            // Si estamos en el último mes definido
            if (mesActual >= MesesMaximoCrecimiento)
            {
                var rango = _crecimientoPorMes[MesesMaximoCrecimiento];
                return _random.NextDouble() * (rango.max - rango.min) + rango.min;
            }

            // Interpolación entre meses
            var rangoActual = _crecimientoPorMes[mesActual];
            var rangoSiguiente = _crecimientoPorMes[Math.Min(mesActual + 1, MesesMaximoCrecimiento)];

            double longitudActual = (rangoActual.min + rangoActual.max) / 2.0;
            double longitudSiguiente = (rangoSiguiente.min + rangoSiguiente.max) / 2.0;

            double longitudInterpolada = longitudActual + (longitudSiguiente - longitudActual) * fraccionMes;

            // Agregar variación natural
            return longitudInterpolada + _random.NextGaussian() * 0.3;
        }

        private double GenerarTemperatura(bool anomalia)
        {
            if (anomalia)
            {
                // Generar temperatura ligeramente fuera del rango óptimo (12-18°C)
                return _random.NextDouble() < 0.5
                    ? Math.Max(8, 12 - _random.NextDouble() * 3)   // Por debajo: 9-12°C
                    : Math.Min(22, 18 + _random.NextDouble() * 4); // Por encima: 18-22°C
            }

            // Temperatura óptima: 12-18°C
            double temperaturaBase = 15.0; // Centro del rango
            return Math.Max(12, Math.Min(18,
                temperaturaBase + _random.NextGaussian() * 1.5));
        }

        private double GenerarConductividad(bool anomalia)
        {
            if (anomalia)
            {
                // Generar conductividad ligeramente fuera del rango ideal (300-800 µS/cm)
                return _random.NextDouble() < 0.5
                    ? Math.Max(200, 300 - _random.NextDouble() * 80)   // Por debajo: 220-300
                    : Math.Min(900, 800 + _random.NextDouble() * 100); // Por encima: 800-900
            }

            // Conductividad ideal: 300-800 µS/cm
            double conductividadBase = 550.0; // Centro del rango
            return Math.Max(300, Math.Min(800,
                conductividadBase + _random.NextGaussian() * 100));
        }

        private double GenerarPH(bool anomalia)
        {
            if (anomalia)
            {
                // Generar pH ligeramente fuera del rango óptimo (7.0-8.5)
                return _random.NextDouble() < 0.5
                    ? Math.Max(6.2, 7.0 - _random.NextDouble() * 0.6)  // Por debajo: 6.4-7.0
                    : Math.Min(9.0, 8.5 + _random.NextDouble() * 0.5); // Por encima: 8.5-9.0
            }

            // pH óptimo: 7.0-8.5
            double pHBase = 7.75; // Centro del rango
            return Math.Max(7.0, Math.Min(8.5,
                pHBase + _random.NextGaussian() * 0.3));
        }

        public void Reset()
        {
            _currentTime = 0;
            _currentLength = 2.5;
            _dataCounter = 0;
            _initialized = false;
        }
    }
}
