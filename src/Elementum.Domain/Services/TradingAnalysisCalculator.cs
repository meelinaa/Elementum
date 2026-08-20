using Elementum.Domain.Constants;

namespace Elementum.Domain.Services;

/// <summary>
/// Result of technical trading-indicator calculation for a single quote snapshot.
/// </summary>
public readonly record struct TradingAnalysis(
    decimal Ch,
    decimal Chp,
    decimal DiffPrevClose,
    string Status,
    decimal VolatilityRange,
    decimal VolatilityPercent);

/// <summary>
/// Domain service: computes change, percentage, previous-close delta, status, and volatility from prices.
/// </summary>
public static class TradingAnalysisCalculator
{
    /// <summary>
    /// Computes trading indicators from OHLC-style prices without magic numbers.
    /// </summary>
    public static TradingAnalysis Calculate(
        decimal currentPrice,
        decimal openPrice,
        decimal highPrice,
        decimal lowPrice,
        decimal prevClose)
    {
        decimal ch = currentPrice - openPrice;
        decimal chp = openPrice > 0
            ? Math.Round((ch / openPrice) * DomainConstants.Trading.PercentageMultiplier, DomainConstants.Trading.DefaultPrecisionDecimals)
            : 0m;
        decimal diffPrevClose = currentPrice - prevClose;

        string status = diffPrevClose >= 0
            ? DomainConstants.Trading.BullishStatus
            : DomainConstants.Trading.BearishStatus;

        decimal volatilityRange = highPrice - lowPrice;
        decimal volatilityPct = lowPrice > 0
            ? Math.Round((volatilityRange / lowPrice) * DomainConstants.Trading.PercentageMultiplier, DomainConstants.Trading.DefaultPrecisionDecimals)
            : 0m;

        return new TradingAnalysis(ch, chp, diffPrevClose, status, volatilityRange, volatilityPct);
    }
}
