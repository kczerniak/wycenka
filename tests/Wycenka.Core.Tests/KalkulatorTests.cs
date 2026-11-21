using Wycenka.Core.Eksport;
using Wycenka.Core.Model;
using Wycenka.Core.Obliczenia;
using Wycenka.Core.Pliki;

namespace Wycenka.Core.Tests;

public class KalkulatorTests
{
    internal static Kosztorys Przyklad()
    {
        var k = new Kosztorys
        {
            Nazwa = "Remont łazienki",
            Inwestor = "Jan Kowalski",
            StawkaRg = 40m,
            ProcentKp = 60m,
            ProcentKz = 5m,
            ProcentZ = 10m,
            ProcentVat = 8m,
        };
        var d1 = new Dzial { Nazwa = "Rozbiórki" };
        d1.Pozycje.Add(new Pozycja { Opis = "Skucie płytek", Jm = "m2", Ilosc = 12.5m, NakladRg = 0.8m, CenaS = 2m });
        var d2 = new Dzial { Nazwa = "Okładziny" };
        d2.Pozycje.Add(new Pozycja { Opis = "Glazura ścienna", Jm = "m2", Ilosc = 20m, NakladRg = 1.2m, CenaM = 85.5m });
        d2.Pozycje.Add(new Pozycja { Opis = "Fuga", Jm = "kg", Ilosc = 3m, CenaM = 19.99m });
        k.Dzialy.Add(d1);
        k.Dzialy.Add(d2);
        return k;
    }

    [Fact]
    public void Przelicz_liczy_pozycje_dzialy_i_narzuty()
    {
        var k = Przyklad();
        var s = Kalkulator.Przelicz(k);

        // R = 12,5*0,8*40 + 20*1,2*40 = 400 + 960
        Assert.Equal(1360m, s.R);
        // M = 20*85,5 + 3*19,99 = 1710 + 59,97
        Assert.Equal(1769.97m, s.M);
        // S = 12,5*2
        Assert.Equal(25m, s.S);
        Assert.Equal(831m, s.Kp);        // 60% z 1385
        Assert.Equal(88.50m, s.Kz);      // 5% z 1769,97 = 88,4985
        Assert.Equal(221.60m, s.Z);      // 10% z 2216
        Assert.Equal(4296.07m, s.Netto);
        Assert.Equal(343.69m, s.Vat);    // 8% z 4296,07 = 343,6856
        Assert.Equal(4639.76m, s.Brutto);

        Assert.Equal(425m, k.Dzialy[0].Wartosc);
        Assert.Equal([1, 2, 3], k.Dzialy.SelectMany(d => d.Pozycje).Select(p => p.Lp));
        Assert.Equal(2, k.Dzialy[1].Numer);
        Assert.Equal(133.5m, k.Dzialy[1].Pozycje[0].CenaJednostkowa);
    }

    [Fact]
    public void Pusty_kosztorys_daje_zera()
    {
        Assert.Equal(Podsumowanie.Puste, Kalkulator.Przelicz(new Kosztorys()));
    }

    [Theory]
    [InlineData(0, "zero złotych 00/100")]
    [InlineData(1, "jeden złoty 00/100")]
    [InlineData(2.5, "dwa złote 50/100")]
    [InlineData(5, "pięć złotych 00/100")]
    [InlineData(12, "dwanaście złotych 00/100")]
    [InlineData(22, "dwadzieścia dwa złote 00/100")]
    [InlineData(115, "sto piętnaście złotych 00/100")]
    [InlineData(1000, "tysiąc złotych 00/100")]
    [InlineData(2014, "dwa tysiące czternaście złotych 00/100")]
    [InlineData(4639.76, "cztery tysiące sześćset trzydzieści dziewięć złotych 76/100")]
    [InlineData(12345.67, "dwanaście tysięcy trzysta czterdzieści pięć złotych 67/100")]
    [InlineData(101000, "sto jeden tysięcy złotych 00/100")]
    [InlineData(1_000_000, "milion złotych 00/100")]
    [InlineData(2_222_222.22, "dwa miliony dwieście dwadzieścia dwa tysiące dwieście dwadzieścia dwa złote 22/100")]
    public void Kwota_slownie(double kwota, string oczekiwane)
    {
        Assert.Equal(oczekiwane, KwotaSlownie.Kwota((decimal)kwota));
    }

