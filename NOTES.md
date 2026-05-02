# Implementation notes

## Where this fits

The app is an ASP.NET Core API with Entity Framework Core persistence and `BackgroundService` workers. Sync lives in the application layer (`SyncService`, `ListSyncer`) and writes through `TodoContext`; it calls the external API via a typed HTTP client. A periodic worker plus an optional manual HTTP trigger run the same sync pipeline. Everything else in the API (CRUD for todo lists, SignalR, other workers) sits outside this document—the notes below focus only on synchronization design.

## Overall approach

The implementation has two main points:

- detect changes via timestamps
- have a single source of truth per sync cycle

Each entity (`TodoList`, `TodoItem`) has three tracking fields: `UpdatedAt` (when it last changed), `SyncedAt` (when it was last synchronized), and `ExternalId` (the id in the external API).

For entities that have already been through at least one sync, `SyncDecision` treats “local change since last sync” as `localUpdatedAt > SyncedAt` (see `SyncDecision.Resolve`). Entities that have never been synced (`SyncedAt` is null) are mostly handled elsewhere—for example, lists without an `ExternalId` go through the create path rather than this comparison—so the timestamp rule above describes the steady-state reconciliation path, not every branch in `ListSyncer`.

## Main components

- **`SyncWorker`**: `BackgroundService` that runs sync on a schedule using `PeriodicTimer`.
- **`SyncService`**: orchestrates the cycle—acquires the semaphore, fetches data, delegates reconciliation, persists, and logs.
- **`ListSyncer`**: all list and item reconciliation (push, pull, delete).
- **`SyncDecision`**: pure function from timestamps to `(push, pull)`. No state or side effects.
- **`ExternalTodoApiClient`**: typed HTTP client with retry and circuit breaker via Polly.

## Design decisions

**Why timestamps instead of Outbox/Rabbit?**

For this scope, I think a message queue would have been over-engineering. The Outbox pattern shines when you need exactly-once guarantees or an audit trail per change, but it adds real operational cost (extra infrastructure, queue operations, at-least-once semantics, etc.). Timestamps are enough here: sync is idempotent and a missed cycle self-heals on the next tick.

**Why `SyncedAt` in addition to `UpdatedAt`?**

`UpdatedAt` alone cannot answer: “Did the user change this row after we last talked to the external API?” You need `SyncedAt` for that: it is the time we last considered this row in sync. When a sync run finishes, we set `SyncedAt` to “now” in a **second** database save. The first save already wrote business changes and set `UpdatedAt`. Custom logic in `SaveChangesAsync` skips updating `UpdatedAt` when we only change sync fields (`SyncedAt`, `ExternalId`). So after that second save, **`SyncedAt` is usually a bit later than `UpdatedAt`**, and the next sync will not try to push the row again. In short: once sync metadata is written, `SyncedAt` should stay **at or after** `UpdatedAt`; the two-save flow and the `SaveChangesAsync` rules keep that true.

**Why last-write-wins on conflicts?**

When both sides changed since the last sync, some policy has to win. I briefly considered making it configurable (LocalWins / ExternalWins / LastWriteWins) but that did not justify the complexity without a concrete requirement (and time was tight). Last-write-wins means: compare timestamps and keep the edit with the later time.

**Why Polly for resilience?**

The external API is outside our control. Without resilience, one timeout or failure fails the whole cycle. The setup I used:

- **Retry with exponential backoff**: 3 attempts with waits of 1s, 2s, 4s.
- **Circuit breaker**: opens after 5 consecutive failures and stays open for 30 seconds so we do not hammer a dead server.

These are reasonable starting points but should be tuned against the real external API.

**Static semaphore in `SyncService`**

The semaphore acts like a **lock**: only one sync run can proceed at a time (like one cook in the kitchen). That stops two overlapping cycles from reading and writing the same database rows in parallel and stepping on each other.

The semaphore is **static** so that single “kitchen pass” is shared across **all** `SyncService` instances (the periodic worker gets one scoped instance per DI scope; the HTTP controller gets another). Without that shared flag, the worker and manual trigger could still run sync in parallel.

## Errors and edge cases

- **Idempotency**: running sync N times against the same state does not duplicate data.
- **External API down**: Polly retries and may open the circuit breaker. The worker picks up again on the next tick; it does not crash the host.
- **List deleted externally**: if a local list has an `ExternalId` but no longer exists on the external API, it is deleted locally (including cascade), it's a conservative choice.
- **Items without `ExternalId` on an already-synced list**: the external API has no endpoint to add items to an existing list. Those cases are logged as warnings and skipped. This is an improvement opportunity in the external API.
- **`source_id` for reconciliation**: when we create a list on the external API we send the local id as `source_id`. If the list already exists there with that `source_id` (e.g. from a half-failed earlier sync), we do not duplicate it.


## If there were more time

- **The external API does not return `updated_at` on item creation responses.** When we create a list and the API returns created items, we cannot set per-item `SyncedAt` perfectly without those timestamps. Ideally the create endpoint would return `updated_at` per item. Today we rely on the two-save pattern; it is a limitation.
- **Partial sync**: if the cycle fails halfway, some entities are synced and others are not. There is no transaction rollback. Outbox could give atomicity at a higher cost than justified here.
- **Configurable conflict policy**: the code could be extended; today only last-write-wins is implemented.
- **Telemetry**: logging exists; richer metrics (cycle duration, conflict rate, etc.) would help.
- **Pagination on external fetch**: `GetAllAsync` loads all lists in one go. At scale, pagination would make sense.

## Assumptions

- The external API is the source of truth for deletions: if something disappears there, we remove it locally.
- Clocks between systems are reasonably in sync. Last-write-wins depends on that.
- The volume of lists and items per sync cycle fits comfortably in memory.
- `IntervalSeconds`, `MaxRetryAttempts`, `CircuitBreakerThreshold`, and `CircuitBreakerDurationSeconds` are configurable per environment via configuration sections (`Sync` in appsettings). Values not overridden fall back to `SyncSettings` defaults (which are reasonable for local development).
