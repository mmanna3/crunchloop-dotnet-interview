using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Dtos;
using TodoApi.Application.Services;

namespace TodoApi.Api.Controllers;

[Route("api/todolists/{listId}/items")]
[ApiController]
public class TodoItemsController(ITodoItemsService todoItemsService) : ControllerBase
{
    private readonly ITodoItemsService _todoItemsService = todoItemsService;

    //GET: api/todolists
    [HttpGet]
    public async Task<ActionResult<IList<TodoItemDTO>>> GetTodoItems(long listId)
    {
        return Ok(await _todoItemsService.GetTodoItems(listId));
    }

    // GET: api/todolists/5/items/1
    [HttpGet("{id}")]
    public async Task<ActionResult<TodoItemDTO>> GetTodoItem(long listId, long id)
    {
        return Ok(await _todoItemsService.GetTodoItem(listId, id));
    }

    // PUT: api/todolists/5/items/1
    // To protect from over-posting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPut("{id}")]
    public async Task<ActionResult> PutTodoItem(long listId, long id, UpdateItemDTO payload)
    {
        return Ok(await _todoItemsService.UpdateTodoItem(listId, id, payload));
    }

    [HttpPost]
    public async Task<ActionResult<TodoItemDTO>> PostTodoItem(long listId, CreateItemDTO payload)
    {
        var created = await _todoItemsService.CreateTodoItem(listId, payload);
        return CreatedAtAction("GetTodoItem", new { listId, id = created.Id }, created);
    }

    // DELETE: api/todolists/5
    [HttpDelete("{id}")]
    public async Task<ActionResult> DeleteTodoItem(long listId, long id)
    {
        await _todoItemsService.DeleteTodoItem(listId, id);
        return NoContent();
    }
}
