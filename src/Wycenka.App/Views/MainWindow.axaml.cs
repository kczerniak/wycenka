using System.ComponentModel;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Wycenka.App.Services;
using Wycenka.App.ViewModels;

namespace Wycenka.App.Views;

public partial class MainWindow : Window
{
    private bool _zamykanieZatwierdzone;

    public MainWindow()
    {
        InitializeComponent();

        // Motyw mógł się zmienić z menu albo w ustawieniach Windowsa (tryb „Systemowy”).
        ActualThemeVariantChanged += (_, _) => (DataContext as MainViewModel)?.OdswiezMotyw();
    }

    protected override void OnDataContextChanged(System.EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is MainViewModel vm)
        {
            vm.Dialogi = new Dialogi(this);
            vm.OdswiezMotyw();
            vm.PropertyChanged += (_, a) =>
            {
                if (a.PropertyName == nameof(MainViewModel.WybranaPozycja) && vm.WybranaPozycja is { } p)
                    SiatkaPozycji.ScrollIntoView(p, null);
            };
        }
    }

    protected override async void OnClosing(WindowClosingEventArgs e)
    {
        base.OnClosing(e);
        if (_zamykanieZatwierdzone || DataContext is not MainViewModel vm || !vm.Zmieniony) return;

        e.Cancel = true;
        if (await vm.PotwierdzPorzucenieAsync())
        {
            _zamykanieZatwierdzone = true;
            Close();
        }
    }

    private void Zakoncz_Click(object? sender, RoutedEventArgs e) => Close();
}
