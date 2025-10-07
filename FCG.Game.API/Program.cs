// FCG.Game.API/Program.cs

using FCG.Game.Application.Services;
using FCG.Game.Infrastructure.Configurations;
using FCG.Game.Infrastructure.EventStore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using EventStore.Client;

var builder = WebApplication.CreateBuilder(args);

// ============================================
// CONFIGURAÇÃO DE SERVIÇOS
// ============================================

// Controllers
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Swagger
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "FCG Games Microservice",
        Version = "v1",
        Description = "API de Jogos com Elasticsearch e EventStore"
    });

    c.AddSecurityDefinition("Bearer", new()
    {
        Description = "JWT Authorization header usando Bearer scheme. Exemplo: 'Bearer {token}'",
        Name = "Authorization",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
        Scheme = "Bearer"
    });

    c.AddSecurityRequirement(new()
    {
        {
            new()
            {
                Reference = new()
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

// ============================================
// ELASTICSEARCH
// ============================================
var elasticUri = builder.Configuration["Elasticsearch:Uri"] ?? "http://localhost:9200";
var elasticClient = ElasticSearchConfig.CreateClient(elasticUri);
builder.Services.AddSingleton(elasticClient);

// ============================================
// EVENTSTORE
// ============================================
var eventStoreConnectionString = builder.Configuration["EventStore:ConnectionString"]
    ?? "esdb://localhost:2113?tls=false";

// Criar cliente do EventStore
var eventStoreClient = EventStoreConfig.CreateClient(eventStoreConnectionString);
builder.Services.AddSingleton(eventStoreClient);

// Registrar repositório do EventStore
builder.Services.AddSingleton<EventStoreRepository>(sp =>
{
    var client = sp.GetRequiredService<EventStoreClient>();
    return new EventStoreRepository(client);
});

// ============================================
// APPLICATION SERVICES
// ============================================
builder.Services.AddScoped<GameService>();
builder.Services.AddScoped<OrderService>();
builder.Services.AddScoped<MetricsService>();

// ============================================
// BACKGROUND SERVICES
// ============================================
// Background Service desabilitado temporariamente (API do EventStore mudou)
// Para habilitar no futuro, descomente a linha abaixo:
// builder.Services.AddHostedService<EventStoreSubscriptionService>();

// ============================================
// JWT AUTHENTICATION
// ============================================
var jwtKey = builder.Configuration["Jwt:Key"]
    ?? throw new InvalidOperationException("JWT Key não configurada");

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

// ============================================
// CORS
// ============================================
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ============================================
// HEALTH CHECKS
// ============================================
builder.Services.AddHealthChecks()
    .AddElasticsearch(elasticUri, name: "elasticsearch", tags: new[] { "db", "search" });

// ============================================
// BUILD DA APLICAÇÃO
// ============================================
var app = builder.Build();

// ============================================
// INICIALIZAÇÃO DOS SERVIÇOS
// ============================================

app.Logger.LogInformation("🚀 Iniciando FCG Games Microservice...");

// Inicializar Elasticsearch
try
{
    app.Logger.LogInformation("🔍 Inicializando Elasticsearch...");
    await ElasticSearchConfig.InitializeIndicesAsync(elasticClient);
    app.Logger.LogInformation("✅ Elasticsearch inicializado com sucesso!");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "❌ Erro ao inicializar Elasticsearch");
    throw; // Em produção, você pode querer continuar mesmo com erro no Elastic
}

// Verificar conexão EventStore
try
{
    app.Logger.LogInformation("📊 Verificando conexão com EventStore...");
    await EventStoreConfig.VerifyConnectionAsync(eventStoreClient);
    await EventStoreConfig.InitializeProjectionsAsync(eventStoreClient);
    app.Logger.LogInformation("✅ EventStore conectado com sucesso!");
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "❌ Erro ao conectar com EventStore");
    throw; // Crítico: sem EventStore não tem Event Sourcing
}

// ============================================
// MIDDLEWARE PIPELINE
// ============================================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FCG Games API v1");
        c.RoutePrefix = "swagger";
    });

    app.Logger.LogInformation("📚 Swagger disponível em: http://localhost:5002/swagger");
}

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHealthChecks("/health");

// ============================================
// LOG DE INICIALIZAÇÃO
// ============================================
app.Logger.LogInformation("================================================");
app.Logger.LogInformation("✨ FCG Games Microservice PRONTO! ✨");
app.Logger.LogInformation("================================================");
app.Logger.LogInformation("🌐 API: http://localhost:5002");
app.Logger.LogInformation("📚 Swagger: http://localhost:5002/swagger");
app.Logger.LogInformation("💚 Health: http://localhost:5002/health");
app.Logger.LogInformation("🔍 Elasticsearch: {ElasticUri}", elasticUri);
app.Logger.LogInformation("📊 EventStore: {EventStoreUri}", eventStoreConnectionString);
app.Logger.LogInformation("================================================");

app.Run();