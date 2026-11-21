using System.Collections.Specialized;
using System.ComponentModel;

namespace Wycenka.Core.Model;

/// <summary>
/// Nasłuchuje zmian w całym drzewie kosztorysu (dane, działy, pozycje, kolekcje)
/// i zgłasza je jednym zdarzeniem. Pomija właściwości wyliczane przez kalkulator.
/// </summary>
public sealed class ObserwatorZmian : IDisposable
{
    private static readonly HashSet<string> Wyliczane =
    [
        nameof(Pozycja.Lp), nameof(Dzial.Numer), nameof(Pozycja.CenaJednostkowa),
        nameof(Pozycja.WartoscR), nameof(Pozycja.WartoscM), nameof(Pozycja.WartoscS), nameof(Pozycja.Wartosc),
    ];

    private readonly Kosztorys _k;
    private readonly List<INotifyPropertyChanged> _obiekty = [];
    private readonly List<INotifyCollectionChanged> _kolekcje = [];

    /// <summary>Argumentem jest nazwa zmienionej właściwości albo null przy zmianie kolekcji.</summary>
    public event Action<string?>? Zmiana;

    public ObserwatorZmian(Kosztorys k)
    {
        _k = k;
        Podepnij();
    }

    private void Podepnij()
    {
        Sledz(_k);
        SledzKolekcje(_k.Dzialy);
        foreach (var d in _k.Dzialy)
        {
            Sledz(d);
            SledzKolekcje(d.Pozycje);
            foreach (var p in d.Pozycje) Sledz(p);
        }
    }

    private void Sledz(INotifyPropertyChanged o)
    {
        o.PropertyChanged += OnPropertyChanged;
        _obiekty.Add(o);
    }

    private void SledzKolekcje(INotifyCollectionChanged c)
    {
        c.CollectionChanged += OnCollectionChanged;
        _kolekcje.Add(c);
    }

    private void Odepnij()
    {
        foreach (var o in _obiekty) o.PropertyChanged -= OnPropertyChanged;
        foreach (var c in _kolekcje) c.CollectionChanged -= OnCollectionChanged;
        _obiekty.Clear();
        _kolekcje.Clear();
    }

    private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is { } n && Wyliczane.Contains(n)) return;
        Zmiana?.Invoke(e.PropertyName);
    }

    private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        // Drzewo jest małe – najprościej podpiąć wszystko od nowa.
        Odepnij();
        Podepnij();
        Zmiana?.Invoke(null);
    }

    public void Dispose()
    {
        Odepnij();
        Zmiana = null;
    }
}
