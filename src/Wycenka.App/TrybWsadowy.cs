using System;
using System.IO;
using Wycenka.Core.Eksport;
using Wycenka.Core.Pliki;

namespace Wycenka.App;

/// <summary>
/// Eksport bez otwierania okna:
/// <c>Wycenka.exe kosztorys.wycenka --pdf wynik.pdf --xlsx wynik.xlsx</c>.
/// Kod wyjścia 0 = sukces, 1 = błąd (opis w pliku obok wyniku: wycenka-blad.txt).
/// </summary>
public static class TrybWsadowy
{
    public static bool Dotyczy(string[] args) =>
        Array.Exists(args, a => a is "--pdf" or "--xlsx");

    public static int Uruchom(string[] args)
    {
        string? wejscie = null, pdf = null, xlsx = null;
        for (var i = 0; i < args.Length; i++)
        {
            switch (args[i])
            {
                case "--pdf" when i + 1 < args.Length: pdf = args[++i]; break;
                case "--xlsx" when i + 1 < args.Length: xlsx = args[++i]; break;
                default: wejscie ??= args[i]; break;
            }
        }

        try
        {
            if (wejscie is null) throw new ArgumentException("Nie podano pliku kosztorysu (.wycenka).");
            var k = PlikKosztorysu.Wczytaj(wejscie);
            if (pdf is not null) EksportPdf.Zapisz(k, pdf);
            if (xlsx is not null) EksportExcel.Zapisz(k, xlsx);
            return 0;
        }
        catch (Exception ex)
        {
            var cel = pdf ?? xlsx ?? wejscie ?? "wycenka";
            var plikBledu = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(cel)) ?? ".", "wycenka-blad.txt");
            try { File.WriteAllText(plikBledu, ex.ToString()); } catch { /* brak dostępu – trudno */ }
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }
}
