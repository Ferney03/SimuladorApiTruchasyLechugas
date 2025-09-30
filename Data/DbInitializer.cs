using AquacultureAPI.Models;

namespace AquacultureAPI.Data
{
    public static class DbInitializer
    {
        public static void Initialize(AquacultureContext context)
        {
            // Si ya hay datos, no volver a insertarlos
            if (context.TruchasData.Any() || context.LechugasData.Any())
            {
                Console.WriteLine("Ya existen datos en la base de datos. No se generarán nuevos datos históricos.");
                return;
            }

            Console.WriteLine("==============================================");
            Console.WriteLine("Generando datos históricos de 2 meses...");
            Console.WriteLine("==============================================");

            // 2 meses = 60 días
            // Datos cada 15 minutos = 4 registros por hora = 96 registros por día
            // Total: 60 días × 96 registros = 5,760 registros
            var inicio = DateTime.UtcNow.AddDays(-60);
            int intervaloSegundos = 900; // 15 minutos = 900 segundos
            int totalRegistros = 60 * 24 * 4; // 5,760 registros

            var random = new Random(42); // Semilla fija para reproducibilidad

            // ---------- Datos históricos para Truchas ----------
            Console.WriteLine($"Generando {totalRegistros:N0} registros de truchas...");
            var truchas = new List<TruchaData>();

            for (int i = 0; i < totalRegistros; i++)
            {
                int tiempoSegundos = i * intervaloSegundos;
                DateTime timestamp = inicio.AddSeconds(tiempoSegundos);

                // Calcular mes actual
                double mesesReales = tiempoSegundos / (30.0 * 24.0 * 60.0 * 60.0);

                // Crecimiento realista de trucha
                double longitudBase = CalcularLongitudTrucha(mesesReales);
                double temperatura = 15.0 + Math.Sin(tiempoSegundos / 10000.0) * 2 + random.NextGaussian() * 0.5;
                double conductividad = 550.0 + random.NextGaussian() * 100;
                double pH = 7.75 + random.NextGaussian() * 0.3;

                // Generar anomalía cada 500 datos
                bool esAnomalia = (i % 500) == 0;
                if (esAnomalia)
                {
                    temperatura = random.NextDouble() < 0.5 ? 10.5 : 20.5;
                    conductividad = random.NextDouble() < 0.5 ? 250 : 850;
                    pH = random.NextDouble() < 0.5 ? 6.6 : 8.8;
                }

                truchas.Add(new TruchaData
                {
                    LongitudCm = Math.Round(Math.Max(2.5, longitudBase + random.NextGaussian() * 0.3), 4),
                    TemperaturaC = Math.Round(Math.Max(12, Math.Min(18, temperatura)), 4),
                    ConductividadUsCm = Math.Round(Math.Max(300, Math.Min(800, conductividad)), 4),
                    pH = Math.Round(Math.Max(7.0, Math.Min(8.5, pH)), 4),
                    TiempoSegundos = tiempoSegundos,
                    Timestamp = timestamp
                });

                // Progreso cada 1000 registros
                if ((i + 1) % 1000 == 0)
                {
                    Console.WriteLine($"  Truchas: {i + 1:N0}/{totalRegistros:N0} registros generados...");
                }
            }

            Console.WriteLine("Insertando datos de truchas en la base de datos...");
            context.TruchasData.AddRange(truchas);
            context.SaveChanges();
            Console.WriteLine($"✓ {truchas.Count:N0} registros de truchas insertados");

            // ---------- Datos históricos para Lechugas ----------
            Console.WriteLine($"\nGenerando {totalRegistros:N0} registros de lechugas...");
            var lechugas = new List<LechugaData>();

            for (int i = 0; i < totalRegistros; i++)
            {
                int tiempoSegundos = i * intervaloSegundos;
                DateTime timestamp = inicio.AddSeconds(tiempoSegundos);

                // Calcular días reales
                double diasReales = tiempoSegundos / 86400.0;

                // Crecimiento realista de lechuga
                double alturaBase = CalcularAlturaLechuga(diasReales);
                double areaBase = Math.Pow(alturaBase / 16.0, 2) * 2000.0;
                double temperatura = 22.0 + Math.Sin(tiempoSegundos / 15000.0) * 1.5 + random.NextGaussian() * 0.4;
                double humedad = 70.0 + Math.Sin(tiempoSegundos / 10000.0) * 10 + random.NextGaussian() * 2;
                double pH = 6.5 + random.NextGaussian() * 0.05;

                lechugas.Add(new LechugaData
                {
                    AlturaCm = Math.Round(Math.Max(1.0, Math.Min(16.0, alturaBase + random.NextGaussian() * 0.2)), 4),
                    AreaFoliarCm2 = Math.Round(Math.Max(10.0, Math.Min(2000.0, areaBase + random.NextGaussian() * 50)), 4),
                    TemperaturaC = Math.Round(Math.Max(15, Math.Min(30, temperatura)), 4),
                    HumedadPorcentaje = Math.Round(Math.Max(50, Math.Min(90, humedad)), 4),
                    pH = Math.Round(Math.Max(5.5, Math.Min(7.5, pH)), 4),
                    TiempoSegundos = tiempoSegundos,
                    Timestamp = timestamp
                });

                // Progreso cada 1000 registros
                if ((i + 1) % 1000 == 0)
                {
                    Console.WriteLine($"  Lechugas: {i + 1:N0}/{totalRegistros:N0} registros generados...");
                }
            }

            Console.WriteLine("Insertando datos de lechugas en la base de datos...");
            context.LechugasData.AddRange(lechugas);
            context.SaveChanges();
            Console.WriteLine($"✓ {lechugas.Count:N0} registros de lechugas insertados");

            Console.WriteLine("==============================================");
            Console.WriteLine("✓ Datos históricos generados exitosamente");
            Console.WriteLine($"  Total truchas: {truchas.Count:N0}");
            Console.WriteLine($"  Total lechugas: {lechugas.Count:N0}");
            Console.WriteLine($"  Rango de fechas: {inicio:yyyy-MM-dd HH:mm} a {DateTime.UtcNow:yyyy-MM-dd HH:mm}");
            Console.WriteLine("==============================================");
        }

