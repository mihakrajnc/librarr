using System.Net.Http.Headers;
using Librarr.Components;
using Librarr.Data;
using Librarr.Services;
using Librarr.Services.Download;
using Librarr.Services.Metadata;
using Librarr.Services.ReleaseSearch;
using Librarr.Settings;
using MudBlazor;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// ------------------------------------------
// Configure Services
// ------------------------------------------

// Add MudBlazor services for UI components & snackbars
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

// Add Razor Components with interactive server components (Blazor Server)
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

// ------------------------------------------
// Controllers
// ------------------------------------------
builder.Services.AddControllers();


// ------------------------------------------
// Authentication & Authorization
// ------------------------------------------

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "LibrarrAuth";
    options.DefaultChallengeScheme = "LibrarrAuth";
}).AddCookie("LibrarrAuth", options =>
{
    options.LoginPath = "/login";
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
    options.Cookie.SameSite = SameSiteMode.Lax;
});

// Add authorization services for Blazor components
builder.Services.AddAuthorizationCore();


// ------------------------------------------
// Application Configuration: Data & Settings
// ------------------------------------------

// Set up persistent configuration and settings directories
var configDir = Environment.GetEnvironmentVariable("LIBRARR_CONFIG_PATH") ??
                Path.Combine(AppContext.BaseDirectory, "config");
var settingsDir = Path.Combine(configDir, "settings");
Directory.CreateDirectory(configDir);
Directory.CreateDirectory(settingsDir);


// ------------------------------------------
// Database Configuration
// ------------------------------------------

var dbFile = Path.Combine(configDir, "librarr.sqlite");
builder.Services.AddSqlite<LibrarrDbContext>($"Data Source={dbFile}", null,
    options => { options.LogTo(Console.WriteLine, LogLevel.Warning); });


// ------------------------------------------
// App Services
// ------------------------------------------

builder.Services.AddHostedService<LibraryImportBackgroundService>();
builder.Services.AddSingleton<SettingsService>(sp => new SettingsService(
    sp.GetRequiredService<ILogger<SettingsService>>(),
    settingsDir
));
builder.Services.AddSingleton<GrabService>();
builder.Services.AddSingleton<SnackMessageBus>();


// ------------------------------------------
// API & HTTP Client Services
// ------------------------------------------

// OpenLibrary metadata service
builder.Services.AddHttpClient<IMetadataService, OpenLibraryMetadataService>(client =>
{
    client.BaseAddress = new Uri("https://openlibrary.org");
});

// Alternative release search service - Prowlarr
/*
builder.Services.AddHttpClient<IReleaseSearchService, ProwlarrReleaseSearchService>((sp, client) =>
{
    var settings = sp.GetRequiredService<SettingsService>().GetSettings<ProwlarrSettingsData>();
    client.BaseAddress = new Uri($"{(settings.UseSSL ? "https" : "http")}://{settings.Host}:{settings.Port}");
    client.DefaultRequestHeaders.Add("X-Api-Key", settings.APIKey);
});
*/

// MaM release search service
builder.Services.AddHttpClient<IReleaseSearchService, MaMReleaseSearchService>((sp, client) =>
    {
        var settings = sp.GetRequiredService<SettingsService>().GetSettings<MaMSettingsData>();
        client.BaseAddress = new Uri(settings.BaseURL);
        client.DefaultRequestHeaders.Add("Cookie", $"mam_id={settings.MaMID}");
        client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("gzip"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("deflate"));
        client.DefaultRequestHeaders.AcceptEncoding.Add(new StringWithQualityHeaderValue("br"));
        client.DefaultRequestHeaders.UserAgent.Add(new ProductInfoHeaderValue("Librarr", "0.1"));
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        AutomaticDecompression = System.Net.DecompressionMethods.GZip |
                                 System.Net.DecompressionMethods.Deflate |
                                 System.Net.DecompressionMethods.Brotli
    });

// Download service via QBT with custom authorization handler
builder.Services.AddHttpClient<IDownloadService, QBTDownloadService>((sp, client) =>
    {
        var settings = sp.GetRequiredService<SettingsService>().GetSettings<QBTSettingsData>();
        client.BaseAddress = new Uri($"{(settings.UseSSL ? "https" : "http")}://{settings.Host}:{settings.Port}");
    })
    .AddHttpMessageHandler<QBTAuthHandler>();
builder.Services.AddTransient<QBTAuthHandler>();


// ------------------------------------------
// Build Application
// ------------------------------------------
var app = builder.Build();

app.MapControllers();

// ------------------------------------------
// Configure HTTP Request Pipeline
// ------------------------------------------
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseAuthentication();
app.UseAuthorization();
app.UseHttpsRedirection();
app.UseAntiforgery();

// Map static assets and set up Razor Components (Blazor Server)
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();


// ------------------------------------------
// Initialize the Database
// ------------------------------------------
var scopeFactory = app.Services.GetRequiredService<IServiceScopeFactory>();
using (var scope = scopeFactory.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<LibrarrDbContext>();

    if (Environment.GetEnvironmentVariable("CLEAR_DB") == "true")
    {
        db.Database.EnsureDeleted();
    }

    if (db.Database.EnsureCreated())
    {
        SeedData.Initialize(db);
    }
}


// ------------------------------------------
// Run the Application
// ------------------------------------------
app.Run();