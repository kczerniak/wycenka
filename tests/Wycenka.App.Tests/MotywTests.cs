using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Wycenka.App.Services;
using Wycenka.App.ViewModels;
using Wycenka.App.Views;

namespace Wycenka.App.Tests;

public class MotywTests
{
    private static string TymczasowePlikUstawien() =>
        Path.Combine(Path.GetTempPath(), $"wycenka-ustawienia-{Guid.NewGuid()}", "ustawienia.json");

    [Fact]
    public void Ustawienia_zapisuja_i_wczytuja_motyw()
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            Assert.Equal(Motyw.Systemowy, UstawieniaAplikacji.Wczytaj(plik).Motyw);

            var u = UstawieniaAplikacji.Wczytaj(plik);
            u.Motyw = Motyw.Ciemny;
            u.Zapisz();

            Assert.Contains("Ciemny", File.ReadAllText(plik));
            Assert.Equal(Motyw.Ciemny, UstawieniaAplikacji.Wczytaj(plik).Motyw);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(plik)!, recursive: true);
        }
    }

    [Fact]
    public void Uszkodzony_plik_ustawien_daje_domyslne()
    {
        var plik = TymczasowePlikUstawien();
        Directory.CreateDirectory(Path.GetDirectoryName(plik)!);
        File.WriteAllText(plik, "{ to nie jest json");
        try
        {
            Assert.Equal(Motyw.Systemowy, UstawieniaAplikacji.Wczytaj(plik).Motyw);
        }
        finally
        {
            Directory.Delete(Path.GetDirectoryName(plik)!, recursive: true);
        }
    }

    [AvaloniaFact]
    public void Przelaczenie_motywu_zmienia_aplikacje_i_jest_zapamietane()
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik));
            Assert.False(vm.CiemnyAktywny);

            vm.PrzelaczMotywCommand.Execute(null);
            Assert.Equal(Motyw.Ciemny, vm.Motyw);
            Assert.True(vm.MotywCiemny);
            Assert.Equal(ThemeVariant.Dark, Application.Current!.ActualThemeVariant);
            Assert.True(vm.CiemnyAktywny);

            // Nowe uruchomienie programu pamięta wybór.
            var vm2 = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik));
            Assert.Equal(Motyw.Ciemny, vm2.Motyw);

            vm.PrzelaczMotywCommand.Execute(null);
            Assert.Equal(Motyw.Jasny, vm.Motyw);
            Assert.Equal(ThemeVariant.Light, Application.Current.ActualThemeVariant);

            vm.MotywSystemowy = true;
            Assert.Equal(Motyw.Systemowy, UstawieniaAplikacji.Wczytaj(plik).Motyw);
        }
        finally
        {
            MotywAplikacji.Zastosuj(Motyw.Systemowy);
            Directory.Delete(Path.GetDirectoryName(plik)!, recursive: true);
        }
    }

    /// <summary>Zrzuty wszystkich zakładek w ciemnym motywie (do przeglądu wyglądu).</summary>
    [AvaloniaFact]
    public async Task Zrzuty_ekranu_w_ciemnym_motywie()
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik)) { Motyw = Motyw.Ciemny };
            await vm.OtworzPlikAsync(Sciezki.Przyklad());
            vm.WybranyDzial = vm.Kosztorys.Dzialy[2];

            var okno = new MainWindow { DataContext = vm };
            okno.WindowState = WindowState.Normal;
            okno.Width = 1440;
            okno.Height = 900;
            okno.Show();
            Dispatcher.UIThread.RunJobs();

            var katalog = Environment.GetEnvironmentVariable("WYCENKA_ZRZUTY")
                          ?? Path.Combine(AppContext.BaseDirectory, "zrzuty");
            Directory.CreateDirectory(katalog);

            var zakladki = okno.GetVisualDescendants().OfType<TabControl>().First();
            string[] nazwy = ["pozycje", "dane", "podsumowanie"];
            for (var i = 0; i < nazwy.Length; i++)
            {
                zakladki.SelectedIndex = i;
                Dispatcher.UIThread.RunJobs();
                var klatka = okno.CaptureRenderedFrame();
                Assert.NotNull(klatka);
                klatka!.Save(Path.Combine(katalog, $"ciemny-{i + 1}-{nazwy[i]}.png"), new PngBitmapEncoderOptions());
            }
            okno.Close();
        }
        finally
        {
            MotywAplikacji.Zastosuj(Motyw.Systemowy);
            Directory.Delete(Path.GetDirectoryName(plik)!, recursive: true);
        }
    }
}