    [Fact]
    public void Zapis_i_odczyt_pliku_zachowuje_dane()
    {
        var k = Przyklad();
        var sciezka = Path.Combine(Path.GetTempPath(), $"wycenka-test-{Guid.NewGuid()}.wycenka");
        try
        {
            PlikKosztorysu.Zapisz(k, sciezka);
            Assert.Contains("Remont łazienki", File.ReadAllText(sciezka));

            var w = PlikKosztorysu.Wczytaj(sciezka);
            Assert.Equal(k.Nazwa, w.Nazwa);
            Assert.Equal(k.ProcentVat, w.ProcentVat);
            Assert.Equal(2, w.Dzialy.Count);
            Assert.Equal(19.99m, w.Dzialy[1].Pozycje[1].CenaM);
            Assert.Equal(Kalkulator.Przelicz(k), Kalkulator.Przelicz(w));
        }
        finally
        {
            File.Delete(sciezka);
        }
    }

    [Fact]
    public void Zapis_do_pliku_tylko_do_odczytu_rzuca_wyjatek_i_nie_zostawia_tmp()
    {
        var dir = Directory.CreateTempSubdirectory("wycenka-ro");
        var sciezka = Path.Combine(dir.FullName, "k.wycenka");
        try
        {
            PlikKosztorysu.Zapisz(Przyklad(), sciezka);
            var przed = File.ReadAllText(sciezka);
            File.SetAttributes(sciezka, FileAttributes.ReadOnly);

            var k = Przyklad();
            k.Nazwa = "Zmieniona nazwa";
            var ex = Assert.Throws<PlikTylkoDoOdczytuException>(() => PlikKosztorysu.Zapisz(k, sciezka));
            Assert.Equal(sciezka, ex.Sciezka);
            Assert.Contains("tylko do odczytu", ex.Message);
            Assert.Equal(przed, File.ReadAllText(sciezka));
            Assert.False(File.Exists(sciezka + ".tmp"));
        }
        finally
        {
            File.SetAttributes(sciezka, FileAttributes.Normal);
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Nieudany_zapis_sprzata_plik_tymczasowy()
    {
        var dir = Directory.CreateTempSubdirectory("wycenka-tmp");
        try
        {
            // Ścieżka docelowa jest katalogiem – przeniesienie pliku tymczasowego musi się nie udać
            // (Windows zgłasza wtedy UnauthorizedAccessException, macOS/Linux IOException).
            var sciezka = Path.Combine(dir.FullName, "katalog.wycenka");
            Directory.CreateDirectory(sciezka);
            Assert.ThrowsAny<Exception>(() => PlikKosztorysu.Zapisz(Przyklad(), sciezka));
            Assert.False(File.Exists(sciezka + ".tmp"));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    private static string TekstExcela(Kosztorys k)
    {
        var dir = Directory.CreateTempSubdirectory("wycenka-xlsx");
        try
        {
            var xlsx = Path.Combine(dir.FullName, "k.xlsx");
            EksportExcel.Zapisz(k, xlsx);
            using var wb = new ClosedXML.Excel.XLWorkbook(xlsx);
            return string.Join("\n", wb.Worksheet(1).CellsUsed().Select(c => c.GetFormattedString()));
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Eksport_pomija_zerowe_narzuty_i_stawke()
    {
        var k = Przyklad();
        var pelny = TekstExcela(k);
        Assert.Contains("Koszty zakupu", pelny);
        Assert.Contains("VAT 8%", pelny);
        Assert.Contains("Wartość brutto", pelny);
        Assert.Contains("Stawka r-g", pelny);

        k.ProcentKz = 0;
        k.ProcentVat = 0;
        k.StawkaRg = 0;
        var okrojony = TekstExcela(k);
        Assert.DoesNotContain("Koszty zakupu", okrojony);
        Assert.DoesNotContain("VAT", okrojony);
        Assert.DoesNotContain("Wartość brutto", okrojony);
        Assert.DoesNotContain("Stawka r-g", okrojony);
        Assert.Contains("Koszty pośrednie", okrojony);
        Assert.Contains("Zysk", okrojony);
        Assert.Contains("Wartość netto", okrojony);
    }

    [Fact]
    public void Eksporty_tworza_pliki()
    {
        var k = Przyklad();
        var dir = Directory.CreateTempSubdirectory("wycenka-eksport");
        try
        {
            var pdf = Path.Combine(dir.FullName, "k.pdf");
            var xlsx = Path.Combine(dir.FullName, "k.xlsx");
            EksportPdf.Zapisz(k, pdf);
            EksportExcel.Zapisz(k, xlsx);
            Assert.True(new FileInfo(pdf).Length > 1000);
            Assert.True(new FileInfo(xlsx).Length > 1000);
        }
        finally
        {
            dir.Delete(recursive: true);
        }
    }
}
