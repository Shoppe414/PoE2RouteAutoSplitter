param(
    [switch]$AllowStale
)

$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $MyInvocation.MyCommand.Path
$supportRoot = Split-Path -Parent $root
$sourceRoot = Join-Path $root 'src\PoE2RouteSetup'
$outputRoot = Join-Path $sourceRoot 'ProperNouns'
$manifestPath = Join-Path $outputRoot 'proper-nouns-manifest.json'
$bossLocalizationPath = Join-Path $supportRoot 'BossWatcher\boss-localizations.json'

# These are the eight non-English languages currently supported by the PoE2
# international client. SetupUI uses this same set, so every selectable non-English
# UI/game language receives an authoritative proper-noun catalog.
$languageCandidates = [ordered]@{
    'fr'    = @('fr')
    'de'    = @('de')
    'es-ES' = @('sp', 'es')
    'ja'    = @('jp', 'ja')
    'ko'    = @('kr', 'ko')
    'pt-BR' = @('pt', 'po')
    'ru'    = @('ru')
    'th'    = @('th')
}

$poe2dbLanguageCodes = @('tw', 'cn', 'us', 'kr', 'ko', 'jp', 'ja', 'ru', 'po', 'pt', 'th', 'fr', 'de', 'sp', 'es')

function Get-WebText([string]$Url, [string]$Referer = 'https://poe2db.tw/us/') {
    $headers = @{
        'User-Agent' = 'Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 Chrome/138 Safari/537.36 PoE2RouteAutoSplitter/3.0.0'
        'Accept' = '*/*'
        'Referer' = $Referer
    }

    # Windows PowerShell 5.1 may decode Invoke-WebRequest.Content with a legacy
    # single-byte code page when a CDN response omits/varies its charset header.
    # That turns valid UTF-8 game names into mojibake such as "PrÃ©teur" and can
    # completely corrupt Japanese/Korean/Thai names. Download raw bytes and decode
    # them explicitly as strict UTF-8 instead.
    $tempPath = Join-Path ([IO.Path]::GetTempPath()) ('PoE2AS-web-' + [Guid]::NewGuid().ToString('N') + '.tmp')
    try {
        Invoke-WebRequest -UseBasicParsing -Uri $Url -Headers $headers -TimeoutSec 30 -OutFile $tempPath | Out-Null
        if (-not (Test-Path -LiteralPath $tempPath -PathType Leaf)) {
            throw "Download did not create a response file for $Url"
        }
        $bytes = [IO.File]::ReadAllBytes($tempPath)
        if ($bytes.Length -eq 0) { throw "Empty response while fetching $Url" }
        $utf8 = New-Object System.Text.UTF8Encoding($false, $true)
        $text = $utf8.GetString($bytes)
        if ($text.Length -gt 0 -and $text[0] -eq [char]0xFEFF) { $text = $text.Substring(1) }
        return $text
    }
    finally {
        Remove-Item -LiteralPath $tempPath -Force -ErrorAction SilentlyContinue
    }
}

function Get-ObjectProperty([object]$Object, [string[]]$Names) {
    if ($null -eq $Object) { return $null }
    foreach ($name in $Names) {
        $property = $Object.PSObject.Properties[$name]
        if ($null -ne $property) { return $property.Value }
    }
    return $null
}

function Convert-ToCatalogItems([object]$Parsed) {
    if ($null -eq $Parsed) { return @() }

    # PoE2DB autocomplete has historically been a top-level array. Keep support for
    # wrapper objects as a defensive compatibility path if the CDN schema changes.
    if ($Parsed -is [System.Array]) { return @($Parsed) }

    foreach ($wrapperName in @('data', 'items', 'results', 'entries', 'autocomplete')) {
        $wrapped = Get-ObjectProperty $Parsed @($wrapperName)
        if ($null -ne $wrapped) {
            if ($wrapped -is [System.Array]) { return @($wrapped) }
            if ($wrapped -is [System.Collections.IEnumerable] -and -not ($wrapped -is [string])) { return @($wrapped) }
        }
    }

    # A single autocomplete item is valid input too.
    $singleLabel = Get-ObjectProperty $Parsed @('label', 'name', 'text', 'title')
    $singleValue = Get-ObjectProperty $Parsed @('value', 'url', 'path', 'href')
    if ($null -ne $singleLabel -or $null -ne $singleValue) { return @($Parsed) }

    return @()
}

