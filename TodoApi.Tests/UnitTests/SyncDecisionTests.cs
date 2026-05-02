using TodoApi.Application.Services;

namespace TodoApi.Tests.UnitTests;

public class SyncDecisionTests
{
    private static readonly DateTime T = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Resolve_WhenNeitherChanged_ReturnsNoPushNoPull()
    {
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: T,
            syncedAt: T,
            externalUpdatedAt: T
        );
        Assert.False(push);
        Assert.False(pull);
    }

    [Fact]
    public void Resolve_WhenOnlyLocalChanged_ReturnsPush()
    {
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: T.AddHours(1),
            syncedAt: T,
            externalUpdatedAt: T
        );
        Assert.True(push);
        Assert.False(pull);
    }

    [Fact]
    public void Resolve_WhenOnlyExternalChanged_ReturnsPull()
    {
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: T,
            syncedAt: T,
            externalUpdatedAt: T.AddHours(1)
        );
        Assert.False(push);
        Assert.True(pull);
    }

    [Fact]
    public void Resolve_WhenBothChanged_LocalNewer_ReturnsPush()
    {
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: T.AddHours(2),
            syncedAt: T,
            externalUpdatedAt: T.AddHours(1)
        );
        Assert.True(push);
        Assert.False(pull);
    }

    [Fact]
    public void Resolve_WhenBothChanged_ExternalNewer_ReturnsPull()
    {
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: T.AddHours(1),
            syncedAt: T,
            externalUpdatedAt: T.AddHours(2)
        );
        Assert.False(push);
        Assert.True(pull);
    }

    [Fact]
    public void Resolve_WhenSyncedAtNull_ExternalChangesAreIgnored()
    {
        // externalChanged requires syncedAt != null — without a baseline we can't know if external changed
        var (push, pull) = SyncDecision.Resolve(
            localUpdatedAt: null,
            syncedAt: null,
            externalUpdatedAt: T
        );
        Assert.False(push);
        Assert.False(pull);
    }
}
