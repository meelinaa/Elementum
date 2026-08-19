using System.ComponentModel.DataAnnotations;
using Elementum.Application.Options;
using Elementum.Cli.Config;
using Elementum.Cli.Exceptions;

namespace Elementum.UnitTests.Application.Options;

public class OptionsValidationTests
{
    private static IList<ValidationResult> ValidateModel(object model)
    {
        var results = new List<ValidationResult>();
        var context = new ValidationContext(model, serviceProvider: null, items: null);
        Validator.TryValidateObject(model, context, results, validateAllProperties: true);
        return results;
    }

    // [R]IGHT-BICEP: Verifies that a valid MetalsApiOptions model passes data annotation validation
    [Fact]
    public void MetalsApiOptions_ValidConfiguration_PassesValidation()
    {
        // Arrange
        var options = new MetalsApiOptions
        {
            BaseUrl = "https://api.edelmetalle.de/public.json",
            Currency = "USD"
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.Empty(errors);
    }

    // [B]OUNDARY / [E]RROR: Verifies that missing or whitespace BaseUrl fails options validation
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MetalsApiOptions_MissingBaseUrl_FailsValidation(string? invalidUrl)
    {
        // Arrange
        var options = new MetalsApiOptions
        {
            BaseUrl = invalidUrl!,
            Currency = "USD"
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MetalsApiOptions.BaseUrl)));
    }

    // [E]RROR: Verifies that malformed URL format fails validation
    [Fact]
    public void MetalsApiOptions_InvalidUrlFormat_FailsValidation()
    {
        // Arrange
        var options = new MetalsApiOptions
        {
            BaseUrl = "not-a-valid-url",
            Currency = "USD"
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MetalsApiOptions.BaseUrl)));
    }

    // [R]IGHT-BICEP: Verifies that valid WorkerScheduleOptions passes validation
    [Fact]
    public void WorkerScheduleOptions_ValidConfiguration_PassesValidation()
    {
        // Arrange
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = 22,
            RetentionDays = 7
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.Empty(errors);
    }

    // [B]OUNDARY / [E]RROR: Verifies that out-of-range interval values fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void WorkerScheduleOptions_InvalidInterval_FailsValidation(int invalidInterval)
    {
        // Arrange
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = invalidInterval,
            DailyRollupHour = 22,
            RetentionDays = 7
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.IngestionIntervalMinutes)));
    }

    // [B]OUNDARY / [E]RROR: Verifies that out-of-range rollup hour values (outside [0, 23]) fail validation
    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void WorkerScheduleOptions_InvalidRollupHour_FailsValidation(int invalidHour)
    {
        // Arrange
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = invalidHour,
            RetentionDays = 7
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.DailyRollupHour)));
    }

    // [B]OUNDARY / [E]RROR: Verifies that out-of-range retention days fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(366)]
    public void WorkerScheduleOptions_InvalidRetentionDays_FailsValidation(int invalidDays)
    {
        // Arrange
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = 22,
            RetentionDays = invalidDays
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.RetentionDays)));
    }

    // [R]IGHT-BICEP: Verifies that valid RateLimitingOptions configuration passes validation
    [Fact]
    public void RateLimitingOptions_ValidConfiguration_PassesValidation()
    {
        // Arrange
        var options = new RateLimitingOptions
        {
            PermitLimit = 100,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.Empty(errors);
    }

    // [B]OUNDARY / [E]RROR: Verifies that non-positive or extreme permit limits fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public void RateLimitingOptions_InvalidPermitLimit_FailsValidation(int invalidLimit)
    {
        // Arrange
        var options = new RateLimitingOptions
        {
            PermitLimit = invalidLimit,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RateLimitingOptions.PermitLimit)));
    }

    // [B]OUNDARY / [E]RROR: Verifies that invalid rate-limiting window seconds fail validation
    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(86401)]
    public void RateLimitingOptions_InvalidWindowSeconds_FailsValidation(int invalidWindow)
    {
        // Arrange
        var options = new RateLimitingOptions
        {
            PermitLimit = 100,
            WindowSeconds = invalidWindow,
            QueueLimit = 0
        };

        // Act
        var errors = ValidateModel(options);

        // Assert
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RateLimitingOptions.WindowSeconds)));
    }

    // [E]RROR: Verifies that missing API base URL from all sources throws CliConfigurationException
    [Fact]
    public void CliConfig_ResolveApiBaseUrl_WhenBothMissing_ThrowsCliConfigurationException()
    {
        // Arrange, Act & Assert
        Assert.Throws<CliConfigurationException>(() => CliConfig.ResolveApiBaseUrl(null, null));
        Assert.Throws<CliConfigurationException>(() => CliConfig.ResolveApiBaseUrl("", "  "));
    }

    // [R]IGHT-BICEP: Verifies that configured base URLs are normalized with trailing slashes
    [Fact]
    public void CliConfig_ResolveApiBaseUrl_WhenProvided_ReturnsNormalizedUrl()
    {
        // Arrange & Act
        var urlFromEnv = CliConfig.ResolveApiBaseUrl("http://localhost:5093/api/v1", null);
        var urlFromFile = CliConfig.ResolveApiBaseUrl(null, "http://remote-api:8080/api/v1/");

        // Assert
        Assert.Equal("http://localhost:5093/api/v1/", urlFromEnv);
        Assert.Equal("http://remote-api:8080/api/v1/", urlFromFile);
    }
}