        private static double CalcularLongitudTrucha(double mesesReales)
        {
            // Crecimiento basado en los rangos del servicio
            if (mesesReales < 1) return 2.5 + (mesesReales * 2.5); // 0-1 mes: 2.5-5 cm
            if (mesesReales < 2) return 5.0 + ((mesesReales - 1) * 3.0); // 1-2 meses: 5-8 cm
            if (mesesReales < 3) return 8.0 + ((mesesReales - 2) * 4.0); // 2-3 meses: 8-12 cm
            if (mesesReales < 4) return 12.0 + ((mesesReales - 3) * 8.0); // 3-4 meses: 12-20 cm
            if (mesesReales < 5) return 20.0 + ((mesesReales - 4) * 12.5); // 4-5 meses: 20-32.5 cm
            if (mesesReales < 6) return 32.5 + ((mesesReales - 5) * 7.5); // 5-6 meses: 32.5-40 cm
            if (mesesReales < 7) return 40.0 + ((mesesReales - 6) * 6.5); // 6-7 meses: 40-46.5 cm
            if (mesesReales < 8) return 46.5 + ((mesesReales - 7) * 5.75); // 7-8 meses: 46.5-52.25 cm
            return 52.25; // 8+ meses: mantener máximo
        }

        private static double CalcularAlturaLechuga(double diasReales)
        {
            // Crecimiento logístico
            double alturaMaxima = 16.0;
            double tasaCrecimiento = 0.08;
            double t0 = 30.0; // Punto de inflexión

            if (diasReales >= 90)
            {
                return alturaMaxima; // Después de 90 días, mantener máximo
            }

            return alturaMaxima / (1 + Math.Exp(-tasaCrecimiento * (diasReales - t0)));
        }
    }

    // Extensión para generar números gaussianos (distribución normal)
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