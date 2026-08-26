param(
    [Parameter(Mandatory = $true)]
    [string] $VocabularyPath,

    [string] $EnglishLocalePath = '',

    [string] $OutputDirectory = ''
)

$ErrorActionPreference = 'Stop'

if ([string]::IsNullOrWhiteSpace($EnglishLocalePath)) {
    $EnglishLocalePath = Join-Path $PSScriptRoot '..\..\MyriaLib\Data\locales\en.json'
}
if ([string]::IsNullOrWhiteSpace($OutputDirectory)) {
    $OutputDirectory = Join-Path $PSScriptRoot '..\Data\Mods\tsamwas-language'
}

function Normalize-Term([string] $Value) {
    $normalized = ($Value -replace '\([^)]*\)', '' -replace '[.!?:;]+$', '').Trim().ToLowerInvariant()
    if ($normalized.StartsWith('to ')) { $normalized = $normalized.Substring(3) }
    return $normalized
}

function Try-TranslateValue([string] $Value, [hashtable] $TermMap, [ref] $Translation) {
    $normalized = Normalize-Term $Value
    if ($TermMap.ContainsKey($normalized)) {
        $Translation.Value = $TermMap[$normalized]
        return $true
    }

    # Translate a longer label or sentence only when every English word has an
    # explicit glossary entry. This avoids half-English, half-Tsāmwas output.
    # Punctuation, whitespace, numbers, and .NET format placeholders are kept.
    $matches = [regex]::Matches($Value, "[A-Za-z]+(?:'[A-Za-z]+)?")
    if ($matches.Count -eq 0) { return $false }

    $builder = [System.Text.StringBuilder]::new()
    $position = 0
    foreach ($match in $matches) {
        $word = $match.Value.ToLowerInvariant()
        if (-not $TermMap.ContainsKey($word)) { return $false }
        [void] $builder.Append($Value.Substring($position, $match.Index - $position))
        [void] $builder.Append($TermMap[$word])
        $position = $match.Index + $match.Length
    }
    [void] $builder.Append($Value.Substring($position))
    $Translation.Value = $builder.ToString()
    return $true
}

$sourceLines = Get-Content -LiteralPath $VocabularyPath -Encoding UTF8
$terms = @{}
$knownEnglishWords = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
$tableHeaders = @()
$tsamwasColumn = -1
$englishColumn = -1

foreach ($line in $sourceLines) {
    if (-not $line.StartsWith('|')) {
        $tableHeaders = @()
        $tsamwasColumn = -1
        $englishColumn = -1
        continue
    }
    $cells = @($line.Trim().Trim('|').Split('|') | ForEach-Object { $_.Trim() })
    if ($cells.Count -eq 0 -or ($cells | Where-Object { $_ -notmatch '^:?-+:?$' }).Count -eq 0) { continue }

    # Discover columns by their headers instead of assuming a fixed table shape.
    # Expansion batches use Tsāmwas/English, English/Tsāmwas, and
    # Tsāmwas/Components/English layouts.
    $detectedTsamwasColumn = -1
    for ($headerIndex = 0; $headerIndex -lt $cells.Count; $headerIndex++) {
        if ($cells[$headerIndex] -match 'mwas$') { $detectedTsamwasColumn = $headerIndex; break }
    }
    $detectedEnglishColumn = [array]::IndexOf($cells, 'English')
    if ($detectedTsamwasColumn -ge 0 -and $detectedEnglishColumn -ge 0) {
        $tableHeaders = $cells
        $tsamwasColumn = $detectedTsamwasColumn
        $englishColumn = $detectedEnglishColumn
        continue
    }
    if ($cells -contains 'Place' -and $cells -contains 'Old form') {
        $tableHeaders = $cells
        continue
    }
    if ($tableHeaders.Count -eq 0 -or $cells.Count -lt $tableHeaders.Count) { continue }

    if ($tsamwasColumn -ge 0 -and $englishColumn -ge 0) {
        $tsamwasCell = ($cells[$tsamwasColumn] -replace '[*`]', '').Trim()
        $englishCell = $cells[$englishColumn].Trim()
        if (-not $tsamwasCell -or -not $englishCell -or $tsamwasCell -match '^!\[') { continue }

        $tsamwasVariants = @($tsamwasCell -split '/' | ForEach-Object { $_.Trim() })
        $englishVariants = @($englishCell -split '/' | ForEach-Object { $_.Trim() })
        # A single Tsāmwas form may intentionally cover several English glosses.
        # If both sides contain multiple unequal lists, their pairing is ambiguous.
        if ($tsamwasVariants.Count -ne 1 -and $tsamwasVariants.Count -ne $englishVariants.Count) { continue }

        for ($index = 0; $index -lt $englishVariants.Count; $index++) {
            $normalized = Normalize-Term $englishVariants[$index]
            if (-not $normalized) { continue }
            $tsamwas = if ($tsamwasVariants.Count -eq 1) { $tsamwasVariants[0] } else { $tsamwasVariants[$index] }
            $terms[$normalized] = $tsamwas
            foreach ($word in ([regex]::Matches($normalized, "[a-z]+(?:'[a-z]+)?"))) {
                [void] $knownEnglishWords.Add($word.Value)
            }
        }
        continue
    }

    $placeIndex = [array]::IndexOf($tableHeaders, 'Place')
    $oldFormIndex = [array]::IndexOf($tableHeaders, 'Old form')
    if ($placeIndex -ge 0 -and $oldFormIndex -ge 0) {
        $oldName = Normalize-Term $cells[$oldFormIndex]
        $placeName = ($cells[$placeIndex] -replace '[*`]', '').Trim()
        if ($oldName -and $placeName) { $terms[$oldName] = $placeName }
    }
}

