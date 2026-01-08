using Microsoft.EntityFrameworkCore;

namespace ConsoleApp.GameEngine
{
    // Database context - bridge between C# and SQLite database
    public class AppDbContext : DbContext
    {
        // GameStates table - saved games
        public DbSet<DbGameState> GameStates { get; set; } = default!;
        
        // GameConfigurations table - saved settings
        public DbSet<DbGameConfiguration> GameConfigurations { get; set; } = default!;
        
        // Constructor for dependency injection
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }
        
        // Database configuration - SQLite connection
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            // If not configured elsewhere, use connect4.db file
            if (!optionsBuilder.IsConfigured)
            {
                optionsBuilder.UseSqlite("Data Source=connect4.db");
            }
        }
        
        // Model configuration - database constraints
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            
            // GameState.SaveName must be unique (no duplicate save names)
            modelBuilder.Entity<DbGameState>()
                .HasIndex(g => g.SaveName)
                .IsUnique();
            
            // GameConfiguration.Name must be unique (no duplicate config names)
            modelBuilder.Entity<DbGameConfiguration>()
                .HasIndex(c => c.Name)
                .IsUnique();
        }
    }
}