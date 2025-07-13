using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Data;
using AquacultureAPI.Models;
using AquacultureAPI.Services;

namespace AquacultureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class LechugasController : ControllerBase
    {
        private readonly AquacultureContext _context;
        private readonly LechugaSimulationService _simulationService;

        public LechugasController(AquacultureContext context, LechugaSimulationService simulationService)
        {
            _context = context;
            _simulationService = simulationService;
        }

        /// <summary>
        /// Obtiene todos los datos de lechugas
        /// </summary>
        [HttpGet]
        public async Task<ActionResult<IEnumerable<LechugaData>>> GetLechugas()
        {
            return await _context.LechugasData.OrderBy(l => l.TiempoSegundos).ToListAsync();
        }

        /// <summary>
        /// Obtiene el último dato de lechuga
        /// </summary>
        [HttpGet("latest")]
        public async Task<ActionResult<LechugaData>> GetLatestLechuga()
        {
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .FirstOrDefaultAsync();

            if (latest == null)
            {
                return NotFound("No hay datos disponibles");
            }

            return latest;
        }

        /// <summary>
        /// Obtiene datos de lechugas en un rango de tiempo específico
        /// </summary>
        [HttpGet("range")]
        public async Task<ActionResult<IEnumerable<LechugaData>>> GetLechugasInRange(
            [FromQuery] int startSeconds = 0,
            [FromQuery] int endSeconds = int.MaxValue)
        {
            return await _context.LechugasData
                .Where(l => l.TiempoSegundos >= startSeconds && l.TiempoSegundos <= endSeconds)
                .OrderBy(l => l.TiempoSegundos)
                .ToListAsync();
        }

        /// <summary>
        /// Obtiene estadísticas de las lechugas
        /// </summary>
        [HttpGet("stats")]
        public async Task<ActionResult<object>> GetLechugaStats()
        {
            var data = await _context.LechugasData.ToListAsync();

            if (!data.Any())
            {
                return NotFound("No hay datos disponibles");
            }

            return new
            {
                TotalRegistros = data.Count,
                AlturaPromedio = data.Average(l => l.AlturaCm),
                AlturaMaxima = data.Max(l => l.AlturaCm),
                AlturaMinima = data.Min(l => l.AlturaCm),
                AreaFoliarPromedio = data.Average(l => l.AreaFoliarCm2),
                TemperaturaPromedio = data.Average(l => l.TemperaturaC),
                HumedadPromedio = data.Average(l => l.HumedadPorcentaje),
                pHPromedio = data.Average(l => l.pH),
                TiempoTotal = data.Max(l => l.TiempoSegundos)
            };
        }

        /// <summary>
        /// Reinicia la simulación de lechugas
        /// </summary>
        [HttpPost("reset")]
        public async Task<ActionResult> ResetSimulation()
        {
            // Limpiar datos existentes
            _context.LechugasData.RemoveRange(_context.LechugasData);
            await _context.SaveChangesAsync();

            // Reiniciar servicio de simulación
            _simulationService.Reset();

            return Ok("Simulación de lechugas reiniciada");
        }

        /// <summary>
        /// Obtiene solo los datos de altura de lechugas
        /// </summary>
        [HttpGet("altura")]
        public async Task<ActionResult<IEnumerable<object>>> GetAlturaData()
        {
            var data = await _context.LechugasData
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    AlturaCm = l.AlturaCm,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de área foliar de lechugas
        /// </summary>
        [HttpGet("area-foliar")]
        public async Task<ActionResult<IEnumerable<object>>> GetAreaFoliarData()
        {
            var data = await _context.LechugasData
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    AreaFoliarCm2 = l.AreaFoliarCm2,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de temperatura de lechugas
        /// </summary>
        [HttpGet("temperatura")]
        public async Task<ActionResult<IEnumerable<object>>> GetTemperaturaData()
        {
            var data = await _context.LechugasData
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    TemperaturaC = l.TemperaturaC,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de humedad de lechugas
        /// </summary>
        [HttpGet("humedad")]
        public async Task<ActionResult<IEnumerable<object>>> GetHumedadData()
        {
            var data = await _context.LechugasData
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    HumedadPorcentaje = l.HumedadPorcentaje,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene solo los datos de pH de lechugas
        /// </summary>
        [HttpGet("ph")]
        public async Task<ActionResult<IEnumerable<object>>> GetPhData()
        {
            var data = await _context.LechugasData
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    pH = l.pH,
                    Timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Obtiene el último valor de altura
        /// </summary>
        [HttpGet("altura/latest")]
        public async Task<ActionResult<object>> GetLatestAltura()
        {
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    AlturaCm = l.AlturaCm,
                    Timestamp = l.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }

        /// <summary>
        /// Obtiene el último valor de área foliar
        /// </summary>
        [HttpGet("area-foliar/latest")]
        public async Task<ActionResult<object>> GetLatestAreaFoliar()
        {
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    AreaFoliarCm2 = l.AreaFoliarCm2,
                    Timestamp = l.Timestamp
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
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    TemperaturaC = l.TemperaturaC,
                    Timestamp = l.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }

        /// <summary>
        /// Obtiene el último valor de humedad
        /// </summary>
        [HttpGet("humedad/latest")]
        public async Task<ActionResult<object>> GetLatestHumedad()
        {
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    HumedadPorcentaje = l.HumedadPorcentaje,
                    Timestamp = l.Timestamp
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
            var latest = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Select(l => new {
                    TiempoSegundos = l.TiempoSegundos,
                    pH = l.pH,
                    Timestamp = l.Timestamp
                })
                .FirstOrDefaultAsync();

            if (latest == null) return NotFound("No hay datos disponibles");
            return Ok(latest);
        }
    }
}
