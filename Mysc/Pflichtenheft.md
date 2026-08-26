# Pflichtenheft — MyriaRPG Abschlussprojekt

**Projekt:** MyriaRPG  
**Version:** 0.2  
**Datum:** 2026-05-23  
**Autor:** Benn  
**Status:** In Bearbeitung

---

## Projektbeschreibung

MyriaRPG ist ein rundenbasiertes Online-Mehrspieler-Rollenspiel (MMORPG), das in der Fantasywelt **Myria** angesiedelt ist. Spieler erkunden die Region **Lura**, kämpfen gegen Monster, erfüllen Quests, treiben Handel miteinander und schließen sich zu Gruppen zusammen.

Das Spiel besteht aus drei Teilprojekten, die gemeinsam als Abschlussprojekt eingereicht werden:

- **MyriaRPG** — Windows-Desktopanwendung (WPF, .NET 8), die das gesamte Spielerlebnis darstellt
- **MyriaLib** — gemeinsame Spiellogik (.NET Class Library), die von Client und Server geteilt wird
- **MyriaServer** — Backend (ASP.NET Core, .NET 8), das Datenpersistenz, Authentifizierung und Echtzeit-Kommunikation übernimmt

Das Projekt demonstriert die vollständige Umsetzung eines vernetzten Spiels mit einem WPF-Client, einer REST-API, SignalR-Echtzeit-Kommunikation, einer SQL-Server-Datenbank (EF Core) sowie einem durchgehenden MVVM-Muster auf Clientseite.

### Spielwelt (Kurzüberblick)

Die Handlung spielt auf dem Kontinent Myria. Die Startregion **Lura** ist das Kerngebiet für Version 0.2. Weitere Regionen (Tyriova, Gavoncaxo, Xervur) sind in der Lore bereits ausgearbeitet und werden in späteren Versionen spielbar gemacht.

Spieler wählen bei der Charaktererstellung aus 8 Rassen und 12 Klassen. Das Fortschrittsystem umfasst Charakter-Level, Klassen-XP, ein dreistufiges Berufssystem (Skill / Wissen / Ruhm) sowie ein Skill-Kombinationssystem.

### Statuslegende (verwendete Markierungen in den Anforderungstabellen)

| Markierung | Bedeutung |
|-----------|-----------|
| `Vorhanden` | Funktion ist bereits vollständig implementiert und funktionsfähig |
| `Neu` | Funktion wird im Rahmen des Abschlussprojekts (v0.2) neu implementiert oder grundlegend überarbeitet |

---

## Inhaltsverzeichnis

