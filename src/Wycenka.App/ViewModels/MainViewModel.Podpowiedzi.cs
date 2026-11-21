using System;
using System.Collections.Generic;
using System.Linq;
using Wycenka.App.Services;
using Wycenka.Core.Model;

namespace Wycenka.App.ViewModels;

/// <summary>
/// Podpowiedzi przy wpisywaniu: wartości już użyte w tym kosztorysie, historia z poprzednich
/// kosztorysów (zapisywana w ustawieniach) i – dla j.m. – typowe jednostki.
/// </summary>
public partial class MainViewModel
{
    private static readonly string[] TypoweJednostki =
        ["m2", "m3", "m", "mb", "szt", "kpl", "kg", "t", "l", "r-g", "m-g", "godz", "rycz", "op", "obmiar"];

    public IReadOnlyList<string> PodpowiedziPodstawa { get; private set; } = [];
    public IReadOnlyList<string> PodpowiedziOpis { get; private set; } = [];
    public IReadOnlyList<string> PodpowiedziJm { get; private set; } = [];

    private IEnumerable<Pozycja> WszystkiePozycje() => Kosztorys.Dzialy.SelectMany(d => d.Pozycje);

    private static IReadOnlyList<string> Polacz(params IEnumerable<string>[] zrodla) =>
        zrodla.SelectMany(z => z)
            .Select(s => s.Trim())
            .Where(s => s.Length > 0)
            .Distinct(StringComparer.CurrentCultureIgnoreCase)
            .ToList();

    private void OdswiezPodpowiedzi()
    {
        PodpowiedziPodstawa = Polacz(WszystkiePozycje().Select(p => p.Podstawa), _ustawienia.HistoriaPodstawa);
        PodpowiedziOpis = Polacz(WszystkiePozycje().Select(p => p.Opis), _ustawienia.HistoriaOpis);
        PodpowiedziJm = Polacz(WszystkiePozycje().Select(p => p.Jm), _ustawienia.HistoriaJm, TypoweJednostki);
        OnPropertyChanged(nameof(PodpowiedziPodstawa));
        OnPropertyChanged(nameof(PodpowiedziOpis));
        OnPropertyChanged(nameof(PodpowiedziJm));
    }

    /// <summary>Zapamiętuje wpisy z kosztorysu w historii podpowiedzi (przy zapisie i eksporcie).</summary>
    private void ZapamietajWpisy()
    {
        var pozycje = WszystkiePozycje().ToList();
        UstawieniaAplikacji.Dopisz(_ustawienia.HistoriaPodstawa, pozycje.Select(p => p.Podstawa));
        UstawieniaAplikacji.Dopisz(_ustawienia.HistoriaOpis, pozycje.Select(p => p.Opis));
        UstawieniaAplikacji.Dopisz(_ustawienia.HistoriaJm, pozycje.Select(p => p.Jm));
        _ustawienia.Zapisz();
    }

    private static bool DotyczyPodpowiedzi(string? wlasciwosc) =>
        wlasciwosc is null or nameof(Pozycja.Podstawa) or nameof(Pozycja.Opis) or nameof(Pozycja.Jm);
}
