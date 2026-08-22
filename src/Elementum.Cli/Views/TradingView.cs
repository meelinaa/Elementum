using Elementum.Application.DTOs;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;
using Elementum.Cli.Rendering;

namespace Elementum.Cli.Views;

/// <summary>
/// Displays trading data for one metal: high/low, open, change, comparison vs previous close, volatility.
/// </summary>
public class TradingView : MetalDetailViewBase
{
    private readonly IHttpCall _api;

    public TradingView(IHttpCall api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <inheritdoc />
    protected override string ViewTitle => "TRADING & DAILY ANALYSIS";

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading daily trading data…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync(string sym, string name)
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var app = AppContext.Current!;
            var currency = app.SelectedCurrency;
            var currencySymbol = app.CurrencySymbol;

            var item = await _api.GetPriceHistoryTradingLatestAsync(sym, currency);

            TradingRenderer.RenderTradingData(item, sym, name, currency, currencySymbol, ViewTitle);
        });
    }
}