function Get-CatalogLabel([object]$Item) {
    $value = Get-ObjectProperty $Item @('label', 'name', 'text', 'title')
    if ($null -eq $value) { return '' }
    $text = [string]$value
    $text = [Net.WebUtility]::HtmlDecode($text)
    $text = $text -replace '<[^>]+>', ' '
    $text = ($text -replace '\s+', ' ').Trim()

    # Defensive cleanup for catalog rows that may embed the same area-details markup
    # seen on localized area pages. Never let route metadata become a display name.
    $metadataMatch = [regex]::Match($text, '\s+(?:Id|Act|Area\s+Level|Connections)\s*:', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if ($metadataMatch.Success) {
        $text = $text.Substring(0, $metadataMatch.Index).Trim()
        $tokens = @($text -split '\s+' | Where-Object { $_.Length -gt 0 })
        if ($tokens.Count -ge 2 -and ($tokens.Count % 2) -eq 0) {
            $half = [int]($tokens.Count / 2)
            $first = (@($tokens[0..($half - 1)]) -join ' ')
            $second = (@($tokens[$half..($tokens.Count - 1)]) -join ' ')
            if ($first -ceq $second) { $text = $first }
        }
    }
    return $text.Trim()
}

function Test-ProperNounValue([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return $false }
    $text = $Value.Trim()
    if ($text.Length -gt 160) { return $false }
    if ($text -match '(?i)(?:^|\s)(?:Id|Connections)\s*:') { return $false }
    return $true
}

function Get-CatalogValue([object]$Item) {
    $value = Get-ObjectProperty $Item @('value', 'url', 'path', 'href')
    if ($null -eq $value) { return '' }
    return ([string]$value).Trim()
}

function Normalize-Path([object]$Value) {
    if ($null -eq $Value) { return '' }
    $text = ([string]$Value).Trim()
    $text = [Net.WebUtility]::HtmlDecode($text)
    $text = $text -replace '\\/', '/'
    $text = ($text -split '[?#]', 2)[0]
    try { $text = [Uri]::UnescapeDataString($text) } catch { }

    # Accept absolute, protocol-relative and relative values. The language segment is
    # transport metadata, not part of the canonical PoE2DB entry identity.
    $text = $text -replace '^https?://(?:www\.)?poe2db\.tw/', ''
    $text = $text -replace '^//(?:www\.)?poe2db\.tw/', ''
    $text = $text.TrimStart('/')

    if ($text -match '^([^/]+)/(.+)$') {
        $first = $matches[1]
        if ($poe2dbLanguageCodes -contains $first.ToLowerInvariant()) {
            $text = $matches[2]
        }
    }

    $text = $text.TrimStart('/')
    return $text.ToLowerInvariant()
}

function Normalize-Label([object]$Value) {
    if ($null -eq $Value) { return '' }
    $text = [Net.WebUtility]::HtmlDecode(([string]$Value).Trim())
    $text = $text -replace '<[^>]+>', ''
    $text = $text -replace '[\u2018\u2019\u02BC]', "'"
    $text = $text -replace '[\u2013\u2014]', '-'
    $text = $text -replace '\s+', ' '
    return $text.ToLowerInvariant()
}

function Get-TargetSlugCandidates([string]$Target) {
    $set = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::OrdinalIgnoreCase)
    $text = $Target.Trim()
    if ($text.Length -eq 0) { return @() }

    foreach ($candidate in @(
        $text,
        ($text -replace ' ', '_'),
        ($text -replace '\s+', '_'),
        (($text -replace '[\u2018\u2019\u02BC]', "'") -replace '\s+', '_')
    )) {
        $normalized = Normalize-Path $candidate
        if ($normalized.Length -gt 0) { [void]$set.Add($normalized) }
    }
    return @($set)
}

# PoE2DB's autocomplete catalog does not reliably expose every campaign/map area
# or every Pinnacle/encounter boss display name. For known route proper nouns, fall
# back to the localized PoE2DB entry page and read its localized page title. The
# canonical English name remains the route identity; page-title localization is display-only.
$pageTitleCache = @{}

function Get-AreaPageLookupTarget([string]$Target) {
    if ([string]::IsNullOrWhiteSpace($Target)) { return '' }
    $value = $Target.Trim()

    # "(blocked)" is a route-state qualifier, not part of the authoritative PoE2
    # area name or page slug. Resolve the real area and re-apply the qualifier only
    # for SetupUI display. This prevents a synthetic slug from landing on a broad
    # search/details page whose heading also contains the area's metadata table.
    $value = $value -replace '\s+\(blocked\)$', ''
    return $value.Trim()
}

