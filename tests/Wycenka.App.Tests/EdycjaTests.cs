using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Headless.XUnit;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Wycenka.App.Services;
using Wycenka.App.ViewModels;
using Wycenka.App.Views;
using Wycenka.Core.Model;

namespace Wycenka.App.Tests;

/// <summary>Uwagi testerki: opis w całości, Enter = nowa linia, strzałki zostają w komórce, podpowiedzi.</summary>
public class EdycjaTests : IDisposable
{
    private readonly string _katalog = Directory.CreateTempSubdirectory("wycenka-edycja").FullName;

    public void Dispose() => Directory.Delete(_katalog, recursive: true);

    private MainViewModel NowyVm() =>
        new(UstawieniaAplikacji.Wczytaj(Path.Combine(_katalog, "ustawienia.json")));

    private static MainWindow Pokaz(MainViewModel vm)
    {
        var okno = new MainWindow { DataContext = vm };
        okno.WindowState = WindowState.Normal;
        okno.Width = 1200;
        okno.Height = 700;
        okno.Show();
        Dispatcher.UIThread.RunJobs();
        return okno;
    }

    private static DataGrid Siatka(MainWindow okno) => okno.FindControl<DataGrid>("SiatkaPozycji")!;

    private static void Edytuj(MainWindow okno, Pozycja p, string kolumna)
    {
        var siatka = Siatka(okno);
        siatka.SelectedItem = p;
        siatka.CurrentColumn = siatka.Columns.First(c => (string?)c.Header == kolumna);
        siatka.Focus();
        Dispatcher.UIThread.RunJobs();
        Assert.True(siatka.BeginEdit());
        Dispatcher.UIThread.RunJobs();
    }

    private static TextBox Fokus(MainWindow okno) =>
        Assert.IsType<TextBox>(okno.FocusManager?.GetFocusedElement());

    [AvaloniaFact]
    public void Dlugi_opis_jest_widoczny_w_calosci()
    {
        var vm = NowyVm();
        var p = vm.WybranaPozycja!;
        var okno = Pokaz(vm);
        var wiersz = Siatka(okno).GetVisualDescendants().OfType<DataGridRow>().First();
        var wysokoscPrzed = wiersz.Bounds.Height;

        p.Opis = "Licowanie ścian płytkami gresowymi 60×120 cm na kleju elastycznym C2TE S1, " +
                 "z docinaniem, fugowaniem i wykonaniem narożników ze szlifowaniem krawędzi pod kątem 45°";
        Dispatcher.UIThread.RunJobs();

        Assert.True(wiersz.Bounds.Height > wysokoscPrzed * 1.5,
            $"Wiersz powinien urosnąć (przed {wysokoscPrzed}, po {wiersz.Bounds.Height})");

        var katalog = Environment.GetEnvironmentVariable("WYCENKA_ZRZUTY")
                      ?? Path.Combine(AppContext.BaseDirectory, "zrzuty");
        Directory.CreateDirectory(katalog);
        okno.CaptureRenderedFrame()!.Save(Path.Combine(katalog, "dlugi-opis.png"), new PngBitmapEncoderOptions());
        okno.Close();
    }

    [AvaloniaFact]
    public void Enter_w_opisie_robi_nowa_linie()
    {
        var vm = NowyVm();
        var p = vm.WybranaPozycja!;
        var okno = Pokaz(vm);

        Edytuj(okno, p, "Opis robót");
        okno.KeyTextInput("Linia pierwsza");
        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        okno.KeyTextInput("Linia druga");
        Dispatcher.UIThread.RunJobs();

        Assert.Equal(["Linia pierwsza", "Linia druga"], p.Opis.Split('\n').Select(l => l.TrimEnd('\r')));
        Assert.NotNull(okno.FocusManager?.GetFocusedElement() as TextBox);   // dalej w edycji
        okno.Close();
    }

