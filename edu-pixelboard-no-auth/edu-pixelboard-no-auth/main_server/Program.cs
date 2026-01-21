using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text.Json;
using PixelBoard.MainServer.Services;
using PixelBoard.MainServer.Configuration;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using GraphQL;
using PixelBoard.WebSockets;

// Many students reading many pixels at once requires many threads...
// But also: they should handle failing or timed-out requests properly on their side
ThreadPool.SetMinThreads(64, 64);

var builder = WebApplication.CreateBuilder(args);

builder.Services.Configure<BoardOptions>(builder.Configuration.GetSection("BoardOptions"));
builder.Services.Configure<PadukOptions>(builder.Configuration.GetSection("PadukOptions"));

builder.Services.AddRazorPages();

builder.Services.AddSingleton<IRedisDbService, RedisDbService>();
builder.Services.AddSingleton<IBoardService, RedisColorDbService>();
// allow for specific read-only or write-only access to the board
builder.Services.AddSingleton<IReadBoardService>(services => services.GetRequiredService<IBoardService>());
builder.Services.AddSingleton<IWriteBoardService>(services => services.GetRequiredService<IBoardService>());
builder.Services.AddSingleton<IPlayerService, PlayerService>();
builder.Services.AddHostedService<TickService>();
builder.Services.AddSingleton<WebSocketService>();

string? kcClientId = builder.Configuration["Keycloak:ClientId"];
string? clientSecret = builder.Configuration["Keycloak:ClientSecret"];
string? kcUrl = builder.Configuration["Keycloak:Url"];
string? kcRealm = builder.Configuration["Keycloak:Realm"];
string? clientUrl = builder.Configuration["ClientUrl"];

if (string.IsNullOrEmpty(kcClientId))
    throw new InvalidOperationException("No Keycloak Client Id configured");
if (string.IsNullOrEmpty(clientSecret))
    throw new InvalidOperationException("No Client Secret configured");
if (string.IsNullOrEmpty(kcUrl))
    throw new InvalidOperationException("No Keycloak URL configured");
if (string.IsNullOrEmpty(kcRealm))
    throw new InvalidOperationException("No Keycloak Realm configured");
if (string.IsNullOrEmpty(clientUrl))
    throw new InvalidOperationException("No Client URL configured");

var webSocketOptions = new WebSocketOptions
{
    KeepAliveInterval = TimeSpan.FromMinutes(2)
};
webSocketOptions.AllowedOrigins.Add(clientUrl);


string game = builder.Configuration.GetValue<string>("Game") ?? "Paduk";
if (game == "Paduk")
{
    builder.Services.AddSingleton(services =>
    {
        PadukGameService gameService = new(
            services.GetRequiredService<ILogger<PadukGameService>>(),
            services.GetRequiredService<IBoardService>(),
            services.GetRequiredService<IOptions<PadukOptions>>(),
            services.GetRequiredService<IOptions<BoardOptions>>()
        );

        return new RedisEventSourcingGameAdapter(
            services.GetRequiredService<IRedisDbService>(),
            gameService,
            services.GetRequiredService<ILogger<RedisEventSourcingGameAdapter>>()
        );
    });
    builder.Services.AddSingleton<IGameService>(services => services.GetRequiredService<RedisEventSourcingGameAdapter>());
    builder.Services.AddSingleton<IArchiveService>(services => services.GetRequiredService<RedisEventSourcingGameAdapter>());
}
else if (game == "PlainBoard")
{
    builder.Services.AddSingleton(services =>
    {
        PlainBoardGameService gameService = new(
            services.GetRequiredService<ILogger<PlainBoardGameService>>(),
            services.GetRequiredService<IBoardService>()
        );

        return new RedisEventSourcingGameAdapter(
            services.GetRequiredService<IRedisDbService>(),
            gameService,
            services.GetRequiredService<ILogger<RedisEventSourcingGameAdapter>>()
        );
    });
    builder.Services.AddSingleton<IGameService>(services => services.GetRequiredService<RedisEventSourcingGameAdapter>());
    builder.Services.AddSingleton<IArchiveService>(services => services.GetRequiredService<RedisEventSourcingGameAdapter>());

}
else
{
    throw new InvalidOperationException($"Invalid game '{game}' specified in configuration.");
}


