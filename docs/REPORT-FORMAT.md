# Format raportów EDUFIXiarz

## Cel

EDUFIXiarz zbiera stan stacji i wykonuje przygotowanie stanowiska. Dane są zapisywane w formatach przeznaczonych do dalszego odczytu przez przyszły EDUFIX Reader.

Źródłem danych dla Readera są CSV snapshotów. HTML jest prezentacją dla technika i nie powinien być parsowany jako źródło danych.

## Snapshot stacji

Każdy odczyt jest niezależnym artefaktem:

- CSV — dane maszynowe,
- HTML — czytelny raport,
- SnapshotId — identyfikator konkretnego odczytu,
- SessionId — identyfikator procesu/serii odczytów,
- StationId — identyfikator urządzenia,
- Stage — ODCZYT, PRZED-PRZYGOTOWANIEM albo PO-PRZYGOTOWANIU,
- FormatVersion — wersja kontraktu danych,
- ApplicationVersion — wersja EDUFIXiarz.

Dwa snapshoty wykonane w ramach jednego przygotowania mają ten sam SessionId, ale różne SnapshotId.

Snapshot może zostać wykonany bez żadnego wdrożenia. Dzięki temu technik może wyłącznie zebrać dane i przekazać CSV/HTML dalej.

## Dane stacji

Snapshot zawiera:

- identyfikację urządzenia: StationId, UUID, hostname, użytkownik, domena/workgroup, producent, model, numer seryjny,
- system: Windows, wersję/build, architekturę, uptime, aktywację i stan usługi Windows Update,
- CPU i RAM,
- płytę główną i BIOS,
- GPU,
- dyski fizyczne i logiczne,
- adaptery sieciowe,
- TPM, Secure Boot i BitLocker,
- zarejestrowany antywirus,
- listę zainstalowanych aplikacji.

## Full Station Report

Pełny raport procesu jest kontenerem:

Raport → Snapshot BEFORE → Preparation → Snapshot AFTER → Audit

StationReport nie jest źródłem pojedynczego stanu stacji. Snapshoty pozostają niezależne i mogą być używane bez raportu procesu.

## Wersjonowanie

SnapshotFormatVersion opisuje kontrakt CSV snapshotu. Zmiana struktury danych wymagająca zmian w Readerze powoduje zwiększenie wersji.

ReportFormatVersion opisuje kontrakt pełnego raportu procesu.

ApplicationVersion opisuje wersję programu, który wygenerował dane. Nie należy używać jej do wyboru parsera.

## CSV

Separator: ;

Każda wartość jest poprawnie cytowana. CSV jest zapisywany z UTF-8 BOM, aby poprawnie otwierał się w Excelu i innych narzędziach Windows.

Reader powinien modelować dane jako:

Snapshot → sekcja → pole → wartość

Nie powinien zależeć od kolejności rekordów.

## Magazyn lokalny

Automatyczne snapshoty z przygotowania są zapisywane w:

%ProgramData%\EDU-FIX\EDUFIXiarz\Snapshots\<StationId>\

W katalogu znajdują się pary CSV + HTML.

## Przygotowanie i audyt

Preparation opisuje wykonane operacje i ich statusy.

Audit opisuje stan kontrolny po operacjach.

Reader może na tej podstawie porównać BEFORE/AFTER, ale nie powinien zakładać, że każde przygotowanie posiada oba snapshoty.

## Przyszły EDUFIX Reader

Reader powinien:

1. pozwalać wskazać katalog,
2. wyszukiwać pliki CSV snapshotów,
3. odrzucać nieobsługiwane wersje formatu,
4. grupować snapshoty po StationId i SessionId,
5. rozpoznawać etap po Stage,
6. porównywać BEFORE/AFTER,
7. umożliwiać filtrowanie wielu stacji,
8. generować zbiorcze podsumowanie.

Reader nie powinien wymagać uruchomionego EDUFIXiarz ani dostępu do jego lokalnego magazynu.
