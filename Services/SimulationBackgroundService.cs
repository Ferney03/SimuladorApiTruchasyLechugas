using Microsoft.AspNetCore.SignalR;
using AquacultureAPI.Data;
using AquacultureAPI.Models;

namespace AquacultureAPI.Services
{
    public class SimulationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHubContext<SimulationHub> _hubContext;
        private readonly TruchaSimulationService _truchaService;
        private readonly LechugaSimulationService _lechugaService;
        private readonly ILogger<SimulationBackgroundService> _logger;

        public SimulationBackgroundService(
            IServiceProvider serviceProvider,
            IHubContext<SimulationHub> hubContext,
            TruchaSimulationService truchaService,
            LechugaSimulationService lechugaService,
            ILogger<SimulationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _hubContext = hubContext;
            _truchaService = truchaService;
            _lechugaService = lechugaService;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            // Esperar un poco para que la aplicación se inicialice completamente
            await Task.Delay(2000, stoppingToken);

            // Inicializar servicios desde la base de datos
            await _truchaService.InitializeFromDatabaseAsync();
            await _lechugaService.InitializeFromDatabaseAsync();

            _logger.LogInformation("Simulación iniciada desde el último punto registrado");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    // Generar datos de truchas
                    var truchaData = await _truchaService.GenerateNextDataAsync();

                    // Generar datos de lechugas
                    var lechugaData = await _lechugaService.GenerateNextDataAsync();

                    // Guardar en base de datos
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<AquacultureContext>();

                        context.TruchasData.Add(truchaData);
                        context.LechugasData.Add(lechugaData);

                        await context.SaveChangesAsync();
                    }

                    // Enviar datos via SignalR
                    await _hubContext.Clients.Group("truchas").SendAsync("TruchaDataUpdate", truchaData, stoppingToken);
                    await _hubContext.Clients.Group("lechugas").SendAsync("LechugaDataUpdate", lechugaData, stoppingToken);

                    _logger.LogInformation($"Datos generados - Tiempo: {truchaData.TiempoSegundos}s, Trucha: {truchaData.LongitudCm}cm, Lechuga: {lechugaData.AlturaCm}cm");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error generando datos de simulación");
                }

                await Task.Delay(15000, stoppingToken); // Cada 15 segundos
            }
        }
    }
}