function Get-TargetPageSlug([string]$Target) {
    $slug = (($Target.Trim() -replace '[\u2018\u2019\u02BC]', "'") -replace '\s+', '_')
    if ([string]::IsNullOrWhiteSpace($slug)) { return '' }
    $escaped = [Uri]::EscapeDataString($slug)
    # Preserve the underscore form used by PoE2DB canonical page paths.
    return $escaped -replace '%5F', '_'
}

function Convert-PageTitleCandidate([string]$Value) {
    if ([string]::IsNullOrWhiteSpace($Value)) { return '' }
    $value = [Net.WebUtility]::HtmlDecode($Value)
    $value = $value -replace '<[^>]+>', ' '
    $value = $value -replace '\s+', ' '
    $value = $value.Trim()
    # Standard PoE2DB title form: "Localized Name - PoE2DB, Path of Exile Wiki xx".
    $value = $value -replace '\s+-\s+PoE2DB(?:,.*)?$', ''
    # Heading fallbacks can include the table suffix, e.g. "Localized Name Attr /7".
    $value = $value -replace '\s+Attr\s*/\d+\s*$', ''

    # Some area pages place the name and an area-details table in one heading. When
    # tags are flattened that becomes "Name Name Id: ... Act: ... Connections: ...".
    # Strip the metadata boundary before this value can enter the authoritative map.
    $value = $value -replace '\s+(?:Id|Act|Area\s+Level|Connections)\s*:.*$', ''
    $value = ($value -replace '\s+', ' ').Trim()

    # The same malformed heading duplicates the localized name before the metadata.
    # Collapse exact repeated token halves, e.g. "할라니 관문 할라니 관문".
    $tokens = @($value -split '\s+' | Where-Object { $_.Length -gt 0 })
    if ($tokens.Count -ge 2 -and ($tokens.Count % 2) -eq 0) {
        $half = [int]($tokens.Count / 2)
        $first = (@($tokens[0..($half - 1)]) -join ' ')
        $second = (@($tokens[$half..($tokens.Count - 1)]) -join ' ')
        if ($first -ceq $second) { $value = $first }
    }

    return $value.Trim()
}

function Get-LocalizedAreaPageTitle([string]$PoeCode, [string]$Target, [string]$Referer) {
    $cacheKey = $PoeCode + '|' + $Target
    if ($pageTitleCache.ContainsKey($cacheKey)) { return [string]$pageTitleCache[$cacheKey] }

    $slug = Get-TargetPageSlug $Target
    if ([string]::IsNullOrWhiteSpace($slug)) {
        $pageTitleCache[$cacheKey] = ''
        return ''
    }

    $url = 'https://poe2db.tw/' + $PoeCode + '/' + $slug
    try {
        $html = Get-WebText $url $Referer
    }
    catch {
        # Missing/synthetic areas are expected to fail here; policy is English fallback.
        $pageTitleCache[$cacheKey] = ''
        return ''
    }

    $candidates = New-Object System.Collections.ArrayList
    foreach ($pattern in @(
        # Prefer page metadata/title nodes. Heading fallbacks are intentionally last
        # because PoE2DB area headings can contain nested area-detail tables.
        # Match the closing attribute quote that opened content=. The previous
        # [^"']+ capture stopped at apostrophes inside perfectly valid double-quoted
        # French titles (for example d'Ogham / L'...), truncating them to "Manoir d"
        # or even a single "L" before they reached SetupUI.
        '<meta[^>]+property=["'']og:title["''][^>]+content=(?<quote>["''])(?<value>.*?)\k<quote>',
        '<meta[^>]+content=(?<quote>["''])(?<value>.*?)\k<quote>[^>]+property=["'']og:title["'']',
        '<title[^>]*>(?<value>.*?)</title>',
        '<h[1-6][^>]*>(?<value>.*?)\s+Attr\s*/\d+\s*</h[1-6]>',
        '<h[1-4][^>]*>(?<value>.*?)</h[1-4]>'
    )) {
        foreach ($match in [regex]::Matches($html, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase -bor [Text.RegularExpressions.RegexOptions]::Singleline)) {
            if ($match.Groups['value'].Success) {
                [void]$candidates.Add($match.Groups['value'].Value)
            }
            elseif ($match.Groups.Count -gt 1) {
                # Compatibility fallback if a future pattern uses a positional group.
                [void]$candidates.Add($match.Groups[1].Value)
            }
        }
    }

    foreach ($candidateRaw in @($candidates)) {
        $candidate = Convert-PageTitleCandidate ([string]$candidateRaw)
        if ([string]::IsNullOrWhiteSpace($candidate)) { continue }
        if ($candidate -match '^(PoE2DB|Path of Exile 2|Path of Exile Wiki)$') { continue }
        if ($candidate -match '(?i)page not found|not found|404') { continue }
        if ($candidate -match '(?i)(?:^|\s)(?:Id|Connections)\s*:') { continue }
        if ($candidate.Length -gt 160) { continue }
        $pageTitleCache[$cacheKey] = $candidate
        return $candidate
    }

    $pageTitleCache[$cacheKey] = ''
    return ''
}

