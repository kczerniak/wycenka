using Wycenka.Core.Model;
using Wycenka.Core.Obliczenia;

namespace Wycenka.Core.Tests;

public class NarzedziaTests
{
    [Theory]
    [InlineData("12", 12)]
    [InlineData("12,5", 12.5)]
    [InlineData("12.5", 12.5)]
    [InlineData("4*2,5+3", 13)]
    [InlineData("4 x 2,5 - 3", 7)]
    [InlineData("(2+3)*1.5", 7.5)]
    [InlineData("10/4", 2.5)]
    [InlineData("-3+1", -2)]
    [InlineData("1 250,5", 1250.5)]
    public void Wyrazenie_liczy_obmiar(string tekst, double oczekiwane)
    {
        Assert.True(Wyrazenie.SprobujObliczyc(tekst, out var w));
        Assert.Equal((decimal)oczekiwane, w);
    }

    [Theory]
    [InlineData("")]
    [InlineData("abc")]
    [InlineData("2+")]
    [InlineData("(2+3")]
    [InlineData("1/0")]
    public void Wyrazenie_odrzuca_bledy(string tekst)
    {
        Assert.False(Wyrazenie.SprobujObliczyc(tekst, out _));
    }

    [Fact]
    public void Obserwator_zglasza_zmiany_ale_nie_wyliczenia()
    {
        var k = KalkulatorTests.Przyklad();
        using var obs = new ObserwatorZmian(k);
        var zmiany = new List<string?>();
        obs.Zmiana += zmiany.Add;

        Kalkulator.Przelicz(k);
        Assert.Empty(zmiany);

        k.Dzialy[1].Pozycje[0].Ilosc = 5;
        Assert.Equal([nameof(Pozycja.Ilosc)], zmiany);

        var nowa = new Pozycja();
        k.Dzialy[0].Pozycje.Add(nowa);
        nowa.Opis = "x";
        Assert.Equal([nameof(Pozycja.Ilosc), null, nameof(Pozycja.Opis)], zmiany);

        var usunieta = k.Dzialy[1];
        k.Dzialy.Remove(usunieta);
        zmiany.Clear();
        usunieta.Nazwa = "nie powinno się liczyć";
        Assert.Empty(zmiany);
    }
}