    [AvaloniaTheory]
    [InlineData("Opis robót")]
    [InlineData("Podstawa")]
    [InlineData("Ilość")]
    public void Strzalka_w_lewo_na_poczatku_tekstu_nie_wychodzi_z_komorki(string kolumna)
    {
        var vm = NowyVm();
        var p = vm.WybranaPozycja!;
        p.Opis = "abc";
        p.Podstawa = "abc";
        p.Ilosc = 12m;
        var okno = Pokaz(vm);

        Edytuj(okno, p, kolumna);
        var pole = Fokus(okno);
        pole.CaretIndex = 0;
        okno.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        okno.KeyPressQwerty(PhysicalKey.ArrowLeft, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Same(pole, okno.FocusManager?.GetFocusedElement());
        Assert.Equal(kolumna, (string?)Siatka(okno).CurrentColumn?.Header);

        okno.KeyPressQwerty(PhysicalKey.ArrowRight, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(1, pole.CaretIndex);
        okno.Close();
    }

    [AvaloniaFact]
    public void Podstawa_podpowiada_wczesniej_wpisana_wartosc()
    {
        var vm = NowyVm();
        vm.WybranaPozycja!.Podstawa = "kalkulacja własna";
        vm.DodajPozycjeCommand.Execute(null);
        var nowa = vm.WybranaPozycja!;
        Assert.Contains("kalkulacja własna", vm.PodpowiedziPodstawa);
        var okno = Pokaz(vm);

        Edytuj(okno, nowa, "Podstawa");
        okno.KeyTextInput("kal");
        Dispatcher.UIThread.RunJobs();

        var podpowiedzi = Fokus(okno).FindAncestorOfType<AutoCompleteBox>()!;
        Assert.True(podpowiedzi.IsDropDownOpen);

        okno.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();

        Assert.Equal("kalkulacja własna", nowa.Podstawa);
        okno.Close();
    }

    [AvaloniaFact]
    public void Opis_podpowiada_i_dalej_przyjmuje_nowe_linie()
    {
        var vm = NowyVm();
        vm.WybranaPozycja!.Opis = "Skucie płytek ceramicznych ze ścian";
        vm.DodajPozycjeCommand.Execute(null);
        var nowa = vm.WybranaPozycja!;
        var okno = Pokaz(vm);

        Edytuj(okno, nowa, "Opis robót");
        okno.KeyTextInput("sku");
        Dispatcher.UIThread.RunJobs();

        var pole = Fokus(okno).FindAncestorOfType<PoleZPodpowiedziami>()!;
        Assert.True(pole.ListaOtwarta);
        Assert.Equal(["Skucie płytek ceramicznych ze ścian"], pole.WidocznePodpowiedzi);

        okno.KeyPressQwerty(PhysicalKey.ArrowDown, RawInputModifiers.None);
        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        Dispatcher.UIThread.RunJobs();
        Assert.Equal("Skucie płytek ceramicznych ze ścian", nowa.Opis);
        Assert.False(pole.ListaOtwarta);

        okno.KeyPressQwerty(PhysicalKey.Enter, RawInputModifiers.None);
        okno.KeyTextInput("wraz z wywozem");
        Dispatcher.UIThread.RunJobs();
        Assert.Equal(["Skucie płytek ceramicznych ze ścian", "wraz z wywozem"],
            nowa.Opis.Split('\n').Select(l => l.TrimEnd('\r')));
        okno.Close();
    }

    [Fact]
    public async Task Podpowiedzi_pamietaja_wpisy_z_poprzednich_kosztorysow()
    {
        var vm = NowyVm();
        vm.WybranaPozycja!.Podstawa = "KNR 2-02 0829-03";
        vm.WybranaPozycja.Opis = "Licowanie ścian płytkami";
        vm.WybranaPozycja.Jm = "m2";
        vm.Dialogi = new SztuczneDialogi { PlikDoZapisu = Path.Combine(_katalog, "k.wycenka") };
        await vm.ZapiszCommand.ExecuteAsync(null);

        // Nowe uruchomienie programu, nowy pusty kosztorys.
        var vm2 = NowyVm();
        Assert.Contains("KNR 2-02 0829-03", vm2.PodpowiedziPodstawa);
        Assert.Contains("Licowanie ścian płytkami", vm2.PodpowiedziOpis);
        Assert.Contains("szt", vm2.PodpowiedziJm);                  // typowe jednostki
        Assert.Single(vm2.PodpowiedziJm, j => j == "m2");          // bez duplikatów
    }
}