function Add-Target([System.Collections.Generic.HashSet[string]]$Set, [object]$Value) {
    if ($null -eq $Value) { return }
    $text = ([string]$Value).Trim()
    if ($text.Length -gt 0) { [void]$Set.Add($text) }
}

function Write-Utf8Json([string]$Path, [object]$Object, [int]$Depth = 8) {
    $json = $Object | ConvertTo-Json -Depth $Depth
    $utf8 = New-Object System.Text.UTF8Encoding($false)
    [IO.File]::WriteAllText($Path, $json + [Environment]::NewLine, $utf8)
}

function Get-PathIndex([object[]]$Items) {
    $index = @{}
    foreach ($item in $Items) {
        $path = Normalize-Path (Get-CatalogValue $item)
        if ([string]::IsNullOrWhiteSpace($path)) { continue }
        if (-not $index.ContainsKey($path)) { $index[$path] = New-Object System.Collections.ArrayList }
        [void]$index[$path].Add($item)
    }
    return $index
}

function Get-CatalogDiagnostic([object[]]$Items) {
    if ($Items.Count -eq 0) { return 'items=0' }
    $sample = $Items[0]
    $props = @($sample.PSObject.Properties.Name) -join ','
    $label = Get-CatalogLabel $sample
    $value = Get-CatalogValue $sample
    if ($label.Length -gt 80) { $label = $label.Substring(0, 80) }
    if ($value.Length -gt 120) { $value = $value.Substring(0, 120) }
    return "items=$($Items.Count); properties=[$props]; sampleLabel='$label'; sampleValue='$value'; normalizedPath='$(Normalize-Path $value)'"
}

$targets = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
$areaPageFallbackTargets = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
# Some Pinnacle boss entries are present in the localized autocomplete catalogs but
# retain their English display label there even though the boss page itself exposes
# the authoritative localized in-game name. Prefer the localized page title for those
# known Pinnacle identities; fall back to the catalog only when the page cannot resolve.
$pinnaclePagePreferredTargets = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
foreach ($csvPath in @(
    (Join-Path $supportRoot '01-Ordered\zones.csv'),
    (Join-Path $supportRoot '02-Flexible\zones.csv')
)) {
    if (-not (Test-Path -LiteralPath $csvPath -PathType Leaf)) { continue }
    foreach ($row in (Import-Csv -LiteralPath $csvPath)) {
        Add-Target $targets $row.AreaName
        Add-Target $areaPageFallbackTargets $row.AreaName
    }
}

$bossesPath = Join-Path $supportRoot 'BossWatcher\bosses.txt'
$pinnacleBossIds = @(
    'atziri_red_queen', 'the_aberration', 'arbiter_of_ash', 'arbiter_of_divinity', 'the_bodach',
    'raven_trickster', 'the_trialmaster', 'vessel_of_kulemak', 'xesht_we_that_are_one', 'zarokh_temporal'
)
$bossLocalizationSources = New-Object System.Collections.ArrayList
if (Test-Path -LiteralPath $bossesPath -PathType Leaf) {
    foreach ($line in (Get-Content -LiteralPath $bossesPath -Encoding UTF8)) {
        $trimmed = $line.Trim()
        if ($trimmed.Length -eq 0 -or $trimmed.StartsWith('#')) { continue }
        $parts = $trimmed.Split('|')
        if ($parts.Length -ge 2) {
            Add-Target $targets $parts[1]
            if ($pinnacleBossIds -contains $parts[0].Trim()) {
                Add-Target $areaPageFallbackTargets $parts[1]
                Add-Target $pinnaclePagePreferredTargets $parts[1]
            }
            [void]$bossLocalizationSources.Add([pscustomobject]@{
                Id = $parts[0].Trim()
                Name = $parts[1].Trim()
            })
        }
    }
}

