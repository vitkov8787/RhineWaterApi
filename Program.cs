using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Data;
using RhineWaterApi.Models;
using RhineWaterApi.Services;
using RhineWaterApi.Endpoints;

var builder = WebApplication.CreateBuilder(args);
// 0.  CORS политика
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()  // Позволява заявки от всякъде (за разработка)
            .AllowAnyMethod()
            .AllowAnyHeader();
    });
});
// 1. Връзка с PostgreSQL
var connectionString = Environment.GetEnvironmentVariable("DATABASE_URL")  ?? throw new Exception("DATABASE_URL not found"); 
builder.Services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

// 2. Регистрация на услуги
builder.Services.AddHttpClient();
builder.Services.AddHostedService<RhineSyncWorker>();

var app = builder.Build();
app.UseCors("AllowAll");
// --- АВТОМАТИЧНО ПРИЛАГАНЕ НА МИГРАЦИИ И SEEDING ---
DbInitializer.Seed(app);
// --- ЕНДПОИНТИ ---

app.MapRhineEndpoints();
//
app.Run();