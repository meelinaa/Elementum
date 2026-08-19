using System.ComponentModel.DataAnnotations;
using Elementum.Application.Options;
using Elementum.Cli.Config;

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

    [Fact]
    public void MetalsApiOptions_ValidConfiguration_PassesValidation()
    {
        var options = new MetalsApiOptions
        {
            BaseUrl = "https://api.edelmetalle.de/public.json",
            Currency = "USD"
        };

        var errors = ValidateModel(options);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void MetalsApiOptions_MissingBaseUrl_FailsValidation(string? invalidUrl)
    {
        var options = new MetalsApiOptions
        {
            BaseUrl = invalidUrl!,
            Currency = "USD"
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MetalsApiOptions.BaseUrl)));
    }

    [Fact]
    public void MetalsApiOptions_InvalidUrlFormat_FailsValidation()
    {
        var options = new MetalsApiOptions
        {
            BaseUrl = "not-a-valid-url",
            Currency = "USD"
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(MetalsApiOptions.BaseUrl)));
    }

    [Fact]
    public void WorkerScheduleOptions_ValidConfiguration_PassesValidation()
    {
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = 22,
            RetentionDays = 7
        };

        var errors = ValidateModel(options);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(1441)]
    public void WorkerScheduleOptions_InvalidInterval_FailsValidation(int invalidInterval)
    {
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = invalidInterval,
            DailyRollupHour = 22,
            RetentionDays = 7
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.IngestionIntervalMinutes)));
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(24)]
    public void WorkerScheduleOptions_InvalidRollupHour_FailsValidation(int invalidHour)
    {
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = invalidHour,
            RetentionDays = 7
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.DailyRollupHour)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    [InlineData(366)]
    public void WorkerScheduleOptions_InvalidRetentionDays_FailsValidation(int invalidDays)
    {
        var options = new WorkerScheduleOptions
        {
            IngestionIntervalMinutes = 60,
            DailyRollupHour = 22,
            RetentionDays = invalidDays
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(WorkerScheduleOptions.RetentionDays)));
    }

    [Fact]
    public void RateLimitingOptions_ValidConfiguration_PassesValidation()
    {
        var options = new RateLimitingOptions
        {
            PermitLimit = 100,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        var errors = ValidateModel(options);

        Assert.Empty(errors);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(100001)]
    public void RateLimitingOptions_InvalidPermitLimit_FailsValidation(int invalidLimit)
    {
        var options = new RateLimitingOptions
        {
            PermitLimit = invalidLimit,
            WindowSeconds = 60,
            QueueLimit = 0
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RateLimitingOptions.PermitLimit)));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    [InlineData(86401)]
    public void RateLimitingOptions_InvalidWindowSeconds_FailsValidation(int invalidWindow)
    {
        var options = new RateLimitingOptions
        {
            PermitLimit = 100,
            WindowSeconds = invalidWindow,
            QueueLimit = 0
        };

        var errors = ValidateModel(options);

        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.MemberNames.Contains(nameof(RateLimitingOptions.WindowSeconds)));
    }

    [Fact]
    public void CliConfig_ResolveApiBaseUrl_WhenBothMissing_ThrowsCliConfigurationException()
    {
        Assert.Throws<Elementum.Cli.Exceptions.CliConfigurationException>(() => CliConfig.ResolveApiBaseUrl(null, null));
        Assert.Throws<Elementum.Cli.Exceptions.CliConfigurationException>(() => CliConfig.ResolveApiBaseUrl("", "  "));
    }

    [Fact]
    public void CliConfig_ResolveApiBaseUrl_WhenProvided_ReturnsNormalizedUrl()
    {
        var urlFromEnv = CliConfig.ResolveApiBaseUrl("http://localhost:5093/api/v1", null);
        Assert.Equal("http://localhost:5093/api/v1/", urlFromEnv);

        var urlFromFile = CliConfig.ResolveApiBaseUrl(null, "http://remote-api:8080/api/v1/");
        Assert.Equal("http://remote-api:8080/api/v1/", urlFromFile);
    }
}
