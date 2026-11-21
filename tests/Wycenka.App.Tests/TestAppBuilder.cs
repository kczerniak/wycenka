using System.Globalization;
using Avalonia;
using Avalonia.Headless;

[assembly: AvaloniaTestApplication(typeof(Wycenka.App.Tests.TestAppBuilder))]

namespace Wycenka.App.Tests;

public class TestAppBuilder
{
    public static AppBuilder BuildAvaloniaApp()
    {
        var pl = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = pl;
        CultureInfo.DefaultThreadCurrentUICulture = pl;
        CultureInfo.CurrentCulture = pl;
        CultureInfo.CurrentUICulture = pl;

        return AppBuilder.Configure<App>()
            .UseSkia()
            .WithInterFont()
            .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false });
    }
}

internal static class Sciezki
{
    public static string KatalogRepo()
    {
        var d = new DirectoryInfo(AppContext.BaseDirectory);
        while (d is not null && !File.Exists(Path.Combine(d.FullName, "Wycenka.sln"))) d = d.Parent;
        return d?.FullName ?? throw new DirectoryNotFoundException("Nie znaleziono katalogu repozytorium.");
    }

    public static string Przyklad() =>
        Directory.GetFiles(Path.Combine(KatalogRepo(), "przyklady"), "*.wycenka").First();
}