1. [Zielbestimmung](#1-zielbestimmung)
2. [Produkteinsatz](#2-produkteinsatz)
3. [Systemarchitektur und Produktübersicht](#3-systemarchitektur-und-produktübersicht)
4. [Produktfunktionen](#4-produktfunktionen)
5. [Produktdaten](#5-produktdaten)
6. [Produktleistungen](#6-produktleistungen)
7. [Benutzungsschnittstelle](#7-benutzungsschnittstelle)
8. [Qualitätsanforderungen](#8-qualitätsanforderungen)
9. [Technische Umgebung](#9-technische-umgebung)
10. [Entwicklungsumgebung](#10-entwicklungsumgebung)
11. [Abgrenzungen](#11-abgrenzungen)
12. [Offene Punkte und Ergänzungen](#12-offene-punkte-und-ergänzungen)
looks greate, next lets look over all updated UI elements and localize everything that currently isn't (should be at least in the ingame window and its pages a lot)
---

## 1. Zielbestimmung

### 1.1 Mussziele (Core Goals — C1 bis C9)

Das Produkt **muss** folgende Ziele erfüllen, um als abgabefertig zu gelten:

| ID | Ziel |
|----|------|
| C1 | Korrekte Startstatistiken bei der Charaktererstellung aus `ClassProfile` ableiten |x
| C2 | Dateinamen-Tippfehler in zwei ViewModel-Dateien beheben (Klassen, Namespaces, Referenzen) |x
| C3 | WPF-Seite für Partyverwaltung (`Page_Party.xaml`) vollständig implementieren |x
| C4 | Online-Spieler-Browser / Lobby als eigene WPF-Seite und Server-Endpunkt umsetzen |x
| C5 | Kartenfehler beheben: Zonengruppenklick und NPC-Tooltip auf der lokalen Karte |x
| C7 | Neue Spielwelt-Inhalte (CO5, Gruppe 1): Royas→Hydea→Xarra Storypath-Locations |x
| C6 | Vollständige WPF-Lokalisierung: alle hartcodierten englischen Strings durch lokalisierte Schlüssel ersetzen |x
| C8 | Partybasierte Raumzugangskontrolle (`RequiredPartySize`) serverseitig erzwingen |x

### 1.2 Wunschziele (Optional Goals — O1 bis O11)

Das Produkt **soll** folgende Ziele erfüllen, sofern die Zeit es erlaubt:

| ID | Ziel | Priorität |
|----|------|-----------|
| O3 | Erweiterte Sammelstellen über bestehende und neue Räume | Mittel |x
| O7 | Kampflog-Verbesserungen (Farbcodierung, Kritische-Treffer-Anzeige) | Mittel |x
| O8 | Statistik-Vorschau bei Charaktererstellung (erfordert C1) | Mittel |x
| O6 | Karten-Layout-Konsistenz (Zonenankerpositionen) | Niedrig |x
| O9 | Rassen- und Klassenprofile verfeinern und ausbalancieren | Niedrig |i
| O10 | Fantasy-Theme-Rework (Typografie, Pergamentfarben, Runen-Rahmen) | Niedrig |x
| O11 | Item- und Skill-Icons (Kunstassets erforderlich) | Niedrig |i

### 1.3 Bonusziele (Optionale Erweiterungen — nur bei verfügbarer Zeit)

Das Produkt **kann** folgende Features umsetzen, sofern nach Abschluss aller Muss- und Wunschziele noch Zeit verbleibt. Diese sind **keine Hauptziele** und werden nicht für die Abgabe vorausgesetzt.

| Feature | Hinweis |
|---------|---------|
| PvP-Kampfsystem | Kein vollständiges Design vorhanden; nur als Bonus bei verbleibender Zeit umsetzbar |o
| Gildensystem | Erfordert neue Datenbankstruktur und Serverarchitektur; nur als Bonus bei verbleibender Zeit |i
| Benutzerdefinierte Myraic-Schriftart | Erfordert Schriftdesigner und Build-Toolchain; nur als Bonus bei verbleibender Zeit |x

### 1.4 Abgrenzungsziele

Das Produkt **wird nicht** folgende Features umsetzen:

| Feature | Begründung |
|---------|------------|
| Achievement-System | Geräteübergreifende Instrumentierung; kein Design |
| Mehrere Weltsprachen | Abhängig von LO1 + LO2; großer Inhalt- und Designaufwand |
| Hintergrundbilder | Kunstassets müssen separat beauftragt/beschafft werden |
| Vollständiger Anti-Cheat / Serverautorität | Architektur-Overhaul; SP-Kampf ist bewusst clientseitig |

---

## 2. Produkteinsatz

### 2.1 Anwendungsbereich

MyriaRPG ist ein Mehrspieler-fähiges Rollenspiel, das als Abschlussprojekt entwickelt wird. Es demonstriert die vollständige Integration eines WPF-Clients mit einem ASP.NET Core-Server über REST und SignalR.

### 2.2 Zielgruppen

- **Primär:** Prüfer und Auftraggeber des Abschlussprojekts (technische Demonstration)
- **Sekundär:** Mitspieler während der Projektpräsentation (Multiplayer-Demo)

### 2.3 Betriebsbedingungen

- Der WPF-Client läuft auf Windows-Rechnern mit .NET-Runtime.
- Der Server wird auf einem Test Rechner mit HTTPS betrieben.
- Für die Präsentation sind mindestens 2 gleichzeitig verbundene Clients vorgesehen (auf einem Gerät). 
- Offline-Betrieb (Einzelspieler) ist über lokale Serverinstanz möglich.

---

## 3. Systemarchitektur und Produktübersicht

Das System besteht aus drei Teilprojekten:

```
┌─────────────────────┐        REST / SignalR        ┌──────────────────────┐
│   MyriaRPG (WPF)    │ ◄──────────────────────────► │  MyriaServer         │
│   Windows-Client    │                              │  ASP.NET Core        │
└─────────┬───────────┘                              └──────────┬───────────┘
          │                                                     │
          └──────────────────────┬──────────────────────────────┘
                                 │
                     ┌───────────▼──────────┐
                     │     MyriaLib         │
                     │  Shared Game Engine  │
                     │  (.NET Class Library)│
                     └──────────────────────┘
```

### 3.1 MyriaLib — Gemeinsame Spiellogik

Zentralisierte Spiellogik, die von Client und Server gemeinsam genutzt wird.

**Kernentitäten:** `Character`, `Monster`, `Item`, `Room`, `NPC`, `Quest`, `Job`, `Class`, `Skill`, `GatheringSpot`, `City`

**Vollständig implementierte Systeme:**
- Kampfsystem (1v1 und Gruppenkkampf mit Rundensystem)
- Sammel-, Handwerks- und Verbesserungssystem
- Berufssystem (3-Aspekt-XP: Skill / Wissen / Ruhm, täglicher Verfall)
- Klassensystem (12 Klassen, `ClassProfile`-Statwachstum, Klassen-XP)
- Questsystem (Kill- und Sammelziele, wiederholbare Quests, mehrseitige Dialoge)
- Skill-System (lernen, einsetzen, kombinieren, zusammengesetzte Skills)
- Tag-/Wochenzyklus (passive Ticks, Limit-Reset)
- Balance (XP-Skalierung, Schadensformel)
- Lokalisierung (EN/DE, JSON-basiert, `[LocalizedKey]`-Attribut)

**Inhalte (aus JSON geladen):**
- 200+ Items, 32 Monster, 40+ Quests, 12 Handwerksrezepte, 12 Klassen, 8 Rassen

### 3.2 MyriaServer — Backend

**Authentifizierung:** JWT Register/Login, Token-Validierungs-Middleware

**Charakterpersistenz:** REST CRUD, SQL Server + EF Core (3 Migrationen)

**Soziale Funktionen (REST):**
- Freundschaftssystem (`FriendsController`)
- Blocksystem (`BlocksController`)

**Echtzeit-Funktionen (SignalR `GameHub`):**
- Raum-Präsenz (`JoinRoom`, `CharacterEntered`, `CharacterLeft`, `RoomCharacters`)
- Chat (Global / Raum / Party / Flüstern)
- Party-System (`PartyService`: Erstellen, Einladen, Annehmen, Ablehnen, Verlassen, Kicken, Leader-Übertragung)
- 1v1-Kampf und Gruppenkkampf
- Serverseitiges Sammeln, Handwerken und Verbessern
- Direkthandel (`TradeService`, Zustandsmaschine)
- Spielershops (`CharacterShopService`, im Speicher, live während Owner online)

### 3.3 MyriaRPG — WPF-Client

**Authentifizierungs- und Charakterfluss:** Login/Register → Charakterauswahl → Charaktererstellung

**Im Spiel:**
- Raumseite mit Beschreibung, Ausgängen, NPCs, Sammelstellen, Kampfauslöser
- Chat-Panel (4 Kanäle)
- Kampfseiten (SP 1v1 und MP-Gruppe)
- NPC-Interaktionspanele (Shop, Handwerk, Verbesserung, Dialog, Berufsmeister, Klasse, Quest)
- Ingame-Menü (Charakter, Inventar, Skills, Berufe, Questliste, Freunde, Karte, Spielershop, Handel, Einstellungen)

---

## 4. Produktfunktionen

### 4.1 Authentifizierung und Charakterverwaltung

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| AF-01 | Das System erlaubt die Registrierung eines neuen Benutzerkontos mit Benutzername und Passwort. | Vorhanden |
| AF-02 | Das System erlaubt die Anmeldung mit gültigem JWT-Token. | Vorhanden |
| AF-03 | Ein Benutzer kann bis zu N Charaktere erstellen, auswählen und löschen. | Vorhanden |
| AF-04 | Bei der Charaktererstellung wählt der Spieler Rasse und Klasse; die Startstatistiken werden korrekt aus dem `ClassProfile` der gewählten Klasse abgeleitet. **(C1)** | Neu |
| AF-05 | Dateinamen, Klassennamen und Namespaces der ViewModels sind fehlerfrei und konsistent. **(C2)** | Neu |

### 4.2 Spielwelt und Navigation

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| SW-01 | Der Spieler bewegt sich zwischen Räumen über sichtbare Ausgänge. | Vorhanden |
| SW-02 | Räume mit `RequiredPartySize > 1` sind im Einzelspieler auch ohne Party zugänglich. **(C8)** | Neu |
| SW-03 | Die lokale Karte zeigt Räume und Zonengruppen; ein Klick auf eine Zonengruppe öffnet den Innenbereich der Zone. **(C5/M6)** | Neu |
| SW-04 | Beim Hovern über einen Raumknoten auf der Karte erscheint ein Tooltip mit den NPC-Namen des Raums. **(C5/M7)** | Neu |
| SW-05 | Die Spielwelt enthält die Storypath-Locations Royas→Hydea→Xarra mit zugehörigen NPCs, Monstern und Questinhalten. **(C7)** | Neu |

### 4.3 Kampfsystem

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| KA-01 | Ein Einzelspieler kann 1v1-Kämpfe gegen Monster starten und bestreiten. | Vorhanden |
| KA-02 | Eine Gruppe von Spielern kann Gruppenkämpfe gegen mehrere Monster starten. | Vorhanden |
| KA-03 | Der Spieler kann Angreifen, Flüchten oder Skill einsetzen. | Vorhanden |
| KA-04 | Der Kampflog zeigt alle Ereignisse (Angriffe, Schaden, Effekte, Ergebnis). | Vorhanden |
| KA-05 | XP und Beute werden nach dem Kampf korrekt verteilt. | Vorhanden |

### 4.4 Party-System

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| PA-01 | Ein Spieler kann eine Party erstellen, andere einladen, Einladungen annehmen oder ablehnen. | Vorhanden |
| PA-02 | Der Party-Leader kann Mitglieder kicken oder die Leitung übertragen. | Vorhanden |
| PA-03 | Jeder Spieler kann die Party verlassen. | Vorhanden |
| PA-04 | Die WPF-Anwendung zeigt eine dedizierte Party-Seite (`Page_Party.xaml`) mit Mitgliederliste (Name, HP-Balken, Mana-Balken), Leader-Markierung sowie Kick-, Transfer- und Verlassen-Schaltflächen. **(C3)** | Neu |

### 4.5 Online-Spieler-Browser / Lobby

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| LO-01 | Der Server stellt `GET /api/lobby/online` bereit und gibt eine Liste aller verbundenen Spieler zurück (`{Name, Level, RaumName}`). **(C4)** | Neu |
| LO-02 | Die WPF-Anwendung zeigt eine Lobby-Seite mit der Online-Liste und einer "Party einladen"-Schaltfläche pro Eintrag. **(C4)** | Neu |
| LO-03 | Die Online-Liste wird bei Bedarf manuell aktualisiert und alle 30 Sekunden automatisch neu geladen. **(C4)** | Neu |

### 4.6 Soziale Funktionen

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| SO-01 | Spieler können Freundschaftsanfragen senden, annehmen und ablehnen. | Vorhanden |
| SO-02 | Spieler können andere Spieler blockieren; geblockte Spieler können kein Flüstern senden. | Vorhanden |
| SO-03 | Der Chat unterstützt Global-, Raum-, Party- und Flüster-Kanäle. | Vorhanden |

### 4.7 Wirtschaft und Interaktion

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| WI-01 | Spieler können bei NPC-Shops einkaufen und verkaufen. | Vorhanden |
| WI-02 | Spieler können Gegenstände handwerken (Crafting) und verbessern (Upgrade). | Vorhanden |
| WI-03 | Spieler können Sammelstellen nutzen, mit täglichen Limits und Berufs-Skill-Multiplikator. | Vorhanden |
| WI-04 | Spieler können miteinander direkt handeln (Direkthandel, Zustandsmaschine). | Vorhanden |
| WI-05 | Spieler können eigene Spielershops öffnen und Waren anbieten; andere können diese durchsuchen und kaufen. | Vorhanden |

### 4.8 Berufs- und Klassensystem

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| BK-01 | Jeder Spieler hat einen aktiven Beruf mit drei XP-Aspekten (Skill, Wissen, Ruhm). | Vorhanden |
| BK-02 | Ein Berufswechsel ist frühestens nach 7 Tagen möglich. | Vorhanden |
| BK-03 | Jeder Spieler hat eine aktive Klasse mit eigenem XP-Kurve und Stat-Wachstum. | Vorhanden |
| BK-04 | Ein Klassenwechsel ist frühestens nach 7 Tagen möglich; 50 % der gleichen Gruppen-XP werden übertragen. | Vorhanden |

### 4.9 Quest-System

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| QU-01 | Quests haben Kill- und/oder Sammelziele; Fortschritt wird automatisch verfolgt. | Vorhanden |
| QU-02 | Wiederholbare Quests respektieren tägliche Limit-Caps. | Vorhanden |
| QU-03 | Quests können Voraussetzungen (Beruf, Klasse, Rasse, Partygröße) haben. | Vorhanden |
| QU-04 | Die Questliste im Ingame-Menü zeigt aktive und verfügbare Quests. | Vorhanden |

### 4.10 Lokalisierung

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| LK-01 | Die gesamte Benutzeroberfläche und alle Spieltexte sind vollständig auf Englisch und Deutsch verfügbar. | Vorhanden |
| LK-02 | Alle noch hartcodierten Strings in XAML-Dateien und ViewModels werden durch lokalisierte Schlüssel ersetzt. **(C6)** | Neu |
| LK-03 | Die Spracheinstellung kann in den Einstellungen zur Laufzeit gewechselt werden. | Vorhanden |

### 4.11 Server-Deployment

| Req-ID | Anforderung | Status |
|--------|-------------|--------|
| SD-01 | Der Server kann mit `appsettings.Production.json` konfiguriert werden (JWT-Secret, CORS, DB-Pfad). **(C9)** | Neu |
| SD-02 | Der Server unterstützt HTTPS (Kestrel + Zertifikat). **(C9)** | Neu |
| SD-03 | Der WPF-Client liest die Server-URL aus einer Konfigurationsdatei oder Build-Variable. **(C9)** | Neu |
| SD-04 | Eine Deployment-Anleitung dokumentiert: `dotnet publish` → Kopieren auf VPS → Als Service betreiben. **(C9)** | Neu |

---

## 5. Produktdaten

### 5.1 Persistente Daten (SQL Server via EF Core)

| Entität | Felder (Auswahl) |
|---------|-----------------|
| Benutzer | Id, Benutzername, Passwort-Hash |
| Charakter | Id, Name, Rasse, Klasse, Level, XP, Stats, Inventar, Skills, Beruf, Position |
| Freundschaft | BenutzerId, FreundId, Status |
| Block | BenutzerId, GeblockterId |

### 5.2 Spielweltdaten (JSON-Dateien in MyriaLib)

| Datei | Inhalt |
|-------|--------|
| `items.json` | 200+ Items (Waffen, Rüstungen, Verbrauchsgüter, Materialien, Währung) |
| `monsters.json` | 32 Monster, Level 1–40+, Loot-Tabellen |
| `rooms.json` / `cities.json` | Raumdefinitionen, Ausgänge, Sammelstellen, Required Party Size |
| `npcs.json` | NPCs mit Typ, Dialog, Shop-/Quest-Referenzen |
| `quests.json` | 40+ Quests mit Zielen, Voraussetzungen, Belohnungen |
| `recipes.json` | 12 Handwerksrezepte mit Knowledge-Voraussetzungen |
| `en.json` / `de.json` | Lokalisierungsstrings |

### 5.3 Flüchtige Daten (In-Memory, Server-Laufzeit)

| Daten | Beschreibung |
|-------|-------------|
| Partys | `PartyService`: aktive Partys pro Session |
| Spielershops | `CharacterShopService`: geöffnete Shops solange Owner online |
| Direkthandel | `TradeService`: aktive Handelssitzungen |
| Raumgegenwart | Spieler-zu-Raum-Mapping im `GameHub` |
| Laufende Kämpfe | `GroupCombatService`: aktive Gruppenkkampf-Encounters |

---

## 6. Produktleistungen

| ID | Anforderung |
|----|-------------|
| PL-01 | Der Server muss mindestens 10 gleichzeitige Verbindungen ohne spürbare Verzögerung unterstützen. |
| PL-02 | Echtzeit-Ereignisse (Raumbetreten, Chatnachrichten, Kampfrunden) dürfen nicht länger als 500 ms verzögert sein. |
| PL-03 | Die lokale Karte muss bei bis zu 100 Räumen flüssig rendert werden (< 16 ms pro Frame). |
| PL-04 | Der WPF-Client startet innerhalb von 5 Sekunden und stellt die Server-Verbindung innerhalb von 3 Sekunden her. |
| PL-05 | Die Lobby-Online-Liste wird automatisch alle 30 Sekunden aktualisiert. |

---

## 7. Benutzungsschnittstelle

### 7.1 Allgemeine Gestaltungsprinzipien

- Light/Dark-Theme-Wechsel zur Laufzeit ohne Neustart
- Vollständige EN/DE-Lokalisierung mit Sprachauswahl in den Einstellungen
- MVVM-Architektur; `RelayCommand` und statischer `Navigation`-Service
- Einheitliche Gestaltung aller Ingame-Menüseiten

### 7.2 Seitenübersicht

| Seite | Beschreibung | Status |
|-------|-------------|--------|
| Login / Register | Authentifizierungsformulare | Vorhanden |
| Charakterauswahl | Liste eigener Charaktere, Neu-Erstellen-Schaltfläche | Vorhanden |
| Charaktererstellung | Rassen- und Klassenwähler mit Preview; Startstatistiken aus ClassProfile | Vorhanden |
| Spielraum (Room Page) | Beschreibung, Ausgänge, NPCs, Sammelstellen, Spielerliste, Kampfauslöser | Vorhanden |
| 1v1-Kampfseite | Angreifen, Flüchten, Skill-Leiste, Kampflog | Vorhanden |
| Gruppen-Kampfseite | Partymitglieder-HP-Panel, Monsterliste, Rundenanzeige | Vorhanden |
| NPC-Shop / Handwerk / Verbesserung / Dialog / Berufsmeister / Klasse / Quest | Dedizierte Panels je NPC-Typ | Vorhanden |
| Charaktermenü | Stats, Klassen-Level + XP-Leiste, unausgegebene Punkte | Vorhanden |
| Inventar | Mehrtab-Ansicht (Ausrüstung / Verbrauchsgüter / Materialien / Alle), Drag-Drop | Vorhanden |
| Skills | Lern-, Kombinations- und Slot-Management-Seite | Vorhanden |
| Berufe | Fortschrittsbalken für alle 3 Aspekte | Vorhanden |
| Questliste | Aktiv / Verfügbar Tabs | Vorhanden |
| Freunde | Liste, eingehende Anfragen, Anfrage senden | Vorhanden |
| Lokale Karte | BFS-Layout, Pan/Zoom, Zonengruppen-Kollaps, aktueller Raum hervorgehoben | Vorhanden |
| Party-Seite | Mitgliederliste (HP/Mana), Leader-Krone, Kick/Transfer/Verlassen **(C3)** | Neu |
| Lobby-Seite | Online-Spieler-Liste, Party-Einladen-Schaltfläche, Aktualisieren **(C4)** | Neu |
| Spielershop | Besitzer-Modus (auflisten/hinzufügen/entfernen) + Käufer-Modus | Vorhanden |
| Handel | Vorschlagen → Bestätigen → Abgeschlossen/Abgebrochen | Vorhanden |
| Einstellungen | Theme, Sprache, Tastenbelegungen | Vorhanden |

---

## 8. Qualitätsanforderungen

| Kategorie | Anforderung |
|-----------|-------------|
| Korrektheit | Startstatistiken bei der Charaktererstellung entsprechen exakt dem ClassProfile der gewählten Klasse. |
| Korrektheit | Raum-Zugangskontrolle wird serverseitig erzwungen; kein clientseitiger Bypass möglich. |
| Zuverlässigkeit | Der Server läuft stabil über die gesamte Präsentationsdauer ohne Neustart. |
| Wartbarkeit | Alle Dateinamen, Klassennamen und Namespaces sind einheitlich und tippfehlerfrei. |
| Lokalisierbarkeit | Alle sichtbaren Texte sind in `en.json` und `de.json` gepflegt; keine Hardcodes in XAML oder ViewModel. |
| Sicherheit | JWT-Secret und DB-Pfad werden nicht in `appsettings.json` eingecheckt; werden über `appsettings.Production.json` oder Umgebungsvariablen gesetzt. |
| Testbarkeit | Kernlogik in MyriaLib ist von UI und Server getrennt und ohne GUI testbar. |

---

## 9. Technische Umgebung

### 9.1 Server-Umgebung

| Komponente | Technologie |
|-----------|-------------|
| Laufzeitumgebung | .NET 8 (ASP.NET Core) |
| Datenbank | SQL Server via Entity Framework Core |
| Echtzeit-Kommunikation | ASP.NET Core SignalR |
| Authentifizierung | JWT Bearer Token |
| HTTPS | Kestrel + TLS-Zertifikat (Let's Encrypt oder Self-Signed) |
| Hosting | VPS (Linux oder Windows Server) |

### 9.2 Client-Umgebung

| Komponente | Technologie |
|-----------|-------------|
| Framework | WPF (.NET 8, Windows only) |
| Architektur | MVVM |
| Echtzeit-Kommunikation | SignalR Client |
| Zielplattform | Windows 10/11 |

### 9.3 Gemeinsam (MyriaLib)

| Komponente | Technologie |
|-----------|-------------|
| Typ | .NET Class Library (.NET 8) |
| Spiellogik | Pure C#, keine UI-Abhängigkeiten |
| Datenhaltung | JSON-Dateien (Items, Räume, Monster, Quests, Lokalisierung) |

---

## 10. Entwicklungsumgebung

| Komponente | Version / Tool |
|-----------|---------------|
| IDE | Visual Studio 2022 / JetBrains Rider |
| Sprache | C# 12 |
| Framework | .NET 8 |
| Versionskontrolle | Git |
| Datenbankmigrationen | EF Core CLI (`dotnet ef migrations add`) |
| Deployment | `dotnet publish` → manuelles Kopieren auf VPS |

---

## 11. Abgrenzungen

Das Produkt schließt folgende Bereiche ausdrücklich **nicht** ein:

- **ConsoleWorldRPG:** Dieses Teilprojekt ist vom Abschlussprojekt-Scope ausgenommen.
- **PvP-Kampfsystem:** Kein vollständiges Design vorhanden; optional als Bonusziel bei verbleibender Zeit (siehe 1.3), aber kein Hauptziel.
- **Gildensystem:** Erfordert neue Datenbankstruktur und Server-Architektur; optional als Bonusziel bei verbleibender Zeit (siehe 1.3), aber kein Hauptziel.
- **Benutzerdefinierte Myraic-Schriftart:** Benötigt Schriftdesigner und spezielle Build-Toolchain; optional als Bonusziel bei verbleibender Zeit (siehe 1.3), aber kein Hauptziel.
- **Achievement-System:** Erfordert geräteübergreifende Instrumentierung; kein Design vorhanden.
- **Mehrere Weltsprachen (LO3):** Abhängig von LO1 und LO2; umfangreicher Inhalt- und Designaufwand.
- **Hintergrundbilder (AR1):** Kunstassets müssen separat beauftragt oder beschafft werden.
- **Vollständiger Anti-Cheat / Serverautorität:** Architektonischer Umbau; SP-Kampf ist bewusst clientseitig konzipiert.

---

## 12. Offene Punkte und Ergänzungen

### 12.1 Zeitplan

| Woche | Inhalt |
|-------|--------|
| 1–4 | Alle Core Goals (C1–C9) |
| 5–7 | Optional Goals O1–O3, O7–O8 |
| 8 | Puffer, Polishing, Präsentationsvorbereitung |
| Bei Zeitgewinn | O4 (MP-Startgebiet), O5 (Lexica) |
| Parallel / Unabhängig | O10 (Theme), O11 (Icons) — wenn Kunstassets verfügbar |

### 12.2 Zeitschätzungen (Core Goals)

| Ziel | Aufwand |
|------|---------|
| C1 — Statistik-Initialisierung | 0,5 Tage |
| C2 — Dateinamen-Tippfehler | 0,5 Tage |
| C3 — Party-Management-Seite | 2–3 Tage |
| C4 — Online-Spieler-Browser | 2–3 Tage |
| C5 — Karten-Zonen-Klick + Tooltip | 1–2 Tage |
| C6 — Lokalisierungs-Sweep | 1–2 Tage |
| C7 — CO5 Gruppe 1 (4 Locations) | 5–8 Tage |
| C8 — Party-Raumgating | 1 Tag |
| C9 — Server-Deployment | 1–2 Tage |
| **Gesamt Core** | **≈ 14–22 Tage (3–4 Wochen)** |

### 12.3 Risiken

| Risiko | Wahrscheinlichkeit | Maßnahme |
|--------|-------------------|----------|
| C7 (Weltinhalt) dauert länger als geplant | Mittel | Scope auf Royas→Hydea reduzieren; Xarra-Rework als O1a deklarieren |
| Server-Deployment-Probleme (C9) | Niedrig | Frühzeitig testen; Fallback: lokaler Server für Präsentation |
| Art Assets für O10/O11 nicht rechtzeitig verfügbar | Hoch | O10/O11 aus Kernscope streichen; nur bei externem Asset-Lieferant umsetzen |
| Kartenfehler (C5) schwer zu debuggen | Niedrig | Isoliertes Test-Canvas erstellen; HitTest schrittweise debuggen |

---

_Erstellt: 2026-05-23_  
_Basierend auf: `Abschlussprojekt_Plan.md` (Stand: 2026-05-20)_
