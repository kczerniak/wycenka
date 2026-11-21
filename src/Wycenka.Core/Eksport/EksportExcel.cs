using ClosedXML.Excel;
using Wycenka.Core.Model;
using Wycenka.Core.Obliczenia;

namespace Wycenka.Core.Eksport;

public static class EksportExcel
{
    private const string FormatZl = "#,##0.00";
    private const string FormatIlosc = "#,##0.###";

    public static void Zapisz(Kosztorys k, string sciezka)
    {
        var sum = Kalkulator.Przelicz(k);

        using var wb = new XLWorkbook();
        ZapiszKosztorys(wb.Worksheets.Add("Kosztorys"), k, sum);
        wb.SaveAs(sciezka);
    }

    private static void ZapiszKosztorys(IXLWorksheet ws, Kosztorys k, Podsumowanie sum)
    {
        var row = 1;
        ws.Cell(row, 1).Value = k.Nazwa;
        ws.Cell(row, 1).Style.Font.SetBold().Font.SetFontSize(14);
        row += 2;

        foreach (var (etykieta, wartosc) in new[]
                 {
                     ("Obiekt", k.Obiekt), ("Adres", k.Adres), ("Inwestor", k.Inwestor),
                     ("Wykonawca", k.Wykonawca), ("Sporządził", k.Sporzadzil),
                     ("Data opracowania", k.DataOpracowania.ToString("dd.MM.yyyy")),
                 })
        {
            if (string.IsNullOrWhiteSpace(wartosc)) continue;
            ws.Cell(row, 1).Value = etykieta;
            ws.Cell(row, 1).Style.Font.SetBold();
            ws.Cell(row, 3).Value = wartosc;
            row++;
        }

        ws.Cell(row, 1).Value = "Stawka r-g";
        ws.Cell(row, 1).Style.Font.SetBold();
        ws.Cell(row, 3).Value = k.StawkaRg;
        ws.Cell(row, 3).Style.NumberFormat.Format = FormatZl + " \"zł/r-g\"";
        row += 2;

        string[] naglowki =
            ["Lp.", "Podstawa", "Opis", "j.m.", "Ilość", "r-g/j.m.", "M zł/j.m.", "S zł/j.m.",
             "Cena jedn.", "R", "M", "S", "Wartość"];
        for (var c = 0; c < naglowki.Length; c++)
            ws.Cell(row, c + 1).Value = naglowki[c];
        var naglowek = ws.Range(row, 1, row, naglowki.Length);
        naglowek.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#DCE6F1"));
        var pierwszyWiersz = row;
        row++;

        foreach (var d in k.Dzialy)
        {
            ws.Cell(row, 1).Value = $"{d.Numer}.";
            ws.Cell(row, 2).Value = d.Nazwa;
            ws.Range(row, 2, row, 9).Merge();
            ws.Cell(row, 10).Value = d.WartoscR;
            ws.Cell(row, 11).Value = d.WartoscM;
            ws.Cell(row, 12).Value = d.WartoscS;
            ws.Cell(row, 13).Value = d.Wartosc;
            var r = ws.Range(row, 1, row, naglowki.Length);
            r.Style.Font.SetBold().Fill.SetBackgroundColor(XLColor.FromHtml("#F2F2F2"));
            row++;

            foreach (var p in d.Pozycje)
            {
                ws.Cell(row, 1).Value = p.Lp;
                ws.Cell(row, 2).Value = p.Podstawa;
                ws.Cell(row, 3).Value = p.Opis;
                ws.Cell(row, 4).Value = p.Jm;
                ws.Cell(row, 5).Value = p.Ilosc;
                ws.Cell(row, 6).Value = p.NakladRg;
                ws.Cell(row, 7).Value = p.CenaM;
                ws.Cell(row, 8).Value = p.CenaS;
                ws.Cell(row, 9).Value = p.CenaJednostkowa;
                ws.Cell(row, 10).Value = p.WartoscR;
                ws.Cell(row, 11).Value = p.WartoscM;
                ws.Cell(row, 12).Value = p.WartoscS;
                ws.Cell(row, 13).Value = p.Wartosc;
                row++;
            }
        }

        var tabela = ws.Range(pierwszyWiersz, 1, row - 1, naglowki.Length);
        tabela.Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin);
        ws.Range(pierwszyWiersz + 1, 5, row - 1, 6).Style.NumberFormat.Format = FormatIlosc;
        ws.Range(pierwszyWiersz + 1, 7, row - 1, 13).Style.NumberFormat.Format = FormatZl;
        ws.Column(3).Style.Alignment.SetWrapText();

        row += 1;
        ws.Cell(row, 2).Value = "Zestawienie kosztów";
        ws.Cell(row, 2).Style.Font.SetBold().Font.SetFontSize(12);
        row++;

        var pierwszaSuma = row;
        foreach (var (etykieta, wartosc, pogrubienie) in WierszePodsumowania(k, sum))
        {
            ws.Cell(row, 2).Value = etykieta;
            ws.Range(row, 2, row, 12).Merge();
            ws.Cell(row, 13).Value = wartosc;
            ws.Cell(row, 13).Style.NumberFormat.Format = FormatZl;
            if (pogrubienie) ws.Range(row, 2, row, 13).Style.Font.SetBold();
            row++;
        }
        ws.Range(pierwszaSuma, 2, row - 1, 13).Style.Border.SetOutsideBorder(XLBorderStyleValues.Thin)
            .Border.SetInsideBorder(XLBorderStyleValues.Thin);

        row++;
        ws.Cell(row, 2).Value = "Słownie: " + KwotaSlownie.Kwota(sum.Brutto);
        ws.Cell(row, 2).Style.Font.SetItalic();

        ws.Columns(1, naglowki.Length).AdjustToContents();
        ws.Column(3).Width = Math.Min(ws.Column(3).Width, 60);
        ws.Column(2).Width = Math.Min(ws.Column(2).Width, 24);
        ws.SheetView.FreezeRows(pierwszyWiersz);
        ws.PageSetup.PageOrientation = XLPageOrientation.Landscape;
        ws.PageSetup.FitToPages(1, 0);
    }

    internal static IEnumerable<(string Etykieta, decimal Wartosc, bool Pogrubienie)> WierszePodsumowania(
        Kosztorys k, Podsumowanie s)
    {
        yield return ("Robocizna (R)", s.R, false);
        yield return ("Materiały (M)", s.M, false);
        yield return ("Sprzęt (S)", s.S, false);
        yield return ("Razem koszty bezpośrednie", s.Bezposrednie, true);
        yield return ($"Koszty pośrednie (Kp) {Formaty.Proc(k.ProcentKp)} od R+S", s.Kp, false);
        yield return ($"Koszty zakupu (Kz) {Formaty.Proc(k.ProcentKz)} od M", s.Kz, false);
        yield return ($"Zysk (Z) {Formaty.Proc(k.ProcentZ)} od R+S+Kp", s.Z, false);
        yield return ("Wartość netto", s.Netto, true);
        yield return ($"VAT {Formaty.Proc(k.ProcentVat)}", s.Vat, false);
        yield return ("Wartość brutto", s.Brutto, true);
    }
}
