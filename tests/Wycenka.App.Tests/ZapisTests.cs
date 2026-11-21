using Wycenka.App.Services;
using Wycenka.App.ViewModels;
using Wycenka.Core.Pliki;

namespace Wycenka.App.Tests;

/// <summary>Okna dialogowe na niby – zapamiętują, co pokazano, i zwracają ustawione odpowiedzi.</summary>
internal sealed class SztuczneDialogi : IDialogi
{
    public List<string> Komunikaty { get; } = [];
    public string? PlikDoZapisu { get; set; }
    public int ZapytaniaOPlikDoZapisu { get; private set; }

    public Task<string?> WybierzPlikDoOtwarciaAsync() => Task.FromResult<string?>(null);

    public Task<string?> WybierzPlikDoZapisuAsync(string sugerowanaNazwa, string rozszerzenie, string opisTypu)
    {
        ZapytaniaOPlikDoZapisu++;
        var p = PlikDoZapisu;
        PlikDoZapisu = null;          // druga prośba = użytkownik anulował
        return Task.FromResult(p);
    }

    public Task<OdpowiedzZapisu> ZapytajOZapisAsync(string nazwaKosztorysu) => Task.FromResult(OdpowiedzZapisu.Anuluj);
    public Task<bool> PotwierdzAsync(string tytul, string tresc) => Task.FromResult(true);

    public Task KomunikatAsync(string tytul, string tresc)
    {
        Komunikaty.Add($"{tytul}: {tresc}");
        return Task.CompletedTask;
    }

    public Task OtworzZewnetrznieAsync(string sciezka) => Task.CompletedTask;
}

public class ZapisTests
{
    [Fact]
    public async Task Zapis_do_pliku_tylko_do_odczytu_proponuje_zapisz_jako()
    {
        var dir = Directory.CreateTempSubdirectory("wycenka-zapis");
        var oryginal = Path.Combine(dir.FullName, "Remont.wycenka");
        var kopia = Path.Combine(dir.FullName, "Remont (kopia).wycenka");
        try
        {
            File.Copy(Sciezki.Przyklad(), oryginal);
            File.SetAttributes(oryginal, FileAttributes.ReadOnly);

            var dialogi = new SztuczneDialogi { PlikDoZapisu = kopia };
            var vm = new MainViewModel(UstawieniaAplikacji.Wczytaj(Path.Combine(dir.FullName, "u.json"))) { Dialogi = dialogi };
            await vm.OtworzPlikAsync(oryginal);
            vm.Kosztorys.Nazwa = "Po zmianach";

            await vm.ZapiszCommand.ExecuteAsync(null);

            var komunikat = Assert.Single(dialogi.Komunikaty);
            Assert.Contains("tylko do odczytu", komunikat);
            Assert.Equal(1, dialogi.ZapytaniaOPlikDoZapisu);
            Assert.Equal(kopia, vm.SciezkaPliku);
            Assert.False(vm.Zmieniony);
            Assert.Equal("Po zmianach", PlikKosztorysu.Wczytaj(kopia).Nazwa);
            Assert.NotEqual("Po zmianach", PlikKosztorysu.Wczytaj(oryginal).Nazwa);
            Assert.Empty(Directory.GetFiles(dir.FullName, "*.tmp"));
        }
        finally
        {
            if (File.Exists(oryginal)) File.SetAttributes(oryginal, FileAttributes.Normal);
            dir.Delete(recursive: true);
        }
    }

    [Fact]
    public void Wersja_jest_bez_skrotu_commita()
    {
        Assert.DoesNotContain("+", MainViewModel.Wersja);
        Assert.StartsWith("0.", MainViewModel.Wersja);
        Assert.Equal(MainViewModel.Wersja.Contains('-'), MainViewModel.WersjaTestowa);
    }
}
