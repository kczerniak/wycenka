using System;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Wycenka.App.Services;

public enum Motyw
{
    Systemowy,
    Jasny,
    Ciemny,
    Brazowy,
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

    /// <summary>Ciemny wariant, na który przełącza przycisk na pasku (Ciemny albo Brązowy).</summary>
    public Motyw OstatniCiemny { get; set; } = Motyw.Ciemny;

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
