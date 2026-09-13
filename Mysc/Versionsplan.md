# Versionsplan – MyriaRPG

Alle drei Projekte (MyriaRPG, MyriaLib, MyriaServer) werden immer gemeinsam versioniert und gleichzeitig auf die gleiche Version gebracht.

---

## v0.2.0 — Abschlussprojekt-Release

Zielzustand: vollständiger Abgabestand für das Abschlussprojekt.

*(Inhalte entsprechen dem aktuellen Entwicklungsstand — hier kein separates Changelog notwendig.)*

---

## v0.2.X — Bugfix-Releases

Nur Fehlerbehebungen, keine neuen Features oder Mechaniken.

- v0.2.1 — TBD
- v0.2.2 — TBD
- ...

---

## v0.3 — Neue Spielmechaniken

Zielzustand: erweiterter Beta-Stand mit neuen Kernmechaniken.

### Geplante Inhalte

- **RuneMage-Klasse** — Runenzeichen-Mechanik aktivieren und vollständig ausbauen (Runen zeichnen, kombinieren, einsetzen)

*(Detaillierte, laufend aktualisierte Planung für v0.3 siehe `v0.3.md` im Repo-Root — dort auch der
aktuelle Stand, u.a. dass RuneMage zurückgestellt wurde.)*

---

## v0.x — Fraktionssystem & reaktive Welt (Versionsnummer noch offen)

Kommt nach v0.3 (eigene Datei `v0.3.md`) und vor v1.0 — welche konkrete 0.x-Nummer (v0.4, v0.5, …)
das wird, ist noch nicht festgelegt. Detailplanung siehe `v0.x.md` im Repo-Root; sobald die
Versionsnummer feststeht, werden diese Überschrift und die Datei entsprechend umbenannt.

Zielzustand: das Fraktionssystem (spielbares Königreich Lura vs. Tyriovisches Imperium, inklusive
Kriegszustand und fraktionsbasiertem PvP) sowie eine darauf aufbauende reaktive Welt sind vollständig
umgesetzt, **bevor** v1.0 beginnt — v1.0 setzt voraus, dass die komplette Story, inklusive des
Lura-Tyriova-Kriegs, bereits spielbar ist.

### Geplante Inhalte

- **Fraktionssystem**: Spieler gehören entweder dem Königreich Lura (verteidigend) oder dem
  Tyriovischen Imperium (angreifend) an; beide befinden sich im Krieg miteinander. Baut auf dem in
  v0.3 geplanten PvP-System auf (eigene PvP-Räume, ohne Fraktionsbezug) — im Kriegszustand wird PvP
  zwischen verfeindeten Fraktionen überall erlaubt, nicht mehr nur in dedizierten PvP-Räumen.
- **Reaktive Welt**: Städte, Monsterpopulationen und andere Weltzustände entwickeln sich auf Basis
  des kollektiven Spielerverhaltens (abgeschlossene Quests, bevorzugte Aufenthaltsorte, gewählte
  Berufe) weiter — neue Städte entstehen, andere verfallen, Monster-Spawnorte verschieben sich, usw.
  Baut direkt auf der in v0.3 geplanten Quest-Überarbeitung auf (NPC-Lagerbestände,
  Raum-Populationszähler, neuer Hintergrund-Tick-Dienst liefern bereits einen Großteil der nötigen
  Weltzustands-Telemetrie). Wirkt in beide Richtungen mit dem Fraktionssystem zusammen: der
  Kriegsverlauf beeinflusst die Weltentwicklung, und umgekehrt.

---

## v1.0 — Erste Vollversion

Zielzustand: vollständige Spielwelt für Lura — die einzige spielbare Region in v1.0 — mit
vollständig umgesetzter Story, inklusive des Kriegs zwischen dem Königreich Lura (verteidigend) und
dem Tyriovischen Imperium (angreifend). Das Tyriovische Imperium ist in v1.0 als Fraktion und
Invasionsmacht innerhalb Luras präsent (siehe die noch unbenannte v0.x-Version oben), aber **nicht** als eigenes bereisbares
Heimatgebiet — das eigentliche Tyriova-Gebiet folgt erst in einem Post-v1.0-Update (siehe unten).

### Gebiete

| Gebiet | Status |
|--------|--------|
| Lura (vollständig) | In Arbeit |

### Sonstige Ziele

- TBD

---

## Post-v1.0 — Erweiterungen

Reihenfolge der Updates noch nicht festgelegt.

### Gavoncaxo-Update

- Gaves
- Rova
- Iraves
- Osela
- *(weitere Gavoncaxo-Inhalte)*

### Xervur-Update

- Zovis
- *(restliche Xervur-Inhalte, inkl. nördlicher Teil bis zu den Cavyro-Ruinen)*

### Tyriova-Update

- Das eigentliche Tyriova-Heimatgebiet wird bereisbar (bisher nur als Fraktion/Invasionsmacht in
  Lura präsent, siehe die noch unbenannte v0.x-Version und v1.0 oben)
- *(weitere Tyriova-Inhalte)*

---

*Hinweis: Welches Update zuerst kommt (Gavoncaxo, Xervur oder Tyriova) ist noch offen und wird erst gegen Ende von v1.0 entschieden.*
