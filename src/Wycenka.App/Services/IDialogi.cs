using System.Threading.Tasks;

namespace Wycenka.App.Services;

public enum OdpowiedzZapisu
{
    Zapisz,
    NieZapisuj,
    Anuluj,
}

public interface IDialogi
{
    Task<string?> WybierzPlikDoOtwarciaAsync();

    Task<string?> WybierzPlikDoZapisuAsync(string sugerowanaNazwa, string rozszerzenie, string opisTypu);

    Task<OdpowiedzZapisu> ZapytajOZapisAsync(string nazwaKosztorysu);

    Task<bool> PotwierdzAsync(string tytul, string tresc);

    Task KomunikatAsync(string tytul, string tresc);

    Task OtworzZewnetrznieAsync(string sciezka);
}
