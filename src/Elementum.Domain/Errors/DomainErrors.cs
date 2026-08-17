using Elementum.Domain.Common;

namespace Elementum.Domain.Errors;

/// <summary>
/// Predefined domain errors for business operations and validations.
/// </summary>
public static class DomainErrors
{
    public static class Metals
    {
        public static readonly Error NotFound = new(
            "Metals.NotFound",
            "No metals were found in the catalog.");

        public static Error NotFoundBySymbol(string symbol) => new(
            "Metals.NotFoundBySymbol",
            $"Metal with symbol '{symbol}' was not found.");
    }

    public static class PriceHistory
    {
        public static readonly Error NotFound = new(
            "PriceHistory.NotFound",
            "No price history records found for the requested criteria.");

        public static Error NotFoundForSymbol(string symbol) => new(
            "PriceHistory.NotFoundForSymbol",
            $"No price history found for symbol '{symbol}'.");

        public static Error InvalidDateRange(string message) => new(
            "PriceHistory.InvalidDateRange",
            message);
    }

    public static class Validation
    {
        public static readonly Error InvalidSymbol = new(
            "Validation.InvalidSymbol",
            "Symbol must be a valid 3-letter precious metal code (e.g. XAU, XAG, XPT, XPD).");

        public static readonly Error InvalidDateRange = new(
            "Validation.InvalidDateRange",
            "Start date cannot be after end date or in the future.");

        public static Error General(string message) => new(
            "Validation.General",
            message);
    }
}
