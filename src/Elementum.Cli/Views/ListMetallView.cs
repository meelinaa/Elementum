using Elementum.Shared.DTOs;
using Elementum.Cli.Api;
using Elementum.Cli.Constants;
using Elementum.Cli.Output;
using Elementum.Cli.Views.Interfaces;
using System.Text.Json;

namespace Elementum.Cli.Views;

/// <summary>
/// Displays the list of all metals (Id, Symbol, Name) in a table. Data from GET metals/all.
/// </summary>
public class ListMetallView : AsyncDetailViewBase
{
    /// <inheritdoc />
    protected override string ViewTitle => "METAL MASTER DATA";

    /// <inheritdoc />
    protected override string LoadingMessage => "Loading metal list…";

    /// <inheritdoc />
    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetMetalListAsync();
            var metals = JsonSerializer.Deserialize<List<MetalsDto>>(json, HttpCall.DefaultJsonOptions);

            var w = CliConstants.ListMetallColumnWidths;

            Console.Clear();
            CliOutputHelper.RenderViewHeader(CliStrings.ListMetalsHeader);

            Console.WriteLine();
            Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.ListMetallTablePrefix, w));
            Console.WriteLine(TableFormatter.BuildRow(CliConstants.ListMetallTablePrefix, w, ["ID", "SYMBOL", "NAME"]));
            Console.WriteLine(TableFormatter.BuildMidBorder(CliConstants.ListMetallTablePrefix, w));

            if (metals == null || metals.Count == 0)
            {
                Console.WriteLine(TableFormatter.BuildEmptyRow(CliConstants.ListMetallTablePrefix, w, CliOutputHelper.NoMetalsFoundMessage));
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.ListMetallTablePrefix, w));
            }
            else
            {
                foreach (var metal in metals)
                {
                    Console.WriteLine(TableFormatter.BuildRow(CliConstants.ListMetallTablePrefix, w, new[]
                    {
                        metal.Id.ToString(),
                        metal.Symbol,
                        metal.Name
                    }));
                }
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.ListMetallTablePrefix, w));
            }

            CliOutputHelper.RenderViewFooter();
        });
    }
}
