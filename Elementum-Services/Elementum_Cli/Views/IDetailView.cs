namespace Elementum_Cli.Views;

public interface IDetailView
{
    void Render();
    void HandleInput(ConsoleKeyInfo key);
}
