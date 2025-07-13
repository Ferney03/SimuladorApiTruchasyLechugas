using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Data;

namespace AquacultureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PredictionController : ControllerBase
    {
        private readonly AquacultureContext _context;
        private readonly ILogger<PredictionController> _logger;

        public PredictionController(AquacultureContext context, ILogger<PredictionController> logger)
        {
            _context = context;
            _logger = logger;
        }

        /// <summary>
        /// Obtiene TODOS los datos de truchas y lechugas para análisis predictivo
        /// ⚠️ ADVERTENCIA: Este endpoint puede devolver ~691,200 registros y tardar varios minutos
        /// </summary>
        [HttpGet("all-data")]
        public async Task<ActionResult<object>> GetAllDataForPrediction()
        {
            try
            {
                _logger.LogInformation("Iniciando consulta de todos los datos para predicción...");

                // Configurar timeout extendido para esta operación
                _context.Database.SetCommandTimeout(600); // 10 minutos

                var startTime = DateTime.Now;

                // Obtener todos los datos de truchas
                _logger.LogInformation("Obteniendo datos de truchas...");
                var truchasData = await _context.TruchasData
                    .AsNoTracking()
                    .OrderBy(t => t.TiempoSegundos)
                    .Select(t => new {
                        id = t.Id,
                        timestamp = t.Timestamp,
                        tiempoSegundos = t.TiempoSegundos,
                        tiempoDias = t.TiempoSegundos / 86400.0,
                        tiempoMeses = t.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        longitudCm = t.LongitudCm,
                        temperaturaC = t.TemperaturaC,
                        conductividadUsCm = t.ConductividadUsCm,
                        pH = t.pH,
                        tipo = "trucha"
                    })
                    .ToListAsync();

                _logger.LogInformation($"Obtenidos {truchasData.Count} registros de truchas");

                // Obtener todos los datos de lechugas
                _logger.LogInformation("Obteniendo datos de lechugas...");
                var lechugasData = await _context.LechugasData
                    .AsNoTracking()
                    .OrderBy(l => l.TiempoSegundos)
                    .Select(l => new {
                        id = l.Id,
                        timestamp = l.Timestamp,
                        tiempoSegundos = l.TiempoSegundos,
                        tiempoDias = l.TiempoSegundos / 86400.0,
                        tiempoMeses = l.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        alturaCm = l.AlturaCm,
                        areaFoliarCm2 = l.AreaFoliarCm2,
                        temperaturaC = l.TemperaturaC,
                        humedadPorcentaje = l.HumedadPorcentaje,
                        pH = l.pH,
                        tipo = "lechuga"
                    })
                    .ToListAsync();

                _logger.LogInformation($"Obtenidos {lechugasData.Count} registros de lechugas");

                // Calcular estadísticas para predicciones
                var statsTruchas = new
                {
                    totalRegistros = truchasData.Count,
                    tiempoInicialSegundos = truchasData.FirstOrDefault()?.tiempoSegundos ?? 0,
                    tiempoFinalSegundos = truchasData.LastOrDefault()?.tiempoSegundos ?? 0,
                    duracionDias = (truchasData.LastOrDefault()?.tiempoSegundos ?? 0) / 86400.0,
                    duracionMeses = (truchasData.LastOrDefault()?.tiempoSegundos ?? 0) / (30.0 * 24.0 * 60.0 * 60.0),
                    longitudInicial = truchasData.FirstOrDefault()?.longitudCm ?? 0,
                    longitudFinal = truchasData.LastOrDefault()?.longitudCm ?? 0,
                    longitudPromedio = truchasData.Average(t => t.longitudCm),
                    longitudMaxima = truchasData.Max(t => t.longitudCm),
                    longitudMinima = truchasData.Min(t => t.longitudCm),
                    temperaturaPromedio = truchasData.Average(t => t.temperaturaC),
                    temperaturaMaxima = truchasData.Max(t => t.temperaturaC),
                    temperaturaMinima = truchasData.Min(t => t.temperaturaC),
                    conductividadPromedio = truchasData.Average(t => t.conductividadUsCm),
                    conductividadMaxima = truchasData.Max(t => t.conductividadUsCm),
                    conductividadMinima = truchasData.Min(t => t.conductividadUsCm),
                    pHPromedio = truchasData.Average(t => t.pH),
                    pHMaximo = truchasData.Max(t => t.pH),
                    pHMinimo = truchasData.Min(t => t.pH),
                    frecuenciaDatos = "cada 15 segundos",
                    parametrosOptimos = new
                    {
                        temperaturaOptima = "12°C - 18°C",
                        pHOptimo = "7.0 - 8.5",
                        conductividadOptima = "300 - 800 µS/cm"
                    }
                };

                var statsLechugas = new
                {
                    totalRegistros = lechugasData.Count,
                    tiempoInicialSegundos = lechugasData.FirstOrDefault()?.tiempoSegundos ?? 0,
                    tiempoFinalSegundos = lechugasData.LastOrDefault()?.tiempoSegundos ?? 0,
                    duracionDias = (lechugasData.LastOrDefault()?.tiempoSegundos ?? 0) / 86400.0,
                    duracionMeses = (lechugasData.LastOrDefault()?.tiempoSegundos ?? 0) / (30.0 * 24.0 * 60.0 * 60.0),
                    alturaInicial = lechugasData.FirstOrDefault()?.alturaCm ?? 0,
                    alturaFinal = lechugasData.LastOrDefault()?.alturaCm ?? 0,
                    alturaPromedio = lechugasData.Average(l => l.alturaCm),
                    alturaMaxima = lechugasData.Max(l => l.alturaCm),
                    alturaMinima = lechugasData.Min(l => l.alturaCm),
                    areaFoliarPromedio = lechugasData.Average(l => l.areaFoliarCm2),
                    areaFoliarMaxima = lechugasData.Max(l => l.areaFoliarCm2),
                    areaFoliarMinima = lechugasData.Min(l => l.areaFoliarCm2),
                    temperaturaPromedio = lechugasData.Average(l => l.temperaturaC),
                    temperaturaMaxima = lechugasData.Max(l => l.temperaturaC),
                    temperaturaMinima = lechugasData.Min(l => l.temperaturaC),
                    humedadPromedio = lechugasData.Average(l => l.humedadPorcentaje),
                    humedadMaxima = lechugasData.Max(l => l.humedadPorcentaje),
                    humedadMinima = lechugasData.Min(l => l.humedadPorcentaje),
                    pHPromedio = lechugasData.Average(l => l.pH),
                    pHMaximo = lechugasData.Max(l => l.pH),
                    pHMinimo = lechugasData.Min(l => l.pH),
                    frecuenciaDatos = "cada 15 segundos",
                    parametrosOptimos = new
                    {
                        temperaturaOptima = "15°C - 30°C",
                        humedadOptima = "50% - 90%",
                        pHOptimo = "5.5 - 7.5"
                    }
                };

                var endTime = DateTime.Now;
                var processingTime = (endTime - startTime).TotalSeconds;

                _logger.LogInformation($"Consulta completada en {processingTime:F2} segundos");

                var response = new
                {
                    metadata = new
                    {
                        consultaRealizada = DateTime.Now,
                        tiempoProcesamiento = $"{processingTime:F2} segundos",
                        totalRegistros = truchasData.Count + lechugasData.Count,
                        advertencia = "Este dataset contiene todos los datos históricos y en tiempo real. Ideal para análisis predictivo y machine learning.",
                        formatoDatos = "Datos cada 15 segundos durante 60+ días",
                        usoRecomendado = new[] {
                            "Análisis de tendencias de crecimiento",
                            "Predicción de crecimiento futuro",
                            "Análisis de correlación entre variables ambientales",
                            "Detección de anomalías",
                            "Modelos de machine learning",
                            "Análisis de series temporales"
                        }
                    },
                    truchas = new
                    {
                        estadisticas = statsTruchas,
                        datos = truchasData
                    },
                    lechugas = new
                    {
                        estadisticas = statsLechugas,
                        datos = lechugasData
                    }
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo todos los datos para predicción");
                return StatusCode(500, new
                {
                    error = "Error interno del servidor",
                    mensaje = "No se pudieron obtener todos los datos. Intente nuevamente o use endpoints con filtros.",
                    detalles = ex.Message
                });
            }
        }

        /// <summary>
        /// Obtiene datos combinados optimizados para predicciones (cada N registros)
        /// </summary>
        [HttpGet("optimized-data")]
        public async Task<ActionResult<object>> GetOptimizedDataForPrediction([FromQuery] int interval = 100)
        {
            try
            {
                _logger.LogInformation($"Obteniendo datos optimizados cada {interval} registros...");

                // Obtener datos de truchas cada N registros
                var truchasData = await _context.TruchasData
                    .AsNoTracking()
                    .Where((t, index) => index % interval == 0)
                    .OrderBy(t => t.TiempoSegundos)
                    .Select(t => new {
                        tiempoSegundos = t.TiempoSegundos,
                        tiempoDias = t.TiempoSegundos / 86400.0,
                        tiempoMeses = t.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        longitudCm = t.LongitudCm,
                        temperaturaC = t.TemperaturaC,
                        conductividadUsCm = t.ConductividadUsCm,
                        pH = t.pH,
                        timestamp = t.Timestamp
                    })
                    .ToListAsync();

                var lechugasData = await _context.LechugasData
                    .AsNoTracking()
                    .Where((l, index) => index % interval == 0)
                    .OrderBy(l => l.TiempoSegundos)
                    .Select(l => new {
                        tiempoSegundos = l.TiempoSegundos,
                        tiempoDias = l.TiempoSegundos / 86400.0,
                        tiempoMeses = l.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        alturaCm = l.AlturaCm,
                        areaFoliarCm2 = l.AreaFoliarCm2,
                        temperaturaC = l.TemperaturaC,
                        humedadPorcentaje = l.HumedadPorcentaje,
                        pH = l.pH,
                        timestamp = l.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    metadata = new
                    {
                        intervalo = interval,
                        descripcion = $"Datos cada {interval} registros para análisis optimizado",
                        truchasRegistros = truchasData.Count,
                        lechugasRegistros = lechugasData.Count,
                        totalRegistros = truchasData.Count + lechugasData.Count
                    },
                    truchas = truchasData,
                    lechugas = lechugasData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo datos optimizados");
                return StatusCode(500, new { error = "Error interno del servidor", detalles = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene datos por rango de tiempo para predicciones específicas
        /// </summary>
        [HttpGet("range-data")]
        public async Task<ActionResult<object>> GetRangeDataForPrediction(
            [FromQuery] int startSeconds = 0,
            [FromQuery] int endSeconds = int.MaxValue)
        {
            try
            {
                var truchasData = await _context.TruchasData
                    .AsNoTracking()
                    .Where(t => t.TiempoSegundos >= startSeconds && t.TiempoSegundos <= endSeconds)
                    .OrderBy(t => t.TiempoSegundos)
                    .Select(t => new {
                        tiempoSegundos = t.TiempoSegundos,
                        tiempoDias = t.TiempoSegundos / 86400.0,
                        longitudCm = t.LongitudCm,
                        temperaturaC = t.TemperaturaC,
                        conductividadUsCm = t.ConductividadUsCm,
                        pH = t.pH,
                        timestamp = t.Timestamp
                    })
                    .ToListAsync();

                var lechugasData = await _context.LechugasData
                    .AsNoTracking()
                    .Where(l => l.TiempoSegundos >= startSeconds && l.TiempoSegundos <= endSeconds)
                    .OrderBy(l => l.TiempoSegundos)
                    .Select(l => new {
                        tiempoSegundos = l.TiempoSegundos,
                        tiempoDias = l.TiempoSegundos / 86400.0,
                        alturaCm = l.AlturaCm,
                        areaFoliarCm2 = l.AreaFoliarCm2,
                        temperaturaC = l.TemperaturaC,
                        humedadPorcentaje = l.HumedadPorcentaje,
                        pH = l.pH,
                        timestamp = l.Timestamp
                    })
                    .ToListAsync();

                return Ok(new
                {
                    metadata = new
                    {
                        rangoInicio = startSeconds,
                        rangoFin = endSeconds,
                        diasInicio = startSeconds / 86400.0,
                        diasFin = endSeconds / 86400.0,
                        truchasRegistros = truchasData.Count,
                        lechugasRegistros = lechugasData.Count
                    },
                    truchas = truchasData,
                    lechugas = lechugasData
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo datos por rango");
                return StatusCode(500, new { error = "Error interno del servidor", detalles = ex.Message });
            }
        }

        /// <summary>
        /// Obtiene solo las métricas de crecimiento para análisis predictivo rápido
        /// </summary>
        [HttpGet("growth-metrics")]
        public async Task<ActionResult<object>> GetGrowthMetrics()
        {
            try
            {
                var truchasGrowth = await _context.TruchasData
                    .AsNoTracking()
                    .OrderBy(t => t.TiempoSegundos)
                    .Select(t => new {
                        tiempoDias = t.TiempoSegundos / 86400.0,
                        tiempoMeses = t.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        longitudCm = t.LongitudCm
                    })
                    .ToListAsync();

                var lechugasGrowth = await _context.LechugasData
                    .AsNoTracking()
                    .OrderBy(l => l.TiempoSegundos)
                    .Select(l => new {
                        tiempoDias = l.TiempoSegundos / 86400.0,
                        tiempoMeses = l.TiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0),
                        alturaCm = l.AlturaCm,
                        areaFoliarCm2 = l.AreaFoliarCm2
                    })
                    .ToListAsync();

                return Ok(new
                {
                    metadata = new
                    {
                        descripcion = "Métricas de crecimiento optimizadas para predicciones",
                        uso = "Ideal para modelos de regresión y análisis de tendencias"
                    },
                    truchas = new
                    {
                        crecimiento = truchasGrowth,
                        limitesEsperados = new
                        {
                            mes0 = "2.5 cm",
                            mes1 = "5 cm",
                            mes2 = "8 cm",
                            mes3 = "12 cm",
                            mes4 = "20 cm",
                            mes5 = "30-35 cm",
                            mes6 = "38-42 cm",
                            mes7 = "45-48 cm",
                            mes8 = "50-55 cm (máximo)"
                        }
                    },
                    lechugas = new
                    {
                        crecimiento = lechugasGrowth,
                        limitesEsperados = new
                        {
                            alturaMaxima = "16 cm",
                            areaFoliarMaxima = "2000 cm²",
                            tiempoCrecimiento = "90 días (3 meses)"
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error obteniendo métricas de crecimiento");
                return StatusCode(500, new { error = "Error interno del servidor", detalles = ex.Message });
            }
        }
    }
}
