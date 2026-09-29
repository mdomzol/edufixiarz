# EDUFIXiarz — format raportu stacji

## Cel

Raport EDUFIXiarza jest artefaktem przekazywanym później do EDUFIX Reader.

- CSV jest formatem źródłowym do odczytu maszynowego.
- HTML jest prezentacją dla człowieka.
- Reader nie powinien traktować HTML jako źródła danych.
- Jedno uruchomienie eksportu tworzy jeden identyfikator ReportId.

## Wersjonowanie

Pole ReportFormatVersion określa wersję kontraktu danych, a ApplicationVersion określa wersję programu EDUFIXiarz.

Reader powinien:
1. odczytać ReportFormatVersion,
2. obsłużyć znane wersje,
3. dla nieznanej wersji wyświetlić komunikat zamiast zgadywać znaczenie pól.

Aktualna wersja formatu: 2.

## CSV

CSV używa separatora ; i nagłówka:

ReportFormatVersion;ApplicationVersion;ReportId;GeneratedAt;Hostname;Sekcja;Pole;Wartość;Status;Szczegóły

Każda wartość jest poprawnie cytowana zgodnie z CSV. Plik jest zapisywany jako UTF-8 z BOM, aby poprawnie otwierał się również w typowych narzędziach Windows.

### Sekcje

- META — identyfikacja producenta, modelu i numeru seryjnego.
- SYSTEM — system operacyjny i parametry środowiska.
- CPU — procesor, rdzenie i wątki.
- RAM — pamięć i zajętość slotów.
- PŁYTA — płyta główna i BIOS.
- GPU — karty graficzne.
- DYSK_FIZYCZNY — dyski fizyczne.
- DYSK_LOGICZNY — woluminy logiczne.
- SIEĆ — aktywne adaptery sieciowe.
- BEZPIECZEŃSTWO — TPM, Secure Boot, BitLocker i antywirus.
- PRZYGOTOWANIE — zaplanowane i wykonane etapy przygotowania stacji.
- AUDYT — wynik kontroli stacji.

## Zasada interpretacji

Reader powinien budować wewnętrzny model raportu na podstawie:

Raport → sekcja → pole → wartość/status/szczegóły

Nie należy polegać na numerze wiersza ani kolejności rekordów. Nowe pola mogą być dodawane w kolejnych wersjach bez zmiany znaczenia istniejących pól.

## Dane wrażliwe

Raport nie zapisuje hasła użytego do dołączenia stacji do domeny. Domain może być zapisane jako informacja konfiguracyjna, natomiast dane uwierzytelniające nie są częścią raportu.

## HTML

HTML jest generowany z tego samego modelu StationReport co CSV. Jego układ, CSS i kolejność sekcji mogą zmieniać się niezależnie od kontraktu CSV.

## Kierunek dla EDUFIX Reader

Reader powinien docelowo umożliwiać:
- wskazanie katalogu z raportami,
- wykrycie plików CSV,
- walidację ReportFormatVersion,
- grupowanie raportów po ReportId i stacji,
- porównanie wielu stacji,
- filtrowanie po sekcjach i statusach,
- generowanie zbiorczego raportu dla technika lub placówki.

Nie należy uzależniać powyższych funkcji od struktury HTML.
