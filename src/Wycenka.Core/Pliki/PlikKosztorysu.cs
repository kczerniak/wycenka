using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using Wycenka.Core.Model;

namespace Wycenka.Core.Pliki;

/// <summary>Zapis i odczyt kosztorysu w pliku .wycenka (JSON, UTF-8).</summary>
public static class PlikKosztorysu
{
    public const string Rozszerzenie = "wycenka";

    private static readonly JsonSerializerOptions Opcje = new()
    {
        WriteIndented = true,
        Encoder = JavaScriptEncoder.Create(UnicodeRanges.All),
    };

    public static void Zapisz(Kosztorys k, string sciezka)
    {
        if (File.Exists(sciezka) && new FileInfo(sciezka).IsReadOnly)
            throw new PlikTylkoDoOdczytuException(sciezka);

        k.WersjaPliku = Kosztorys.AktualnaWersjaPliku;
        var tmp = sciezka + ".tmp";
        try
        {
            using (var fs = File.Create(tmp))
            {
                JsonSerializer.Serialize(fs, k, Opcje);
            }
            File.Move(tmp, sciezka, overwrite: true);
        }
        catch
        {
            // Nie zostawiaj po sobie pliku tymczasowego.
            try { File.Delete(tmp); } catch { /* trudno */ }
            throw;
        }
    }

    public static Kosztorys Wczytaj(string sciezka)
    {
        using var fs = File.OpenRead(sciezka);
        var k = JsonSerializer.Deserialize<Kosztorys>(fs, Opcje)
                ?? throw new InvalidDataException("Plik jest pusty.");
        if (k.WersjaPliku > Kosztorys.AktualnaWersjaPliku)
            throw new InvalidDataException("Plik pochodzi z nowszej wersji programu Wycenka.");
        return k;
    }
}

/// <summary>Plik docelowy ma atrybut „tylko do odczytu” – trzeba zapisać pod inną nazwą.</summary>
public sealed class PlikTylkoDoOdczytuException(string sciezka)
    : IOException($"Plik „{Path.GetFileName(sciezka)}” jest tylko do odczytu.")
{
    public string Sciezka { get; } = sciezka;
}
