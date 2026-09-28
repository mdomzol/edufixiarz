# EDUFIXiarz

EDUFIXiarz is a Windows workstation preparation tool for EDU-FIX IT.

## Initial build

The first build provides a native Windows desktop interface for:

- changing the workstation hostname,
- joining an Active Directory domain,
- removing selected OEM/bloatware packages,
- installing a predefined application set,
- viewing execution status and logs.

Administrative actions require elevation. Domain credentials are never stored by the application.

## Requirements

- Windows 10/11
- .NET 8 Desktop Runtime / SDK for development
- Administrator privileges for system changes

## Build

Open `EDUFIXiarz.sln` in Visual Studio 2022 or run:

```powershell
dotnet build EDUFIXiarz.sln -c Release
```

The GitHub Actions workflow also builds the Windows application automatically.


### Standardowy zestaw aplikacji

EDUFIXiarz przygotowuje stację z następującym zestawem:

- Adobe Acrobat Reader (64-bit)
- Everything
- Google Chrome
- Mozilla Firefox
- 7-Zip
- Visual Studio Code
- VLC
- Notepad++

### Czyszczenie Office

Opcja **„Wyczyść Microsoft Office / 365”** jest celowo wyłączona domyślnie. Po zaznaczeniu EDUFIXiarz usuwa wykryty pakiet Microsoft 365 Apps przez WinGet oraz pakiet Microsoft 365 z Microsoft Store/AppX, jeśli jest obecny.

Po operacji należy wykonać restart przed wdrożeniem właściwego pakietu Office zakupionego przez jednostkę.

Docelowo funkcję warto rozszerzyć o Office Deployment Tool (ODT), aby obsłużyć również pełne czyszczenie instalacji Click-to-Run i starszych instalacji MSI.
