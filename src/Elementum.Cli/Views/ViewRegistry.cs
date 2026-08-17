using Elementum.Cli.Enums;
using Elementum.Cli.Views.Interfaces;

namespace Elementum.Cli.Views;

/// <summary>Factory and cache for detail views. One instance per <see cref="DetailView"/> for the process lifetime.</summary>
public static class ViewRegistry
{
    private static readonly Dictionary<DetailView, IDetailView> _cache = [];
    private static readonly Lock _lock = new();

    /// <summary>Returns the view instance for the given type; creates and caches it on first request.</summary>
    public static IDetailView Get(DetailView view)
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

    /// <summary>Creates a new instance of the view type (Dashboard, Trading, History, etc.).</summary>
    private static IDetailView Create(DetailView view)
    {
        return view switch
        {
            DetailView.Dashboard => new DashboardView(),
            DetailView.ListMetals => new ListMetallView(),
            DetailView.TradingView => new TradingView(),
            DetailView.KaratCalculator => new KaratCalculatorView(),
            DetailView.History => new HistoryView(),
            DetailView.Info => new InfoView(),
            _ => new DashboardView()
        };
    }

    /// <summary>Returns true if the view requires the user to select a metal (Trading, Karat, History) before showing data.</summary>
    public static bool RequiresMetalSelection(DetailView view)
    {
        return view is DetailView.TradingView or DetailView.KaratCalculator or DetailView.History;
    }
}
