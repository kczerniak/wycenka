using System.Globalization;

namespace Wycenka.Core.Eksport;

internal static class Formaty
{
    public static readonly CultureInfo Pl = CultureInfo.GetCultureInfo("pl-PL");

    public static string Zl(decimal v) => v.ToString("N2", Pl);
    public static string Ilosc(decimal v) => v.ToString("0.###", Pl);
    public static string Proc(decimal v) => v.ToString("0.##", Pl) + "%";
}
