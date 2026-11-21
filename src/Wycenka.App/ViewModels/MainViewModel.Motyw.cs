using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Wycenka.App.Services;

namespace Wycenka.App.ViewModels;

/// <summary>Jasny / ciemny motyw.</summary>
public partial class MainViewModel
{
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(MotywSystemowy), nameof(MotywJasny), nameof(MotywCiemny))]
    public partial Motyw Motyw { get; set; }

    partial void OnMotywChanged(Motyw value)
    {
        MotywAplikacji.Zastosuj(value);
        if (_ustawienia.Motyw != value)
        {
            _ustawienia.Motyw = value;
            _ustawienia.Zapisz();
        }
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

    /// <summary>Czy faktycznie wyświetlany jest ciemny motyw (także gdy wynika z ustawień Windowsa).</summary>
    public bool CiemnyAktywny => MotywAplikacji.CiemnyAktywny;

    /// <summary>Wywoływane przez widok, gdy zmieni się motyw systemu lub aplikacji.</summary>
    public void OdswiezMotyw() => OnPropertyChanged(nameof(CiemnyAktywny));

    [RelayCommand]
    private void PrzelaczMotyw() => Motyw = CiemnyAktywny ? Motyw.Jasny : Motyw.Ciemny;
}
