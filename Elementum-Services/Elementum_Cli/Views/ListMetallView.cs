using Elementum.Shared.Objects;
using Elementum_Cli.Helper;
using Elementum_Cli.Providers;
using System.Text.Json;

namespace Elementum_Cli.Views;

public class ListMetallView : AsyncDetailViewBase
{
    protected override string ViewTitle => "METAL MASTER DATA";
    protected override string LoadingMessage => "Loading metal list…";

    protected override async Task LoadAndRenderAsync()
    {
        await ConsoleLoader.RunAsync(async () =>
        {
            var json = await HttpCall.GetMetalListAsync();
            var metals = JsonSerializer.Deserialize<List<Metals>>(json, HttpCall.DefaultJsonOptions);

            var w = CliConstants.ListMetallColumnWidths;

            Console.Clear();
            CliOutputHelper.RenderViewHeader(CliStrings.ListMetalsHeader);

            Console.WriteLine();
            Console.WriteLine(TableFormatter.BuildTopBorder(CliConstants.ListMetallTablePrefix, w));
            Console.WriteLine(TableFormatter.BuildRow(CliConstants.ListMetallTablePrefix, w, new[] { "ID", "SYMBOL", "NAME" }));
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
                        metal.Symbol ?? "",
                        metal.Name ?? ""
                    }));
                }
                Console.WriteLine(TableFormatter.BuildBottomBorder(CliConstants.ListMetallTablePrefix, w));
            }

            CliOutputHelper.RenderViewFooter();
        });
    }
}
