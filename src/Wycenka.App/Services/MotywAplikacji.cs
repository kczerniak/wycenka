using System.Collections.Generic;
using Avalonia;
using Avalonia.Media;
using Avalonia.Styling;
using Avalonia.Themes.Fluent;

namespace Wycenka.App.Services;

/// <summary>Przełączanie motywu całej aplikacji (jasny / ciemny / brązowy).</summary>
public static class MotywAplikacji
{
    /// <summary>
    /// Własny wariant „Brązowy”: dziedziczy po ciemnym, ma własne tło (TloAplikacji w App.axaml).
    /// FluentTheme przyjmuje palety tylko dla Light/Dark, więc na czas motywu brązowego
    /// FluentTheme jest zastępowany nowym, z brązową paletą „Dark” (patrz <see cref="Zastosuj"/>).
    /// </summary>
    public static readonly ThemeVariant Brazowy = new("Brazowy", ThemeVariant.Dark);

    // Ciepłe brązy: tło jak gorzka czekolada, tekst kremowy, akcent karmelowy.
    private static readonly Dictionary<string, Color> KoloryBrazowe = new()
    {
        ["Accent"] = Color.Parse("#FFC8894B"),

        ["AltHigh"] = Color.Parse("#FF2A201B"),
        ["AltLow"] = Color.Parse("#332A201B"),
        ["AltMedium"] = Color.Parse("#992A201B"),
        ["AltMediumHigh"] = Color.Parse("#CC2A201B"),
        ["AltMediumLow"] = Color.Parse("#662A201B"),

        ["BaseHigh"] = Color.Parse("#FFF2E6D8"),
        ["BaseLow"] = Color.Parse("#33F2E6D8"),
        ["BaseMedium"] = Color.Parse("#99F2E6D8"),
        ["BaseMediumHigh"] = Color.Parse("#CCF2E6D8"),
        ["BaseMediumLow"] = Color.Parse("#66F2E6D8"),

        ["ChromeAltLow"] = Color.Parse("#FFF2E6D8"),
        ["ChromeDisabledHigh"] = Color.Parse("#FF3A2C24"),
        ["ChromeDisabledLow"] = Color.Parse("#FF8E7B6B"),
        ["ChromeGray"] = Color.Parse("#FF7A6656"),
        ["ChromeHigh"] = Color.Parse("#FF7A6656"),
        ["ChromeLow"] = Color.Parse("#FF231A15"),
        ["ChromeMedium"] = Color.Parse("#FF2E231D"),
        ["ChromeMediumLow"] = Color.Parse("#FF3A2C24"),
        ["ChromeWhite"] = Color.Parse("#FFF2E6D8"),

        ["ListLow"] = Color.Parse("#19F2E6D8"),
        ["ListMedium"] = Color.Parse("#33F2E6D8"),
        ["RegionColor"] = Color.Parse("#FF2A201B"),
    };

    // Akcenty jak w App.axaml (palety „Light” i „Dark”).
    private static readonly Color AkcentJasny = Color.Parse("#FF2563EB");
    private static readonly Color AkcentCiemny = Color.Parse("#FF3B82F6");

    private static bool _brazowaAktywna;

    public static void Zastosuj(Motyw motyw)
    {
        if (Application.Current is not { } app) return;

        var brazowa = motyw == Motyw.Brazowy;
        if (brazowa != _brazowaAktywna)
        {
            // Fluent buforuje pędzle wariantu ciemnego przy pierwszym użyciu, więc sama zmiana palety
            // nie przemaluje paneli. Podmieniamy cały FluentTheme na nowy – bez zbuforowanych pędzli.
            var indeks = -1;
            for (var i = 0; i < app.Styles.Count; i++)
                if (app.Styles[i] is FluentTheme) { indeks = i; break; }

            if (indeks >= 0)
            {
                var ciemna = new ColorPaletteResources();
                if (brazowa)
                {
                    foreach (var (wlasciwosc, kolor) in KoloryBrazowe)
                        typeof(ColorPaletteResources).GetProperty(wlasciwosc)!.SetValue(ciemna, kolor);
                }
                else
                {
                    ciemna.Accent = AkcentCiemny;
                }

                var nowy = new FluentTheme();
                nowy.Palettes[ThemeVariant.Light] = new ColorPaletteResources { Accent = AkcentJasny };
                nowy.Palettes[ThemeVariant.Dark] = ciemna;
                app.Styles[indeks] = nowy;

                _brazowaAktywna = brazowa;
            }
        }

        app.RequestedThemeVariant = motyw switch
        {
            Motyw.Jasny => ThemeVariant.Light,
            Motyw.Ciemny => ThemeVariant.Dark,
            Motyw.Brazowy => Brazowy,
            _ => ThemeVariant.Default,
        };
    }

    /// <summary>Czy wyświetlany jest ciemny wariant (ciemny, brązowy albo ciemny z ustawień Windowsa).</summary>
    public static bool CiemnyAktywny
    {
        get
        {
            var v = Application.Current?.ActualThemeVariant;
            return v == ThemeVariant.Dark || v?.InheritVariant == ThemeVariant.Dark;
        }
    }
}
