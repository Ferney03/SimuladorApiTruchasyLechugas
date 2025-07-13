using Microsoft.EntityFrameworkCore;
using AquacultureAPI.Data;
using AquacultureAPI.Services;
using Microsoft.Extensions.Logging;

var builder = WebApplication.CreateBuilder(args);

// CONFIGURAR PARA ESCUCHAR EN TODAS LAS INTERFACES DE RED
builder.WebHost.UseUrls("http://0.0.0.0:55839");

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Add Entity Framework
builder.Services.AddDbContext<AquacultureContext>(options =>
{
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions =>
        {
            sqlOptions.CommandTimeout(300); // 5 minutos de timeout
            sqlOptions.EnableRetryOnFailure(3); // Reintentos automáticos
        });
});

// Add SignalR
builder.Services.AddSignalR();

// Add simulation services
builder.Services.AddSingleton<TruchaSimulationService>();
builder.Services.AddSingleton<LechugaSimulationService>();
builder.Services.AddHostedService<SimulationBackgroundService>();

// Agregar el servicio de seed después de AddHostedService
builder.Services.AddScoped<DataSeedService>();

// Add CORS para permitir acceso desde cualquier origen
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// REMOVER UseHttpsRedirection para evitar problemas de puerto
// app.UseHttpsRedirection();

app.UseCors("AllowAll");
app.UseAuthorization();

app.MapControllers();
app.MapHub<SimulationHub>("/simulationHub");

// Ensure database is created and seed data
using (var scope = app.Services.CreateScope())
{
    try
    {
        var context = scope.ServiceProvider.GetRequiredService<AquacultureContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

        logger.LogInformation("Creando base de datos...");
        context.Database.SetCommandTimeout(600); // 10 minutos para operaciones de inicialización
        context.Database.EnsureCreated();

        logger.LogInformation("Iniciando seed de datos históricos...");
        var seedService = scope.ServiceProvider.GetRequiredService<DataSeedService>();
        await seedService.SeedHistoricalDataAsync();

        logger.LogInformation("Inicialización completada exitosamente");
        logger.LogInformation("API disponible en: http://0.0.0.0:55839");
        logger.LogInformation("Swagger disponible en: http://0.0.0.0:55839/swagger");
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error durante la inicialización de la base de datos");
        // No lanzar la excepción para que la aplicación pueda continuar
    }
}

app.Run();
