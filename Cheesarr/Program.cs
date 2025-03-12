using MudBlazor.Services;
using Cheesarr.Components;
using Cheesarr.Components.Pages.Settings;
using Cheesarr.Data;
using Cheesarr.Services;
using Cheesarr.Settings;
using MudBlazor;

var builder = WebApplication.CreateBuilder(args);

// Add MudBlazor services
builder.Services.AddMudServices();

builder.Services.AddMudServices(config =>
{
    config.SnackbarConfiguration.PositionClass = Defaults.Classes.Position.BottomLeft;

    config.SnackbarConfiguration.PreventDuplicates = false;
    config.SnackbarConfiguration.NewestOnTop = false;
    config.SnackbarConfiguration.ShowCloseIcon = true;
    config.SnackbarConfiguration.VisibleStateDuration = 10000;
    config.SnackbarConfiguration.HideTransitionDuration = 500;
    config.SnackbarConfiguration.ShowTransitionDuration = 500;
    config.SnackbarConfiguration.SnackbarVariant = Variant.Filled;
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// Database
builder.Services.AddSqlite<CheesarrDbContext>("Data Source=db/mouse.db");

builder.Services.AddHttpClient<OpenLibraryService>(client => { client.BaseAddress = new Uri("https://openlibrary.org/"); });
// builder.Services.AddScoped<OpenLibraryService>();

builder.Services.AddHostedService<GrabBackgroundService>();
builder.Services.AddSingleton<GrabStateUpdater>();

builder.Services.AddSingleton<SettingsService>();
builder.Services.AddSingleton<GrabService>();
builder.Services.AddSingleton<SnackMessageBus>();

builder.Services.AddHttpClient<ProwlarrService>((sp, client) =>
{
    var settings = sp.GetRequiredService<SettingsService>().GetSettings<ProwlarrSettingsData>();
    
    client.BaseAddress = new Uri($"{(settings.UseSSL ? "https": "http")}://{settings.Host}:{settings.Port}");
    client.DefaultRequestHeaders.Add("X-Api-Key", settings.APIKey);
});

builder.Services.AddHttpClient<QBTService>((sp, client) =>
{
    var settings = sp.GetRequiredService<SettingsService>().GetSettings<QBTSettingsData>();
    client.BaseAddress = new Uri($"{(settings.UseSSL ? "https": "http")}://{settings.Host}:{settings.Port}");
    client.DefaultRequestHeaders.Add("X-Api-Key", settings.APIKey);
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

// Initialize the database
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<CheesarrDbContext>();
    if(Environment.GetEnvironmentVariable("CLEAR_DB") == "true")
    {
        db.Database.EnsureDeleted();
    }
    if (db.Database.EnsureCreated())
    {
        SeedData.Initialize(db);
    }
}

app.Run();