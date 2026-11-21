using System.Globalization;
using Avalonia.Data;
using Wycenka.App.Converters;
using Wycenka.App.ViewModels;

namespace Wycenka.App.Tests;

public class MainViewModelTests
{
    [Fact]
    public void Nowy_kosztorys_jest_czysty_i_ma_jedna_pozycje()
    {
        var vm = new MainViewModel();
        Assert.False(vm.Zmieniony);
        Assert.Single(vm.Kosztorys.Dzialy);
        Assert.Same(vm.Kosztorys.Dzialy[0], vm.WybranyDzial);
        Assert.Same(vm.WybranyDzial!.Pozycje[0], vm.WybranaPozycja);
        Assert.DoesNotContain("●", vm.Tytul);
    }

    [Fact]
    public void Edycja_pozycji_przelicza_sumy_i_oznacza_zmiany()
    {
        var vm = new MainViewModel();
        vm.Kosztorys.StawkaRg = 50m;
        vm.Kosztorys.ProcentKp = 0;
        vm.Kosztorys.ProcentKz = 0;
        vm.Kosztorys.ProcentZ = 0;
        vm.Kosztorys.ProcentVat = 23m;

        var p = vm.WybranaPozycja!;
        p.Ilosc = 10m;
        p.NakladRg = 0.5m;
        p.CenaM = 20m;

        Assert.True(vm.Zmieniony);
        Assert.StartsWith("●", vm.Tytul);
        Assert.Equal(250m + 200m, p.Wartosc);
        Assert.Equal(450m, vm.Podsumowanie.Netto);
        Assert.Equal(553.50m, vm.Podsumowanie.Brutto);
        Assert.Equal("pięćset pięćdziesiąt trzy złote 50/100", vm.Slownie);
    }

    [Fact]
    public void Dodawanie_duplikowanie_przesuwanie_i_usuwanie_pozycji()
    {
        var vm = new MainViewModel();
        var pierwsza = vm.WybranaPozycja!;
        pierwsza.Opis = "A";
        pierwsza.Jm = "m3";

        vm.DodajPozycjeCommand.Execute(null);
        var druga = vm.WybranaPozycja!;
        Assert.NotSame(pierwsza, druga);
        Assert.Equal("m3", druga.Jm);              // j.m. podpowiadana z poprzedniej pozycji
        druga.Opis = "B";

        vm.DuplikujPozycjeCommand.Execute(null);
        Assert.Equal(["A", "B", "B"], vm.WybranyDzial!.Pozycje.Select(p => p.Opis));
        Assert.Equal([1, 2, 3], vm.WybranyDzial.Pozycje.Select(p => p.Lp));

        vm.WybranaPozycja = pierwsza;
        vm.PozycjaWDolCommand.Execute(null);
        Assert.Equal(["B", "A", "B"], vm.WybranyDzial.Pozycje.Select(p => p.Opis));
        Assert.Same(pierwsza, vm.WybranaPozycja);
        Assert.Equal(2, pierwsza.Lp);

        vm.UsunPozycjeCommand.Execute(null);
        Assert.Equal(["B", "B"], vm.WybranyDzial.Pozycje.Select(p => p.Opis));
        Assert.NotNull(vm.WybranaPozycja);
    }

    [Fact]
    public async Task Dzialy_mozna_dodawac_przesuwac_i_usuwac()
    {
        var vm = new MainViewModel();
        var d1 = vm.WybranyDzial!;
        vm.DodajDzialCommand.Execute(null);
        var d2 = vm.WybranyDzial!;
        Assert.Equal(2, vm.Kosztorys.Dzialy.Count);
        Assert.Equal(2, d2.Numer);
        Assert.Empty(d2.Pozycje);
        Assert.Null(vm.WybranaPozycja);

        vm.DzialWGoreCommand.Execute(null);
        Assert.Same(d2, vm.Kosztorys.Dzialy[0]);
        Assert.Equal(1, d2.Numer);

        // Pusty dział usuwa się bez pytania (brak okien dialogowych w teście).
        await vm.UsunDzialCommand.ExecuteAsync(null);
        Assert.Same(d1, Assert.Single(vm.Kosztorys.Dzialy));
        Assert.Same(d1, vm.WybranyDzial);
    }

    [Fact]
    public async Task Otwarcie_pliku_ustawia_sciezke_i_czysty_stan()
    {
        var vm = new MainViewModel();
        await vm.OtworzPlikAsync(Sciezki.Przyklad());
        Assert.False(vm.Zmieniony);
        Assert.Equal(3, vm.Kosztorys.Dzialy.Count);
        Assert.Equal(15987.08m, vm.Podsumowanie.Brutto);
        Assert.EndsWith(".wycenka – Wycenka", vm.Tytul);
    }
}

public class LiczbaConverterTests
{
    private static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl-PL");

    public LiczbaConverterTests()
    {
        CultureInfo.CurrentCulture = Pl;
    }

    [Theory]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5", 12.5)]
    [InlineData("4*2,5+3", 13)]
    [InlineData("1 234,50 zł", 1234.5)]
    [InlineData("", 0)]
    public void Wpisany_tekst_zamienia_na_liczbe(string tekst, double oczekiwane)
    {
        var w = LiczbaConverter.Instance.ConvertBack(tekst, typeof(decimal), null, Pl);
        Assert.Equal((decimal)oczekiwane, Assert.IsType<decimal>(w));
    }

    [Fact]
    public void Bledny_tekst_zwraca_blad_walidacji()
    {
        var w = LiczbaConverter.Instance.ConvertBack("abc", typeof(decimal), null, Pl);
        var n = Assert.IsType<BindingNotification>(w);
        Assert.Equal(BindingErrorType.DataValidationError, n.ErrorType);
    }

    [Fact]
    public void Wyswietla_w_polskim_formacie()
    {
        var t = (string)LiczbaConverter.Instance.Convert(1234.5m, typeof(string), "N2", Pl)!;
        Assert.Equal("1234,50", t.Replace(" ", "").Replace(" ", "").Replace(" ", ""));
        Assert.Contains(",", t);
    }
}
