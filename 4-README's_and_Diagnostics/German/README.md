# PoE2 Route AutoSplitter

Ein Einrichtungswerkzeug und LiveSplit-Autosplitter für **Path of Exile 2 Kampagnen-Speedruns**.

Aktuelle Version: **v3.0.0 Release Candidate**.

PoE2 Route AutoSplitter bietet vordefinierte und benutzerdefinierte Routen für:

* Erkundung / Gebietsabschluss
* Boss Rush
* Kombinierte Erkundung + Boss Rush
* Campaign Any%
* Campaign 100%
* Nur erforderliche Kampagnenbosse
* 0.5 Pinnacle-Bosse
* Temple of Chaos
* Trial of the Sekhemas
* Benutzerdefinierte Routen
* Maps

Die enthaltene Anwendung **PoE2RouteSetup** übernimmt den größten Teil der Einrichtung.

Beim Öffnen des Pausenmenüs können Spiel und LiveSplit-Timer synchron pausiert werden.
Die Game-Time-Option von LiveSplit schließt Ladezeiten aus und pausiert den Timer, wenn die entsprechende Option aktiviert ist.

Screenshots: https://imgur.com/a/VgiRn6o

---
# Run-Regeln

Ich habe versucht, das Tool so unabhängig wie möglich von einem bestimmten Regelwerk zu gestalten. Spieler haben daher viel Freiheit bei der Wahl ihrer Run-Regeln und Auslöser.

Bei einem frischen Start in Riverbank ist die kurze Zeit zwischen dem Aufwachen des Charakters und dem Gespräch mit The Wounded Man absichtlich nicht gewertet. So bleibt Zeit, Einstellungen zu korrigieren, „skip tutorial“ zu wählen oder andere Optionen anzupassen, bevor der Run wirklich beginnt. Nach der Interaktion mit The Wounded Man startet die Zeit bei seiner letzten Einleitungszeile.

Zone-Transition-Starts werden aktiv, sobald der Charakter die festgelegte Zone betritt. Bei dynamischen Runs startet der Timer und das Tracking also erst, wenn genau diese Zone betreten wird, selbst wenn der Run in einer anderen Zone beginnt.

Aufgrund der Spiellänge wurde GameTimeWatcher entwickelt. Dieses kleine Programm weist LiveSplit an, seine Game Time zu pausieren, solange das Pause-Game-Menü oder das Mikrotransaktionsmenü geöffnet ist. Dadurch können Spieler Pausen machen oder Situationen erledigen, die ihre volle Aufmerksamkeit erfordern. Andere Menüs pausieren die Zeit nicht, weil der Charakter dort weiterhin kontrolliert werden kann. Auch während Ingame-Zwischensequenzen läuft der Timer weiter, da das Inventar zugänglich ist und zur Run-Optimierung genutzt werden kann. Derzeit pausiert die Zeit nur bei Ladebildschirmen, im Pausenmenü und im Mikrotransaktionsshop.

---

# Download

