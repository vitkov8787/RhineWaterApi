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

        // 1. Почистване: Изтриваме записи в RhineLevels, по-стари от 48 часа (за да не пълним базата излишно)
        var cutOff = DateTime.UtcNow.AddDays(-2);
        var oldLevels = await db.RhineLevels.Where(r => r.MeasuredAt < cutOff).ToListAsync();
        if (oldLevels.Any()) 
        { 
            db.RhineLevels.RemoveRange(oldLevels); 
        }

        // 2. Теглене на данни от PegelOnline API
        var client = _httpClientFactory.CreateClient();
        client.DefaultRequestHeaders.Add("User-Agent", "RhineApp/1.0");
        var url = "https://www.pegelonline.wsv.de/weлсbservices/rest-api/v2/stations.json?waters=RHEIN,WAAL&includeTimeseries=true&includeCurrentMeasurement=true";
        
        var apiData = await client.GetFromJsonAsync<List<PegelStationDto>>(url);
        if (apiData == null || !apiData.Any()) return;

        // 3. Зареждаме конфигурациите и днешната история в речници (Dictionaries)
       
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

            // ПРОВЕРКА: Имаме ли конфигурация (GLW и др.) за тази станция?
            if (configs.TryGetValue(stationKey, out var config))
            {
                // Изчисления
                var channelDepth = (levelCm - config.GlwCm) + config.GuaranteedDepthCm;
                var recommendedDraft = channelDepth - config.SafetyMarginCm;

                // ЛОГИКА ЗА ИСТОРИЯТА (ГРАФИКАТА)
                if (todayHistories.TryGetValue(stationKey, out var history))
                {
                    // Ако вече има запис за днес, запазваме само най-ниските намерени стойности (Math.Min)
                    bool historyUpdated = false;

                    if (levelCm < history.MinWaterLevelCm) { history.MinWaterLevelCm = levelCm; historyUpdated = true; }
                    if (channelDepth < history.MinChannelDepthCm) { history.MinChannelDepthCm = channelDepth; historyUpdated = true; }
                    if (recommendedDraft < history.MinRecommendedDraftCm) { history.MinRecommendedDraftCm = recommendedDraft; historyUpdated = true; }

                    if (historyUpdated)
                    {
                        history.UpdatedAt = DateTime.UtcNow;
                        hasChanges = true;
                    }
                }
                else
                {
                    // Ако няма запис за днес, създаваме нов
                    var newHistory = new DepthHistory
                    {
                        StationName = s.Name,
                        Date = today,
                        MinWaterLevelCm = levelCm,
                        MinChannelDepthCm = channelDepth,
                        MinRecommendedDraftCm = recommendedDraft,
                        UpdatedAt = DateTime.UtcNow
                    };
                    db.DepthHistories.Add(newHistory);
                    todayHistories[stationKey] = newHistory; // Добавяме в локалния речник
                    hasChanges = true;
                }
            }
            
            // ЛОГИКА ЗА ТЕКУЩИТЕ НИВА (За таблицата/Dashboard)
            // Проверяваме дали точно това измерване (час/минута) вече не е записано
            var levelExists = await db.RhineLevels.AnyAsync(r => r.StationName == s.Name && r.MeasuredAt == utcTime);
            if (!levelExists)
            {
                db.RhineLevels.Add(new RhineWaterLevel 
                {
                    StationName = s.Name, 
                    Kilometer = s.Km, 
                    LevelCm = levelCm, 
                    MeasuredAt = utcTime
                });
                newLevelsCount++;
                hasChanges = true;
            }
        }

        // 4. Записваме всичко наведнъж в базата данни
        if (hasChanges) 
        { 
            await db.SaveChangesAsync(); 
            _logger.LogInformation("Синхронизация завършена: +{levels} нови нива и обновена история.", newLevelsCount);
        }
        else
        {
            _logger.LogInformation("Синхронизация: Няма нови данни за записване.");
        }
    }
}