using Elementum.Application.Models;
using FluentValidation;

namespace Elementum.Application.Validation;

/// <summary>
/// Defensive schema and plausibility validator for incoming external <see cref="EdelmetalleApiResponse"/> payloads.
/// Prevents corrupted or anomalous upstream data from entering the database.
/// </summary>
public class EdelmetalleApiResponseValidator : AbstractValidator<EdelmetalleApiResponse>
{
    private const long MinValidUnixTimestamp = 1700000000; // Late 2023 baseline
    private const int MaxFutureSecondsTolerance = 300;     // Max 5 minutes clock skew tolerance

    public EdelmetalleApiResponseValidator()
    {
        RuleFor(x => x.Timestamp)
            .GreaterThan(MinValidUnixTimestamp)
            .WithMessage("Timestamp must be a valid Unix timestamp after 2023.")
            .LessThanOrEqualTo(_ => DateTimeOffset.UtcNow.ToUnixTimeSeconds() + MaxFutureSecondsTolerance)
            .WithMessage("Timestamp cannot be in the future.");

        RuleFor(x => x.GoldUsd)
            .InclusiveBetween(100m, 20000m)
            .WithMessage("Gold (USD) price is outside plausible market boundaries (100 - 20,000 USD).");

        RuleFor(x => x.GoldEur)
            .InclusiveBetween(100m, 20000m)
            .WithMessage("Gold (EUR) price is outside plausible market boundaries (100 - 20,000 EUR).");

        RuleFor(x => x.SilberUsd)
            .InclusiveBetween(1m, 1000m)
            .WithMessage("Silver (USD) price is outside plausible market boundaries (1 - 1,000 USD).");

        RuleFor(x => x.SilberEur)
            .InclusiveBetween(1m, 1000m)
            .WithMessage("Silver (EUR) price is outside plausible market boundaries (1 - 1,000 EUR).");

        RuleFor(x => x.PlatinUsd)
            .InclusiveBetween(50m, 10000m)
            .WithMessage("Platinum (USD) price is outside plausible market boundaries (50 - 10,000 USD).");

        RuleFor(x => x.PlatinEur)
            .InclusiveBetween(50m, 10000m)
            .WithMessage("Platinum (EUR) price is outside plausible market boundaries (50 - 10,000 EUR).");

        RuleFor(x => x.PalladiumUsd)
            .InclusiveBetween(50m, 10000m)
            .WithMessage("Palladium (USD) price is outside plausible market boundaries (50 - 10,000 USD).");

        RuleFor(x => x.PalladiumEur)
            .InclusiveBetween(50m, 10000m)
            .WithMessage("Palladium (EUR) price is outside plausible market boundaries (50 - 10,000 EUR).");

        RuleFor(x => x.WechselkursUsdEur)
            .InclusiveBetween(0.4m, 2.5m)
            .WithMessage("USD/EUR exchange rate is outside plausible market boundaries (0.4 - 2.5).");
    }
}
