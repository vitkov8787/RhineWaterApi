using Microsoft.EntityFrameworkCore;
using RhineWaterApi.Models;

namespace RhineWaterApi.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }
    public DbSet<RhineWaterLevel> RhineLevels => Set<RhineWaterLevel>();
    public DbSet<StationConfig> StationConfigs => Set<StationConfig>();
}