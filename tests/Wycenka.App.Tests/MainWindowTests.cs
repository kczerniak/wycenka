using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Wycenka.App.ViewModels;
using Wycenka.App.Views;

namespace Wycenka.App.Tests;

public class MainWindowTests
{
    private static MainWindow Pokaz(MainViewModel vm, int szer = 1440, int wys = 900)
    {
        var okno = new MainWindow { DataContext = vm };
        okno.WindowState = WindowState.Normal;
        okno.Width = szer;
        okno.Height = wys;
        okno.Show();
        Dispatcher.UIThread.RunJobs();
        return okno;
    }

    [AvaloniaFact]
    public void Wpisanie_wyrazenia_w_komorke_ilosci_zapisuje_wynik()
    {
        var vm = new MainViewModel();
        var p = vm.WybranaPozycja!;
        p.CenaM = 10m;
        var okno = Pokaz(vm);

        var siatka = okno.FindControl<DataGrid>("SiatkaPozycji")!;
        var kolumnaIlosc = siatka.Columns.First(c => (string?)c.Header == "Ilość");
        siatka.SelectedItem = p;
        siatka.CurrentColumn = kolumnaIlosc;
        siatka.Focus();
        Dispatcher.UIThread.RunJobs();

        Assert.True(siatka.BeginEdit());
        Dispatcher.UIThread.RunJobs();

        var pole = Assert.IsType<TextBox>(okno.FocusManager?.GetFocusedElement());
        pole.SelectAll();
        okno.KeyTextInput("4*2,5+3");
        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(13m, p.Ilosc);
        Assert.Equal(130m, p.WartoscM);
        Assert.True(vm.Zmieniony);
        okno.Close();
    }

    [AvaloniaFact]
    public void Bledna_liczba_w_komorce_nie_zmienia_wartosci()
    {
        var vm = new MainViewModel();
        var p = vm.WybranaPozycja!;
        p.Ilosc = 7m;
        var okno = Pokaz(vm);

        var siatka = okno.FindControl<DataGrid>("SiatkaPozycji")!;
        siatka.SelectedItem = p;
        siatka.CurrentColumn = siatka.Columns.First(c => (string?)c.Header == "Ilość");
        siatka.Focus();
        Dispatcher.UIThread.RunJobs();
        siatka.BeginEdit();
        Dispatcher.UIThread.RunJobs();

        var pole = Assert.IsType<TextBox>(okno.FocusManager?.GetFocusedElement());
        pole.SelectAll();
        okno.KeyTextInput("siedem");
        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(7m, p.Ilosc);
        okno.Close();
    }

    /// <summary>
    /// Renderuje wszystkie zakładki z przykładowym kosztorysem do PNG
    /// (katalog z WYCENKA_ZRZUTY albo bin/.../zrzuty) – do przeglądu wyglądu.
    /// </summary>
    [AvaloniaFact]
    public async Task Zrzuty_ekranu_zakladek()
    {
        var vm = new MainViewModel();
        await vm.OtworzPlikAsync(Sciezki.Przyklad());
        vm.WybranyDzial = vm.Kosztorys.Dzialy[2];
        var okno = Pokaz(vm);

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
            klatka!.Save(Path.Combine(katalog, $"{i + 1}-{nazwy[i]}.png"), new Avalonia.Media.Imaging.PngBitmapEncoderOptions());
        }

        okno.Close();
    }
}
