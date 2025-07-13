using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Data;
using AquacultureAPI.Models;
using AquacultureAPI.Services;

namespace AquacultureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TruchasController : ControllerBase
    {
        private readonly AquacultureContext _context;
        private readonly TruchaSimulationService _simulationService;

        public TruchasController(AquacultureContext context, TruchaSimulationService simulationService)
        {
            _context = context;
            _simulationService = simulationService;
        }

        /// <summary>
        /// Obtiene todos los datos de truchas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<TruchaData>>> GetTruchas()
        {
            return await _context.TruchasData.OrderBy(t => t.TiempoSegundos).ToListAsync();
        }

        /// <summary>
        /// Obtiene el último dato de trucha
        /// </summary>
        [HttpGet("latest")]
        public async Task<ActionResult<TruchaData>> GetLatestTrucha()
        {
            var latest = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .FirstOrDefaultAsync();

            if (latest == null)
            {
                return NotFound("No hay datos disponibles");
            }

            return latest;
        }

        /// <summary>
        /// Obtiene datos de truchas en un rango de tiempo específico
        /// </summary>
        [HttpGet("range")]
        public async Task<ActionResult<IEnumerable<TruchaData>>> GetTruchasInRange(
            [FromQuery] int startSeconds = 0,
            [FromQuery] int endSeconds = int.MaxValue)
        {
            return await _context.TruchasData
                .Where(t => t.TiempoSegundos >= startSeconds && t.TiempoSegundos <= endSeconds)
                .OrderBy(t => t.TiempoSegundos)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene estadísticas de las truchas
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetTruchaStats()
        {
            var data = await _context.TruchasData.ToListAsync();

            if (!data.Any())
            {
                return NotFound("No hay datos disponibles");
            }

            return new
            {
                TotalRegistros = data.Count,
                LongitudPromedio = data.Average(t => t.LongitudCm),
                LongitudMaxima = data.Max(t => t.LongitudCm),
                LongitudMinima = data.Min(t => t.LongitudCm),
                TemperaturaPromedio = data.Average(t => t.TemperaturaC),
                ConductividadPromedio = data.Average(t => t.ConductividadUsCm),
                pHPromedio = data.Average(t => t.pH),
                TiempoTotal = data.Max(t => t.TiempoSegundos)
            };
        }

        /// <summary>
        /// Reinicia la simulación de truchas
        /// </summary>
        [HttpPost("reset")]
        public async Task<ActionResult> ResetSimulation()
        {
            // Limpiar datos existentes
            _context.TruchasData.RemoveRange(_context.TruchasData);
            await _context.SaveChangesAsync();

            // Reiniciar servicio de simulación
            _simulationService.Reset();

            return Ok("Simulación de truchas reiniciada");
        }

        /// <summary>
        /// Obtiene solo los datos de longitud de truchas
        /// </summary>
        [HttpGet("longitud")]
        public async Task<ActionResult<IEnumerable<object>>> GetLongitudData()
        {
            var data = await _context.TruchasData
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    LongitudCm = t.LongitudCm,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de temperatura de truchas
        /// </summary>
        [HttpGet("temperatura")]
        public async Task<ActionResult<IEnumerable<object>>> GetTemperaturaData()
        {
            var data = await _context.TruchasData
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    TemperaturaC = t.TemperaturaC,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de conductividad de truchas
        /// </summary>
        [HttpGet("conductividad")]
        public async Task<ActionResult<IEnumerable<object>>> GetConductividadData()
        {
            var data = await _context.TruchasData
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    ConductividadUsCm = t.ConductividadUsCm,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de pH de truchas
        /// </summary>
        [HttpGet("ph")]
        public async Task<ActionResult<IEnumerable<object>>> GetPhData()
        {
            var data = await _context.TruchasData
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    pH = t.pH,
                    Timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene el último valor de longitud
        /// </summary>
        [HttpGet("longitud/latest")]
        public async Task<ActionResult<object>> GetLatestLongitud()
        {
            var latest = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    LongitudCm = t.LongitudCm,
                    Timestamp = t.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }

        /// <summary>
        /// Obtiene el último valor de temperatura
        /// </summary>
        [HttpGet("temperatura/latest")]
        public async Task<ActionResult<object>> GetLatestTemperatura()
        {
            var latest = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    TemperaturaC = t.TemperaturaC,
                    Timestamp = t.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }

        /// <summary>
        /// Obtiene el último valor de conductividad
        /// </summary>
        [HttpGet("conductividad/latest")]
        public async Task<ActionResult<object>> GetLatestConductividad()
        {
            var latest = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    ConductividadUsCm = t.ConductividadUsCm,
                    Timestamp = t.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }

        /// <summary>
        /// Obtiene el último valor de pH
        /// </summary>
        [HttpGet("ph/latest")]
        public async Task<ActionResult<object>> GetLatestPh()
        {
            var latest = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Select(t => new {
                    TiempoSegundos = t.TiempoSegundos,
                    pH = t.pH,
                    Timestamp = t.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }
    }
}
