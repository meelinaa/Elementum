using Elementum_Cli.Enums;

namespace Elementum_Cli.Views;

public static class ViewRegistry
{
    public static IDetailView Get(DetailView view)
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
