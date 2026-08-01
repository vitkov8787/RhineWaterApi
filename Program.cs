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
    
    //db.StationConfigs.RemoveRange(db.StationConfigs);
    //db.SaveChanges();

    if (!db.StationConfigs.Any())
    {
        db.StationConfigs.AddRange(new List<StationConfig>
        {
            // === ГОРЕН РЕЙН ===
            new() { StationName = "KONSTANZ-RHEIN", GlwCm = 223, GuaranteedDepthCm = 250, IsMainStation = false },
            new() { StationName = "Basel-Rheinhalle", GlwCm = 501, GuaranteedDepthCm = 300, IsMainStation = false },
            new() { StationName = "RHEINWEILER", GlwCm = 205, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "BREISACH", GlwCm = 158, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "RUST", GlwCm = 65, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "OTTENHEIM", GlwCm = 255, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "KEHL-KRONENHOF", GlwCm = 160, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "IFFEZHEIM", GlwCm = 105, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "PLITTERSDORF", GlwCm = 185, GuaranteedDepthCm = 210, IsMainStation = false },

            // МАКСАУ (Главен пегел за Горен Рейн)
            new() { StationName = "MAXAU", GlwCm = 372, GuaranteedDepthCm = 210, IsMainStation = true },
            new() { StationName = "PHILIPPSBURG", GlwCm = 165, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "SPEYER", GlwCm = 237, GuaranteedDepthCm = 210, IsMainStation = false },

            // МАНХАЙМ
            new() { StationName = "MANNHEIM", GlwCm = 155, GuaranteedDepthCm = 210, IsMainStation = true },
            new() { StationName = "WORMS", GlwCm = 68, GuaranteedDepthCm = 210, IsMainStation = false },

            // === СРЕДЕН РЕЙН ===
            new() { StationName = "NIERSTEIN-OPPENHEIM", GlwCm = 145, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "Bodenheim", GlwCm = 140, GuaranteedDepthCm = 210, IsMainStation = false },

            // МАЙНЦ
            new() { StationName = "MAINZ", GlwCm = 171, GuaranteedDepthCm = 210, IsMainStation = true },

            // ЙОСТРИХ
            new() { StationName = "OESTRICH", GlwCm = 92, GuaranteedDepthCm = 190, IsMainStation = true },
            new() { StationName = "BINGEN", GlwCm = 97, GuaranteedDepthCm = 190, IsMainStation = false },
            new() { StationName = "KAUB", GlwCm = 77, GuaranteedDepthCm = 190, IsMainStation = false },
            new() { StationName = "SANKT GOAR", GlwCm = 77, GuaranteedDepthCm = 190, IsMainStation = false },
            new() { StationName = "BOPPARD", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "BRAUBACH", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "KOBLENZ", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "Neuwied Stadt", GlwCm = 100, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "ANDERNACH", GlwCm = 91, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "OBERWINTER", GlwCm = 100, GuaranteedDepthCm = 210, IsMainStation = false },

            // === ДОЛЕН РЕЙН ===
            new() { StationName = "BONN", GlwCm = 142, GuaranteedDepthCm = 250, IsMainStation = false },

            // КЬОЛН
            new() { StationName = "KÖLN", GlwCm = 139, GuaranteedDepthCm = 250, IsMainStation = true },

            // ДЮСЕЛДОРФ
            new() { StationName = "DÜSSELDORF", GlwCm = 91, GuaranteedDepthCm = 250, IsMainStation = true },

            // ДУИЗБУРГ
            new() { StationName = "DUISBURG-RUHRORT", GlwCm = 227, GuaranteedDepthCm = 250, IsMainStation = true },
            new() { StationName = "WESEL", GlwCm = 174, GuaranteedDepthCm = 250, IsMainStation = false },
            new() { StationName = "REES", GlwCm = 118, GuaranteedDepthCm = 250, IsMainStation = false },
            new() { StationName = "EMMERICH", GlwCm = 74, GuaranteedDepthCm = 250, IsMainStation = false },

            // === ХОЛАНДСКИ УЧАСТЪК ===
            new() { StationName = "LOBITH", GlwCm = 733, GuaranteedDepthCm = 280, IsMainStation = false },
            new() { StationName = "PANNERDENSE KOP", GlwCm = 700, GuaranteedDepthCm = 280, IsMainStation = false },

            // НАЙМИНГЕН (Nijmegen на река Ваал - Холандия)
            new() { StationName = "NIJMEGEN", GlwCm = 520, GuaranteedDepthCm = 280, IsMainStation = true },

            // ТИЛ (Вал Тил / Tiel на река Ваал - Холандия)
            new() { StationName = "TIEL", GlwCm = 435, GuaranteedDepthCm = 280, IsMainStation = true }
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

                IsMainStation = config?.IsMainStation ?? false,

                Calculation = config == null ? null : new
                {
                    GlwReference = config.GlwCm,
                    Fahrrinnentiefe = config.GuaranteedDepthCm,
                    EstimatedTotalDepth = channelDepth,
                    RecommendedMaxDraft = maxDraft,
                    SafetyReserve = config.SafetyMarginCm
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