builder.Services
    .AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
    })
    // For the API, a signed JWT must be obtained on the student client and provided here.
    .AddJwtBearer("JwtBearer", options =>
    {
        // All details will be fetched automatically according to OIDC
        options.Authority = $"{kcUrl}/realms/{kcRealm}";
        // TODO: figure this out to work properly in dev and online
        options.TokenValidationParameters.ValidAudience = "student_client";
        options.TokenValidationParameters.ValidateIssuer = false;
        options.RequireHttpsMetadata = false;
        options.MapInboundClaims = false;

        // this is to help student debugging their client implementations
        options.IncludeErrorDetails = true;
        options.Events = new JwtBearerEvents()
        {
            OnChallenge = ctx =>
            {
                ctx.HandleResponse();
                ctx.Response.StatusCode = StatusCodes.Status401Unauthorized;
                ctx.Response.ContentType = "application/json";

                if (ctx.AuthenticateFailure is SecurityTokenExpiredException authenticationException)
                {
                    ctx.Response.Headers["x-token-expired"] = authenticationException.Expires.ToString("o");
                    ctx.ErrorDescription = $"Your JWT has expired on {authenticationException.Expires.ToString("o")}";
                }

                var newResponse = JsonSerializer.Serialize(
                    new
                    {
                        error = ctx.Error ?? "invalid_token",
                        error_description = ctx.ErrorDescription ?? "Missing a valid JWT"
                    }
                );
                return ctx.Response.WriteAsync(newResponse);
            }
        };
    })
    // For the main server /Admin pages we have to log in through the main server frontend and then use the token stored in the cookie.
    .AddCookie()
    .AddOpenIdConnect(options =>
    {
        options.Authority = $"{builder.Configuration["Keycloak:Url"]}/realms/{builder.Configuration["Keycloak:Realm"]}";
        options.RequireHttpsMetadata = false;// the main server runs on the same system as keycloak, even in production
        options.ClientId = kcClientId;
        options.ClientSecret = builder.Configuration["Keycloak:ClientSecret"];
        options.ResponseType = "code";
        options.SaveTokens = true;
        options.Scope.Add("openid");
        options.Scope.Add("profile");
        options.GetClaimsFromUserInfoEndpoint = true;
        options.UseTokenLifetime = true;
    });
builder.Services
    .AddControllers()
    .AddJsonOptions(options =>
    {
        // preserve pascal case for JSON responses
        options.JsonSerializerOptions.PropertyNamingPolicy = null;
    });
builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("Admin", policy => policy.RequireClaim("team", "0"));
});

// GraphQL server services
builder.Services.AddSingleton<BoardQuery>();
builder.Services.AddSingleton<BoardSubscription>();

builder.Services.AddGraphQL(b => b
    .AddErrorInfoProvider(opt => opt.ExposeExceptionDetails = true)
    .AddAutoSchema<BoardQuery>(s => s.WithSubscription<BoardSubscription>())
    .AddAutoClrMappings()
    .AddSystemTextJson()
    );

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "Pixel Board API", Version = "v1" });
});

var concurrencyPolicy = "Concurrency";
builder.Services.AddRateLimiter(_ => _
    .AddConcurrencyLimiter(policyName: concurrencyPolicy, options =>
    {
        options.PermitLimit = 30;
        options.QueueLimit = 200;
    }));

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("v1/swagger.json", "Pixel Board API V1");
});

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.MapStaticAssets();

app.UseRouting();

// this can help with running with https behind a proxy
var forwardingOptions = new ForwardedHeadersOptions()
{
    ForwardedHeaders = Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor | Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedProto
};
forwardingOptions.KnownNetworks.Clear();
forwardingOptions.KnownProxies.Clear();
app.UseForwardedHeaders(forwardingOptions);

app.UseAuthentication();
app.UseAuthorization();
app.UseAntiforgery();

app.UseWebSockets(webSocketOptions);
app.MapWebSocketHandler("/ws-pixels");

app.MapRazorPages().WithStaticAssets();
app.UseRateLimiter();
app.MapControllers().RequireRateLimiting(concurrencyPolicy);

app.UseGraphQL("/graphql");
app.UseGraphQLPlayground(
    "/graphql-play",
    new GraphQL.Server.Ui.Playground.PlaygroundOptions
    {
        GraphQLEndPoint = "/graphql",
        SubscriptionsEndPoint = "/graphql",
    });
app.UseGraphQLGraphiQL();

app.Run();
