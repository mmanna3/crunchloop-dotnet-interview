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
    .AddControllers();

builder.Logging.ClearProviders();
builder.Logging.AddConsole();

var app = builder.Build();

app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseAuthorization();
app.MapControllers();
app.Run();

public partial class Program { }
