using Microsoft.EntityFrameworkCore;
using TodoApi.Domain.Models;

namespace TodoApi.Persistence;

public class TodoContext(DbContextOptions<TodoContext> options) : DbContext(options)
{
    public DbSet<TodoList> TodoList { get; set; } = default!;
    public DbSet<TodoItem> TodoItem { get; set; } = default!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<TodoList>().HasIndex(t => t.ExternalId);

        modelBuilder.Entity<TodoItem>().HasIndex(t => t.ExternalId);
    }
}
