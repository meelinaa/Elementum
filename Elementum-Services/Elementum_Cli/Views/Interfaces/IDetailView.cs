namespace Elementum_Cli.Views.Interfaces;

public interface IDetailView
{
    Task RenderAsync();
    void HandleInput(ConsoleKeyInfo key);
}