$englishLocale = Get-Content -LiteralPath $EnglishLocalePath -Raw -Encoding UTF8 | ConvertFrom-Json
$translatedLocale = [ordered]@{}
$translatedKeys = [System.Collections.Generic.List[object]]::new()
$untranslatedKeys = [System.Collections.Generic.List[object]]::new()

foreach ($property in $englishLocale.PSObject.Properties) {
    $english = [string] $property.Value
    $translated = ''
    if (Try-TranslateValue $english $terms ([ref] $translated)) {
        $translatedLocale[$property.Name] = $translated
        $translatedKeys.Add([pscustomobject]@{ Key = $property.Name; English = $english; Tsamwas = $translated })
    }
    else {
        # A complete key set is essential: Localization has no per-key fallback.
        # Keep English here until the vocabulary supports an intentional translation.
        $translatedLocale[$property.Name] = $english
        $untranslatedKeys.Add([pscustomobject]@{ Key = $property.Name; English = $english })
    }
}

$localeDirectory = Join-Path $OutputDirectory 'Data\locales'
[System.IO.Directory]::CreateDirectory($localeDirectory) | Out-Null
$utf8 = [System.Text.UTF8Encoding]::new($false)
$localeJson = $translatedLocale | ConvertTo-Json -Depth 4
[System.IO.File]::WriteAllText((Join-Path $localeDirectory 'tsamwas.json'), $localeJson + [Environment]::NewLine, $utf8)

$stopWords = [System.Collections.Generic.HashSet[string]]::new([System.StringComparer]::OrdinalIgnoreCase)
@('a','an','the','and','or','but','if','then','than','that','this','these','those','to','of','in','on','at','by','for','from','with','without','into','out','up','down','over','under','through','between','as','is','are','was','were','be','been','being','it','its','you','your','yours','he','she','they','them','their','we','our','i','me','my','who','which','what','when','where','why','how','not','no','yes','can','could','may','might','will','would','should','do','does','did','has','have','had','here','there','again','all','any','some','only','more','most','very') | ForEach-Object { [void] $stopWords.Add($_) }

$missing = @{}
foreach ($entry in $untranslatedKeys) {
    $plain = $entry.English -replace '\{[^}]+\}', ' '
    foreach ($match in [regex]::Matches($plain.ToLowerInvariant(), "[a-z]+(?:'[a-z]+)?")) {
        $word = $match.Value
        if ($stopWords.Contains($word) -or $knownEnglishWords.Contains($word) -or $word.Length -lt 2) { continue }
        if (-not $missing.ContainsKey($word)) {
            $missing[$word] = [pscustomobject]@{ Count = 0; Examples = [System.Collections.Generic.List[string]]::new() }
        }
        $missing[$word].Count++
        if ($missing[$word].Examples.Count -lt 3 -and -not $missing[$word].Examples.Contains($entry.Key)) {
            $missing[$word].Examples.Add($entry.Key)
        }
    }
}

$gapPath = Join-Path (Split-Path -Parent (Resolve-Path -LiteralPath $VocabularyPath)) 'MISSING_VOCABULARY.md'
$builder = [System.Text.StringBuilder]::new()
[void] $builder.AppendLine('# Tsāmwas vocabulary needed by Myria.Wpf')
[void] $builder.AppendLine()
[void] $builder.AppendLine('This checklist was generated from the English locale and the supplied Tsāmwas reference. The pack intentionally leaves unsupported strings in English so the UI remains usable instead of displaying missing-key markers.')
[void] $builder.AppendLine()
[void] $builder.AppendLine("- Locale keys: $($translatedKeys.Count + $untranslatedKeys.Count)")
[void] $builder.AppendLine("- Direct Tsāmwas translations: $($translatedKeys.Count)")
[void] $builder.AppendLine("- English fallback strings: $($untranslatedKeys.Count)")
[void] $builder.AppendLine("- Distinct missing word forms: $(@($missing.Keys).Count)")
[void] $builder.AppendLine()
[void] $builder.AppendLine('Add the words below to the source vocabulary with an English gloss, then rerun `Tools/GenerateTsamwasLanguagePack.ps1`. Counts indicate priority; example locale keys show the game context. Inflected forms are listed separately when the game uses them separately, so you can decide the correct Tsāmwas grammar.')
[void] $builder.AppendLine()
[void] $builder.AppendLine('| English word/form | Uses | Example locale keys |')
[void] $builder.AppendLine('|---|---:|---|')
foreach ($item in ($missing.GetEnumerator() | Sort-Object @{ Expression = { $_.Value.Count }; Descending = $true }, Name)) {
    [void] $builder.AppendLine("| $($item.Key) | $($item.Value.Count) | $([string]::Join(', ', $item.Value.Examples)) |")
}
[void] $builder.AppendLine()
[void] $builder.AppendLine('## Currently translated locale entries')
[void] $builder.AppendLine()
[void] $builder.AppendLine('| Locale key | English | Tsāmwas |')
[void] $builder.AppendLine('|---|---|---|')
foreach ($entry in ($translatedKeys | Sort-Object Key)) {
    [void] $builder.AppendLine("| $($entry.Key) | $($entry.English -replace '\|','\\|') | $($entry.Tsamwas -replace '\|','\\|') |")
}
[System.IO.File]::WriteAllText($gapPath, $builder.ToString(), $utf8)

Write-Output "Generated $($translatedKeys.Count) direct translations and $($untranslatedKeys.Count) English fallbacks."
Write-Output "Missing vocabulary checklist: $gapPath"
