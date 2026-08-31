using Xunit;

// Alle Testklassen teilen sich eine SQLite-Datei (AdminDatabase.db) und starten jeweils ihre
// eigene Browser-Instanz. Parallel ausgeführt kollidieren die Klassen bei DB-Schreibzugriffen
// (SQLite verträgt keine hohen nebenläufigen Schreiblasten), deshalb läuft die gesamte
// E2E-Suite sequenziell.
[assembly: CollectionBehavior(DisableTestParallelization = true)]
