using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Data;

namespace AquacultureAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class GraphicsController : ControllerBase
    {
        private readonly AquacultureContext _context;

        public GraphicsController(AquacultureContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Datos optimizados para gráfica de crecimiento de truchas (cada 1000 registros)
        /// </summary>
        [HttpGet("truchas/crecimiento")]
        public async Task<ActionResult<object>> GetTruchaCrecimientoData()
        {
            var data = await _context.TruchasData
                .Where((t, index) => index % 1000 == 0) // Cada 1000 registros
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    tiempo = t.TiempoSegundos / 86400.0, // Convertir a días
                    longitud = t.LongitudCm,
                    timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Datos optimizados para gráfica de crecimiento de lechugas (cada 1000 registros)
        /// </summary>
        [HttpGet("lechugas/crecimiento")]
        public async Task<ActionResult<object>> GetLechugaCrecimientoData()
        {
            var data = await _context.LechugasData
                .Where((l, index) => index % 1000 == 0)
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    tiempo = l.TiempoSegundos / 86400.0,
                    altura = l.AlturaCm,
                    areaFoliar = l.AreaFoliarCm2,
                    timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Últimos N registros para gráficas en tiempo real
        /// </summary>
        [HttpGet("truchas/ultimos/{cantidad}")]
        public async Task<ActionResult<object>> GetUltimosTruchaData(int cantidad = 100)
        {
            var data = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Take(cantidad)
                .OrderBy(t => t.TiempoSegundos)
                .Select(t => new {
                    tiempo = t.TiempoSegundos / 86400.0,
                    longitud = t.LongitudCm,
                    temperatura = t.TemperaturaC,
                    conductividad = t.ConductividadUsCm,
                    pH = t.pH,
                    timestamp = t.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Últimos N registros de lechugas para gráficas en tiempo real
        /// </summary>
        [HttpGet("lechugas/ultimos/{cantidad}")]
        public async Task<ActionResult<object>> GetUltimosLechugaData(int cantidad = 100)
        {
            var data = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Take(cantidad)
                .OrderBy(l => l.TiempoSegundos)
                .Select(l => new {
                    tiempo = l.TiempoSegundos / 86400.0,
                    altura = l.AlturaCm,
                    areaFoliar = l.AreaFoliarCm2,
                    temperatura = l.TemperaturaC,
                    humedad = l.HumedadPorcentaje,
                    pH = l.pH,
                    timestamp = l.Timestamp
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Datos por día (promedio diario) para gráficas de tendencias
        /// </summary>
        [HttpGet("truchas/diario")]
        public async Task<ActionResult<object>> GetTruchaDailyData()
        {
            var data = await _context.TruchasData
                .GroupBy(t => t.TiempoSegundos / 86400) // Agrupar por día
                .Select(g => new {
                    dia = g.Key,
                    longitudPromedio = g.Average(t => t.LongitudCm),
                    longitudMaxima = g.Max(t => t.LongitudCm),
                    longitudMinima = g.Min(t => t.LongitudCm),
                    temperaturaPromedio = g.Average(t => t.TemperaturaC),
                    conductividadPromedio = g.Average(t => t.ConductividadUsCm),
                    pHPromedio = g.Average(t => t.pH),
                    registros = g.Count()
                })
                .OrderBy(x => x.dia)
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Datos por día de lechugas (promedio diario)
        /// </summary>
        [HttpGet("lechugas/diario")]
        public async Task<ActionResult<object>> GetLechugaDailyData()
        {
            var data = await _context.LechugasData
                .GroupBy(l => l.TiempoSegundos / 86400)
                .Select(g => new {
                    dia = g.Key,
                    alturaPromedio = g.Average(l => l.AlturaCm),
                    alturaMaxima = g.Max(l => l.AlturaCm),
                    alturaMinima = g.Min(l => l.AlturaCm),
                    areaFoliarPromedio = g.Average(l => l.AreaFoliarCm2),
                    temperaturaPromedio = g.Average(l => l.TemperaturaC),
                    humedadPromedio = g.Average(l => l.HumedadPorcentaje),
                    pHPromedio = g.Average(l => l.pH),
                    registros = g.Count()
                })
                .OrderBy(x => x.dia)
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>
        /// Dashboard completo con datos actuales
        /// </summary>
        [HttpGet("dashboard")]
        public async Task<ActionResult<object>> GetDashboard()
        {
            var ultimaTrucha = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .FirstOrDefaultAsync();

            var ultimaLechuga = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .FirstOrDefaultAsync();

            var statsTruchas = await _context.TruchasData
                .GroupBy(t => 1)
                .Select(g => new {
                    total = g.Count(),
                    longitudPromedio = g.Average(t => t.LongitudCm),
                    longitudMaxima = g.Max(t => t.LongitudCm),
                    temperaturaPromedio = g.Average(t => t.TemperaturaC)
                })
                .FirstOrDefaultAsync();

            var statsLechugas = await _context.LechugasData
                .GroupBy(l => 1)
                .Select(g => new {
                    total = g.Count(),
                    alturaPromedio = g.Average(l => l.AlturaCm),
                    alturaMaxima = g.Max(l => l.AlturaCm),
                    areaFoliarPromedio = g.Average(l => l.AreaFoliarCm2)
                })
                .FirstOrDefaultAsync();

            return Ok(new
            {
                truchas = new
                {
                    ultimo = ultimaTrucha,
                    estadisticas = statsTruchas
                },
                lechugas = new
                {
                    ultimo = ultimaLechuga,
                    estadisticas = statsLechugas
                },
                timestamp = DateTime.Now
            });
        }

        /// <summary>
        /// Comparación de variables ambientales
        /// </summary>
        [HttpGet("ambiente/comparacion")]
        public async Task<ActionResult<object>> GetAmbienteComparacion()
        {
            var truchaTemp = await _context.TruchasData
                .OrderByDescending(t => t.TiempoSegundos)
                .Take(100)
                .Select(t => new { tiempo = t.TiempoSegundos / 86400.0, temperatura = t.TemperaturaC, pH = t.pH })
                .ToListAsync();

            var lechugaTemp = await _context.LechugasData
                .OrderByDescending(l => l.TiempoSegundos)
                .Take(100)
                .Select(l => new { tiempo = l.TiempoSegundos / 86400.0, temperatura = l.TemperaturaC, pH = l.pH, humedad = l.HumedadPorcentaje })
                .ToListAsync();

            return Ok(new
            {
                truchas = truchaTemp,
                lechugas = lechugaTemp
            });
        }

        /// <summary>
        /// Datos de truchas - último registro por día usando SQL Raw
        /// </summary>
        [HttpGet("truchas/diario-ultimo")]
        public async Task<ActionResult<object>> GetTruchaDailyLastData()
        {
            try
            {
                var sql = @"
                    WITH DailyMax AS (
                        SELECT 
                            FLOOR(TiempoSegundos / 86400.0) as Dia,
                            MAX(TiempoSegundos) as MaxTiempo
                        FROM TruchasData 
                        GROUP BY FLOOR(TiempoSegundos / 86400.0)
                    )
                    SELECT 
                        t.Id,
                        FLOOR(t.TiempoSegundos / 86400.0) as Dia,
                        t.TiempoSegundos,
                        t.TiempoSegundos / 86400.0 as TiempoDias,
                        t.LongitudCm,
                        t.TemperaturaC,
                        t.ConductividadUsCm,
                        t.pH,
                        t.Timestamp
                    FROM TruchasData t
                    INNER JOIN DailyMax dm ON FLOOR(t.TiempoSegundos / 86400.0) = dm.Dia 
                                          AND t.TiempoSegundos = dm.MaxTiempo
                    ORDER BY t.TiempoSegundos";

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 120;

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        dia = Convert.ToInt32(reader["Dia"]),
                        tiempoSegundos = Convert.ToInt32(reader["TiempoSegundos"]),
                        tiempoDias = Convert.ToDouble(reader["TiempoDias"]),
                        longitudCm = Convert.ToDouble(reader["LongitudCm"]),
                        temperaturaC = Convert.ToDouble(reader["TemperaturaC"]),
                        conductividadUsCm = Convert.ToDouble(reader["ConductividadUsCm"]),
                        pH = Convert.ToDouble(reader["pH"]),
                        timestamp = Convert.ToDateTime(reader["Timestamp"])
                    });
                }

                return Ok(new
                {
                    metadata = new
                    {
                        descripcion = "Último dato de cada día",
                        frecuenciaOriginal = "cada 15 segundos",
                        frecuenciaResultante = "último dato diario",
                        totalDias = results.Count,
                        registrosPorDia = 5760
                    },
                    datos = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error obteniendo datos diarios de truchas", detalles = ex.Message });
            }
        }

        /// <summary>
        /// Datos de lechugas - último registro por día usando SQL Raw
        /// </summary>
        [HttpGet("lechugas/diario-ultimo")]
        public async Task<ActionResult<object>> GetLechugaDailyLastData()
        {
            try
            {
                var sql = @"
                    WITH DailyMax AS (
                        SELECT 
                            FLOOR(TiempoSegundos / 86400.0) as Dia,
                            MAX(TiempoSegundos) as MaxTiempo
                        FROM LechugasData 
                        GROUP BY FLOOR(TiempoSegundos / 86400.0)
                    )
                    SELECT 
                        l.Id,
                        FLOOR(l.TiempoSegundos / 86400.0) as Dia,
                        l.TiempoSegundos,
                        l.TiempoSegundos / 86400.0 as TiempoDias,
                        l.AlturaCm,
                        l.AreaFoliarCm2,
                        l.TemperaturaC,
                        l.HumedadPorcentaje,
                        l.pH,
                        l.Timestamp
                    FROM LechugasData l
                    INNER JOIN DailyMax dm ON FLOOR(l.TiempoSegundos / 86400.0) = dm.Dia 
                                          AND l.TiempoSegundos = dm.MaxTiempo
                    ORDER BY l.TiempoSegundos";

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                using var command = connection.CreateCommand();
                command.CommandText = sql;
                command.CommandTimeout = 120;

                var results = new List<object>();
                using var reader = await command.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    results.Add(new
                    {
                        dia = Convert.ToInt32(reader["Dia"]),
                        tiempoSegundos = Convert.ToInt32(reader["TiempoSegundos"]),
                        tiempoDias = Convert.ToDouble(reader["TiempoDias"]),
                        alturaCm = Convert.ToDouble(reader["AlturaCm"]),
                        areaFoliarCm2 = Convert.ToDouble(reader["AreaFoliarCm2"]),
                        temperaturaC = Convert.ToDouble(reader["TemperaturaC"]),
                        humedadPorcentaje = Convert.ToDouble(reader["HumedadPorcentaje"]),
                        pH = Convert.ToDouble(reader["pH"]),
                        timestamp = Convert.ToDateTime(reader["Timestamp"])
                    });
                }

                return Ok(new
                {
                    metadata = new
                    {
                        descripcion = "Último dato de cada día",
                        frecuenciaOriginal = "cada 15 segundos",
                        frecuenciaResultante = "último dato diario",
                        totalDias = results.Count,
                        registrosPorDia = 5760
                    },
                    datos = results
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error obteniendo datos diarios de lechugas", detalles = ex.Message });
            }
        }

        /// <summary>
        /// Datos combinados - último registro por día de truchas y lechugas usando SQL Raw
        /// </summary>
        [HttpGet("combinado/diario-ultimo")]
        public async Task<ActionResult<object>> GetCombinedDailyLastData()
        {
            try
            {
                // Consulta para truchas
                var sqlTruchas = @"
                    WITH DailyMax AS (
                        SELECT 
                            FLOOR(TiempoSegundos / 86400.0) as Dia,
                            MAX(TiempoSegundos) as MaxTiempo
                        FROM TruchasData 
                        GROUP BY FLOOR(TiempoSegundos / 86400.0)
                    )
                    SELECT 
                        FLOOR(t.TiempoSegundos / 86400.0) as Dia,
                        t.TiempoSegundos,
                        t.TiempoSegundos / 86400.0 as TiempoDias,
                        t.LongitudCm,
                        t.TemperaturaC,
                        t.ConductividadUsCm,
                        t.pH,
                        t.Timestamp
                    FROM TruchasData t
                    INNER JOIN DailyMax dm ON FLOOR(t.TiempoSegundos / 86400.0) = dm.Dia 
                                          AND t.TiempoSegundos = dm.MaxTiempo
                    ORDER BY t.TiempoSegundos";

                // Consulta para lechugas
                var sqlLechugas = @"
                    WITH DailyMax AS (
                        SELECT 
                            FLOOR(TiempoSegundos / 86400.0) as Dia,
                            MAX(TiempoSegundos) as MaxTiempo
                        FROM LechugasData 
                        GROUP BY FLOOR(TiempoSegundos / 86400.0)
                    )
                    SELECT 
                        FLOOR(l.TiempoSegundos / 86400.0) as Dia,
                        l.TiempoSegundos,
                        l.TiempoSegundos / 86400.0 as TiempoDias,
                        l.AlturaCm,
                        l.AreaFoliarCm2,
                        l.TemperaturaC,
                        l.HumedadPorcentaje,
                        l.pH,
                        l.Timestamp
                    FROM LechugasData l
                    INNER JOIN DailyMax dm ON FLOOR(l.TiempoSegundos / 86400.0) = dm.Dia 
                                          AND l.TiempoSegundos = dm.MaxTiempo
                    ORDER BY l.TiempoSegundos";

                var connection = _context.Database.GetDbConnection();
                await connection.OpenAsync();

                // Ejecutar consulta de truchas
                var truchasResults = new List<object>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlTruchas;
                    command.CommandTimeout = 120;
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        truchasResults.Add(new
                        {
                            dia = Convert.ToInt32(reader["Dia"]),
                            tiempoSegundos = Convert.ToInt32(reader["TiempoSegundos"]),
                            tiempoDias = Convert.ToDouble(reader["TiempoDias"]),
                            longitudCm = Convert.ToDouble(reader["LongitudCm"]),
                            temperaturaC = Convert.ToDouble(reader["TemperaturaC"]),
                            conductividadUsCm = Convert.ToDouble(reader["ConductividadUsCm"]),
                            pH = Convert.ToDouble(reader["pH"]),
                            timestamp = Convert.ToDateTime(reader["Timestamp"]),
                            tipo = "trucha"
                        });
                    }
                }

                // Ejecutar consulta de lechugas
                var lechugasResults = new List<object>();
                using (var command = connection.CreateCommand())
                {
                    command.CommandText = sqlLechugas;
                    command.CommandTimeout = 120;
                    using var reader = await command.ExecuteReaderAsync();

                    while (await reader.ReadAsync())
                    {
                        lechugasResults.Add(new
                        {
                            dia = Convert.ToInt32(reader["Dia"]),
                            tiempoSegundos = Convert.ToInt32(reader["TiempoSegundos"]),
                            tiempoDias = Convert.ToDouble(reader["TiempoDias"]),
                            alturaCm = Convert.ToDouble(reader["AlturaCm"]),
                            areaFoliarCm2 = Convert.ToDouble(reader["AreaFoliarCm2"]),
                            temperaturaC = Convert.ToDouble(reader["TemperaturaC"]),
                            humedadPorcentaje = Convert.ToDouble(reader["HumedadPorcentaje"]),
                            pH = Convert.ToDouble(reader["pH"]),
                            timestamp = Convert.ToDateTime(reader["Timestamp"]),
                            tipo = "lechuga"
                        });
                    }
                }

                return Ok(new
                {
                    metadata = new
                    {
                        descripcion = "Último dato de cada día para truchas y lechugas",
                        frecuenciaOriginal = "cada 15 segundos",
                        frecuenciaResultante = "último dato diario",
                        diasTruchas = truchasResults.Count,
                        diasLechugas = lechugasResults.Count,
                        registrosPorDia = 5760
                    },
                    truchas = truchasResults,
                    lechugas = lechugasResults
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error obteniendo datos diarios combinados", detalles = ex.Message });
            }
        }
    }
}
