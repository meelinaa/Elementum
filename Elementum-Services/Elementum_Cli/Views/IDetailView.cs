namespace Elementum_Cli.Views;

public interface IDetailView
{
    Task RenderAsync();
    void HandleInput(ConsoleKeyInfo key);
}
