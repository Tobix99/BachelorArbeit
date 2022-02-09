# BachelorArbeit

In diesem Repository befindet sich der Quellcode der Bachelorarbeit von Tobias Heinze. Dabei ist das Repository in 3 Unterordner  unterteil:

- Applet
- Host
- Virutal PCSC

## Applet 

In diesem Ordner befindet sich der JavaCard-Quellcode der 2 Umsetzungen: "HelloWorld" und der "konzeptionelle Beweis"

## Host

In diesem Ordner befindet sich das Visual-Studio Projekt der Hostanwendung. Dieses wurde mit Visual-Studio 2022 erstelt. Die genau beschreibung der Klassen befindet sich dabei in der Abschlussarbeit.

## Virtual PCSC

In diesem Ordner ist das Start-Skript für den Simulator abgelegt. Dabei muss vorher der Simulator "jcardsim-3.0.5-SNAPSHOT.jar" von [hier](https://github.com/licel/jcardsim) heruntergeladen und einfach in diesem Verzeichnis abgelegt werden. Des Weiteren sind hier 2 "ZIP"-Dateien abgelegt, welche die ".class"-Dateien zum starten des Simualtors beinhlaten.

Wichtig:
Dabeo sollte nach dem Start nicht vergessen werden, das Applet mithilfe der Host-Anwednung in dem Simulator zu installieren. Dafür im Hauptmenu der Host-Anwendung "4" drücken und das zu installierende Applet auswählen.

Um das zu ladene Applet zu ändern, bitte die enstsprechenden Zeilen in "Virtual PCSC/jcardsim.cfg" ändern


### Speedtest

Der die Speedtest.zip kann hierbei auch in den Ordner Virtual PCSC abgelegt werden. Hierbei ist es wichtig, die "start_speedtest.bat" auszuführen, damit diese ".ZIP"-Datei beim Laden berücksichtigt wird.