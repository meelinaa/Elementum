using Elementum_Cli.Enums;
using Elementum_Cli.Views.Interfaces;

namespace Elementum_Cli.Views;

/// <summary>Factory and cache for detail views. One instance per <see cref="DetailView"/> for the process lifetime.</summary>
public static class ViewRegistry
{
    private static readonly Dictionary<DetailView, IDetailView> _cache = [];
    private static readonly Lock _lock = new();

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

    public static bool RequiresMetalSelection(DetailView view)
    {
        return view is DetailView.TradingView or DetailView.KaratCalculator or DetailView.History;
    }
}