$mapDbPath = Join-Path $supportRoot 'BossWatcher\map-bosses.json'
if (Test-Path -LiteralPath $mapDbPath -PathType Leaf) {
    $mapDb = (Get-Content -LiteralPath $mapDbPath -Raw -Encoding UTF8) | ConvertFrom-Json
    foreach ($map in @($mapDb.Maps)) {
        Add-Target $targets $map.MapName
        Add-Target $areaPageFallbackTargets $map.MapName
        foreach ($boss in @($map.Bosses)) { Add-Target $targets $boss.Name }
    }
    foreach ($boss in @($mapDb.EventBosses)) { Add-Target $targets $boss.Name }
}

# Stable game proper nouns used by SetupUI policy panels but not guaranteed to be in zones.csv.
foreach ($extra in @('Trial of the Sekhemas', 'The Trial of Chaos', 'The Temple of Chaos', "Atziri's Temple", 'Vaal Ruins', 'Royal Architect', 'Atziri')) {
    Add-Target $targets $extra
}

New-Item -ItemType Directory -Force -Path $outputRoot | Out-Null
# Keep the staging directory deliberately short. Windows PowerShell 5.1 / .NET Framework
# still hits the classic MAX_PATH boundary for System.IO.WriteAllText on some systems.
# A long extracted project path plus the old '.refresh-<32-char-guid>' directory could
# make proper-nouns-manifest.json land at exactly 260 characters even though the final
# ProperNouns path itself was valid. Use an 8-character nonce instead; staging remains
# beside the destination files, so the existing move/replace behavior stays on one volume.
$refreshNonce = [Guid]::NewGuid().ToString('N').Substring(0, 8)
$tempRoot = Join-Path $outputRoot ('.r-' + $refreshNonce)

