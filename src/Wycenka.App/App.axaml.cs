using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Wycenka.App.ViewModels;
using Wycenka.App.Views;

namespace Wycenka.App;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            var vm = new MainViewModel();
            desktop.MainWindow = new MainWindow { DataContext = vm };

            // Plik .wycenka przekazany z linii poleceń (np. „Otwórz za pomocą…”).
            var plik = desktop.Args?.FirstOrDefault(File.Exists);
            if (plik is not null) _ = vm.OtworzPlikAsync(plik);
        }

        base.OnFrameworkInitializationCompleted();
    }
}
