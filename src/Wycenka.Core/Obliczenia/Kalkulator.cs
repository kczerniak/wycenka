using Wycenka.Core.Model;

namespace Wycenka.Core.Obliczenia;

/// <summary>Wynik przeliczenia całego kosztorysu (kwoty w zł, zaokrąglone do groszy).</summary>
public sealed record Podsumowanie(
    decimal R,
    decimal M,
    decimal S,
    decimal Kp,
    decimal Kz,
    decimal Z,
    decimal Netto,
    decimal Vat,
    decimal Brutto)
{
    public decimal Bezposrednie => R + M + S;

    public static Podsumowanie Puste { get; } = new(0, 0, 0, 0, 0, 0, 0, 0, 0);
}

/// <summary>
/// Liczy kosztorys metodą uproszczoną:
/// R = ilość × nakład r-g × stawka, M = ilość × cena M, S = ilość × cena S,
/// Kp = %Kp × (R + S), Kz = %Kz × M, Z = %Z × (R + S + Kp),
/// Netto = R + M + S + Kp + Kz + Z, VAT = %VAT × Netto.
/// </summary>
public static class Kalkulator
{
    public static decimal Grosze(decimal kwota) => Math.Round(kwota, 2, MidpointRounding.AwayFromZero);

    private static decimal Procent(decimal podstawa, decimal procent) => Grosze(podstawa * procent / 100m);

    /// <summary>
    /// Przelicza kosztorys: uzupełnia numerację i wartości w działach i pozycjach,
    /// zwraca podsumowanie z narzutami.
    /// </summary>
    public static Podsumowanie Przelicz(Kosztorys k)
    {
        decimal r = 0, m = 0, s = 0;
        int lp = 0, nrDzialu = 0;

        foreach (var dzial in k.Dzialy)
        {
            decimal dr = 0, dm = 0, ds = 0;
            dzial.Numer = ++nrDzialu;

            foreach (var p in dzial.Pozycje)
            {
                p.Lp = ++lp;
                p.WartoscR = Grosze(p.Ilosc * p.NakladRg * k.StawkaRg);
                p.WartoscM = Grosze(p.Ilosc * p.CenaM);
                p.WartoscS = Grosze(p.Ilosc * p.CenaS);
                p.Wartosc = p.WartoscR + p.WartoscM + p.WartoscS;
                p.CenaJednostkowa = Grosze(p.NakladRg * k.StawkaRg + p.CenaM + p.CenaS);

                dr += p.WartoscR;
                dm += p.WartoscM;
                ds += p.WartoscS;
            }

            dzial.WartoscR = dr;
            dzial.WartoscM = dm;
            dzial.WartoscS = ds;
            dzial.Wartosc = dr + dm + ds;

            r += dr;
            m += dm;
            s += ds;
        }

        var kp = Procent(r + s, k.ProcentKp);
        var kz = Procent(m, k.ProcentKz);
        var z = Procent(r + s + kp, k.ProcentZ);
        var netto = r + m + s + kp + kz + z;
        var vat = Procent(netto, k.ProcentVat);

        return new Podsumowanie(r, m, s, kp, kz, z, netto, vat, netto + vat);
    }
}
