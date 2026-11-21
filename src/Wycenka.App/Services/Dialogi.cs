using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Platform.Storage;
using Wycenka.Core.Pliki;

namespace Wycenka.App.Services;

/// <summary>Implementacja okien dialogowych oparta o okno-właściciela.</summary>
public sealed class Dialogi(Window wlasciciel) : IDialogi
{
    private static readonly FilePickerFileType TypKosztorysu = new("Kosztorys Wycenka")
    {
        Patterns = [$"*.{PlikKosztorysu.Rozszerzenie}"],
    };

    public async Task<string?> WybierzPlikDoOtwarciaAsync()
    {
        var pliki = await wlasciciel.StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Otwórz kosztorys",
            AllowMultiple = false,
            FileTypeFilter = [TypKosztorysu, FilePickerFileTypes.All],
        });
        return pliki.FirstOrDefault()?.TryGetLocalPath();
    }

    public async Task<string?> WybierzPlikDoZapisuAsync(string sugerowanaNazwa, string rozszerzenie, string opisTypu)
    {
        var plik = await wlasciciel.StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Zapisz jako",
            SuggestedFileName = $"{sugerowanaNazwa}.{rozszerzenie}",
            DefaultExtension = rozszerzenie,
            ShowOverwritePrompt = true,
            FileTypeChoices = [new FilePickerFileType(opisTypu) { Patterns = [$"*.{rozszerzenie}"] }],
        });
        var sciezka = plik?.TryGetLocalPath();
        if (sciezka is not null && !sciezka.EndsWith("." + rozszerzenie, StringComparison.OrdinalIgnoreCase))
            sciezka += "." + rozszerzenie;
        return sciezka;
    }

    public async Task<OdpowiedzZapisu> ZapytajOZapisAsync(string nazwaKosztorysu)
    {
        var wynik = await Pokaz("Wycenka",
            $"Kosztorys „{nazwaKosztorysu}” ma niezapisane zmiany. Zapisać je?",
            "Zapisz", "Nie zapisuj", "Anuluj");
        return wynik switch
        {
            0 => OdpowiedzZapisu.Zapisz,
            1 => OdpowiedzZapisu.NieZapisuj,
            _ => OdpowiedzZapisu.Anuluj,
        };
    }

    public async Task<bool> PotwierdzAsync(string tytul, string tresc) =>
        await Pokaz(tytul, tresc, "Tak", "Nie") == 0;

    public Task KomunikatAsync(string tytul, string tresc) => Pokaz(tytul, tresc, "OK");

    public async Task OtworzZewnetrznieAsync(string sciezka)
    {
        try
        {
            await wlasciciel.Launcher.LaunchFileInfoAsync(new FileInfo(sciezka));
        }
        catch
        {
            // Brak programu do otwarcia pliku – nie jest to błąd eksportu.
        }
    }

    /// <summary>Okno z treścią i przyciskami; zwraca indeks klikniętego przycisku albo -1.</summary>
    private Task<int> Pokaz(string tytul, string tresc, params string[] przyciski)
    {
        var okno = new Window
        {
            Title = tytul,
            SizeToContent = SizeToContent.WidthAndHeight,
            CanResize = false,
            ShowInTaskbar = false,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            MinWidth = 380,
            MaxWidth = 560,
        };

        var rzad = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right,
            Spacing = 8,
        };

        for (var i = 0; i < przyciski.Length; i++)
        {
            var nr = i;
            var b = new Button
            {
                Content = przyciski[i],
                MinWidth = 96,
                HorizontalContentAlignment = HorizontalAlignment.Center,
                IsDefault = i == 0,
                IsCancel = i == przyciski.Length - 1,
            };
            if (i == 0) b.Classes.Add("accent");
            b.Click += (_, _) => okno.Close(nr);
            rzad.Children.Add(b);
        }

        okno.Content = new StackPanel
        {
            Margin = new Thickness(24, 20),
            Spacing = 20,
            Children =
            {
                new SelectableTextBlock { Text = tresc, TextWrapping = TextWrapping.Wrap },
                rzad,
            },
        };

        return PokazModalnie(okno);
    }

    private async Task<int> PokazModalnie(Window okno)
    {
        var wynik = await okno.ShowDialog<int?>(wlasciciel);
        return wynik ?? -1;
    }
}
