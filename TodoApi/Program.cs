using System.Threading.Channels;
using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Hubs;
using TodoApi.Api.Middleware;
using TodoApi.Application;
using TodoApi.Application.Services;
using TodoApi.Application.Workers;
using TodoApi.Domain.Repositories;
using TodoApi.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder.Services.Configure<WorkerSettings>(settings =>
    builder.Configuration.GetSection("Worker").Bind(settings)
);
builder
    .Services.AddDbContext<TodoContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("TodoContext"))
    )
    .AddScoped<ITodoListService, TodoListService>()
    .AddScoped<ITodoItemsService, TodoItemsService>()
    .AddScoped<ICompleteAllItemsService, CompleteAllItemsService>()
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
