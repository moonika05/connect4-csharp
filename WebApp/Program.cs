using ConsoleApp.GameEngine;
using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

// Add session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// REMOVE THIS: (ära registreeri siin!)
// builder.Services.AddScoped<IGameRepository, JsonRepository>();

// Register BOTH repositories with different lifetimes
builder.Services.AddScoped<JsonRepository>();
builder.Services.AddScoped<DbRepository>();

// Add factory for repository selection
builder.Services.AddScoped<IGameRepository>(sp =>
{
    // This will be overridden per-request based on session
    // Default to JsonRepository
    return sp.GetRequiredService<JsonRepository>();
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// Enable session (MUST be after UseRouting, before MapRazorPages)
app.UseSession();

app.MapRazorPages();

app.Run();