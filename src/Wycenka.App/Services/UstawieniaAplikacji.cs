using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Avalonia;
using Avalonia.Styling;

namespace Wycenka.App.Services;

public enum Motyw
{
    Systemowy,
    Jasny,
    Ciemny,
}

/// <summary>
/// Ustawienia programu zapamiętywane między uruchomieniami
/// (%APPDATA%\Wycenka\ustawienia.json). Błędy odczytu/zapisu są ignorowane.
/// </summary>
public sealed class UstawieniaAplikacji
{
    private static readonly JsonSerializerOptions Opcje = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    public static string DomyslnaSciezka => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Wycenka", "ustawienia.json");

    public Motyw Motyw { get; set; } = Motyw.Systemowy;

    [JsonIgnore]
    public string Sciezka { get; private set; } = DomyslnaSciezka;

    public static UstawieniaAplikacji Wczytaj(string? sciezka = null)
    {
        sciezka ??= DomyslnaSciezka;
        UstawieniaAplikacji u;
        try
        {
            u = File.Exists(sciezka)
                ? JsonSerializer.Deserialize<UstawieniaAplikacji>(File.ReadAllText(sciezka), Opcje) ?? new()
                : new();
        }
        catch
        {
            u = new();
        }
        u.Sciezka = sciezka;
        return u;
    }

    public void Zapisz()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Sciezka)!);
            File.WriteAllText(Sciezka, JsonSerializer.Serialize(this, Opcje));
        }
        catch
        {
            // Brak zapisu ustawień nie może blokować pracy.
        }
    }
}

/// <summary>Przełączanie jasnego/ciemnego motywu całej aplikacji.</summary>
public static class MotywAplikacji
{
    public static void Zastosuj(Motyw motyw)
    {
        if (Application.Current is not { } app) return;
        app.RequestedThemeVariant = motyw switch
        {
            Motyw.Jasny => ThemeVariant.Light,
            Motyw.Ciemny => ThemeVariant.Dark,
            _ => ThemeVariant.Default,
        };
    }

    public static bool CiemnyAktywny => Application.Current?.ActualThemeVariant == ThemeVariant.Dark;
}
