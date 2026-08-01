using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Models;

namespace RhineWaterApi.Data;

public static class DbInitializer
{
    public static void Seed(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Автоматично прилагане на миграции
        db.Database.Migrate();

        // 2. Взимаме станциите от кода по-долу
        var stationsInCode = GetStations();

        // 3. УМНАТА ЛОГИКА (Upsert: Update + Insert)
        foreach (var station in stationsInCode)
        {
            // Търсим дали станцията вече съществува в базата
            var existingStation = db.StationConfigs
                .FirstOrDefault(x => x.StationName == station.StationName);

            if (existingStation != null)
            {
                // АКО СЪЩЕСТВУВА -> Обновяваме данните ѝ с тези от кода
                // Така ако промениш някое число долу, то ще се обнови в базата!
                existingStation.GlwCm = station.GlwCm;
                existingStation.GuaranteedDepthCm = station.GuaranteedDepthCm;
                existingStation.IsMainStation = station.IsMainStation;
            }
            else
            {
                // АКО Е НОВА -> Добавяме я в базата
                db.StationConfigs.Add(station);
            }
        }

        // Записваме промените наведнъж
        db.SaveChanges();
    }

    private static List<StationConfig> GetStations()
    {
        return new List<StationConfig>
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
            new() { StationName = "SPEYER", GlwCm = 237, GuaranteedDepthCm = 210, IsMainStation = true },

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
            new() { StationName = "KAUB", GlwCm = 77, GuaranteedDepthCm = 190, IsMainStation = true },
            new() { StationName = "SANKT GOAR", GlwCm = 166, GuaranteedDepthCm = 190, IsMainStation = false },
            new() { StationName = "BOPPARD", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = false },
            // new() { StationName = "BRAUBACH", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = false },
            new() { StationName = "KOBLENZ", GlwCm = 77, GuaranteedDepthCm = 210, IsMainStation = true },
            new() { StationName = "Neuwied Stadt", GlwCm = 100, GuaranteedDepthCm = 250, IsMainStation = false },
            new() { StationName = "ANDERNACH", GlwCm = 91, GuaranteedDepthCm = 250, IsMainStation = false },
            new() { StationName = "OBERWINTER", GlwCm = 100, GuaranteedDepthCm = 250, IsMainStation = false },

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
            new() { StationName = "EMMERICH", GlwCm = 74, GuaranteedDepthCm = 250, IsMainStation = true },

            // === ХОЛАНДСКИ УЧАСТЪК ===
            new() { StationName = "LOBITH", GlwCm = 733, GuaranteedDepthCm = 280, IsMainStation = true },
            new() { StationName = "PANNERDENSE KOP", GlwCm = 700, GuaranteedDepthCm = 280, IsMainStation = false },

            // НАЙМИНГЕН (Nijmegen на река Ваал - Холандия)
            new() { StationName = "NIJMEGEN HAVEN", GlwCm = 516, GuaranteedDepthCm = 280, IsMainStation = true },

            // ТИЛ (Вал Тил / Tiel на река Ваал - Холандия)
            new() { StationName = "TIEL", GlwCm = 255, GuaranteedDepthCm = 280, IsMainStation = true }
        };
    }
}