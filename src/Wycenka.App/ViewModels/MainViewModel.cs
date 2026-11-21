using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wycenka.App.Services;
using Wycenka.Core.Eksport;
using Wycenka.Core.Model;
using Wycenka.Core.Obliczenia;
using Wycenka.Core.Pliki;

namespace Wycenka.App.ViewModels;

public partial class MainViewModel : ViewModelBase
{
    private ObserwatorZmian? _obserwator;

    /// <summary>Okna dialogowe – ustawiane przez widok.</summary>
    public IDialogi? Dialogi { get; set; }

    public MainViewModel()
    {
        UstawKosztorys(Kosztorys.UtworzNowy(), null);
    }

    [ObservableProperty]
    public partial Kosztorys Kosztorys { get; set; } = null!;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UsunDzialCommand), nameof(DzialWGoreCommand), nameof(DzialWDolCommand))]
    public partial Dzial? WybranyDzial { get; set; }

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UsunPozycjeCommand), nameof(DuplikujPozycjeCommand),
        nameof(PozycjaWGoreCommand), nameof(PozycjaWDolCommand))]
    public partial Pozycja? WybranaPozycja { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Slownie))]
    public partial Podsumowanie Podsumowanie { get; set; } = Podsumowanie.Puste;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tytul))]
    public partial string? SciezkaPliku { get; set; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(Tytul))]
    public partial bool Zmieniony { get; set; }

    [ObservableProperty]
    public partial string Status { get; set; } = "Gotowy";

    public string Tytul =>
        (Zmieniony ? "● " : "") +
        (SciezkaPliku is null ? Kosztorys.Nazwa : Path.GetFileName(SciezkaPliku)) +
        " – Wycenka";

    public string Slownie => KwotaSlownie.Kwota(Podsumowanie.Brutto);

    public string Wersja =>
        typeof(MainViewModel).Assembly.GetName().Version?.ToString(3) ?? "";

    partial void OnWybranyDzialChanged(Dzial? value)
    {
        WybranaPozycja = value?.Pozycje.FirstOrDefault();
    }

    // ---------------------------------------------------------------- stan

    private void UstawKosztorys(Kosztorys k, string? sciezka)
    {
        _obserwator?.Dispose();
        Kosztorys = k;
        SciezkaPliku = sciezka;
        _obserwator = new ObserwatorZmian(k);
        _obserwator.Zmiana += OnZmiana;
        Przelicz();
        Zmieniony = false;
        WybranyDzial = k.Dzialy.FirstOrDefault();
        OnPropertyChanged(nameof(Tytul));
    }

    private void OnZmiana(string? wlasciwosc)
    {
        Zmieniony = true;
        Przelicz();
        if (wlasciwosc == nameof(Kosztorys.Nazwa)) OnPropertyChanged(nameof(Tytul));
    }

    private void Przelicz() => Podsumowanie = Kalkulator.Przelicz(Kosztorys);

    // ---------------------------------------------------------------- plik

    /// <summary>Pyta o zapis niezapisanych zmian. Zwraca false, gdy użytkownik anulował.</summary>
    public async Task<bool> PotwierdzPorzucenieAsync()
    {
        if (!Zmieniony || Dialogi is null) return true;
        return await Dialogi.ZapytajOZapisAsync(Kosztorys.Nazwa) switch
        {
            OdpowiedzZapisu.Zapisz => await ZapiszAsync(),
            OdpowiedzZapisu.NieZapisuj => true,
            _ => false,
        };
    }

    [RelayCommand]
    private async Task Nowy()
    {
        if (!await PotwierdzPorzucenieAsync()) return;
        UstawKosztorys(Kosztorys.UtworzNowy(), null);
        Status = "Utworzono nowy kosztorys";
    }

    [RelayCommand]
    private async Task Otworz()
    {
        if (Dialogi is null || !await PotwierdzPorzucenieAsync()) return;
        var sciezka = await Dialogi.WybierzPlikDoOtwarciaAsync();
        if (sciezka is not null) await OtworzPlikAsync(sciezka);
    }

    public async Task OtworzPlikAsync(string sciezka)
    {
        try
        {
            UstawKosztorys(PlikKosztorysu.Wczytaj(sciezka), sciezka);
            Status = $"Otwarto {sciezka}";
        }
        catch (Exception ex)
        {
            await PokazBlad($"Nie udało się otworzyć pliku:\n{sciezka}", ex);
        }
    }

    [RelayCommand]
    private Task Zapisz() => ZapiszAsync();

    [RelayCommand]
    private Task ZapiszJako() => ZapiszJakoAsync();

    private async Task<bool> ZapiszAsync()
    {
        if (SciezkaPliku is null) return await ZapiszJakoAsync();
        return await ZapiszDoAsync(SciezkaPliku);
    }

    private async Task<bool> ZapiszJakoAsync()
    {
        if (Dialogi is null) return false;
        var sciezka = await Dialogi.WybierzPlikDoZapisuAsync(
            NazwaPliku(), PlikKosztorysu.Rozszerzenie, "Kosztorys Wycenka");
        return sciezka is not null && await ZapiszDoAsync(sciezka);
    }

    private async Task<bool> ZapiszDoAsync(string sciezka)
    {
        try
        {
            PlikKosztorysu.Zapisz(Kosztorys, sciezka);
            SciezkaPliku = sciezka;
            Zmieniony = false;
            Status = $"Zapisano {sciezka}";
            return true;
        }
        catch (Exception ex)
        {
            await PokazBlad($"Nie udało się zapisać pliku:\n{sciezka}", ex);
            return false;
        }
    }

    [RelayCommand]
    private Task EksportPdf() => EksportujAsync("pdf", "Dokument PDF", Core.Eksport.EksportPdf.Zapisz);

    [RelayCommand]
    private Task EksportExcel() => EksportujAsync("xlsx", "Skoroszyt Excel", Core.Eksport.EksportExcel.Zapisz);

    private async Task EksportujAsync(string rozszerzenie, string opis, Action<Kosztorys, string> eksport)
    {
        if (Dialogi is null) return;
        var sciezka = await Dialogi.WybierzPlikDoZapisuAsync(NazwaPliku(), rozszerzenie, opis);
        if (sciezka is null) return;
        try
        {
            Status = "Eksportowanie…";
            var k = Kosztorys;
            await Task.Run(() => eksport(k, sciezka));
            Przelicz();
            Status = $"Wyeksportowano {sciezka}";
            await Dialogi.OtworzZewnetrznieAsync(sciezka);
        }
        catch (Exception ex)
        {
            Status = "Eksport nie powiódł się";
            await PokazBlad($"Nie udało się wyeksportować pliku:\n{sciezka}", ex);
        }
    }

    private string NazwaPliku()
    {
        var baza = SciezkaPliku is not null ? Path.GetFileNameWithoutExtension(SciezkaPliku) : Kosztorys.Nazwa;
        var niedozwolone = Path.GetInvalidFileNameChars().Concat(['<', '>', ':', '"', '/', '\\', '|', '?', '*']);
        foreach (var c in niedozwolone) baza = baza.Replace(c, '_');
        return string.IsNullOrWhiteSpace(baza) ? "kosztorys" : baza.Trim();
    }

    private Task PokazBlad(string tresc, Exception ex) =>
        Dialogi?.KomunikatAsync("Błąd", $"{tresc}\n\n{ex.Message}") ?? Task.CompletedTask;

    [RelayCommand]
    private Task OProgramie() =>
        Dialogi?.KomunikatAsync("O programie",
            $"Wycenka {Wersja}\nProgram do sporządzania kosztorysów budowlanych.\n\n" +
            "W polach liczbowych można wpisywać działania, np. 4*2,5+3.") ?? Task.CompletedTask;

    // ---------------------------------------------------------------- działy

    private bool JestDzial() => WybranyDzial is not null;

    [RelayCommand]
    private void DodajDzial()
    {
        var d = new Dzial { Nazwa = $"Dział {Kosztorys.Dzialy.Count + 1}" };
        var idx = WybranyDzial is null ? Kosztorys.Dzialy.Count : Kosztorys.Dzialy.IndexOf(WybranyDzial) + 1;
        Kosztorys.Dzialy.Insert(idx, d);
        WybranyDzial = d;
    }

    [RelayCommand(CanExecute = nameof(JestDzial))]
    private async Task UsunDzial()
    {
        if (WybranyDzial is not { } d) return;
        if (d.Pozycje.Count > 0 && Dialogi is not null &&
            !await Dialogi.PotwierdzAsync("Usuń dział",
                $"Usunąć dział „{d.Nazwa}” razem z {d.Pozycje.Count} pozycjami?"))
            return;

        var idx = Kosztorys.Dzialy.IndexOf(d);
        Kosztorys.Dzialy.RemoveAt(idx);
        WybranyDzial = Kosztorys.Dzialy.Count == 0 ? null : Kosztorys.Dzialy[Math.Min(idx, Kosztorys.Dzialy.Count - 1)];
    }

    [RelayCommand(CanExecute = nameof(JestDzial))]
    private void DzialWGore() => Przesun(-1);

    [RelayCommand(CanExecute = nameof(JestDzial))]
    private void DzialWDol() => Przesun(1);

    private void Przesun(int o)
    {
        if (WybranyDzial is not { } d) return;
        var i = Kosztorys.Dzialy.IndexOf(d);
        var j = i + o;
        if (j < 0 || j >= Kosztorys.Dzialy.Count) return;
        Kosztorys.Dzialy.Move(i, j);
        WybranyDzial = d;
    }

    // ---------------------------------------------------------------- pozycje

    private bool JestPozycja() => WybranaPozycja is not null && WybranyDzial is not null;

    [RelayCommand]
    private void DodajPozycje()
    {
        if (WybranyDzial is null) DodajDzial();
        var d = WybranyDzial!;
        var p = new Pozycja { Jm = WybranaPozycja?.Jm ?? "m2" };
        WstawPoWybranej(d, p);
    }

    [RelayCommand(CanExecute = nameof(JestPozycja))]
    private void DuplikujPozycje()
    {
        if (WybranyDzial is { } d && WybranaPozycja is { } p) WstawPoWybranej(d, p.Kopia());
    }

    private void WstawPoWybranej(Dzial d, Pozycja p)
    {
        var idx = WybranaPozycja is null ? d.Pozycje.Count : d.Pozycje.IndexOf(WybranaPozycja) + 1;
        if (idx <= 0) idx = d.Pozycje.Count;
        d.Pozycje.Insert(idx, p);
        WybranaPozycja = p;
    }

    [RelayCommand(CanExecute = nameof(JestPozycja))]
    private void UsunPozycje()
    {
        if (WybranyDzial is not { } d || WybranaPozycja is not { } p) return;
        var idx = d.Pozycje.IndexOf(p);
        d.Pozycje.RemoveAt(idx);
        WybranaPozycja = d.Pozycje.Count == 0 ? null : d.Pozycje[Math.Min(idx, d.Pozycje.Count - 1)];
    }

    [RelayCommand(CanExecute = nameof(JestPozycja))]
    private void PozycjaWGore() => PrzesunPozycje(-1);

    [RelayCommand(CanExecute = nameof(JestPozycja))]
    private void PozycjaWDol() => PrzesunPozycje(1);

    private void PrzesunPozycje(int o)
    {
        if (WybranyDzial is not { } d || WybranaPozycja is not { } p) return;
        var i = d.Pozycje.IndexOf(p);
        var j = i + o;
        if (j < 0 || j >= d.Pozycje.Count) return;
        d.Pozycje.Move(i, j);
        WybranaPozycja = p;
    }
}
