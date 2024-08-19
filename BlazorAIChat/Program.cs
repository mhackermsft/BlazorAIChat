using BlazorAIChat;
using BlazorAIChat.Authentication;
using BlazorAIChat.Components;
using BlazorAIChat.Models;
using BlazorAIChat.Services;
using BlazorAIChat.Utils;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.Configure<AppSettings>(builder.Configuration);
builder.Services.AddHttpClient("retryHttpClient").AddPolicyHandler(RetryHelper.GetRetryPolicy());
builder.Services.AddDbContext<AIChatDBContext>();
builder.Services.AddScoped<UserService>();
builder.Services.AddSingleton<ChatHistoryService>();

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

app.Run();