try {
    Write-Host "Refreshing authoritative PoE2 proper nouns from PoE2DB ($($targets.Count) canonical targets)..."
    $homeUrl = 'https://poe2db.tw/us/'
    $homeHtml = Get-WebText $homeUrl $homeUrl
    $headerMatch = [regex]::Match($homeHtml, 'https://cdn\.poe2db\.tw/js/poedb_header\.[a-f0-9]+\.js', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $headerMatch.Success) { throw 'PoE2DB header script URL was not found on the homepage.' }
    $headerUrl = $headerMatch.Value
    $headerJs = Get-WebText $headerUrl $homeUrl

    $autocompleteUrls = [ordered]@{}
    $datasets = @{}
    $resolvedPoeCodes = [ordered]@{}

    # English is the canonical path/name index.
    $englishMatch = [regex]::Match($headerJs, 'autocompletecb_us\.[a-z0-9]+\.json', [Text.RegularExpressions.RegexOptions]::IgnoreCase)
    if (-not $englishMatch.Success) { throw "PoE2DB English autocomplete file was not found in $headerUrl" }
    $englishUrl = 'https://cdn.poe2db.tw/json/' + $englishMatch.Value
    $autocompleteUrls['us'] = $englishUrl
    Write-Host '  downloading us catalog...'
    $englishParsed = (Get-WebText $englishUrl $homeUrl) | ConvertFrom-Json
    $datasets['us'] = @(Convert-ToCatalogItems $englishParsed)
    if ($datasets['us'].Count -eq 0) { throw "PoE2DB 'us' autocomplete catalog did not contain a recognized item array." }
    Write-Host ('    us schema: ' + (Get-CatalogDiagnostic $datasets['us']))

    foreach ($uiCode in $languageCandidates.Keys) {
        $selectedPoeCode = $null
        $selectedMatch = $null
        foreach ($candidate in @($languageCandidates[$uiCode])) {
            $pattern = 'autocompletecb_' + [regex]::Escape($candidate) + '\.[a-z0-9]+\.json'
            $m = [regex]::Match($headerJs, $pattern, [Text.RegularExpressions.RegexOptions]::IgnoreCase)
            if ($m.Success) {
                $selectedPoeCode = $candidate
                $selectedMatch = $m
                break
            }
        }
        if ($null -eq $selectedPoeCode -or $null -eq $selectedMatch) {
            throw "PoE2DB autocomplete file for SetupUI language '$uiCode' was not found. Tried: $(@($languageCandidates[$uiCode]) -join ', ')"
        }
        $resolvedPoeCodes[$uiCode] = $selectedPoeCode
        $url = 'https://cdn.poe2db.tw/json/' + $selectedMatch.Value
        $autocompleteUrls[$selectedPoeCode] = $url
        Write-Host "  downloading $selectedPoeCode catalog for $uiCode..."
        $parsed = (Get-WebText $url $homeUrl) | ConvertFrom-Json
        $datasets[$selectedPoeCode] = @(Convert-ToCatalogItems $parsed)
        if ($datasets[$selectedPoeCode].Count -eq 0) { throw "PoE2DB '$selectedPoeCode' autocomplete catalog did not contain a recognized item array." }
        Write-Host ("    {0} schema: {1}" -f $selectedPoeCode, (Get-CatalogDiagnostic $datasets[$selectedPoeCode]))
    }

    $targetByNormalized = @{}
    foreach ($target in $targets) {
        $key = Normalize-Label $target
        if (-not $targetByNormalized.ContainsKey($key)) { $targetByNormalized[$key] = New-Object System.Collections.ArrayList }
        [void]$targetByNormalized[$key].Add($target)
    }

    # Find canonical PoE2DB paths from the English catalog. Prefer exact labels, then
    # fall back to canonical URL slug matching. This keeps English strings as the source
    # identity while tolerating markup or catalog-label formatting changes.
    $englishCandidates = @{}
    foreach ($item in $datasets['us']) {
        $labelKey = Normalize-Label (Get-CatalogLabel $item)
        $path = Normalize-Path (Get-CatalogValue $item)
        if ([string]::IsNullOrWhiteSpace($path)) { continue }

        if ($targetByNormalized.ContainsKey($labelKey)) {
            foreach ($target in @($targetByNormalized[$labelKey])) {
                if (-not $englishCandidates.ContainsKey($target)) { $englishCandidates[$target] = New-Object System.Collections.ArrayList }
                if (-not $englishCandidates[$target].Contains($path)) { [void]$englishCandidates[$target].Add($path) }
            }
        }
    }

    $englishPathIndex = Get-PathIndex $datasets['us']
    foreach ($target in $targets) {
        if ($englishCandidates.ContainsKey($target) -and $englishCandidates[$target].Count -gt 0) { continue }
        foreach ($candidatePath in @(Get-TargetSlugCandidates $target)) {
            if (-not $englishPathIndex.ContainsKey($candidatePath)) { continue }
            if (-not $englishCandidates.ContainsKey($target)) { $englishCandidates[$target] = New-Object System.Collections.ArrayList }
            if (-not $englishCandidates[$target].Contains($candidatePath)) { [void]$englishCandidates[$target].Add($candidatePath) }
        }
    }

    Write-Host ("  canonical English paths: {0}/{1}" -f $englishCandidates.Count, $targets.Count)
    if ($englishCandidates.Count -lt 20) {
        throw (("PoE2DB English catalog resolved only {0}/{1} canonical targets. " +
            "This indicates a catalog schema/path mismatch. {2}") -f $englishCandidates.Count, $targets.Count, (Get-CatalogDiagnostic $datasets['us']))
    }

    $coverage = [ordered]@{}
    $missingByLanguage = [ordered]@{}
    $pendingOutputs = [ordered]@{}
    foreach ($uiCode in $languageCandidates.Keys) {
        $poeCode = $resolvedPoeCodes[$uiCode]
        $pathIndex = Get-PathIndex $datasets[$poeCode]
        $resolved = @{}
        $missing = New-Object System.Collections.ArrayList
        $ambiguous = New-Object System.Collections.ArrayList
        $areaPageFallbackAttempted = 0
        $areaPageFallbackResolved = 0
        $pinnaclePagePreferredResolved = 0

        foreach ($target in @($targets | Sort-Object)) {
            # Build-Release.ps1 enables StrictMode. An if-expression unwraps a one-item
            # collection when its output is assigned, which can turn $paths into a scalar
            # string. Under StrictMode a scalar string has no Count property, so normalize
            # the collection with a separate assignment before testing Count.
            $paths = @()
            if ($englishCandidates.ContainsKey($target)) {
                $paths = @($englishCandidates[$target])
            }
            if ($paths.Count -eq 0 -and -not $areaPageFallbackTargets.Contains($target)) {
                [void]$missing.Add($target)
                continue
            }

            $labels = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
            foreach ($path in $paths) {
                if (-not $pathIndex.ContainsKey($path)) { continue }
                foreach ($item in @($pathIndex[$path])) {
                    $label = Get-CatalogLabel $item
                    if (Test-ProperNounValue $label) { [void]$labels.Add($label) }
                }
            }

            # Campaign/map areas use a page-title fallback when the localized
            # autocomplete catalog has no usable label. Known Pinnacle bosses are
            # deliberately page-preferred: PoE2DB's autocomplete can expose the
            # canonical English boss label in a non-English catalog even while the
            # boss page has the authoritative localized display name. In that case,
            # accepting the autocomplete label makes the Maps endpoint dropdown stay
            # English despite a successful refresh.
            $preferPageTitle = $pinnaclePagePreferredTargets.Contains($target)
            if ($preferPageTitle -or ($labels.Count -eq 0 -and $areaPageFallbackTargets.Contains($target))) {
                $areaPageFallbackAttempted++
                $pageLookupTarget = Get-AreaPageLookupTarget $target
                $pageTitle = Get-LocalizedAreaPageTitle $poeCode $pageLookupTarget $homeUrl
                if (-not [string]::IsNullOrWhiteSpace($pageTitle)) {
                    if ($target -match '(?i)\s+\(blocked\)$') { $pageTitle += ' (blocked)' }
                    if (Test-ProperNounValue $pageTitle) {
                        if ($preferPageTitle) {
                            $labels.Clear()
                            $pinnaclePagePreferredResolved++
                        }
                        [void]$labels.Add($pageTitle)
                        $areaPageFallbackResolved++
                    }
                }
            }

            if ($labels.Count -eq 1) {
                $resolved[$target] = @($labels)[0]
            }
            elseif ($labels.Count -gt 1) {
                [void]$ambiguous.Add($target)
            }
            else {
                [void]$missing.Add($target)
            }
        }

        $ordered = [ordered]@{}
        foreach ($key in @($resolved.Keys | Sort-Object)) { $ordered[$key] = $resolved[$key] }
        $pendingOutputs[$uiCode] = $ordered
        $coverage[$uiCode] = [ordered]@{
            poe2dbLanguage = $poeCode
            resolved = $resolved.Count
            totalTargets = $targets.Count
            missing = $missing.Count
            ambiguous = $ambiguous.Count
            areaPageFallbackAttempted = $areaPageFallbackAttempted
            areaPageFallbackResolved = $areaPageFallbackResolved
            pinnaclePagePreferredAttempted = $pinnaclePagePreferredTargets.Count
            pinnaclePagePreferredResolved = $pinnaclePagePreferredResolved
        }
        $missingByLanguage[$uiCode] = [ordered]@{
            missing = @($missing)
            ambiguous = @($ambiguous)
        }
        Write-Host ("  {0}: {1}/{2} authoritative names (page title {3}/{4}; Pinnacle page preferred {5}/{6})" -f $uiCode, $resolved.Count, $targets.Count, $areaPageFallbackResolved, $areaPageFallbackAttempted, $pinnaclePagePreferredResolved, $pinnaclePagePreferredTargets.Count)
    }

    # Validate every language before touching the checked-in/embedded resources. The old
    # implementation wrote 0-entry JSON files first and only then threw, which made
    # -AllowStale retain the broken files on the next attempt.
    foreach ($uiCode in $languageCandidates.Keys) {
        if ([int]$coverage[$uiCode].resolved -lt 20) {
            $poeCode = $coverage[$uiCode].poe2dbLanguage
            throw (("Authoritative proper-noun refresh for '{0}' resolved only {1} names; refusing to replace the existing catalog. {2}") -f `
                $uiCode, $coverage[$uiCode].resolved, (Get-CatalogDiagnostic $datasets[$poeCode]))
        }
    }

    # Build the BossWatcher localization database from the exact same resolved
    # canonical-name mapping. This keeps SetupUI display names and BossWatcher OCR
    # identities sourced from one authoritative refresh instead of maintaining a
    # separate sparse hand-written catalog. Missing names remain unavailable rather
    # than being guessed or machine-translated.
    $bossEntries = New-Object System.Collections.ArrayList
    $bossCoverage = [ordered]@{}
    foreach ($uiCode in $languageCandidates.Keys) { $bossCoverage[$uiCode] = 0 }
    foreach ($bossSource in @($bossLocalizationSources)) {
        if ([string]::IsNullOrWhiteSpace($bossSource.Id) -or [string]::IsNullOrWhiteSpace($bossSource.Name)) { continue }
        $names = [ordered]@{}
        foreach ($uiCode in $languageCandidates.Keys) {
            $catalog = $pendingOutputs[$uiCode]
            if ($catalog.Contains($bossSource.Name)) {
                $localized = [string]$catalog[$bossSource.Name]
                if (-not [string]::IsNullOrWhiteSpace($localized)) {
                    $names[$uiCode] = @($localized)
                    $bossCoverage[$uiCode] = [int]$bossCoverage[$uiCode] + 1
                }
            }
        }
        if ($names.Count -gt 0) {
            [void]$bossEntries.Add([ordered]@{
                Id = [string]$bossSource.Id
                Names = $names
            })
        }
    }

    foreach ($uiCode in $languageCandidates.Keys) {
        Write-Host ("  {0}: {1}/{2} BossWatcher boss names" -f $uiCode, $bossCoverage[$uiCode], $bossLocalizationSources.Count)
        if ([int]$bossCoverage[$uiCode] -lt 10) {
            throw "Authoritative BossWatcher localization for '$uiCode' resolved only $($bossCoverage[$uiCode]) boss names; refusing to replace the existing database."
        }
    }

    $bossLocalizationDatabase = [ordered]@{
        SchemaVersion = 1
        DatabaseVersion = ('poe2db-' + [DateTime]::UtcNow.ToString('yyyyMMddTHHmmssZ'))
        Purpose = 'Authoritative PoE2 game-client boss names for multilingual BossWatcher OCR, refreshed from PoE2DB by canonical English entry identity. Missing names are omitted; no machine translation.'
        Bosses = @($bossEntries)
    }

    $manifest = [ordered]@{
        schemaVersion = 3
        generatedUtc = [DateTime]::UtcNow.ToString('o')
        source = 'PoE2DB multilingual autocomplete catalogs plus localized campaign/map area and Pinnacle boss entry pages'
        homepage = $homeUrl
        headerScript = $headerUrl
        autocomplete = $autocompleteUrls
        canonicalTargetCount = $targets.Count
        campaignAndMapAreaPageFallbackTargetCount = $areaPageFallbackTargets.Count
        pinnacleBossPagePreferredTargetCount = $pinnaclePagePreferredTargets.Count
        canonicalEnglishPathCount = $englishCandidates.Count
        languages = $coverage
        unresolved = $missingByLanguage
        bossWatcher = [ordered]@{
            sourceBossCount = $bossLocalizationSources.Count
            localizedEntryCount = $bossEntries.Count
            coverage = $bossCoverage
            output = $bossLocalizationPath
        }
        policy = 'Canonical English target -> exact English label or canonical URL slug -> normalized same PoE2DB entry path. Campaign/map area names missing from autocomplete may use the localized title from the same canonical PoE2DB page slug. Known Pinnacle boss identities prefer that localized page title because non-English autocomplete catalogs can retain the English boss label. Missing/ambiguous names remain English in SetupUI and unavailable to non-English BossWatcher OCR; no game proper noun is machine-translated.'
    }

    New-Item -ItemType Directory -Force -Path $tempRoot | Out-Null
    foreach ($uiCode in $languageCandidates.Keys) {
        Write-Utf8Json (Join-Path $tempRoot ($uiCode + '.json')) $pendingOutputs[$uiCode] 4
    }
    Write-Utf8Json (Join-Path $tempRoot 'proper-nouns-manifest.json') $manifest 12
    Write-Utf8Json (Join-Path $tempRoot 'boss-localizations.json') $bossLocalizationDatabase 12

    foreach ($uiCode in $languageCandidates.Keys) {
        Move-Item -LiteralPath (Join-Path $tempRoot ($uiCode + '.json')) -Destination (Join-Path $outputRoot ($uiCode + '.json')) -Force
    }
    Move-Item -LiteralPath (Join-Path $tempRoot 'proper-nouns-manifest.json') -Destination $manifestPath -Force
    Move-Item -LiteralPath (Join-Path $tempRoot 'boss-localizations.json') -Destination $bossLocalizationPath -Force

    Write-Host 'Authoritative proper-noun and BossWatcher localization refresh complete.'
}
catch {
    if ($AllowStale) {
        Write-Warning ("Proper-noun refresh failed; keeping existing embedded resources because -AllowStale was specified. " + $_.Exception.Message)
        return
    }
    throw ("Authoritative PoE2 proper-noun refresh failed. Existing embedded resources were left unchanged. " + $_.Exception.Message)
}
finally {
    if (Test-Path -LiteralPath $tempRoot) {
        Remove-Item -LiteralPath $tempRoot -Recurse -Force -ErrorAction SilentlyContinue
    }
}
