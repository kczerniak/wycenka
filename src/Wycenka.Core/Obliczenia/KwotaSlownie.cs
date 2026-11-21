using System.Text;

namespace Wycenka.Core.Obliczenia;

/// <summary>Zamiana kwoty na zapis słowny, np. „sto dwadzieścia trzy złote 45/100”.</summary>
public static class KwotaSlownie
{
    private static readonly string[] Jednosci =
        ["", "jeden", "dwa", "trzy", "cztery", "pięć", "sześć", "siedem", "osiem", "dziewięć"];

    private static readonly string[] Nastki =
        ["dziesięć", "jedenaście", "dwanaście", "trzynaście", "czternaście", "piętnaście",
         "szesnaście", "siedemnaście", "osiemnaście", "dziewiętnaście"];

    private static readonly string[] Dziesiatki =
        ["", "", "dwadzieścia", "trzydzieści", "czterdzieści", "pięćdziesiąt",
         "sześćdziesiąt", "siedemdziesiąt", "osiemdziesiąt", "dziewięćdziesiąt"];

    private static readonly string[] Setki =
        ["", "sto", "dwieście", "trzysta", "czterysta", "pięćset",
         "sześćset", "siedemset", "osiemset", "dziewięćset"];

    private static readonly string[][] Rzedy =
    [
        ["", "", ""],
        ["tysiąc", "tysiące", "tysięcy"],
        ["milion", "miliony", "milionów"],
        ["miliard", "miliardy", "miliardów"],
        ["bilion", "biliony", "bilionów"],
    ];

    private static readonly string[] Zlote = ["złoty", "złote", "złotych"];

    /// <summary>Odmiana: 1 → [0], 2–4 (bez 12–14) → [1], reszta → [2].</summary>
    public static string Odmiana(long n, string[] formy)
    {
        if (n == 1) return formy[0];
        var j = n % 10;
        var d = n % 100;
        return j is >= 2 and <= 4 && d is < 12 or > 14 ? formy[1] : formy[2];
    }

    public static string Liczba(long n)
    {
        if (n == 0) return "zero";
        if (n < 0) return "minus " + Liczba(-n);

        var czesci = new List<string>();
        var rzad = 0;
        while (n > 0)
        {
            var trojka = (int)(n % 1000);
            if (trojka > 0)
            {
                if (rzad >= Rzedy.Length) throw new ArgumentOutOfRangeException(nameof(n));
                var slowa = rzad > 0 && trojka == 1
                    ? Rzedy[rzad][0]
                    : Trojka(trojka) + (rzad > 0 ? " " + Odmiana(trojka, Rzedy[rzad]) : "");
                czesci.Insert(0, slowa);
            }
            n /= 1000;
            rzad++;
        }
        return string.Join(" ", czesci);
    }

    private static string Trojka(int n)
    {
        var sb = new StringBuilder();
        void Dodaj(string s)
        {
            if (s.Length == 0) return;
            if (sb.Length > 0) sb.Append(' ');
            sb.Append(s);
        }

        Dodaj(Setki[n / 100]);
        var reszta = n % 100;
        if (reszta is >= 10 and < 20)
        {
            Dodaj(Nastki[reszta - 10]);
        }
        else
        {
            Dodaj(Dziesiatki[reszta / 10]);
            Dodaj(Jednosci[reszta % 10]);
        }
        return sb.ToString();
    }

    public static string Kwota(decimal kwota)
    {
        kwota = Kalkulator.Grosze(kwota);
        var minus = kwota < 0;
        if (minus) kwota = -kwota;

        var zlote = (long)decimal.Truncate(kwota);
        var grosze = (int)((kwota - zlote) * 100);

        var tekst = $"{Liczba(zlote)} {Odmiana(zlote, Zlote)} {grosze:00}/100";
        return minus ? "minus " + tekst : tekst;
    }
}
