namespace Elementum.Domain.Constants;

/// <summary>
/// Central domain-wide constants for symbols, currencies, physical conversion factors, and trading metrics.
/// </summary>
public static class DomainConstants
{
    /// <summary>Exact physical mass of one Troy Ounce (oz t) in grams.</summary>
    public const decimal TroyOunceInGrams = 31.1034768m;

    /// <summary>Standard ISO/Ticker symbols for precious metals.</summary>
    public static class Symbols
    {
        public const string Gold = "XAU";
        public const string Silver = "XAG";
        public const string Platinum = "XPT";
        public const string Palladium = "XPD";
    }

    /// <summary>Standard human-readable display names for precious metals.</summary>
    public static class Names
    {
        public const string Gold = "Gold";
        public const string Silver = "Silver";
        public const string Platinum = "Platinum";
        public const string Palladium = "Palladium";
    }

    /// <summary>Supported ISO currency codes.</summary>
    public static class Currencies
    {
        public const string Usd = "USD";
        public const string Eur = "EUR";
    }

    /// <summary>Constants used in technical trading metrics and indicators.</summary>
    public static class Trading
    {
        public const string BullishStatus = "BULLISH ▲";
        public const string BearishStatus = "BEARISH ▼";
        public const decimal PercentageMultiplier = 100m;
        public const int DefaultPrecisionDecimals = 2;
    }
}
