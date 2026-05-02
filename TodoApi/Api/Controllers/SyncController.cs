using Microsoft.AspNetCore.Mvc;
using TodoApi.Application.Dtos;
using TodoApi.Application.Services;

namespace TodoApi.Api.Controllers;

[Route("api/sync")]
[ApiController]
public class SyncController(ISyncService syncService) : ControllerBase
{
    private readonly ISyncService _syncService = syncService;

    // POST: api/sync/trigger
    [HttpPost("trigger")]
    public async Task<ActionResult<SyncTriggerResponseDTO>> Trigger(CancellationToken ct)
    {
        var result = await _syncService.SyncAsync(ct);
        return Ok(SyncTriggerResponseDTO.From(result));
    }
}
