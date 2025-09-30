using AquacultureAPI.Data;
using AquacultureAPI.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.OpenApi.Models;

var builder = WebApplication.CreateBuilder(args);

// ------------------- Servicios -------------------
// Controladores
builder.Services.AddControllers();

// Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Aquaculture API",
        Version = "v1",
        Description = "API para gestión del sistema acuapónico"
    });
});

// Entity Framework (registrar DbContext)
builder.Services.AddDbContext<AquacultureContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection"))
);

// Registrar servicios personalizados para inyección de dependencias
builder.Services.AddSingleton<TruchaSimulationService>();
builder.Services.AddSingleton<LechugaSimulationService>();
builder.Services.AddHostedService<SimulationBackgroundService>();

// CRÍTICO: Registrar el BackgroundService que genera datos cada 15 segundos
builder.Services.AddHostedService<SimulationBackgroundService>();

// SignalR (si lo usas para enviar datos en tiempo real)
builder.Services.AddSignalR();

// CORS (permite conexiones desde cualquier origen/red)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ------------------- App -------------------
var app = builder.Build();

// Forzar a escuchar en todas las interfaces de red (LAN/WiFi)
app.Urls.Add("http://0.0.0.0:5100");

// Aplicar migraciones y ejecutar seeder (solo una vez)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var logger = services.GetRequiredService<ILogger<Program>>();
    try
    {
        var context = services.GetRequiredService<AquacultureContext>();

        logger.LogInformation("Aplicando migraciones pendientes...");
        context.Database.Migrate();

        logger.LogInformation("Ejecutando DbInitializer para datos históricos...");
        DbInitializer.Initialize(context);

        logger.LogInformation("✓ Base de datos lista");
    }
    catch (Exception ex)
    {
        logger.LogError(ex, "❌ Error aplicando migraciones o inicializando datos");
    }
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "Aquaculture API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowAll");
app.UseAuthorization();
app.MapControllers();

// Si usas SignalR, mapea el hub
app.MapHub<SimulationHub>("/simulationHub");

app.Run();