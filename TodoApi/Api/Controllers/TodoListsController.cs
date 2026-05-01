using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Dtos;
using TodoApi.Application.Services;
using TodoApi.Domain.Models;

namespace TodoApi.Api.Controllers;

[Route("api/todolists")]
[ApiController]
public class TodoListsController(
    ITodoListService todoListService,
    ICompleteAllItemsService completeAllItemsService
) : ControllerBase
{
    private readonly ITodoListService _todoListService = todoListService;
    private readonly ICompleteAllItemsService _completeAllItemsService = completeAllItemsService;

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
    public async Task<ActionResult> PutTodoList(long id, UpdateListDTO payload)
    {
        var todoList = await _todoListService.UpdateTodoList(id, payload);
        if (todoList == null)
        {
            return NotFound();
        }

        return Ok(todoList);
    }

    // POST: api/todolists
    // To protect from over-posting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
    [HttpPost]
    public async Task<ActionResult<TodoList>> PostTodoList(CreateListDTO payload)
    {
        var todoList = await _todoListService.CreateTodoList(payload);

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

    // POST: api/todolists/5/complete-all-items
    [HttpPost("{listId}/complete-all-items")]
    public async Task<ActionResult<CompleteAllItemsResponseDTO>> CompleteAllItems(
        long listId,
        CompleteAllItemsRequestDTO payload
    )
    {
        var result = await _completeAllItemsService.StartAsync(listId, payload.ConnectionId);
        return Accepted(result);
    }
}
