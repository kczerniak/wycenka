using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wycenka.App.Services;

namespace Wycenka.App.ViewModels;

/// <summary>Motyw: systemowy / jasny / ciemny / brązowy.</summary>
public partial class MainViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MotywSystemowy), nameof(MotywJasny), nameof(MotywCiemny), nameof(MotywBrazowy),
        nameof(NazwaMotywu))]
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

    /// <summary>Nazwa wybranego motywu – napis na przycisku na pasku narzędzi.</summary>
    public string NazwaMotywu => Motyw switch
    {
        Motyw.Jasny => "Jasny",
        Motyw.Ciemny => "Ciemny",
        Motyw.Brazowy => "Brązowy",
        _ => "Systemowy",
    };

    /// <summary>Wywoływane przez widok, gdy zmieni się motyw systemu lub aplikacji.</summary>
    public void OdswiezMotyw() => OnPropertyChanged(nameof(CiemnyAktywny));

    /// <summary>Ctrl+T: jasny ↔ ostatnio wybrany ciemny (zwykły albo brązowy).</summary>
    [RelayCommand]
    private void PrzelaczMotyw() =>
        Motyw = CiemnyAktywny ? Motyw.Jasny : _ustawienia.OstatniCiemny is Motyw.Brazowy ? Motyw.Brazowy : Motyw.Ciemny;
}
