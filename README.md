# SoEn-SS2025-FK-LJ

## Development

Es gibt im Development Ordner einen `pre-commit` Hook, der automatisch den Code formatiert (hoffentlich). Das sollte die Commits übersichtlicher machen. Lokal muss diese Datei in `.git/hooks/pre-commit` kopiert werden.
Um das Staging nicht kaputt zu machen ist es allerdings so, dass der Hook wenn er Sachen formatieren musste einen Fehler auswirft und man diese Umformatierung dann erneut stagen muss, Ich hätte ein `git add .` einbauen können, allerdings würde der Hook einen dann zwingen immer alle Änderungen zu commiten.
