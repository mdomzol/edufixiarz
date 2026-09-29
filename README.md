# EDUFIXiarz

EDUFIXiarz is a native Windows workstation preparation and inventory tool for EDU-FIX IT. It automates repeatable setup tasks and produces a hardware/security report that can be exported for documentation.

## Funkcje

### Raport stacji
- hostname, producent, model i numer seryjny,
- Windows, architektura, wersja/build i czas pracy,
- CPU, rdzenie/wątki i RAM,
- płyta główna i BIOS,
- GPU,
- dyski fizyczne oraz wolne miejsce na dyskach logicznych,
- aktywne interfejsy sieciowe i adresy MAC,
- TPM, Secure Boot i BitLocker,
- wykryte produkty antywirusowe,
- eksport aktualnego raportu do JSON lub CSV.

Raport jest odświeżany przy starcie oraz ręcznie przyciskiem **ODŚWIEŻ RAPORT**.

### Przygotowanie stacji
- zmiana hostname,
- dołączenie do domeny Active Directory,
- usuwanie wybranych pakietów AppX/OEM,
- czyszczenie wykrytych składników Office / Microsoft 365,
- instalacja aplikacji przez WinGet,
- walidacja konfiguracji przed startem,
- blokada zmian UI podczas wykonywania operacji,
- pasek postępu etapów,
- końcowe potwierdzenie wykonania.

Dołączanie do domeny jest wykonywane jako ostatni etap. Hasło domenowe jest przekazywane tylko na czas operacji i nie jest zapisywane.

### Profile stanowisk
Dostępne są profile:
- **Szkoła** — hostname, czyszczenie AppX i standardowy zestaw aplikacji,
- **Biuro** — jak wyżej + czyszczenie Office / Microsoft 365,
- **Developer** — hostname, czyszczenie AppX i narzędzia developerskie,
- **Pełne przygotowanie** — wszystkie bezpieczne etapy oraz pełny katalog aplikacji.

Profile można również zapisywać i wczytywać jako pliki JSON. Pliki profilu nie zawierają haseł.

### Audyt stacji
Audyt wykonuje kontrolę gotowości stanowiska i bezpieczeństwa:
- Windows Update,
- aktywacja Windows,
- TPM i Secure Boot,
- BitLocker dysku systemowego,
- wolne miejsce na dysku systemowym,
- członkowie lokalnej grupy administratorów,
- obecność kluczowych usług Windows,
- wykrycie zarejestrowanego produktu antywirusowego,
- dostępność WinGet,
- oczekujący restart systemu.

Wyniki są oznaczane jako **OK**, **WARN** lub **ERROR** i mogą zostać wykorzystane do protokołu odbioru.

### Protokół odbioru stacji
Po wykonaniu raportu sprzętowego i audytu można wygenerować protokół odbioru w formacie HTML. Dokument zawiera identyfikację stacji, czas kontroli, podsumowanie wyników, szczegóły każdej kontroli oraz pola na podpis osoby wykonującej i przedstawiciela jednostki.

### Dziennik
Dziennik operacji jest dostępny w aplikacji i dodatkowo zapisywany lokalnie w:

`C:\ProgramData\EDU-FIX\EDUFIXiarz\Logs\YYYY-MM-DD.log`

Błędy zapisu dziennika nie przerywają operacji administracyjnych.

## Wymagania

- Windows 10/11,
- .NET 8 Desktop Runtime do uruchomienia opublikowanej aplikacji,
- .NET 8 SDK do developmentu,
- uprawnienia administratora do wykonywania zmian systemowych,
- WinGet dla instalacji aplikacji i części operacji Office.

## Build

Otwórz `EDUFIXiarz.sln` w Visual Studio 2022 lub uruchom:

```powershell
dotnet build EDUFIXiarz.sln -c Release
```

GitHub Actions wykonuje automatyczny build rozwiązania.

## Bezpieczeństwo

Aplikacja wymaga elevacji administratora przez manifest. Operacje systemowe są wykonywane lokalnie przez PowerShell/CIM lub narzędzia Windows.

EDUFIXiarz nie zapisuje haseł domenowych. Profil JSON przechowuje wyłącznie konfigurację stanowiska i identyfikatory wybranych aplikacji.

Opcja **Wyczyść Microsoft Office / 365** jest domyślnie wyłączona. Po jej użyciu zalecany jest restart przed wdrożeniem właściwego pakietu Office jednostki.

## Standardowy zestaw aplikacji

- Adobe Acrobat Reader (64-bit)
- Everything
- Google Chrome
- Mozilla Firefox
- 7-Zip
- Visual Studio Code
- VLC
- Notepad++
- LibreOffice
- PuTTY
- GIMP

## Wersja

Aktualna wersja: **1.2.1**
