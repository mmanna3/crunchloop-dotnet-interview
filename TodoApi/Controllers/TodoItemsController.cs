using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TodoApi.Dtos;
using TodoApi.Models;

namespace TodoApi.Controllers
{
    [Route("api/todolists/{listId}/items")]
    [ApiController]
    public class TodoItemsController : ControllerBase
    {
        private readonly TodoContext _context;

        public TodoItemsController(TodoContext context)
        {
            _context = context;
        }

        //GET: api/todolists
        [HttpGet]
        public async Task<ActionResult<IList<TodoItem>>> GetTodoItems(long listId)
        {
            if (!TodoListExists(listId))
            {
                return NotFound();
            }

            return Ok(await _context.TodoItem.Where(item => item.TodoListId == listId).ToListAsync());
        }

        // GET: api/todolists/5/items/1
        [HttpGet("{id}")]
        public async Task<ActionResult<TodoItem>> GetTodoItem(long listId, long id)
        {            
            if (!TodoListExists(listId))
            {
                return NotFound();
            }

            var todoItem = await _context.TodoItem
                                    .SingleOrDefaultAsync(item => item.TodoListId == listId && item.Id == id);

            return todoItem == null ? NotFound() : Ok(todoItem);
        }

        // PUT: api/todolists/5
        // To protect from over-posting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        // [HttpPut("{id}")]
        // public async Task<ActionResult> PutTodoList(long id, UpdateTodoList payload)
        // {
        //     var todoList = await _context.TodoList.FindAsync(id);

        //     if (todoList == null)
        //     {
        //         return NotFound();
        //     }

        //     todoList.Name = payload.Name;
        //     await _context.SaveChangesAsync();

        //     return Ok(todoList);
        // }

                
        [HttpPost]
        public async Task<ActionResult<TodoItem>> PostTodoItem(long listId, CreateTodoItemDto payload)
        {
            if (!TodoListExists(listId))
            {
                return NotFound("Todo list not found");
            }
            
            var todoItem = new TodoItem { Description = payload.Description, TodoListId = listId };

            _context.TodoItem.Add(todoItem);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetTodoItem", new { listId = listId, id = todoItem.Id }, todoItem);
        }

        // DELETE: api/todolists/5
        // [HttpDelete("{id}")]
        // public async Task<ActionResult> DeleteTodoList(long id)
        // {
        //     var todoList = await _context.TodoList.FindAsync(id);
        //     if (todoList == null)
        //     {
        //         return NotFound();
        //     }

        //     _context.TodoList.Remove(todoList);
        //     await _context.SaveChangesAsync();

        //     return NoContent();
        // }

        // private bool TodoListExists(long id)
        // {
        //     return (_context.TodoList?.Any(e => e.Id == id)).GetValueOrDefault();
        // }

        private bool TodoListExists(long id)
        {
            return (_context.TodoList?.Any(e => e.Id == id)).GetValueOrDefault();
        }
    }
}
