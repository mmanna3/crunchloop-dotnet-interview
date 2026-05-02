using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Http;
using Polly;
using Polly.Extensions.Http;
using TodoApi.Api.Hubs;
using TodoApi.Api.Middleware;
using TodoApi.Application;
using TodoApi.Application.ExternalApi;
using TodoApi.Application.Services;
using TodoApi.Application.Workers;
using TodoApi.Domain.Repositories;
using TodoApi.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<WorkerSettings>(settings =>
    builder.Configuration.GetSection("Worker").Bind(settings)
);

var syncSection = builder.Configuration.GetSection("Sync");
var syncSettings = syncSection.Get<SyncSettings>() ?? new SyncSettings();
builder.Services.Configure<SyncSettings>(syncSection);

var retryPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .WaitAndRetryAsync(
        syncSettings.MaxRetryAttempts,
        attempt => TimeSpan.FromSeconds(Math.Pow(2, attempt - 1))
    );

var circuitBreakerPolicy = HttpPolicyExtensions
    .HandleTransientHttpError()
    .CircuitBreakerAsync(
        syncSettings.CircuitBreakerThreshold,
        TimeSpan.FromSeconds(syncSettings.CircuitBreakerDurationSeconds)
    );

builder
    .Services.AddHttpClient<IExternalTodoApiClient, ExternalTodoApiClient>(client =>
    {
        if (!string.IsNullOrEmpty(syncSettings.ExternalApiBaseUrl))
            client.BaseAddress = new Uri(syncSettings.ExternalApiBaseUrl);
    })
    .AddPolicyHandler(retryPolicy)
    .AddPolicyHandler(circuitBreakerPolicy);
builder
    .Services.AddDbContext<TodoContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("TodoContext"))
    )
    .AddScoped<ITodoListService, TodoListService>()
    .AddScoped<ITodoItemsService, TodoItemsService>()
    .AddScoped<ICompleteAllItemsService, CompleteAllItemsService>()
    .AddScoped<ISyncService, SyncService>()
    .AddScoped<ITodoListsRepository, TodoListsRepository>()
    .AddScoped<ITodoItemsRepository, TodoItemsRepository>()
    .AddSingleton(Channel.CreateUnbounded<CompleteAllItemsJob>())
    .AddHostedService<CompleteAllItemsWorker>()
    .AddSignalR()
    .Services.AddEndpointsApiExplorer()
    .AddControllers()
    .Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
        {
            policy
                .SetIsOriginAllowed(_ => true)
                .AllowAnyHeader()
                .AllowAnyMethod()
                .AllowCredentials();
        })
    );

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseCors();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthorization();
app.MapControllers();
app.MapHub<TodoHub>("/hubs/todo");
app.Run();

public partial class Program { }
