# Wycenka

Program do sporządzania kosztorysów budowlanych na Windows. Jeden plik `Wycenka.exe` – bez instalacji, bez .NET na komputerze użytkownika.

## Co potrafi

- **Kosztorys w działach i pozycjach** – podstawa (np. KNR), opis, j.m., ilość, nakład robocizny (r-g/j.m.), ceny materiałów i sprzętu za j.m.
- **Narzuty** – stawka roboczogodziny, koszty pośrednie (Kp od R+S), koszty zakupu (Kz od M), zysk (Z od R+S+Kp), VAT.
- **Przeliczanie na żywo** – wartości pozycji, działów, zestawienie kosztów i kwota słownie.
- **Kalkulator w komórkach** – w polach liczbowych można wpisać obmiar, np. `4*2,5+3`. Działa przecinek i kropka.
- **Wygodna edycja opisów** – długi opis widać w całości, Enter w opisie robi nową linię (zatwierdzenie: Tab lub kliknięcie obok), strzałki nie wyrzucają z edytowanej komórki.
- **Podpowiedzi** – w polach Podstawa, Opis i j.m. po wpisaniu pierwszych liter pojawiają się wcześniej użyte wartości (także z poprzednich kosztorysów) i typowe jednostki.
- **Eksport do PDF i Excela (.xlsx)** – zestawienie kosztów bez wierszy o wartości 0,00 zł; bez VAT kończy się „Wartością całościową”.
- **Zapis do pliku `.wycenka`** (JSON), ostrzeżenie o niezapisanych zmianach.
- **Motywy: jasny, ciemny i brązowy** – domyślnie jak w Windowsie; wybór z listy pod przyciskiem „Motyw” na pasku narzędzi lub w menu Widok → Motyw, Ctrl+T szybko przełącza jasny ↔ ciemny. Wybór jest zapamiętywany.

Przykładowy kosztorys: [`przyklady/Remont łazienki.wycenka`](przyklady/).

### Skróty klawiszowe

| Skrót | Akcja |
|---|---|
| Ctrl+N | Nowy kosztorys |
| Ctrl+O | Otwórz |
| Ctrl+S / Ctrl+Shift+S | Zapisz / Zapisz jako |
| Ctrl+P | Eksport do PDF |
| Ctrl+E | Eksport do Excela |
| Ctrl+T | Przełącz jasny/ciemny motyw |

### Wiersz poleceń

```
Wycenka.exe kosztorys.wycenka --pdf wynik.pdf --xlsx wynik.xlsx
```

Eksportuje bez otwierania okna (kod wyjścia 0 = OK, 1 = błąd; szczegóły w `wycenka-blad.txt`).

## Metoda liczenia

| Składnik | Wzór |
|---|---|
| R (robocizna) | ilość × nakład r-g × stawka r-g |
| M (materiały) | ilość × cena M |
| S (sprzęt) | ilość × cena S |
| Kp | %Kp × (R + S) |
| Kz | %Kz × M |
| Z | %Z × (R + S + Kp) |
| Netto | R + M + S + Kp + Kz + Z |
| Brutto | Netto + %VAT × Netto |

Kwoty są zaokrąglane do groszy na poziomie pozycji i narzutów.

## Budowanie

Wymagany [.NET SDK 10](https://dotnet.microsoft.com/download). Exe dla Windowsa da się zbudować na dowolnym systemie (także na macOS).

```bash
# testy
dotnet test

# uruchomienie (Windows / macOS / Linux)
dotnet run --project src/Wycenka.App

# jeden plik exe – Windows x64 (zwykłe PC) i ARM64 (np. Parallels na Macu z Apple Silicon)
dotnet publish src/Wycenka.App -c Release -r win-x64   -o artifacts/win-x64
dotnet publish src/Wycenka.App -c Release -r win-arm64 -o artifacts/win-arm64
```

GitHub Actions buduje i testuje projekt na Windowsie przy każdym pushu. Po wypchnięciu taga `v*` (np. `v0.1.0`) tworzy wydanie z gotowymi plikami exe.

## Struktura

```
src/Wycenka.Core   model, obliczenia, zapis plików, eksport PDF/Excel (bez UI)
src/Wycenka.App    aplikacja Avalonia (MVVM, CommunityToolkit.Mvvm)
tests/             testy jednostkowe + testy UI w trybie headless
przyklady/         przykładowe kosztorysy
```

## Licencje zewnętrzne

- [Avalonia](https://avaloniaui.net/) – MIT
- [ClosedXML](https://github.com/ClosedXML/ClosedXML) – MIT
- [QuestPDF](https://www.questpdf.com/license/) – licencja Community (darmowa dla firm z przychodem poniżej 1 mln USD rocznie)
- Czcionka Lato – SIL Open Font License (`src/Wycenka.Core/Fonts/OFL.txt`)
