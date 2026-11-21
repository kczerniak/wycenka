using QuestPDF.Drawing;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using Wycenka.Core.Model;
using Wycenka.Core.Obliczenia;

namespace Wycenka.Core.Eksport;

public static class EksportPdf
{
    static EksportPdf()
    {
        // Licencja Community: darmowa dla firm z przychodem < 1 mln USD rocznie.
        QuestPDF.Settings.License = LicenseType.Community;

        // Czcionki Lato (OFL) są wbudowane w bibliotekę, żeby program był jednym plikiem .exe
        // i nie zależał od czcionek zainstalowanych w systemie.
        var asm = typeof(EksportPdf).Assembly;
        foreach (var nazwa in asm.GetManifestResourceNames().Where(n => n.EndsWith(".ttf", StringComparison.OrdinalIgnoreCase)))
        {
            using var s = asm.GetManifestResourceStream(nazwa)!;
            FontManager.RegisterFontFromStream(s);
        }
    }

    private const string Czcionka = "Lato";

    private const string KolorNaglowka = "#DCE6F1";
    private const string KolorDzialu = "#F2F2F2";

    public static void Zapisz(Kosztorys k, string sciezka)
    {
        var sum = Kalkulator.Przelicz(k);
        Utworz(k, sum).GeneratePdf(sciezka);
    }

    public static byte[] Generuj(Kosztorys k)
    {
        var sum = Kalkulator.Przelicz(k);
        return Utworz(k, sum).GeneratePdf();
    }

    private static Document Utworz(Kosztorys k, Podsumowanie sum) => Document.Create(doc =>
    {
        doc.Page(page =>
        {
            page.Size(PageSizes.A4);
            page.Margin(1.5f, Unit.Centimetre);
            page.DefaultTextStyle(t => t.FontFamily(Czcionka).FontSize(9));

            page.Header().PaddingBottom(8).Column(col =>
            {
                col.Item().Text(k.Nazwa).FontSize(16).Bold();
                col.Item().PaddingTop(4).Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(95);
                        c.RelativeColumn();
                    });
                    void Wiersz(string etykieta, string wartosc)
                    {
                        if (string.IsNullOrWhiteSpace(wartosc)) return;
                        t.Cell().Text(etykieta).SemiBold();
                        t.Cell().Text(wartosc);
                    }
                    Wiersz("Obiekt:", k.Obiekt);
                    Wiersz("Adres:", k.Adres);
                    Wiersz("Inwestor:", k.Inwestor);
                    Wiersz("Wykonawca:", k.Wykonawca);
                    Wiersz("Sporządził:", k.Sporzadzil);
                    Wiersz("Data opracowania:", k.DataOpracowania.ToString("dd.MM.yyyy"));
                    Wiersz("Stawka r-g:", Formaty.Zl(k.StawkaRg) + " zł");
                });
            });

            page.Content().Column(col =>
            {
                col.Item().Table(t =>
                {
                    t.ColumnsDefinition(c =>
                    {
                        c.ConstantColumn(26);   // Lp
                        c.ConstantColumn(96);   // Podstawa
                        c.RelativeColumn();     // Opis
                        c.ConstantColumn(32);   // j.m.
                        c.ConstantColumn(48);   // Ilość
                        c.ConstantColumn(60);   // Cena jedn.
                        c.ConstantColumn(68);   // Wartość
                    });

                    t.Header(h =>
                    {
                        foreach (var n in new[] { "Lp.", "Podstawa", "Opis", "j.m.", "Ilość", "Cena jedn.", "Wartość" })
                            h.Cell().Element(KomorkaNaglowka).Text(n).SemiBold();
                    });

                    foreach (var d in k.Dzialy)
                    {
                        t.Cell().Element(KomorkaDzialu).Text($"{d.Numer}.").SemiBold();
                        t.Cell().ColumnSpan(5).Element(KomorkaDzialu).Text(d.Nazwa).SemiBold();
                        t.Cell().Element(KomorkaDzialu).AlignRight().Text(Formaty.Zl(d.Wartosc)).SemiBold();

                        foreach (var p in d.Pozycje)
                        {
                            t.Cell().Element(Komorka).Text(p.Lp.ToString());
                            t.Cell().Element(Komorka).Text(p.Podstawa);
                            t.Cell().Element(Komorka).Text(p.Opis);
                            t.Cell().Element(Komorka).Text(p.Jm);
                            t.Cell().Element(Komorka).AlignRight().Text(Formaty.Ilosc(p.Ilosc));
                            t.Cell().Element(Komorka).AlignRight().Text(Formaty.Zl(p.CenaJednostkowa));
                            t.Cell().Element(Komorka).AlignRight().Text(Formaty.Zl(p.Wartosc));
                        }
                    }
                });

                col.Item().PaddingTop(14).ShowEntire().Column(z =>
                {
                    z.Item().Text("Zestawienie kosztów").FontSize(11).Bold();
                    z.Item().PaddingTop(4).AlignRight().Width(300).Table(t =>
                    {
                        t.ColumnsDefinition(c =>
                        {
                            c.RelativeColumn();
                            c.ConstantColumn(90);
                        });
                        foreach (var (etykieta, wartosc, bold) in EksportExcel.WierszePodsumowania(k, sum))
                        {
                            var l = t.Cell().Element(Komorka).Text(etykieta);
                            var v = t.Cell().Element(Komorka).AlignRight().Text(Formaty.Zl(wartosc) + " zł");
                            if (bold)
                            {
                                l.SemiBold();
                                v.SemiBold();
                            }
                        }
                    });
                    z.Item().PaddingTop(6).AlignRight()
                        .Text("Słownie: " + KwotaSlownie.Kwota(sum.Brutto)).Italic();

                    if (!string.IsNullOrWhiteSpace(k.Uwagi))
                    {
                        z.Item().PaddingTop(12).Text("Uwagi").SemiBold();
                        z.Item().Text(k.Uwagi);
                    }
                });
            });

            page.Footer().AlignCenter().Text(x =>
            {
                x.DefaultTextStyle(s => s.FontSize(8).FontColor(Colors.Grey.Darken1));
                x.Span("Strona ");
                x.CurrentPageNumber();
                x.Span(" z ");
                x.TotalPages();
            });
        });
    }).WithMetadata(new DocumentMetadata { Title = k.Nazwa, Creator = "Wycenka" });

    private static IContainer Komorka(IContainer c) =>
        c.BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).PaddingVertical(3).PaddingHorizontal(3);

    private static IContainer KomorkaNaglowka(IContainer c) =>
        c.Background(KolorNaglowka).Border(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);

    private static IContainer KomorkaDzialu(IContainer c) =>
        c.Background(KolorDzialu).BorderBottom(0.5f).BorderColor(Colors.Grey.Lighten1).Padding(3);
}
