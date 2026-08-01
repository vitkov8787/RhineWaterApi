using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Data;
using RhineWaterApi.Models;
using RhineWaterApi.DTOs;

namespace RhineWaterApi.Services;

public class RhineSyncWorker : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RhineSyncWorker> _logger;

    public RhineSyncWorker(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, ILogger<RhineSyncWorker> logger)
    {
        _serviceProvider = serviceProvider;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try { await SyncData(); }
            catch (Exception ex) { _logger.LogError(ex, "Грешка в SyncWorker."); }

            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

    private async Task SyncData()
    {
        using var scope = _serviceProvider.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // 1. Почистване: Пазим последните 2 дни (48 часа)
        var cutOff = DateTime.UtcNow.AddDays(-2);
        var old = await db.RhineLevels.Where(r => r.MeasuredAt < cutOff).ToListAsync();
        if (old.Any()) { db.RhineLevels.RemoveRange(old); await db.SaveChangesAsync(); }

        // 2. Теглене от API
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "RhineApp/1.0");
        var url = "https://www.pegelonline.wsv.de/webservices/rest-api/v2/stations.json?waters=RHEIN,WAAL&includeTimeseries=true&includeCurrentMeasurement=true";
        
        var apiData = await client.GetFromJsonAsync<List<PegelStationDto>>(url);
        if (apiData == null) return;

        int added = 0;
        foreach (var s in apiData)
        {
            var series = s.Timeseries?.FirstOrDefault(t => t.Shortname == "W");
            if (series?.CurrentMeasurement == null) continue;

            var utcTime = series.CurrentMeasurement.Timestamp.ToUniversalTime();
            
            
            var levelCm =
                (int)Math.Round(series.CurrentMeasurement.Value);

            var config = await db.StationConfigs
                .FirstOrDefaultAsync(c =>
                    c.StationName.ToLower() == s.Name.ToLower());

            if (config != null)
            {
                var channelDepth =
                    (levelCm - config.GlwCm)
                    + config.GuaranteedDepthCm;

                var recommendedDraft =
                    channelDepth - config.SafetyMarginCm;

                var today =
                    DateOnly.FromDateTime(DateTime.UtcNow);

                var history =
                    await db.DepthHistories.FirstOrDefaultAsync(h =>
                        h.StationName == s.Name &&
                        h.Date == today);

                if (history == null)
                {
                    db.DepthHistories.Add(new DepthHistory
                    {
                        StationName = s.Name,
                        Date = today,
                        MinWaterLevelCm = levelCm,
                        MinChannelDepthCm = channelDepth,
                        MinRecommendedDraftCm = recommendedDraft,
                        UpdatedAt = DateTime.UtcNow
                    });
                }
                else
                {
                    history.MinWaterLevelCm =
                        Math.Min(history.MinWaterLevelCm, levelCm);

                    history.MinChannelDepthCm =
                        Math.Min(history.MinChannelDepthCm, channelDepth);

                    history.MinRecommendedDraftCm =
                        Math.Min(history.MinRecommendedDraftCm, recommendedDraft);

                    history.UpdatedAt = DateTime.UtcNow;
                }
            }
            
            if (!await db.RhineLevels.AnyAsync(r => r.StationName == s.Name && r.MeasuredAt == utcTime))
            {
                db.RhineLevels.Add(new RhineWaterLevel {
                    StationName = s.Name, Kilometer = s.Km, LevelCm = (int)Math.Round(series.CurrentMeasurement.Value), MeasuredAt = utcTime
                });
                added++;
            }
        }
        if (added > 0) await db.SaveChangesAsync();
        _logger.LogInformation("Синхронизация: +{count} нови записа.", added);
    }
}