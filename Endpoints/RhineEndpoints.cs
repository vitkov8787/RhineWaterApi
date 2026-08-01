using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Data;

namespace RhineWaterApi.Endpoints;

public static class RhineEndpoints
{
    // Това е "разширяващ метод" (Extension Method)
    public static void MapRhineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rhine");

// 1. АКТУАЛНО СЪСТОЯНИЕ (ОПТИМИЗИРАНО)
group.MapGet("/latest", async (AppDbContext db) =>
{
    // Вземаме само последното измерване за всяка станция директно чрез LINQ
    // Това е много по-бързо, защото базата ни дава само 50-тина записа, а не 10 000
    var latestLevels = await db.RhineLevels
        .AsNoTracking()
        .GroupBy(l => l.StationName)
        .Select(g => g.OrderByDescending(x => x.MeasuredAt).FirstOrDefault())
        .ToListAsync();

    var configs = await db.StationConfigs.AsNoTracking().ToDictionaryAsync(c => c.StationName.ToLower());

    var result = latestLevels
        .Where(l => l != null)
        .Select(l =>
        {
            configs.TryGetValue(l.StationName.ToLower(), out var config);

            int? channelDepth = null;
            int? maxDraft = null;

            if (config != null)
            {
                channelDepth = (l.LevelCm - config.GlwCm) + config.GuaranteedDepthCm;
                maxDraft = channelDepth - config.SafetyMarginCm;
            }

            return new
            {
                l.StationName,
                l.Kilometer,
                CurrentLevelCm = l.LevelCm,
                l.MeasuredAt,
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

// 2. ЦЯЛАТА ИСТОРИЯ (С ЛИМИТ)
group.MapGet("/history", async (AppDbContext db) =>
{
    // Вземаме само последните 100 записа, за да не претоварим мрежата
    var history = await db.RhineLevels
        .AsNoTracking()
        .OrderByDescending(r => r.MeasuredAt)
        .Take(100) 
        .ToListAsync();

    return Results.Ok(history);
});

        // 3. ИСТОРИЯ ЗА ГРАФИКАТА (Важно за фронтенда!)
        group.MapGet("/depth-history", async (string station, int? days, AppDbContext db) =>
        {
            var query = db.DepthHistories.AsNoTracking().Where(x => x.StationName == station);

            if (days.HasValue)
            {
                var fromDate = DateOnly.FromDateTime(DateTime.UtcNow.AddDays(-days.Value));
                query = query.Where(x => x.Date >= fromDate);
            }

            var result = await query
                .OrderBy(x => x.Date)
                .Select(x => new
                {
                    x.Date,
                    x.MinWaterLevelCm,
                    x.MinChannelDepthCm,
                    x.MinRecommendedDraftCm
                })
                .ToListAsync();

            return Results.Ok(result);
        });

        // 4. СПИСЪК СЪС СТАНЦИИ
        group.MapGet("/stations", async (AppDbContext db) =>
        {
            var configs = await db.StationConfigs
                .AsNoTracking()
                .Select(x => new 
                {
                    x.StationName,
                    x.GlwCm,
                    x.GuaranteedDepthCm
                })
                .OrderBy(x => x.StationName)
                .ToListAsync();

            return Results.Ok(configs);
        });
    }
}