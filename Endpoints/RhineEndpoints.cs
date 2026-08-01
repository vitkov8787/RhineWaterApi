using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Data;

namespace RhineWaterApi.Endpoints;

public static class RhineEndpoints
{
    // Това е "разширяващ метод" (Extension Method)
    public static void MapRhineEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/rhine");

        // 1. АКТУАЛНО СЪСТОЯНИЕ
        group.MapGet("/latest", async (AppDbContext db) =>
        {
            var allLevels = await db.RhineLevels.AsNoTracking().ToListAsync();
            var configs = await db.StationConfigs.AsNoTracking().ToListAsync();

            var result = allLevels
                .GroupBy(l => l.StationName)
                .Select(group =>
                {
                    var lastUpdate = group.OrderByDescending(l => l.MeasuredAt).First();
                    var config = configs.FirstOrDefault(c => 
                        c.StationName.Equals(lastUpdate.StationName, StringComparison.OrdinalIgnoreCase));

                    int? channelDepth = null;
                    int? maxDraft = null;

                    if (config != null)
                    {
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

        // 2. ЦЯЛАТА ИСТОРИЯ (за таблица)
        group.MapGet("/history", async (AppDbContext db) =>
        {
            return Results.Ok(await db.RhineLevels.AsNoTracking()
                .OrderByDescending(r => r.MeasuredAt)
                .ToListAsync());
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