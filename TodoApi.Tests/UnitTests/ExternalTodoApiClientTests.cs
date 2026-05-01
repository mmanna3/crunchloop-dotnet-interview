using System.Net;
using System.Net.Http.Json;
using Microsoft.Extensions.Logging.Abstractions;
using Polly;
using Polly.CircuitBreaker;
using Polly.Extensions.Http;
using TodoApi.Application.ExternalApi;
using TodoApi.Application.ExternalApi.Dtos;

namespace TodoApi.Tests.UnitTests;

public class ExternalTodoApiClientTests
{
    private static (ExternalTodoApiClient client, FakeHttpMessageHandler handler) BuildClient(
        int retryCount = 3,
        int circuitBreakerThreshold = 5
    )
    {
        var fakeHandler = new FakeHttpMessageHandler();

        var retryPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .WaitAndRetryAsync(retryCount, _ => TimeSpan.Zero);

        var circuitBreakerPolicy = HttpPolicyExtensions
            .HandleTransientHttpError()
            .CircuitBreakerAsync(circuitBreakerThreshold, TimeSpan.FromMinutes(1));

        // Mirrors the production pipeline order: retry (outer) → circuit breaker → handler
        var cbHandler = new PollyDelegatingHandler(circuitBreakerPolicy)
        {
            InnerHandler = fakeHandler,
        };
        var retryHandler = new PollyDelegatingHandler(retryPolicy) { InnerHandler = cbHandler };

        var httpClient = new HttpClient(retryHandler)
        {
            BaseAddress = new Uri("https://test.example.com/"),
        };

        var client = new ExternalTodoApiClient(
            httpClient,
            NullLogger<ExternalTodoApiClient>.Instance
        );
        return (client, fakeHandler);
    }

    [Fact]
    public async Task GetAllAsync_WhenSuccess_ReturnsDeserializedLists()
    {
        var (client, handler) = BuildClient();
        handler.EnqueueJson(
            new List<ExternalTodoListDto>
            {
                new()
                {
                    Id = "ext-1",
                    Name = "List 1",
                    Items = [],
                },
            }
        );

        var result = await client.GetAllAsync();

        Assert.Single(result);
        Assert.Equal("ext-1", result[0].Id);
        Assert.Equal("List 1", result[0].Name);
    }

    [Fact]
    public async Task DeleteTodoListAsync_WhenNotFound_DoesNotThrow()
    {
        var (client, handler) = BuildClient();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        await client.DeleteTodoListAsync("ext-1");
    }

    [Fact]
    public async Task DeleteTodoItemAsync_WhenNotFound_DoesNotThrow()
    {
        var (client, handler) = BuildClient();
        handler.Enqueue(new HttpResponseMessage(HttpStatusCode.NotFound));

        await client.DeleteTodoItemAsync("ext-list-1", "ext-item-1");
    }

    [Fact]
    public async Task GetAllAsync_OnTransientError_RetriesConfiguredNumberOfTimes()
    {
        var (client, handler) = BuildClient(retryCount: 3, circuitBreakerThreshold: 10);
        handler.EnqueueServerErrors(4); // initial attempt + 3 retries, all fail

        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAllAsync());

        Assert.Equal(4, handler.CallCount);
    }

    [Fact]
    public async Task GetAllAsync_OnTransientError_RetriesUntilSuccess()
    {
        var (client, handler) = BuildClient(retryCount: 3, circuitBreakerThreshold: 10);
        handler.EnqueueServerErrors(2);
        handler.EnqueueJson(
            new List<ExternalTodoListDto>
            {
                new() { Id = "ext-1", Name = "List 1" },
            }
        );

        var result = await client.GetAllAsync();

        Assert.Single(result);
        Assert.Equal(3, handler.CallCount); // 2 failures + 1 success
    }

    [Fact]
    public async Task GetAllAsync_AfterCircuitBreakerThreshold_OpenCircuitFastFails()
    {
        var (client, handler) = BuildClient(retryCount: 0, circuitBreakerThreshold: 2);
        handler.EnqueueServerErrors(2);

        // Trigger the two failures that open the circuit
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAllAsync());
        await Assert.ThrowsAsync<HttpRequestException>(() => client.GetAllAsync());

        var callCountWhenCircuitOpened = handler.CallCount;

        // Circuit is open: Polly throws BrokenCircuitException<HttpResponseMessage> (subtype) without reaching the handler
        await Assert.ThrowsAnyAsync<BrokenCircuitException>(() => client.GetAllAsync());
        Assert.Equal(callCountWhenCircuitOpened, handler.CallCount);
    }
}

// Replicates what AddPolicyHandler does in the production HttpClient pipeline
internal sealed class PollyDelegatingHandler(IAsyncPolicy<HttpResponseMessage> policy)
    : DelegatingHandler
{
    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    ) => policy.ExecuteAsync(() => base.SendAsync(request, ct));
}

internal sealed class FakeHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _queue = new();
    public int CallCount { get; private set; }

    public void Enqueue(HttpResponseMessage response) => _queue.Enqueue(response);

    public void EnqueueJson<T>(T body) =>
        _queue.Enqueue(
            new HttpResponseMessage(HttpStatusCode.OK) { Content = JsonContent.Create(body) }
        );

    public void EnqueueServerErrors(int count)
    {
        for (var i = 0; i < count; i++)
            _queue.Enqueue(new HttpResponseMessage(HttpStatusCode.InternalServerError));
    }

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct
    )
    {
        CallCount++;
        return Task.FromResult(
            _queue.TryDequeue(out var response)
                ? response
                : new HttpResponseMessage(HttpStatusCode.InternalServerError)
        );
    }
}
