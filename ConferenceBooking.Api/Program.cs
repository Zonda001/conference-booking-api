using ConferenceBooking.Api.Application;
using ConferenceBooking.Api.Data;
using ConferenceBooking.Api.Pricing;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("Default")
                      ?? "Data Source=conference.db"));

// Сітка тарифів незмінна, тому один екземпляр на застосунок.
builder.Services.AddSingleton(TariffSchedule.Default);
builder.Services.AddScoped<PriceCalculator>();
builder.Services.AddScoped<ReservationService>();
builder.Services.AddScoped<ReportService>();

builder.Services.AddControllers();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new()
    {
        Title = "Conference Booking API",
        Version = "v1",
        Description = "Управління конференц-залами, бронюваннями і розрахунком вартості оренди."
    });

    var xml = Path.Combine(AppContext.BaseDirectory,
        $"{System.Reflection.Assembly.GetExecutingAssembly().GetName().Name}.xml");
    if (File.Exists(xml)) options.IncludeXmlComments(xml);
});

var app = builder.Build();

app.UseExceptionHandler();

// База створюється і наповнюється початковими даними з ТЗ на старті.
using (var scope = app.Services.CreateScope())
{
    scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.MapControllers();

app.Run();

/// <summary>Потрібен, щоб інтеграційні тести могли підняти застосунок.</summary>
public partial class Program;
