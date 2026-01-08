using ConsoleApp.GameEngine.Storage.Database;
using ConsoleApp.GameEngine.Storage.Json;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorPages();

// Session support
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Register repositories
builder.Services.AddScoped<IGameRepository, JsonRepository>();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthorization();

// CRITICAL: UseSession MUST be AFTER UseRouting and BEFORE MapRazorPages!
app.UseSession();  // <-- CHECK THIS IS HERE!

app.MapRazorPages();

app.Run();