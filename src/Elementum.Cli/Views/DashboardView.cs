using Elementum.Application.DTOs;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;
using Elementum.Cli.Rendering;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>
/// Displays the current market overview: latest price per metal in a table (ID, Name, Price USD, Price EUR, Changes % since start of day).
/// </summary>
public class DashboardView : AsyncDetailViewBase
{
    private readonly IHttpCall _api;

    public DashboardView(IHttpCall api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    protected override string ViewTitle => CliStrings.DashboardTitle;

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading live market data…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var overview = await _api.GetLiveMarketOverviewAsync();
            DashboardRenderer.RenderDashboard(overview, ViewTitle);
        });
    }
}
