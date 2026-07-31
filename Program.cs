using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Data;
using RhineWaterApi.Models;
using RhineWaterApi.Services;

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
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    // Автоматично изпълнява миграциите при старт
    db.Database.Migrate();

    if (!db.StationConfigs.Any())
    {
        db.StationConfigs.AddRange(new List<StationConfig>
        {
            //  ФОРМУЛИ (GlW 2022 данни за 2026г.)
            new() { StationName = "KONSTANZ-RHEIN", GlwCm = 223, GuaranteedDepthCm = 250 },
            new() { StationName = "Basel-Rheinhalle", GlwCm = 490, GuaranteedDepthCm = 300 },
            new() { StationName = "RHEINWEILER", GlwCm = 205, GuaranteedDepthCm = 210 },
            new() { StationName = "BREISACH", GlwCm = 158, GuaranteedDepthCm = 210 },
            new() { StationName = "RUST", GlwCm = 65, GuaranteedDepthCm = 210 },
            new() { StationName = "OTTENHEIM", GlwCm = 255, GuaranteedDepthCm = 210 },
            new() { StationName = "KEHL-KRONENHOF", GlwCm = 160, GuaranteedDepthCm = 210 },
            new() { StationName = "IFFEZHEIM", GlwCm = 105, GuaranteedDepthCm = 210 },
            new() { StationName = "PLITTERSDORF", GlwCm = 185, GuaranteedDepthCm = 210 },
            new() { StationName = "MAXAU", GlwCm = 300, GuaranteedDepthCm = 210 },
            new() { StationName = "PHILIPPSBURG", GlwCm = 165, GuaranteedDepthCm = 210 },
            new() { StationName = "SPEYER", GlwCm = 161, GuaranteedDepthCm = 210 },
            new() { StationName = "MANNHEIM", GlwCm = 155, GuaranteedDepthCm = 210 },
            new() { StationName = "WORMS", GlwCm = 104, GuaranteedDepthCm = 210 },
            new() { StationName = "NIERSTEIN-OPPENHEIM", GlwCm = 145, GuaranteedDepthCm = 210 },
            new() { StationName = "Bodenheim", GlwCm = 140, GuaranteedDepthCm = 210 },
            new() { StationName = "MAINZ", GlwCm = 165, GuaranteedDepthCm = 210 },
            new() { StationName = "OESTRICH", GlwCm = 87, GuaranteedDepthCm = 190 },
            new() { StationName = "BINGEN", GlwCm = 79, GuaranteedDepthCm = 190 },
            new() { StationName = "KAUB", GlwCm = 78, GuaranteedDepthCm = 190 },
            new() { StationName = "SANKT GOAR", GlwCm = 78, GuaranteedDepthCm = 190 },
            new() { StationName = "BOPPARD", GlwCm = 78, GuaranteedDepthCm = 210 },
            new() { StationName = "BRAUBACH", GlwCm = 78, GuaranteedDepthCm = 210 },
            new() { StationName = "KOBLENZ", GlwCm = 78, GuaranteedDepthCm = 210 },
            new() { StationName = "Neuwied Stadt", GlwCm = 100, GuaranteedDepthCm = 210 },
            new() { StationName = "ANDERNACH", GlwCm = 100, GuaranteedDepthCm = 210 },
            new() { StationName = "OBERWINTER", GlwCm = 100, GuaranteedDepthCm = 210 },
            new() { StationName = "BONN", GlwCm = 131, GuaranteedDepthCm = 250 },
            new() { StationName = "KÖLN", GlwCm = 139, GuaranteedDepthCm = 250 },
            new() { StationName = "DÜSSELDORF", GlwCm = 114, GuaranteedDepthCm = 250 },
            new() { StationName = "DUISBURG-RUHRORT", GlwCm = 153, GuaranteedDepthCm = 250 },
            new() { StationName = "WESEL", GlwCm = 138, GuaranteedDepthCm = 280 },
            new() { StationName = "REES", GlwCm = 92, GuaranteedDepthCm = 280 },
            new() { StationName = "EMMERICH", GlwCm = 7, GuaranteedDepthCm = 280 },
            new() { StationName = "LOBITH", GlwCm = 595, GuaranteedDepthCm = 280 },
            new() { StationName = "PANNERDENSE KOP", GlwCm = 560, GuaranteedDepthCm = 280 }
        });
        db.SaveChanges();
    }
}

// --- ЕНДПОИНТИ ---

// 1. АКТУАЛНО СЪСТОЯНИЕ С ИЗЧИСЛЕНИЯ
app.MapGet("/api/rhine/latest", async (AppDbContext db) =>
{
    var allLevels = await db.RhineLevels.AsNoTracking().ToListAsync();
    var configs = await db.StationConfigs.AsNoTracking().ToListAsync();

    var result = allLevels
        .GroupBy(l => l.StationName)
        .Select(group =>
        {
            var lastUpdate = group.OrderByDescending(l => l.MeasuredAt).First();
            
            // Търсим настройките за станцията
            var config = configs.FirstOrDefault(c => 
                c.StationName.Equals(lastUpdate.StationName, StringComparison.OrdinalIgnoreCase));

            int? channelDepth = null;
            int? maxDraft = null;

            if (config != null)
            {
                // ТУК Е ЛОГИКАТА НА ФОРМУЛАТА:
                channelDepth = (lastUpdate.LevelCm - config.GlwCm) + config.GuaranteedDepthCm;
                maxDraft = channelDepth - config.SafetyMarginCm;
            }

            return new
            {
                lastUpdate.StationName,
                lastUpdate.Kilometer,
                CurrentLevelCm = lastUpdate.LevelCm,
                lastUpdate.MeasuredAt,
                Calculation = config == null ? null : new
                {
                    GlwReference = config.GlwCm,
                    Fahrrinnentiefe = config.GuaranteedDepthCm,
                    EstimatedTotalDepth = channelDepth,
                    RecommendedMaxDraft = maxDraft,
                    SafetyReserve = config.SafetyMarginCm // Твоят запас е тук!
                },
                Status = maxDraft switch
                {
                    null => "No Configuration",
                    < 160 => "CRITICAL SHALLOW",
                    < 230 => "RESTRICTED DRAFT",
                    _ => "GOOD NAVIGATION"
                }
            };
        })
        .OrderBy(r => r.Kilometer)
        .ToList();

    return Results.Ok(result);
});

// 2. История (опционално)
app.MapGet("/api/rhine/history", async (AppDbContext db) =>
{
    return Results.Ok(await db.RhineLevels.AsNoTracking().OrderByDescending(r => r.MeasuredAt).ToListAsync());
});

app.Run();