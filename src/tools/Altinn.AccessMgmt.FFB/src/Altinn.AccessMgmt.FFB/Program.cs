using Altinn.AccessMgmt.FFB.Components;
using Altinn.AccessMgmt.FFB.Config;
using Altinn.AccessMgmt.FFB.Jobs;
using Altinn.AccessMgmt.FFB.Jobs.Models;
using Altinn.AccessMgmt.FFB.Services;
using Altinn.AccessMgmt.FFB.Services.Contracts;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(new WebApplicationOptions
{
    Args = args,
    ContentRootPath = AppContext.BaseDirectory // external appsettings.json is found next to the exe in single-file publish
});

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Authentication: every request (pages, Blazor circuit, error pages) requires a signed-in operator.
var authConfig = builder.Configuration.GetSection(AuthenticationConfig.SectionName).Get<AuthenticationConfig>() ?? new AuthenticationConfig();
authConfig.Validate();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.ExpireTimeSpan = TimeSpan.FromHours(8);
        options.SlidingExpiration = false;
    })
    .AddOpenIdConnect(options =>
    {
        options.Authority = authConfig.Authority;
        options.ClientId = authConfig.ClientId;
        options.ClientSecret = string.IsNullOrWhiteSpace(authConfig.ClientSecret) ? null : authConfig.ClientSecret;
        options.CallbackPath = authConfig.CallbackPath;
        options.ResponseType = OpenIdConnectResponseType.Code;
        options.UsePkce = true;
        options.SaveTokens = false;
        options.GetClaimsFromUserInfoEndpoint = true;
        options.MapInboundClaims = false;
        options.TokenValidationParameters.NameClaimType = "name";
        options.TokenValidationParameters.RoleClaimType = authConfig.RoleClaimType;
        options.Scope.Clear();
        options.Scope.Add("openid");
        options.Scope.Add("profile");
    });

builder.Services.AddAuthorization(options =>
{
    var policy = new AuthorizationPolicyBuilder().RequireAuthenticatedUser();
    if (!string.IsNullOrWhiteSpace(authConfig.RequiredRole))
    {
        policy.RequireRole(authConfig.RequiredRole);
    }

    options.DefaultPolicy = policy.Build();
    options.FallbackPolicy = options.DefaultPolicy;
});

builder.Services.AddCascadingAuthenticationState();

// Environment infrastructure
builder.Services.Configure<EnvironmentsConfig>(builder.Configuration);
builder.Services.AddSingleton<IEnvironmentDbContextFactory, EnvironmentDbContextFactory>();
builder.Services.AddScoped<EnvironmentState>();

// Tool and page data services (Services/Tools + Services/PageData) are registered
// by convention — see ServiceCollectionExtensions.AddPageServices.
builder.Services.AddPageServices();

// Job infrastructure
builder.Services.AddSingleton<IJobRunStore, JobRunStore>();

builder.Services.Configure<NotificationsConfig>(builder.Configuration.GetSection("Notifications"));
builder.Services.AddHttpClient("telegram");
builder.Services.AddSingleton<INotificationService, TelegramNotificationService>();

builder.Services.AddSingleton<IJobRunner, JobRunner>();

builder.Services.Configure<JobSchedulesConfig>(builder.Configuration.GetSection("JobSchedules"));

// Register as the concrete type first so both IJobScheduler and IHostedService share the same instance.
builder.Services.AddSingleton<JobSchedulerService>();
builder.Services.AddSingleton<IJobScheduler>(p => p.GetRequiredService<JobSchedulerService>());
builder.Services.AddHostedService(p => p.GetRequiredService<JobSchedulerService>());

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.MapStaticAssets().AllowAnonymous();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .RequireAuthorization();

app.MapPost("/signout", (HttpContext ctx) => TypedResults.SignOut(
        new AuthenticationProperties { RedirectUri = "/" },
        [CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme]))
    .RequireAuthorization();

app.Run();
