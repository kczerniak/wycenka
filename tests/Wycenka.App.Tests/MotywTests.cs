using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Media;
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
    private static void UsunKatalog(string plik)
    {
        var d = Path.GetDirectoryName(plik)!;
        if (Directory.Exists(d)) Directory.Delete(d, recursive: true);
    }

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
            UsunKatalog(plik);
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
            UsunKatalog(plik);
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
            UsunKatalog(plik);
        }
    }

    /// <summary>Kolor piksela z wyrenderowanego okna (tak, jak widzi go użytkownik).</summary>
    private static Color Piksel(Window okno, int x, int y)
    {
        Dispatcher.UIThread.RunJobs();
        var klatka = okno.CaptureRenderedFrame()!;
        using var fb = klatka.Lock();
        var b = new byte[4];
        System.Runtime.InteropServices.Marshal.Copy(fb.Address + y * fb.RowBytes + x * 4, b, 0, 4);
        return fb.Format == Avalonia.Platform.PixelFormat.Rgba8888
            ? Color.FromArgb(b[3], b[0], b[1], b[2])
            : Color.FromArgb(b[3], b[2], b[1], b[0]);
    }

    // Puste miejsce na liście działów (panel w kolorze ChromeMediumLow).
    private static Color KolorPaneli(Window okno) => Piksel(okno, 100, 400);

    [AvaloniaFact]
    public void Motyw_brazowy_zmienia_kolory_i_jest_zapamietany_dla_przycisku()
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik));
            var okno = new MainWindow { DataContext = vm };
            okno.WindowState = WindowState.Normal;
            okno.Width = 900;
            okno.Height = 600;
            okno.Show();
            Assert.Equal("Ciemny", vm.NazwaCiemnego);

            vm.MotywCiemny = true;
            var paneleCiemne = KolorPaneli(okno);

            vm.MotywBrazowy = true;
            Assert.Equal(MotywAplikacji.Brazowy, Application.Current!.ActualThemeVariant);
            Assert.True(vm.CiemnyAktywny);
            Assert.Equal(Color.Parse("#FF3A2C24"), KolorPaneli(okno));

            // Przycisk: z brązowego na jasny i z powrotem na brązowy (zapamiętany).
            vm.PrzelaczMotywCommand.Execute(null);
            Assert.Equal(Motyw.Jasny, vm.Motyw);
            Assert.False(vm.CiemnyAktywny);
            Assert.Equal("Brązowy", vm.NazwaCiemnego);
            vm.PrzelaczMotywCommand.Execute(null);
            Assert.Equal(Motyw.Brazowy, vm.Motyw);

            var u = UstawieniaAplikacji.Wczytaj(plik);
            Assert.Equal(Motyw.Brazowy, u.Motyw);
            Assert.Equal(Motyw.Brazowy, u.OstatniCiemny);

            // Powrót na zwykły ciemny przywraca jego kolory.
            vm.MotywCiemny = true;
            Assert.Equal(ThemeVariant.Dark, Application.Current.ActualThemeVariant);
            Assert.Equal(paneleCiemne, KolorPaneli(okno));
            Assert.NotEqual(Color.Parse("#FF3A2C24"), paneleCiemne);
            Assert.Equal("Ciemny", vm.NazwaCiemnego);
            okno.Close();
        }
        finally
        {
            MotywAplikacji.Zastosuj(Motyw.Systemowy);
            UsunKatalog(plik);
        }
    }

    /// <summary>Zrzuty wszystkich zakładek w ciemnych motywach (do przeglądu wyglądu).</summary>
    [AvaloniaTheory]
    [InlineData(Motyw.Ciemny, "ciemny")]
    [InlineData(Motyw.Brazowy, "brazowy")]
    public async Task Zrzuty_ekranu_w_ciemnych_motywach(Motyw motyw, string prefiks)
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik)) { Motyw = motyw };
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
                klatka!.Save(Path.Combine(katalog, $"{prefiks}-{i + 1}-{nazwy[i]}.png"), new PngBitmapEncoderOptions());
            }
            okno.Close();
        }
        finally
        {
            MotywAplikacji.Zastosuj(Motyw.Systemowy);
            UsunKatalog(plik);
        }
    }

    /// <summary>Przełączanie motywu przy otwartym oknie – zrzut po każdym kroku.</summary>
    [AvaloniaFact]
    public void Przelaczanie_motywu_na_zywo_zrzuty()
    {
        var plik = TymczasowePlikUstawien();
        try
        {
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(plik)) { Motyw = Motyw.Ciemny };
            var okno = new MainWindow { DataContext = vm };
            okno.WindowState = WindowState.Normal;
            okno.Width = 900;
            okno.Height = 500;
            okno.Show();

            var katalog = Environment.GetEnvironmentVariable("WYCENKA_ZRZUTY")
                          ?? Path.Combine(AppContext.BaseDirectory, "zrzuty");
            Directory.CreateDirectory(katalog);

            Motyw[] kroki = [Motyw.Ciemny, Motyw.Brazowy, Motyw.Ciemny, Motyw.Jasny, Motyw.Brazowy];
            for (var i = 0; i < kroki.Length; i++)
            {
                vm.Motyw = kroki[i];
                    okno.CaptureRenderedFrame()!.Save(Path.Combine(katalog, $"na-zywo-{i + 1}-{kroki[i]}.png"),
                    new PngBitmapEncoderOptions());
            }
            okno.Close();
        }
        finally
        {
            MotywAplikacji.Zastosuj(Motyw.Systemowy);
            UsunKatalog(plik);
        }
    }
}
