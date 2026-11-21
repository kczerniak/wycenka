using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wycenka.App.Services;

namespace Wycenka.App.ViewModels;

/// <summary>Motyw: systemowy / jasny / ciemny / brązowy.</summary>
public partial class MainViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MotywSystemowy), nameof(MotywJasny), nameof(MotywCiemny), nameof(MotywBrazowy))]
    public partial Motyw Motyw { get; set; }

    partial void OnMotywChanged(Motyw value)
    {
        MotywAplikacji.Zastosuj(value);

        var zmiana = _ustawienia.Motyw != value;
        _ustawienia.Motyw = value;
        if (value is Motyw.Ciemny or Motyw.Brazowy && _ustawienia.OstatniCiemny != value)
        {
            _ustawienia.OstatniCiemny = value;
            zmiana = true;
        }
        if (zmiana) _ustawienia.Zapisz();

        OnPropertyChanged(nameof(NazwaCiemnego));
        OdswiezMotyw();
    }

    // Do pozycji menu typu „radio”.
    public bool MotywSystemowy
    {
        get => Motyw == Motyw.Systemowy;
        set { if (value) Motyw = Motyw.Systemowy; }
    }

    public bool MotywJasny
    {
        get => Motyw == Motyw.Jasny;
        set { if (value) Motyw = Motyw.Jasny; }
    }

    public bool MotywCiemny
    {
        get => Motyw == Motyw.Ciemny;
        set { if (value) Motyw = Motyw.Ciemny; }
    }

    public bool MotywBrazowy
    {
        get => Motyw == Motyw.Brazowy;
        set { if (value) Motyw = Motyw.Brazowy; }
    }

    /// <summary>Czy faktycznie wyświetlany jest ciemny wariant (także gdy wynika z ustawień Windowsa).</summary>
    public bool CiemnyAktywny => MotywAplikacji.CiemnyAktywny;

    /// <summary>Napis na przycisku, gdy jest jasno: dokąd przełączy („Ciemny” albo „Brązowy”).</summary>
    public string NazwaCiemnego => _ustawienia.OstatniCiemny == Motyw.Brazowy ? "Brązowy" : "Ciemny";

    /// <summary>Wywoływane przez widok, gdy zmieni się motyw systemu lub aplikacji.</summary>
    public void OdswiezMotyw() => OnPropertyChanged(nameof(CiemnyAktywny));

    [RelayCommand]
    private void PrzelaczMotyw() =>
        Motyw = CiemnyAktywny ? Motyw.Jasny : _ustawienia.OstatniCiemny is Motyw.Brazowy ? Motyw.Brazowy : Motyw.Ciemny;
}
