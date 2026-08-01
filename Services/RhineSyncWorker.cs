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
            try 
            { 
                _logger.LogInformation("Стартиране на синхронизация: {time}", DateTime.Now);
                await SyncData(); 
            }
            catch (Exception ex) 
            { 
                _logger.LogError(ex, "Грешка по време на синхронизация в RhineSyncWorker."); 
            }

            // Изчакване 15 минути преди следващото теглене
            await Task.Delay(TimeSpan.FromMinutes(15), stoppingToken);
        }
    }

  private async Task SyncData()
{
    using var scope = _serviceProvider.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

    // 1. Оптимизирано изтриване (EF Core 7+) - става с 1 заявка директно в базата
    var cutOff = DateTime.UtcNow.AddDays(-2);
    await db.RhineLevels.Where(r => r.MeasuredAt < cutOff).ExecuteDeleteAsync();

    // 2. Теглене на данни
    var client = _httpClientFactory.CreateClient();
    client.DefaultRequestHeaders.Add("User-Agent", "RhineApp/1.0");
    var url = "https://www.pegelonline.wsv.de/webservices/rest-api/v2/stations.json?waters=RHEIN,WAAL&includeTimeseries=true&includeCurrentMeasurement=true";
    
    var apiData = await client.GetFromJsonAsync<List<PegelStationDto>>(url);
    if (apiData == null || !apiData.Any()) return;

    

    // Вземаме всички уникални ключове (Име + Час), които ВЕЧЕ са в базата за последните 2 часа
    var recentThreshold = DateTime.UtcNow.AddHours(-2);
    var existingLevels = await db.RhineLevels
        .Where(r => r.MeasuredAt > recentThreshold)
        .Select(r => new { r.StationName, r.MeasuredAt })
        .ToListAsync();

    // Правим HashSet за светкавична проверка в паметта
    var existingSet = new HashSet<string>(existingLevels.Select(x => $"{x.StationName}_{x.MeasuredAt}"));

    var configs = await db.StationConfigs.ToDictionaryAsync(c => c.StationName.ToLower());
    var today = DateOnly.FromDateTime(DateTime.UtcNow);
    var todayHistories = await db.DepthHistories
        .Where(h => h.Date == today)
        .ToDictionaryAsync(h => h.StationName.ToLower());

    bool hasChanges = false;
    int newLevelsCount = 0;

    foreach (var s in apiData)
    {
        var series = s.Timeseries?.FirstOrDefault(t => t.Shortname == "W");
        if (series?.CurrentMeasurement == null) continue;

        var utcTime = series.CurrentMeasurement.Timestamp.ToUniversalTime();
        var levelCm = (int)Math.Round(series.CurrentMeasurement.Value);
        var stationKey = s.Name.ToLower();

        // 1. ПРОВЕРКА ЗА ИСТОРИЯ (същата логика)
        if (configs.TryGetValue(stationKey, out var config))
        {
            var channelDepth = (levelCm - config.GlwCm) + config.GuaranteedDepthCm;
            var recommendedDraft = channelDepth - config.SafetyMarginCm;

            if (todayHistories.TryGetValue(stationKey, out var history))
            {
                bool historyUpdated = false;
                if (levelCm < history.MinWaterLevelCm) { history.MinWaterLevelCm = levelCm; historyUpdated = true; }
                if (channelDepth < history.MinChannelDepthCm) { history.MinChannelDepthCm = channelDepth; historyUpdated = true; }
                if (recommendedDraft < history.MinRecommendedDraftCm) { history.MinRecommendedDraftCm = recommendedDraft; historyUpdated = true; }

                if (historyUpdated) { history.UpdatedAt = DateTime.UtcNow; hasChanges = true; }
            }
            else
            {
                var newHistory = new DepthHistory {
                    StationName = s.Name, Date = today, MinWaterLevelCm = levelCm,
                    MinChannelDepthCm = channelDepth, MinRecommendedDraftCm = recommendedDraft, UpdatedAt = DateTime.UtcNow
                };
                db.DepthHistories.Add(newHistory);
                todayHistories[stationKey] = newHistory;
                hasChanges = true;
            }
        }

        // 2. ПРОВЕРКА ЗА ТЕКУЩИ НИВА (ОПТИМИЗИРАНА)
        var checkKey = $"{s.Name}_{utcTime}";
        if (!existingSet.Contains(checkKey)) // Проверяваме в HashSet-а, а не в базата!
        {
            db.RhineLevels.Add(new RhineWaterLevel {
                StationName = s.Name, Kilometer = s.Km, LevelCm = levelCm, MeasuredAt = utcTime
            });
            newLevelsCount++;
            hasChanges = true;
            // Добавяме в сета, за да не го добавим повторно, ако API-то върне дубликати
            existingSet.Add(checkKey); 
        }
    }

    if (hasChanges) 
    { 
        await db.SaveChangesAsync(); 
        _logger.LogInformation("Синхронизация: +{levels} нови нива.", newLevelsCount);
    }
}
}