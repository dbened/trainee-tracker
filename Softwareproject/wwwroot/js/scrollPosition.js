// Wird ausgeführt, sobald die Seite vollständig geladen wurde.
// Falls zuvor eine Scrollposition gespeichert wurde,
// wird automatisch wieder zu dieser Position gesprungen.
document.addEventListener("DOMContentLoaded", function () {

    // Gespeicherte Scrollposition aus dem Session Storage lesen.
    const savedPosition =
        sessionStorage.getItem("lessonplan-scroll");

    // Nur scrollen, wenn tatsächlich eine Position gespeichert wurde.
    if (savedPosition !== null) {
        
        // Zur gespeicherten Position zurückspringen.
        // Die X-Position bleibt 0, nur vertikal wird gescrollt.
        window.scrollTo(
            0,
            parseInt(savedPosition)
        );
    }
});

// LEON
// Wird ausgeführt, bevor die Seite verlassen wird.
// Speichert die aktuelle vertikale Scrollposition,
// damit sie beim nächsten Laden wiederhergestellt werden kann.
window.addEventListener("beforeunload", function () {

    sessionStorage.setItem(
        "lessonplan-scroll",
        window.scrollY
    );
});