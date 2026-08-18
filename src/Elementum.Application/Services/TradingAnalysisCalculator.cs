using Elementum.Domain.Constants;

namespace Elementum.Application.Services;

/// <summary>
/// Domain/Application calculation engine for technical trading metrics (changes, percentage, volatility, status).
/// </summary>
public static class TradingAnalysisCalculator
{
    /// <summary>
    /// Computes trading indicators from prices without magic numbers.
    /// </summary>
    public static (decimal Ch, decimal Chp, decimal DiffPrevClose, string Status, decimal VolatilityRange, decimal VolatilityPercent)
        Calculate(decimal currentPrice, decimal openPrice, decimal highPrice, decimal lowPrice, decimal prevClose)
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

        return (ch, chp, diffPrevClose, status, volatilityRange, volatilityPct);
    }
}