Der Download ist [hier](https://github.com/ScottHoppe414/PoE2RouteAutoSplitter/tags) zu finden.

ODER

Öffne den Bereich **Releases** dieses GitHub-Repositories und lade die neueste Version herunter:

**`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`**

Für die meisten Benutzer wird das Installationsprogramm empfohlen.

Alternativ kann eine portable ZIP-Datei angeboten werden. In diesem Fall muss PowerShell verwendet werden, um `\Setup-UI[Configuration]\Build.ps1` auszuführen und `RouteSetup.exe` zu erzeugen.

---

# Schnellstart

## 1. PoE2 Route AutoSplitter installieren

Ausführen:

`PoE2RouteAutoSplitter-vX.X.X-Setup.exe`

Den Installationsanweisungen folgen.

Nach der Installation öffnen:

**PoE2 Route AutoSplitter**

Dadurch wird die Routeneinrichtung gestartet.

---

## 2. Route auswählen

Die Setup-Anwendung enthält eine Liste vordefinierter Routen.

Wähle die gewünschte Route aus.

Beispiele:

* Campaign Any%
* Campaign 100%
* Nur erforderliche Bosse
* Erkundungsrouten
* Boss-Rush-Routen
* Kombinierte Erkundung + Boss Rush

Über **Custom Route** kann außerdem eine eigene Route erstellt werden.

---

## 3. LiveSplit-Konfiguration erzeugen

Nach Auswahl der Route auf Generate klicken.

Die Anwendung erzeugt die benötigten Dateien im Verzeichnis:

`LiveSplit Target`

Dieser Ordner enthält die für die gewählte Route benötigten LiveSplit-Dateien.

Der Inhalt von **LiveSplit Target** wird jedes Mal ersetzt, wenn eine neue Konfiguration erzeugt wird.

---

# LiveSplit einrichten

In LiveSplit müssen zwei Dinge eingerichtet werden:

1. Die Split-Datei (`.lss`)
2. Der Scriptable Auto Splitter (`.asl`)

## Split-Datei laden

Im erzeugten Ordner **LiveSplit Target** die `.lss`-Datei suchen und mit LiveSplit öffnen.

Alternativ kann sie manuell geladen werden über:

**File → Open Splits → From File**

Anschließend die erzeugte `.lss`-Datei auswählen.

---

## Scriptable Auto Splitter hinzufügen

Das Autosplitter-Skript muss manuell zum LiveSplit-Layout hinzugefügt werden.

In LiveSplit:

1. Rechtsklick auf LiveSplit.
2. **Edit Layout** auswählen.
3. Auf **+** klicken.
4. Auswählen:

   **Control → Scriptable Auto Splitter**

5. Die neue Komponente **Scriptable Auto Splitter** auswählen.
6. Die `.asl`-Datei im Ordner **LiveSplit Target** auswählen.
7. Das Layout speichern.

Dieser Pfad muss nur geändert werden, wenn die erzeugten Dateien verschoben werden oder eine Konfiguration mit einer anderen ASL-Datei verwendet wird.

> PoE2 Route AutoSplitter erzeugt oder ersetzt dein LiveSplit-Layout **nicht**.

Das Layout bleibt vollständig unter deiner Kontrolle.

---

# Boss-Rush-Einrichtung

Routen mit Boss-Tracking verwenden das enthaltene Programm **BossWatcher**.

BossWatcher liest Bossnamen aus dem Spiel und sendet Bossereignisse an den Autosplitter.

Wenn die gewählte Route BossWatcher benötigt, verwende in PoE2 Route Setup die Schaltfläche:

**Start BossWatcher**

Ein Konsolenfenster wird geöffnet.

Im normalen Betrieb zeigt BossWatcher nur nützliche Ereignisse an, zum Beispiel:

* Boss entdeckt
* Boss besiegt
* Kampfdauer

Beispiel:

`[21:42:18] Encountered: The Executioner`

`[21:43:07] Defeated: The Executioner | Fight time: 49.213 s`

Während des Runs ist keine Interaktion mit der BossWatcher-Konsole erforderlich.

Lass sie während des Speedruns geöffnet.

---

# Erkundungsrouten

Erkundungsrouten erkennen, wenn der Charakter bestimmte Gebiete in Path of Exile 2 betritt.

BossWatcher wird für reine Erkundungsrouten **nicht benötigt**.

Der Autosplitter liest die Gebietswechsel-Informationen von Path of Exile 2 automatisch.

---

# Kombinierte Erkundung + Boss Rush

Kombinierte Routen verfolgen:

* Gebietsabschlüsse
* Bossbesiegungen

Für diese Routen:

1. Die erzeugte `.lss` laden.
2. Scriptable Auto Splitter auf die erzeugte `.asl` verweisen lassen.
3. BossWatcher über PoE2 Route Setup starten.
4. Den Run beginnen.

Gebiets- und Bossziele werden anschließend von derselben Route verwaltet.

---

# Benutzerdefinierte Routen

In PoE2 Route Setup **Custom Route** auswählen, um eine eigene Route zu erstellen.

Enthalten sein können:

* Gebiete
* Bosse
* Gebiete und Bosse

Die gewünschten Ziele hinzufügen und in die gewünschte Reihenfolge bringen.

Anschließend die Konfiguration erzeugen.

Die Anwendung erstellt im Ordner **LiveSplit Target**:

* `.lss`
* `.asl`
* Routenkonfiguration

Diese Dateien werden mit denselben LiveSplit-Schritten wie oben geladen.

---

# Trials

Für Trial of the Sekhemas und Temple of Chaos vorgesehen.

Die Startbedingung ist der erste Eintritt in den eigentlichen Trial. Das Foyer, in dem die Vorbereitung erfolgt, wird nicht getrackt.

Es gibt zwei Endbedingungen:

1. Du wählst, wie weit du im Trial laufen möchtest. Wird der Boss der festgelegten Tiefe besiegt, endet der Trial erfolgreich. Ein nicht abgeschlossener Trial gilt als fehlgeschlagener Run und muss manuell neu gestartet werden.

2. Das Verlassen des Trials markiert ihn als abgeschlossen. Diese Option ist für Spieler gedacht, die das Verlassen der Trial-Arena als Endbedingung verwenden möchten. Loot, Caches, Händler und Ascendancy-Auswahl werden dann Teil des Runs.

---

# Vaal Ruins

Das Foyer wird aus Übergangsgründen als Grenzzone behandelt. Das Betreten des Konsolenraums von einer Map aus wird daher als Verlassen der Map gewertet und nicht als Untergebiet dieser Map.

Vaal Ruins befinden sich weiterhin in Entwicklung.

---

# Maps

Die Vorbereitung einer Map wird nicht gewertet, solange sich der Spieler in einem Hideout oder einem anderen Map-Hub befindet. Beim Betreten der Map startet der Timer automatisch. Nach dem Besiegen des Gebiets-Bosses wird beim ersten Verlassen gesplittet. Wird die Map vor dem Bosskill verlassen, läuft der Timer weiter. Dadurch kann der Boss schnell getötet, die Map verlassen und dieselbe Map anschließend für zusätzlichen Inhalt mit pausiertem Timer erneut betreten werden. (Alternative Regel unten.)

Map-Runs bieten mehrere Endbedingungen:

* Feste Anzahl an Maps
* Bis zum ersten Tod (Deathless Run)
* Manuelles Ende
* Besiegen eines bestimmten Pinnacle-Bosses

Für Death Tracking gibt es drei Optionen:
* Kein Death Tracking
* Nur erster Tod
* Tode zählen

Bei „erster Tod“ oder „Tode zählen“ muss der Charaktername exakt so eingegeben werden, wie er im Spiel erscheint. Die Client-Logs werden gelesen, um den Tod des Charakters zu erkennen.

Es gibt zwei Pausenregeln:

* Der Bosskill gilt als Map-Abschlussereignis; der Split endet beim ersten Verlassen nach dem Bosskill. Dies entspricht ungefähr der Map-Abschlusslogik von PoE2.
* Alternative Regel: Der Timer pausiert nur während Ladebildschirmen, bei manueller Pause oder im Mikrotransaktionsmenü (falls aktiviert). Zu allen anderen Zeiten läuft er weiter, einschließlich Map-Vorbereitung, Inventarverwaltung und Loot-Auswertung.

# Routen wechseln

So wechselst du zu einer anderen Route:

1. PoE2 Route Setup öffnen.
2. Die neue Route auswählen.
3. Die Konfiguration erneut erzeugen.
4. Die neue `.lss` in LiveSplit öffnen.
5. Prüfen, dass Scriptable Auto Splitter auf die `.asl` in **LiveSplit Target** zeigt.
6. BossWatcher starten, wenn die neue Route Boss-Erkennung benötigt.

Der vorherige Inhalt von **LiveSplit Target** wird ersetzt.

---

# Einen Run starten

Nach abgeschlossener Einrichtung:

1. Path of Exile 2 öffnen.
2. LiveSplit öffnen.
3. Die `.lss` der Route laden.
4. Sicherstellen, dass Scriptable Auto Splitter die richtige `.asl` verwendet.
5. BossWatcher starten, wenn die Route Bosse verwendet.
6. Den Run beginnen.

Der Autosplitter verwaltet die konfigurierten Routenziele automatisch.

---

# Aktualisieren

Wenn eine neue Version erscheint:

1. Das neueste Installationsprogramm aus **GitHub Releases** herunterladen.
2. Das Installationsprogramm ausführen.
3. PoE2 Route Setup öffnen.
4. Die Route erneut erzeugen.

Das persönliche LiveSplit-Layout muss nicht ersetzt werden.

---

# Fehlerbehebung

## Bosse lösen keine Splits aus

Prüfe Folgendes:

* BossWatcher läuft.
* BossWatcher wurde aus PoE2 Route Setup gestartet.
* Die gewählte Route enthält tatsächlich Bossziele.
* LiveSplits Scriptable Auto Splitter zeigt auf die erzeugte `.asl`.

---

## Gebiete lösen keine Splits aus

Prüfe Folgendes:

* Path of Exile 2 läuft.
* LiveSplits Scriptable Auto Splitter zeigt auf die richtige `.asl`.
* Die richtige Erkundungsroute wurde erzeugt.
* Die richtige `.lss` ist geladen.

---

## LiveSplit öffnet die falschen Splits

Öffne die `.lss` direkt aus:

`LiveSplit Target`

oder verwende:

**File → Open Splits → From File**

---

## Nach einem Routenwechsel funktioniert etwas nicht mehr

Erzeuge die neue Route erneut und prüfe:

* Die richtige `.lss` ist geladen.
* Scriptable Auto Splitter zeigt auf die aktuelle `.asl` in **LiveSplit Target**.

---

## BossWatcher zeigt einen Fehler

BossWatcher schließen und über **Start BossWatcher** in PoE2 Route Setup erneut starten.

Wenn das Problem weiter besteht, den angezeigten Fehler bei der Fehlermeldung mit angeben.

---
## BossWatcher splittet zu früh oder beim Tod des Spielers

BossWatcher registriert, wenn die Boss-Lebensleiste vom Bildschirm verschwindet. Dies kann verschiedene Ursachen haben. Der Benutzer muss entscheiden, ob der Split korrekt war. Standardmäßig wird angenommen, dass der Boss gestorben ist, und der Split wird ausgelöst. Falls der Split ohne abgeschlossenen Bosskampf erfolgt, kann der Split rückgängig gemacht werden. LiveSplit kehrt dadurch in den vorherigen Zustand zurück und der Boss kann mit der aktuellen Zeit erneut versucht werden. Der Hotkey zum Rückgängigmachen eines Splits befindet sich in den LiveSplit-Einstellungen.

---

# Für LiveSplit erzeugte Dateien

Abhängig von der ausgewählten Route kann **LiveSplit Target** enthalten:

### `.lss`

Die LiveSplit-Splitliste.

### `.asl`

Das Autosplitter-Skript für LiveSplits Scriptable-Auto-Splitter-Komponente.

### Routen-/Konfigurationsdateien

Sie legen fest, welche Gebiete und/oder Bosse zur ausgewählten Route gehören.

### Boss-Ereignisdateien

Werden von BossWatcher und bossfähigen Autosplittern verwendet.

Diese Dateien nicht manuell bearbeiten, außer du weißt genau, was du änderst.

Im normalen Betrieb werden sie über **PoE2 Route Setup** erzeugt.

---

# Wichtig

PoE2 Route AutoSplitter steuert oder ersetzt dein persönliches LiveSplit-Layout **nicht**.

Du bist selbst verantwortlich für:

* Timer-Darstellung
* Split-Farben
* Schriftarten
* Fenstergröße
* Vergleichseinstellungen
* Andere LiveSplit-Komponenten

PoE2 Route AutoSplitter stellt nur die Routen-Splits und die Autosplitter-Konfiguration bereit.

---

# Probleme melden

Bitte bei einer Problemmeldung angeben:

* Version von PoE2 Route AutoSplitter
* Verwendete Route/verwendeter Modus
* Ob BossWatcher lief
* Erwartetes Verhalten
* Tatsächliches Verhalten
* Fehlermeldungen von PoE2 Route Setup, BossWatcher oder LiveSplit

Dadurch lassen sich Probleme deutlich leichter reproduzieren und beheben.

---

# Paketprüfung und Diagnose

SHA-256-Manifeste zur Überprüfung von Release- und Runtime-Dateien befinden sich in:

`3 - verification files`

Dort werden außerdem Setup-Prüfmanifeste, SHA-256-Manifeste pro Run, Audit-Protokolle und lesbare Run-Zusammenfassungen gespeichert. Sie liegen außerhalb von `LiveSplit Target`, damit das Erzeugen einer neuen Route frühere Run-Auditdateien nicht löscht.

Diagnoseprotokolle von SetupUI, BossWatcher und GameTimeWatcher werden zentral gespeichert unter:

`4-README's_and_Diagnostics\Diagnostics`

Diagnose-PNGs werden gespeichert unter:

`4-README's_and_Diagnostics\Diagnostics\images`

---

# Aktuelle Hauptversion

**PoE2 Route AutoSplitter 3.x**

Version 3 ergänzt mehrsprachige SetupUI- und Spielsprachenunterstützung, verifizierte lokalisierte Boss- und Gebietsnamen, erweiterte Regeln für Kampagne, Trials, Vaal Ruins und Maps, zentralisierte Diagnose- und Prüfdaten sowie eine adaptive, höhenbasierte BossWatcher-Erfassungsgeometrie für normale 16:9-, Ultrawide- und Super-Ultrawide-Spielclients.
