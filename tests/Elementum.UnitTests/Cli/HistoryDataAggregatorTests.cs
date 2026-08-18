using Elementum.Application.DTOs;
using Elementum.Cli.Aggregation;
using Elementum.Cli.Enums;

namespace Elementum.UnitTests.Cli;

public class HistoryDataAggregatorTests
{
    [Fact]
    public void Aggregate_WhenNullOrEmpty_ReturnsNull()
    {
        Assert.Null(HistoryDataAggregator.Aggregate(null, HistoryPeriod.Daily, 10));
        Assert.Null(HistoryDataAggregator.Aggregate([], HistoryPeriod.Daily, 10));
    }

    [Fact]
    public void Aggregate_FiltersByCurrency()
    {
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Currency = "USD", Price = 2500m, EntryDate = new DateOnly(2026, 8, 1) },
            new() { Id = 2, Currency = "EUR", Price = 2300m, EntryDate = new DateOnly(2026, 8, 1) }
        };

        var resultEur = HistoryDataAggregator.Aggregate(list, HistoryPeriod.Daily, 10, "EUR");
        Assert.NotNull(resultEur);
        Assert.Single(resultEur);
        Assert.Equal(2300m, resultEur[0].Price);

        var resultUsd = HistoryDataAggregator.Aggregate(list, HistoryPeriod.Daily, 10, "USD");
        Assert.NotNull(resultUsd);
        Assert.Single(resultUsd);
        Assert.Equal(2500m, resultUsd[0].Price);
    }

    [Fact]
    public void Aggregate_Daily_ReturnsLatestDayTicks()
    {
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Currency = "EUR", Price = 2000m, EntryDate = new DateOnly(2026, 8, 17) },
            new() { Id = 2, Currency = "EUR", Price = 2100m, EntryDate = new DateOnly(2026, 8, 18) },
            new() { Id = 3, Currency = "EUR", Price = 2200m, EntryDate = new DateOnly(2026, 8, 18) }
        };

        var result = HistoryDataAggregator.Aggregate(list, HistoryPeriod.Daily, 10, "EUR");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.All(result, r => Assert.Equal(new DateOnly(2026, 8, 18), r.EntryDate));
    }

    [Fact]
    public void Aggregate_Monthly_ReturnsLastTickOfEachDay()
    {
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Currency = "EUR", Price = 2000m, EntryDate = new DateOnly(2026, 8, 17) },
            new() { Id = 2, Currency = "EUR", Price = 2050m, EntryDate = new DateOnly(2026, 8, 17) }, // last tick of day 17
            new() { Id = 3, Currency = "EUR", Price = 2100m, EntryDate = new DateOnly(2026, 8, 18) }  // last tick of day 18
        };

        var result = HistoryDataAggregator.Aggregate(list, HistoryPeriod.Monthly, 10, "EUR");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2050m, result[0].Price);
        Assert.Equal(2100m, result[1].Price);
    }

    [Fact]
    public void Aggregate_Yearly_ComputesMonthlyAverages()
    {
        var list = new List<PriceHistoryDto>
        {
            new() { Id = 1, Currency = "EUR", Price = 2000m, EntryDate = new DateOnly(2026, 1, 10) },
            new() { Id = 2, Currency = "EUR", Price = 3000m, EntryDate = new DateOnly(2026, 1, 20) }, // Jan Avg: 2500
            new() { Id = 3, Currency = "EUR", Price = 4000m, EntryDate = new DateOnly(2026, 2, 15) }  // Feb Avg: 4000
        };

        var result = HistoryDataAggregator.Aggregate(list, HistoryPeriod.Yearly, 12, "EUR");
        Assert.NotNull(result);
        Assert.Equal(2, result.Count);
        Assert.Equal(2500m, result[0].Price);
        Assert.Equal(4000m, result[1].Price);
    }
}
