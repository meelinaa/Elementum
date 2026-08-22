using Elementum.Cli.Api;
using Elementum.Cli.Enums;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>Factory and cache for detail views.</summary>
public sealed class ViewRegistry
{
    private readonly IHttpCall _api;
    private readonly Dictionary<DetailView, IDetailView> _cache = [];
    private readonly Lock _lock = new();

    public ViewRegistry(IHttpCall api)
    {
        ArgumentNullException.ThrowIfNull(api);
        _api = api;
    }

    /// <summary>Returns the view instance for the given type; creates and caches it on first request.</summary>
    public IDetailView Get(DetailView view)
    {
        lock (_lock)
        {
            if (!_cache.TryGetValue(view, out var instance))
            {
                instance = Create(view);
                _cache[view] = instance;
            }
            return instance;
        }
    }

    private IDetailView Create(DetailView view) =>
        view switch
        {
            DetailView.Dashboard => new DashboardView(_api),
            DetailView.TradingView => new TradingView(_api),
            DetailView.History => new HistoryView(_api),
            DetailView.Info => new InfoView(),
            _ => new DashboardView(_api)
        };

    /// <summary>Returns true if the view requires the user to select a metal (Trading, History) before showing data.</summary>
    public static bool RequiresMetalSelection(DetailView view) =>
        view is DetailView.TradingView or DetailView.History;
}
