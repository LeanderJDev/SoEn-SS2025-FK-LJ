# Musikspieler

## Idee

Programmieren eines Musikspieler Programms, welches die Funktionen eines Plattenspielers simuliert. Dabei ist eine einzelne Schallplatte ein Song, welche in verschiedene Schubfächer (Playlisten) einsortiert werden kann. Die Schallplatte kann dann auf den Plattenspieler gelegt und abgespielt werden. Dabei kann der Tonarm frei bewegt werden, um vor- oder zurück zu spulen.

Die genauere Ausführung der Idee ist im [Wiki](https://github.com/LeanderJDev/SoEn-SS2025-FK-LJ/wiki/Konzept) zu finden.

## Installation

Solange noch keine Releases erstellt wurden braucht man Godot 4.4, um das Projekt zu öffnen. Das Projekt kann dann über den Godot Editor gestartet werden.
Geplant sind Releases für Windows und Linux, die dann als Standalone Versionen laufen.

## Development

Es gibt im Development Ordner einen `pre-commit` Hook, der automatisch den Code formatiert (hoffentlich). Das sollte die Commits übersichtlicher machen. Lokal muss diese Datei in `.git/hooks/pre-commit` kopiert werden.
Ggf muss der `pre-commit` Hook noch mit `chmod +x pre-commit` ausführbar gemacht werden.
Um das Staging nicht kaputt zu machen ist es allerdings so, dass der Hook wenn er Sachen formatieren musste einen Fehler auswirft und man diese Umformatierung dann erneut stagen muss, Ich hätte ein `git add .` einbauen können, allerdings würde der Hook einen dann zwingen immer alle Änderungen zu commiten.

## Mitwirkende

<table cellspacing="0" cellpadding="8" style="">
	<tr>
		<td align="center" valign="middle" style="padding: 16px 0; border: 0;">
			<a href="https://github.com/Friedy630" style="display: flex; align-items: center; gap: 16px; text-decoration: none;">
				<img src="https://github.com/Friedy630.png" width="32" style="vertical-align: middle;"/>
				<span style="font-size:1.3em; vertical-align: middle;"><b>@Friedy630</b></span>
			</a>
		</td>
	</tr>
	<tr>
		<td align="center" valign="middle" style="padding: 16px 0; border: 0;">
			<a href="https://github.com/LeanderJDev" style="display: flex; align-items: center; gap: 16px; text-decoration: none;">
				<img src="https://github.com/LeanderJDev.png" width="32" style="vertical-align: middle;"/>
				<span style="font-size:1.3em; vertical-align: middle;"><b>@LeanderJDev</b></span>
			</a>
		</td>
	</tr>
</table>
