using System.Collections.ObjectModel;
using System.Text.Json.Serialization;
using CommunityToolkit.Mvvm.ComponentModel;

namespace Wycenka.Core.Model;

/// <summary>
/// Kosztorys budowlany: dane ogólne, narzuty i działy z pozycjami.
/// Robocizna liczona jest jako nakład r-g × stawka roboczogodziny,
/// materiały i sprzęt jako ceny jednostkowe (zł / j.m.).
/// </summary>
public partial class Kosztorys : ObservableObject
{
    public const int AktualnaWersjaPliku = 1;

    public int WersjaPliku { get; set; } = AktualnaWersjaPliku;

    [ObservableProperty] public partial string Nazwa { get; set; } = "Nowy kosztorys";
    [ObservableProperty] public partial string Obiekt { get; set; } = "";
    [ObservableProperty] public partial string Adres { get; set; } = "";
    [ObservableProperty] public partial string Inwestor { get; set; } = "";
    [ObservableProperty] public partial string Wykonawca { get; set; } = "";
    [ObservableProperty] public partial string Sporzadzil { get; set; } = "";
    [ObservableProperty] public partial DateTime DataOpracowania { get; set; } = DateTime.Today;
    [ObservableProperty] public partial string Uwagi { get; set; } = "";

    /// <summary>Stawka roboczogodziny [zł/r-g].</summary>
    [ObservableProperty] public partial decimal StawkaRg { get; set; } = 35m;

    /// <summary>Koszty pośrednie [% od R + S].</summary>
    [ObservableProperty] public partial decimal ProcentKp { get; set; } = 65m;

    /// <summary>Koszty zakupu materiałów [% od M].</summary>
    [ObservableProperty] public partial decimal ProcentKz { get; set; } = 5m;

    /// <summary>Zysk [% od R + S + Kp].</summary>
    [ObservableProperty] public partial decimal ProcentZ { get; set; } = 10m;

    /// <summary>Podatek VAT [% od wartości netto].</summary>
    [ObservableProperty] public partial decimal ProcentVat { get; set; } = 23m;

    public ObservableCollection<Dzial> Dzialy { get; set; } = [];

    public static Kosztorys UtworzNowy()
    {
        var k = new Kosztorys();
        var dzial = new Dzial { Nazwa = "Roboty przygotowawcze" };
        dzial.Pozycje.Add(new Pozycja());
        k.Dzialy.Add(dzial);
        return k;
    }
}

public partial class Dzial : ObservableObject
{
    [ObservableProperty] public partial string Nazwa { get; set; } = "Nowy dział";

    public ObservableCollection<Pozycja> Pozycje { get; set; } = [];

    // --- wartości wyliczane (nie zapisywane do pliku) ---

    [JsonIgnore, ObservableProperty] public partial int Numer { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscR { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscM { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscS { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal Wartosc { get; set; }
}

public partial class Pozycja : ObservableObject
{
    /// <summary>Podstawa wyceny, np. „KNR 2-01 0101-01” albo „kalkulacja własna”.</summary>
    [ObservableProperty] public partial string Podstawa { get; set; } = "";
    [ObservableProperty] public partial string Opis { get; set; } = "";
    [ObservableProperty] public partial string Jm { get; set; } = "m2";
    [ObservableProperty] public partial decimal Ilosc { get; set; }

    /// <summary>Nakład robocizny [r-g / j.m.].</summary>
    [ObservableProperty] public partial decimal NakladRg { get; set; }

    /// <summary>Cena materiałów [zł / j.m.].</summary>
    [ObservableProperty] public partial decimal CenaM { get; set; }

    /// <summary>Cena pracy sprzętu [zł / j.m.].</summary>
    [ObservableProperty] public partial decimal CenaS { get; set; }

    // --- wartości wyliczane (nie zapisywane do pliku) ---

    [JsonIgnore, ObservableProperty] public partial int Lp { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal CenaJednostkowa { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscR { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscM { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal WartoscS { get; set; }
    [JsonIgnore, ObservableProperty] public partial decimal Wartosc { get; set; }

    public Pozycja Kopia() => new()
    {
        Podstawa = Podstawa,
        Opis = Opis,
        Jm = Jm,
        Ilosc = Ilosc,
        NakladRg = NakladRg,
        CenaM = CenaM,
        CenaS = CenaS,
    };
}
