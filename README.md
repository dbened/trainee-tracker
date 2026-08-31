# Trainee Tracker

Eine Webanwendung zur Verwaltung von Trainees, Lerninhalten, Lernfortschritten und der wöchentlichen Planung.

Das Projekt wurde im Rahmen eines Softwareprojekts im Bachelorstudiengang Informatik an der Universität Augsburg als Teamprojekt entwickelt.

## Funktionen

- Verwaltung von Trainees
- Verwaltung von Lektionen und Lehrplänen
- Erfassung des Lernfortschritts
- Wochenplanung und Aufgabenplanung
- Verwaltung von Pausen
- Feedback und Anfragen
- Statistiken
- Unit- und End-to-End-Tests
- SQLite-Datenbank
- Lokale Demo-Daten für die Portfolio-Version

## Verwendete Technologien

- **C#**
- **.NET 10**
- **ASP.NET Core MVC**
- **Blazor**
- **Entity Framework Core**
- **SQLite**
- **HTML / CSS / JavaScript**
- **xUnit**
- **Selenium**

## Projektstruktur

```text
Softwareproject/
├── Controllers/       # MVC-Controller
├── Data/              # Datenbank-Kontext und Repositories
├── Models/            # Datenmodelle und ViewModels
├── Services/          # Anwendungs- und Geschäftslogik
├── Views/             # Razor Views
├── wwwroot/           # CSS und JavaScript
├── Program.cs
└── MyApi.csproj

SoftwareProject.UnitTests/
└── Unit-Tests

Softwareproject.E2ETests/
└── End-to-End-Tests
```

## Mein Beitrag

Das Projekt wurde als Teamprojekt entwickelt. Mein persönlicher Schwerpunkt lag auf der **WeekPlan-Funktionalität**, also der Planung und Darstellung des Wochenplans.
Die Dateien im Data- und Model-Ordner wurden gemeinsam erstellt.

### Backend

- `Controllers/WeekPlanController.cs`
- `Services/IWeekPlanService.cs`
- `Services/WeekPlanService.cs`

### Frontend

- der vollständige Ordner `Views/WeekPlan/`

### Tests

- `WeekPlanServiceTests.cs`
- `WeekPlanE2ETests.cs`

Mein Aufgabenbereich umfasste damit sowohl die Backend- und Frontend-Entwicklung als auch die Erstellung automatisierter Unit- und End-to-End-Tests.

## Anwendung starten

### Voraussetzungen

- .NET 10 SDK
- Optional: Docker

### Lokale Ausführung

Repository klonen bzw. herunterladen und zum Webprojekt wechseln:

```bash
cd Softwareproject
```

Abhängigkeiten wiederherstellen:

```bash
dotnet restore
```

Projekt kompilieren:

```bash
dotnet build
```

Anwendung starten:

```bash
dotnet run
```

Die lokale URL wird anschließend im Terminal angezeigt.
Die Anmeldedaten beim Login finden sich in Data/Initializer.cs.

## Tests ausführen

Vom Root-Verzeichnis des Projekts:

```bash
dotnet test
```

Damit werden die vorhandenen Unit- und End-to-End-Tests ausgeführt.

> Für die End-to-End-Tests kann es erforderlich sein, die Anwendung lokal zu starten und eine kompatible Chrome-/ChromeDriver-Installation bereitzustellen.

## Docker

Das Projekt enthält ebenfalls ein `Dockerfile` und eine `docker-compose.yml`.

Die Anwendung kann mit Docker Compose gebaut und gestartet werden:

```bash
docker compose up --build
```

## Portfolio-Version

Bei diesem Repository handelt es sich um eine bereinigte Version des universitären Teamprojekts.

Projektbezogene Integrationen und Daten, die für die Demonstration der Anwendung nicht erforderlich sind, wurden entfernt bzw. durch lokale Demo-Daten ersetzt. Für die Ausführung werden keine externen Unternehmenssysteme oder Zugangsdaten benötigt.

## Hinweis

Dies ist ein **Teamprojekt und kein allein entwickeltes Projekt**. Die unter **Mein Beitrag** aufgeführten Dateien und Bereiche geben die Teile an, für die ich hauptsächlich verantwortlich war.
