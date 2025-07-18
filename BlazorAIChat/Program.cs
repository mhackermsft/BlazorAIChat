#pragma warning disable SKEXP0010, SKEXP0001, SKEXP0020, KMEXP00
using BlazorAIChat;
using BlazorAIChat.Authentication;
using BlazorAIChat.Components;
using BlazorAIChat.Models;
using BlazorAIChat.Services;
using BlazorAIChat.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel;
using ModelContextProtocol.Client;

var builder = WebApplication.CreateBuilder(args);

// Configure logging to default to the console
builder.Logging.ClearProviders();
builder.Logging.AddConsole();
builder.Logging.SetMinimumLevel(LogLevel.Information);

builder.Services.Configure<AppSettings>(builder.Configuration);

// Register a default HttpClient for streaming/long-lived connections
builder.Services.AddHttpClient("defaultHttpClient");

//Register an HttpClient that has a retry policy handler. Used for Azure OpenAI calls.
builder.Services.AddHttpClient("retryHttpClient").AddPolicyHandler(RetryHelper.GetRetryPolicy());

builder.Services.AddDbContext<AIChatDBContext>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ChatHistoryService>();
builder.Services.AddScoped<AIService>();
builder.Services.AddSingleton<AISearchService>();

builder.Services.AddScoped<UserMcpService>();
builder.Services.AddScoped<McpPluginProvider>();

// Register the Kernel using DI - now scoped per request to support user-specific plugins
builder.Services.AddScoped<Kernel>(serviceProvider =>
{
    var appSettings = serviceProvider.GetRequiredService<IOptions<AppSettings>>().Value;
    var httpClientFactory = serviceProvider.GetRequiredService<IHttpClientFactory>();
    var httpClient = httpClientFactory.CreateClient("retryHttpClient");

    var kernelBuilder = Kernel.CreateBuilder()
        .AddAzureOpenAIChatCompletion(
            appSettings.AzureOpenAIChatCompletion.DeploymentName,
            appSettings.AzureOpenAIChatCompletion.Endpoint,
            appSettings.AzureOpenAIChatCompletion.ApiKey,
            httpClient: httpClient)
        .AddAzureOpenAIEmbeddingGenerator(
            appSettings.AzureOpenAIEmbedding.DeploymentName,
            appSettings.AzureOpenAIChatCompletion.Endpoint,
            appSettings.AzureOpenAIChatCompletion.ApiKey,
            httpClient: httpClient);

    // Note: MCP plugins will be added dynamically per user in AIService
    kernelBuilder.Services.AddLogging(services => services.AddConsole().SetMinimumLevel(LogLevel.Trace));
    return kernelBuilder.Build();
});

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddCircuitOptions(options => options.DetailedErrors = true);

builder.Services.AddCascadingAuthenticationState();

builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie();

builder.Services.AddAuthorizationCore();

builder.Services.Configure<FormOptions>(options =>
{
    options.MultipartBodyLengthLimit = int.MaxValue;
});

var app = builder.Build();

//setup EF database and migrate to latest version
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AIChatDBContext>();
    context.Database.Migrate();
}

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseStaticFiles();
app.UseAntiforgery();

app.UseAuthentication();

//Add easy auth middleware
app.UseMiddleware<EasyAuthMiddleware>();

app.UseAuthorization();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

//Before we start the app, ensure that the KNN folder exists on the filesystem
if (!Directory.Exists("KNN"))
{
    Directory.CreateDirectory("KNN");
}
if (!Directory.Exists("SFS"))
{
    Directory.CreateDirectory("SFS");
}

app.Run();
