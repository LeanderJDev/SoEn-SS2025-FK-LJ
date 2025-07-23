function Test-FileUtf8WithLine {
    param([string]$path)

    $bytes = [System.IO.File]::ReadAllBytes($path)
    $utf8 = New-Object System.Text.UTF8Encoding($false, $true)  # throwOnInvalidBytes = $true

    try {
        $utf8.GetString($bytes) | Out-Null
        return $null  # Kein Fehler
    } catch [System.Text.DecoderFallbackException] {
        $errorIndex = $_.Exception.Index

        # Datei als Bytes lesen und bis zum Fehlerindex in Text umwandeln
        $beforeErrorBytes = $bytes[0..($errorIndex - 1)]
        $textBeforeError = $utf8.GetString($beforeErrorBytes)

        # Zeilennummer ermitteln (Anzahl Zeilenumbrüche + 1)
        $lineNumber = ($textBeforeError -split "`n").Count

        return $lineNumber
    }
}

$ordner = Get-Location
$fehlerGefunden = $false

Get-ChildItem -Path $ordner -Recurse -Filter "*.cs" | ForEach-Object {
    $line = Test-FileUtf8WithLine -path $_.FullName
    if ($line -ne $null) {
        Write-Host "Ungültige UTF-8 Bytes in Datei: $($_.FullName) in Zeile: $line"
        $fehlerGefunden = $true
    }
}

if (-not $fehlerGefunden) {
    Write-Host "Alle .cs-Dateien sind UTF-8-kodiert."
}