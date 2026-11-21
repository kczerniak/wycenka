using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;

namespace Wycenka.App.Views;

/// <summary>
/// Wieloliniowe pole tekstowe z podpowiedziami (do opisu robót).
/// Enter wstawia nową linię; gdy lista podpowiedzi jest otwarta: ↓/↑ wybiera, Enter wstawia
/// wybraną podpowiedź, Esc zamyka listę. (AutoCompleteBox usuwa znaki nowej linii, więc tu się nie nadaje.)
/// </summary>
public class PoleZPodpowiedziami : UserControl
{
    public static readonly StyledProperty<string?> TextProperty =
        AvaloniaProperty.Register<PoleZPodpowiedziami, string?>(nameof(Text), defaultBindingMode: BindingMode.TwoWay);

    public static readonly StyledProperty<IEnumerable<string>?> PodpowiedziProperty =
        AvaloniaProperty.Register<PoleZPodpowiedziami, IEnumerable<string>?>(nameof(Podpowiedzi));

    public const int MaksPodpowiedzi = 12;

    private readonly Popup _lista;
    private readonly ListBox _pozycje;
    private bool _synchronizacja;

    public string? Text
    {
        get => GetValue(TextProperty);
        set => SetValue(TextProperty, value);
    }

    public IEnumerable<string>? Podpowiedzi
    {
        get => GetValue(PodpowiedziProperty);
        set => SetValue(PodpowiedziProperty, value);
    }

    /// <summary>Wewnętrzne pole tekstowe.</summary>
    public TextBox Pole { get; }

    public bool ListaOtwarta => _lista.IsOpen;

    public IReadOnlyList<string> WidocznePodpowiedzi => (_pozycje.ItemsSource as IReadOnlyList<string>) ?? [];

    public PoleZPodpowiedziami()
    {
        Pole = new TextBox
        {
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            BorderThickness = new Thickness(0),
            MinHeight = 0,
            Padding = new Thickness(11, 6),
            VerticalContentAlignment = VerticalAlignment.Center,
        };
        Pole.TextChanged += (_, _) => PoZmianieTekstu();

        _pozycje = new ListBox { Focusable = false, MaxHeight = 260 };
        _pozycje.AddHandler(PointerReleasedEvent, (_, _) => WstawWybrana(), RoutingStrategies.Bubble, handledEventsToo: true);

        var ramka = new Border { BorderThickness = new Thickness(1), Child = _pozycje };
        ramka.Bind(Border.BackgroundProperty, ramka.GetResourceObservable("TloAplikacji"));
        ramka.Bind(Border.BorderBrushProperty, ramka.GetResourceObservable("SystemControlForegroundBaseLowBrush"));

        _lista = new Popup
        {
            PlacementTarget = Pole,
            Placement = PlacementMode.BottomEdgeAlignedLeft,
            IsLightDismissEnabled = true,
            Child = ramka,
        };

        Content = new Panel { Children = { Pole, _lista } };
        AddHandler(KeyDownEvent, PrzedKlawiszem, RoutingStrategies.Tunnel);
    }

    protected override void OnPropertyChanged(AvaloniaPropertyChangedEventArgs change)
    {
        base.OnPropertyChanged(change);
        if (change.Property == TextProperty && !_synchronizacja && Pole.Text != Text)
        {
            _synchronizacja = true;
            Pole.Text = Text;
            _synchronizacja = false;
        }
    }

    private void PoZmianieTekstu()
    {
        if (_synchronizacja) return;
        _synchronizacja = true;
        Text = Pole.Text;
        _synchronizacja = false;
        if (Pole.IsKeyboardFocusWithin) Filtruj();
    }

    /// <summary>Pokazuje podpowiedzi zaczynające się od wpisanego tekstu (bez względu na wielkość liter).</summary>
    public void Filtruj()
    {
        var wpisany = (Pole.Text ?? "").TrimStart();
        var pasujace = wpisany.Length == 0
            ? []
            : (Podpowiedzi ?? [])
                .Where(p => p.StartsWith(wpisany, StringComparison.CurrentCultureIgnoreCase) &&
                            !p.Equals(wpisany, StringComparison.CurrentCultureIgnoreCase))
                .Take(MaksPodpowiedzi)
                .ToList();

        _pozycje.ItemsSource = pasujace;
        _pozycje.SelectedIndex = -1;
        if (pasujace.Count > 0)
        {
            _lista.MinWidth = Pole.Bounds.Width;
            _lista.IsOpen = true;
        }
        else
        {
            _lista.IsOpen = false;
        }
    }

    private void WstawWybrana()
    {
        if (_pozycje.SelectedItem is not string wybrana) return;
        _lista.IsOpen = false;
        Pole.Text = wybrana;
        Pole.CaretIndex = wybrana.Length;
        Pole.Focus();
    }

    private void PrzedKlawiszem(object? sender, KeyEventArgs e)
    {
        if (!_lista.IsOpen) return;
        var liczba = WidocznePodpowiedzi.Count;
        switch (e.Key)
        {
            case Key.Down:
                _pozycje.SelectedIndex = Math.Min(_pozycje.SelectedIndex + 1, liczba - 1);
                e.Handled = true;
                break;
            case Key.Up:
                _pozycje.SelectedIndex = Math.Max(_pozycje.SelectedIndex - 1, -1);
                e.Handled = true;
                break;
            case Key.Enter when _pozycje.SelectedIndex >= 0:
                WstawWybrana();
                e.Handled = true;
                break;
            case Key.Escape:
                _lista.IsOpen = false;
                e.Handled = true;
                break;
        }
    }
}
