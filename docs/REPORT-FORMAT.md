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
- TPM i Secure Boot (BitLocker pozostaje dostępny wyłącznie w danych audytu),
- zarejestrowany antywirus,
- listę zainstalowanych aplikacji.

## Full Station Report

Pełny raport procesu jest kontenerem:

Raport → Snapshot BEFORE → Preparation → Snapshot AFTER → Audit

StationReport nie jest źródłem pojedynczego stanu stacji. Snapshoty pozostają niezależne i mogą być używane bez raportu procesu.

## Wersjonowanie

SnapshotFormatVersion: 3

ReportFormatVersion: 4

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


## Aktualny przepływ EDUFIXiarz

1. ODCZYT STACJI — odczyt sprzętu i oprogramowania.
2. Automatyczny snapshot ODCZYT — niezależny CSV + HTML.
3. PLAN PRZYGOTOWANIA — wybrane operacje i aplikacje.
4. SNAPSHOT PRZED-PRZYGOTOWANIEM — zapis automatyczny przed zmianami.
5. WYKONANIE — realizacja przygotowania.
6. ODCZYT KONTROLNY — ponowny odczyt po zmianach.
7. SNAPSHOT PO-PRZYGOTOWANIU — zapis automatyczny.
8. AUDYT KOŃCOWY — automatyczna kontrola po udanym przygotowaniu.
9. RAPORT — zawiera stan przed, wykonanie, stan po, wykryte zmiany i audyt.

Snapshoty pozostają niezależnymi artefaktami. Reader może analizować pojedynczy odczyt bez uruchamiania procesu przygotowania.


## Kontrakt snapshotu dla EDUFIX Reader

Snapshot CSV jest podstawowym, stabilnym formatem wymiany danych między EDUFIXiarzem a przyszłym EDUFIX Readerem.

### Nagłówek CSV

`SnapshotFormatVersion;ApplicationVersion;SnapshotId;SessionId;CapturedAt;Stage;StationId;Hostname;Sekcja;Pole;Wartość`

### Zasady parsowania

- separator pól: `;`,
- każda wartość jest cytowana zgodnie z CSV i może zawierać średniki lub znaki nowej linii,
- kodowanie: UTF-8 z BOM,
- kolejność rekordów nie ma znaczenia,
- rekord identyfikujemy przez `Sekcja + Pole`,
- pola wielowartościowe (np. aplikacje, dyski, GPU, sieć) występują jako wiele rekordów z tym samym `Pole`,
- `SnapshotId` identyfikuje pojedynczy plik/odczyt,
- `SessionId` służy do grupowania odczytów z jednego procesu,
- `StationId` służy do grupowania tej samej stacji niezależnie od sesji,
- `Stage` określa charakter odczytu: `ODCZYT`, `PRZED-PRZYGOTOWANIEM`, `PO-PRZYGOTOWANIU`.

### Zasada kompatybilności

Reader powinien najpierw sprawdzić `SnapshotFormatVersion`. Nie powinien zgadywać znaczenia pól na podstawie wersji aplikacji ani nazwy pliku.

Usunięcie pola z kontraktu powoduje zwiększenie wersji formatu. Dlatego bieżący brak BitLockera w snapshotach jest częścią **formatu 3**. Informacja o BitLockerze pozostaje elementem audytu EDUFIXiarza i nie jest częścią danych snapshotu.

### Dane do tabeli zbiorczej

Reader powinien normalizować snapshot do jednego rekordu stacji/odczytu oraz tabel szczegółowych dla danych wielowartościowych. Dzięki temu można później wygenerować jednocześnie:

1. tabelę zbiorczą wszystkich stanowisk,
2. PDF do przeglądu,
3. raport HTML,
4. porównanie BEFORE/AFTER,
5. filtrowanie po stacji, sesji, etapie i wybranym polu.

HTML snapshotu pozostaje prezentacją. Reader nie powinien go parsować.
