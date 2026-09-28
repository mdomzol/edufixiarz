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
