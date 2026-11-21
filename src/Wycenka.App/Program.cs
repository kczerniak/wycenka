using System;
using System.Globalization;
using Avalonia;

namespace Wycenka.App;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static int Main(string[] args)
    {
        // Program jest po polsku – liczby, daty i waluta zawsze w formacie pl-PL,
        // niezależnie od ustawień regionalnych Windowsa.
        var pl = CultureInfo.GetCultureInfo("pl-PL");
        CultureInfo.DefaultThreadCurrentCulture = pl;
        CultureInfo.DefaultThreadCurrentUICulture = pl;
        CultureInfo.CurrentCulture = pl;
        CultureInfo.CurrentUICulture = pl;

        if (TrybWsadowy.Dotyczy(args))
            return TrybWsadowy.Uruchom(args);

        return BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
