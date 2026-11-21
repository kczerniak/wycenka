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
        k.WersjaPliku = Kosztorys.AktualnaWersjaPliku;
        var tmp = sciezka + ".tmp";
        using (var fs = File.Create(tmp))
        {
            JsonSerializer.Serialize(fs, k, Opcje);
        }
        File.Move(tmp, sciezka, overwrite: true);
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
