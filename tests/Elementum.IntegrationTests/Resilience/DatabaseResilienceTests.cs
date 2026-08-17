using System.Net.Sockets;
using Elementum.Domain.Entities;
using Elementum.Infrastructure.Data.Interfaces;
using Elementum.Infrastructure.Data.Resilience;
using Moq;

namespace Elementum.IntegrationTests.Resilience;

public class DatabaseResilienceTests
{
    [Fact]
    public async Task ResilientDbContext_WhenTransientExceptionOccurs_RetriesAndSucceeds()
    {
        var attempts = 0;
        var innerMock = new Mock<IElementumDbContext>();
        var list = new List<PriceHistory> { new() { Id = 1, MetalId = 1, Price = 2500m } };

        var transientEx = new SocketException((int)SocketError.ConnectionReset);

        innerMock.Setup(x => x.GetPriceHistoryAllLatest(It.IsAny<CancellationToken>()))
            .Returns(() =>
            {
                attempts++;
                if (attempts < 3)
                {
                    throw transientEx;
                }
                return Task.FromResult<IEnumerable<PriceHistory>>(list);
            });

        var options = new ElementumDbContextResilienceOptions
        {
            MaxRetryCount = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            UseExponentialBackoff = false
        };
        var policy = DatabaseResiliencePolicy.BuildRetryPolicy(options);
        var resilientContext = new ResilientElementumDbContext(innerMock.Object, policy);

        var result = await resilientContext.GetPriceHistoryAllLatest(CancellationToken.None);

        Assert.NotNull(result);
        Assert.Equal(3, attempts);
        Assert.Single(result);
    }

    [Fact]
    public async Task ResilientDbContext_WhenMaxRetriesExceeded_PropagatesException()
    {
        var innerMock = new Mock<IElementumDbContext>();
        var transientEx = new SocketException((int)SocketError.ConnectionReset);

        innerMock.Setup(x => x.GetPriceHistoryAllLatest(It.IsAny<CancellationToken>()))
            .ThrowsAsync(transientEx);

        var options = new ElementumDbContextResilienceOptions
        {
            MaxRetryCount = 2,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            UseExponentialBackoff = false
        };
        var policy = DatabaseResiliencePolicy.BuildRetryPolicy(options);
        var resilientContext = new ResilientElementumDbContext(innerMock.Object, policy);

        await Assert.ThrowsAsync<SocketException>(() => resilientContext.GetPriceHistoryAllLatest(CancellationToken.None));
    }
}
