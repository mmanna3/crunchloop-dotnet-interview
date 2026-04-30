using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Dtos;
using TodoApi.Application.Services;
using TodoApi.Domain.Models;

namespace TodoApi.Api.Controllers;

[Route("api/todolists")]
[ApiController]
public class TodoListsController(ITodoListService todoListService) : ControllerBase
{
    private readonly ITodoListService _todoListService = todoListService;

    // GET: api/todolists
    [HttpGet]
    public async Task<ActionResult<IList<TodoList>>> GetTodoLists()
    {
        return Ok(await _todoListService.GetTodoLists());
    }

    // GET: api/todolists/5
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoList>> GetTodoList(long id)
    {
        var todoList = await _todoListService.GetTodoList(id);

        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(todoList);
    }

    // PUT: api/todolists/5
    // To protect from over-posting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoList(long id, UpdateTodoList payload)
    {
        var todoList = await _todoListService.UpdateTodoList(id, payload.Name);
        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(todoList);
    }

    // POST: api/todolists
    // To protect from over-posting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TodoList>> PostTodoList(CreateTodoList payload)
    {
        var todoList = await _todoListService.CreateTodoList(payload.Name);

        return CreatedAtAction("GetTodoList", new { id = todoList.Id }, todoList);
    }

    // DELETE: api/todolists/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoList(long id)
    {
        var deleted = await _todoListService.DeleteTodoList(id);
        if (!deleted)
        {
            return NotFound();
        }

        return NoContent();
    }
}
