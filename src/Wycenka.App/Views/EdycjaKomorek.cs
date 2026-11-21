using System.Linq;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace Wycenka.App.Views;

/// <summary>
/// Zachowanie pól w edytowanej komórce siatki pozycji:
/// – strzałki ←/→ (oraz Home/End) przesuwają kursor w tekście i nigdy nie wyrzucają z komórki
///   (domyślnie DataGrid przechodził do sąsiedniej komórki, gdy kursor był na brzegu tekstu);
/// – w polu wieloliniowym (opis robót) dotyczy to też ↑/↓, a Enter wstawia nową linię;
/// – edycja zaczęta pisaniem zaczyna się od wpisanej litery (jak w zwykłych kolumnach);
/// – Enter na podświetlonej podpowiedzi wybiera ją.
/// </summary>
public static class EdycjaKomorek
{
    private static readonly ConditionalWeakTable<Control, object> Podpiete = new();

    public static void Podlacz(DataGrid siatka)
    {
        siatka.PreparingCellForEdit += OnPreparingCellForEdit;
        siatka.AddHandler(InputElement.KeyDownEvent, PrzedKlawiszem, RoutingStrategies.Tunnel);
    }

    private static void OnPreparingCellForEdit(object? sender, DataGridPreparingCellForEditEventArgs e)
    {
        var pole = e.EditingElement;
        if (!Podpiete.TryGetValue(pole, out _))
        {
            Podpiete.Add(pole, new object());
            pole.AddHandler(InputElement.KeyDownEvent, PoKlawiszu, RoutingStrategies.Bubble);
        }

        if (pole is AutoCompleteBox or PoleZPodpowiedziami)
        {
            var wpisany = (e.EditingEventArgs as TextInputEventArgs)?.Text;
            // Szablon pola jest gotowy dopiero po pierwszym przebiegu układu.
            Dispatcher.UIThread.Post(() =>
            {
                if (PoleTekstowe(pole) is not { } tekst) return;
                if (!string.IsNullOrEmpty(wpisany)) tekst.Text = wpisany;
                tekst.Focus();
                tekst.ClearSelection();
                tekst.CaretIndex = tekst.Text?.Length ?? 0;
                if (!string.IsNullOrEmpty(wpisany) && pole is PoleZPodpowiedziami opis) opis.Filtruj();
            });
        }
    }

    private static TextBox? PoleTekstowe(Control c) => c.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();

    private static bool Wieloliniowe(object? pole) => pole is Control c && c.Classes.Contains("wieloliniowy");

    /// <summary>Klawisze, których pole tekstowe nie obsłużyło (kursor na brzegu) – nie oddajemy ich siatce.</summary>
    private static void PoKlawiszu(object? sender, KeyEventArgs e)
    {
        if (e.KeyModifiers is not (KeyModifiers.None or KeyModifiers.Shift)) return;
        switch (e.Key)
        {
            case Key.Left:
            case Key.Right:
            case Key.Home:
            case Key.End:
                e.Handled = true;
                break;
            case Key.Up:
            case Key.Down:
                if (Wieloliniowe(sender)) e.Handled = true;
                break;
        }
    }

    /// <summary>
    /// Enter przy otwartej liście podpowiedzi AutoCompleteBox z zaznaczoną pozycją wybiera ją.
    /// (Na poziomie siatki, bo AutoCompleteBox sam przechwytuje Enter, zanim dotrze do pola tekstowego.)
    /// </summary>
    private static void PrzedKlawiszem(object? sender, KeyEventArgs e)
    {
        if (e.Key != Key.Enter || e.KeyModifiers != KeyModifiers.None) return;
        if ((e.Source as Visual)?.FindAncestorOfType<AutoCompleteBox>(includeSelf: true) is not { } podpowiedzi) return;
        if (!podpowiedzi.IsDropDownOpen || podpowiedzi.SelectedItem is not string wybrana) return;

        podpowiedzi.Text = wybrana;
        podpowiedzi.IsDropDownOpen = false;
        if (PoleTekstowe(podpowiedzi) is { } tekst) tekst.CaretIndex = wybrana.Length;
        e.Handled = true;
    }
}
