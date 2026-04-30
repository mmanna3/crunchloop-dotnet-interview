using Microsoft.EntityFrameworkCore;
using TodoApi.Api.Middleware;
using TodoApi.Application.Services;
using TodoApi.Domain.Repositories;
using TodoApi.Persistence;

var builder = WebApplication.CreateBuilder(args);
builder
    .Services.AddDbContext<TodoContext>(opt =>
        opt.UseSqlServer(builder.Configuration.GetConnectionString("TodoContext"))
    )
    .AddScoped<ITodoListService, TodoListService>()
    .AddScoped<ITodoItemsService, TodoItemsService>()
    .AddScoped<ITodoListsRepository, TodoListsRepository>()
    .AddScoped<ITodoItemsRepository, TodoItemsRepository>()
    .AddEndpointsApiExplorer()
    .AddControllers()
    .Services.AddCors(options =>
        options.AddDefaultPolicy(policy =>
            policy.WithOrigins("http://localhost:5173").AllowAnyHeader().AllowAnyMethod()
        )
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
app.Run();

public partial class Program { }
