<#
.SYNOPSIS
    Migrates CrowdRepo.data from the old monolithic format to the new lean format.

.DESCRIPTION
    Reads the old CrowdRepo.data, writes one <Name>.json per character under
    data/characters/, then writes a lean CrowdRepo.data with only crowd structure.
    Run this ONCE before launching the updated HeroVirtualTabletop application.

.PARAMETER GameDir
    Path to the City of Heroes game directory.
    Defaults to "C:\hero-desktop\City Of Heroes".

.EXAMPLE
    .\MigrateRepo.ps1

.EXAMPLE
    .\MigrateRepo.ps1 -GameDir "D:\Games\City Of Heroes"
#>
param(
    [string]$GameDir = ""
)

$ErrorActionPreference = "Stop"

# ---------------------------------------------------------------------------
# Resolve paths
# ---------------------------------------------------------------------------
if (-not $GameDir) {
    $GameDir = "C:\hero-desktop\City Of Heroes"
}

$dataDir    = Join-Path $GameDir "data"
$charDir    = Join-Path $dataDir "characters"
$repoFile   = Join-Path $dataDir "CrowdRepo.data"
$repoBackup = Join-Path $dataDir "CrowdRepo.data.bak"

Write-Host ""
Write-Host "=== CrowdRepo Migration ===" -ForegroundColor Cyan
Write-Host "Game dir : $GameDir"
Write-Host "Repo     : $repoFile"
Write-Host "Char dir : $charDir"
Write-Host ""

# ---------------------------------------------------------------------------
# Validate
# ---------------------------------------------------------------------------
# If a .bak already exists, migrate FROM the backup (the original old file)
# so we don't accidentally re-migrate an already-lean CrowdRepo.data.
$sourceFile = $repoFile
if (Test-Path $repoBackup) {
    Write-Host "Found backup - migrating from: $repoBackup" -ForegroundColor Cyan
    $sourceFile = $repoBackup
} elseif (-not (Test-Path $repoFile)) {
    Write-Host "ERROR: CrowdRepo.data not found at: $repoFile" -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# Load Newtonsoft.Json
# ---------------------------------------------------------------------------
$scriptRoot = Split-Path -Parent $MyInvocation.MyCommand.Path
$nj = Join-Path $scriptRoot "..\HerovirtualTableTop\HeroVirtualTabletop.WPF\Shell\HeroVirtualTableTop.Shell\bin\Debug\Newtonsoft.Json.dll"
if (-not (Test-Path $nj)) {
    $nj = Join-Path $scriptRoot "..\HerovirtualTableTop\HeroVirtualTabletop.WPF\Shell\HeroVirtualTableTop.Shell\bin\Release\Newtonsoft.Json.dll"
}
if (-not (Test-Path $nj)) {
    Write-Host "ERROR: Newtonsoft.Json.dll not found. Build the solution first." -ForegroundColor Red
    exit 1
}
Add-Type -Path $nj
Write-Host "Loaded: $nj"

# ---------------------------------------------------------------------------
# Read old file as JToken (schema-agnostic)
# ---------------------------------------------------------------------------
Write-Host "Reading: $sourceFile ..."
$rawJson = [System.IO.File]::ReadAllText($sourceFile, [System.Text.Encoding]::UTF8)

$token = [Newtonsoft.Json.Linq.JToken]::Parse($rawJson)

if ($token.Type -ne [Newtonsoft.Json.Linq.JTokenType]::Array) {
    Write-Host "ERROR: CrowdRepo.data is not a JSON array. Already in new format?" -ForegroundColor Red
    exit 1
}

# Detect if source is already in new lean format (has Members, no CrowdMemberCollection)
$firstCrowd = $token[0]
if ($firstCrowd -ne $null) {
    $membersNode = $firstCrowd["Members"]
    $oldNode     = $firstCrowd["CrowdMemberCollection"]
    if ($membersNode -ne $null -and $oldNode -eq $null) {
        Write-Host "Source file is already in the new lean format. Nothing to migrate." -ForegroundColor Yellow
        exit 0
    }
}

# ---------------------------------------------------------------------------
# Build $id -> JObject map from "All Characters" crowd members.
# The old format (PreserveReferencesHandling.Objects) stores full character
# data in "All Characters", and only {"$ref":"N"} in nested crowds.
# Scanning the full 44 MB tree recursively hits PS5 stack limits, so we
# only scan the flat All Characters list.
# ---------------------------------------------------------------------------
Write-Host "Building reference map from All Characters ..."
$refMap = @{}

$dollarId  = '$id'
$dollarRef = '$ref'

foreach ($crowdToken in $token) {
    $nameTok = $crowdToken['Name']
    if ($nameTok -eq $null) { continue }
    if ($nameTok.ToString() -ne 'All Characters') { continue }

    $coll = $crowdToken['CrowdMemberCollection']
    if ($coll -eq $null) { break }
    foreach ($member in $coll) {
        $idTok   = $member[$dollarId]
        $nameProp = $member['Name']
        if ($idTok -ne $null) {
            $refMap[$idTok.ToString()] = $member
        }
    }
    break
}

Write-Host "  Reference map built: $($refMap.Count) entries"

# Resolve a token: if it is a {"$ref":"N"} object, look it up in the map
function Resolve-Ref {
    param($t)
    if ($t -eq $null) { return $null }
    $refTok = $t[$dollarRef]
    if ($refTok -ne $null) {
        $id = $refTok.ToString()
        if ($refMap.ContainsKey($id)) { return $refMap[$id] }
        return $null
    }
    return $t
}

# ---------------------------------------------------------------------------
# Prepare output directory
# ---------------------------------------------------------------------------
if (-not (Test-Path $charDir)) {
    New-Item -ItemType Directory -Path $charDir | Out-Null
    Write-Host "Created directory: $charDir"
}

# ---------------------------------------------------------------------------
# Helper: sanitize a string for use as a file name
# ---------------------------------------------------------------------------
function Get-SafeFileName {
    param([string]$n)
    $invalid = [System.IO.Path]::GetInvalidFileNameChars()
    foreach ($c in $invalid) { $n = $n.Replace([string]$c, '_') }
    return $n
}

# ---------------------------------------------------------------------------
# Converts a CrowdMemberCollection entry JToken to a lean CrowdMemberEntry hashtable.
# Extracts full character data to a file as a side effect.
# ---------------------------------------------------------------------------
$charactersSaved = @{}

function Convert-Member {
    param($memberToken)

    # Resolve $ref references — nested crowd members in the old format are
    # stored as {"$ref":"N"} pointing to the full object in "All Characters"
    $resolved = Resolve-Ref $memberToken
    if ($resolved -eq $null) { return $null }

    $dollarTypeToken = $resolved['$type']
    $typeStr = if ($dollarTypeToken -ne $null) { $dollarTypeToken.ToString() } else { '' }
    $isCrowd = ($typeStr -like '*CrowdModel*') -or ($resolved['CrowdMemberCollection'] -ne $null)

    $nameToken  = $resolved['Name']
    $name       = if ($nameToken -ne $null) { $nameToken.ToString() } else { '' }
    $orderToken = $memberToken['Order']  # use original token for Order (ref has none)
    $order      = if ($orderToken -ne $null) { [int]$orderToken.ToString() } else { 0 }

    if ($isCrowd) {
        $nestedMembers = New-Object System.Collections.ArrayList
        $nestedColl    = $resolved['CrowdMemberCollection']
        if ($nestedColl -ne $null) {
            foreach ($child in $nestedColl) {
                $converted = Convert-Member $child
                if ($converted -ne $null) { [void]$nestedMembers.Add($converted) }
            }
        }

        $entry = @{
            Name    = $name
            Order   = $order
            IsCrowd = $true
            Members = $nestedMembers.ToArray()
        }

        $sp = $resolved['SavedPositions']
        if ($sp -ne $null -and $sp.Count -gt 0) {
            $entry['SavedPositions'] = $sp
        }

        return $entry
    }
    else {
        # Extract character data to its own file (using resolved full object)
        if ($name -ne '' -and -not $script:charactersSaved.ContainsKey($name)) {
            $safeName = Get-SafeFileName $name
            $charFile = Join-Path $script:charDir ($safeName + '.json')

            $charCopy = $resolved.DeepClone()
            $charCopy.Remove('RosterCrowd') | Out-Null
            $charCopy.Remove('IsExpanded')  | Out-Null
            $charCopy.Remove('IsMatched')   | Out-Null
            $charCopy.Remove('Order')       | Out-Null

            $charJson = $charCopy.ToString([Newtonsoft.Json.Formatting]::Indented)
            [System.IO.File]::WriteAllText($charFile, $charJson, [System.Text.Encoding]::UTF8)
            $script:charactersSaved[$name] = $true
        }

        return @{
            Name    = $name
            Order   = $order
            IsCrowd = $false
        }
    }
}

# ---------------------------------------------------------------------------
# Converts a top-level crowd JToken to a lean CrowdDefinition hashtable
# ---------------------------------------------------------------------------
function Convert-Crowd {
    param($crowdToken)

    $crowdToken = Resolve-Ref $crowdToken
    if ($crowdToken -eq $null) { return $null }

    $nameToken  = $crowdToken['Name']
    $name       = if ($nameToken -ne $null) { $nameToken.ToString() } else { '' }
    $orderToken = $crowdToken['Order']
    $order      = if ($orderToken -ne $null) { [int]$orderToken.ToString() } else { 0 }

    $members = New-Object System.Collections.ArrayList
    $coll    = $crowdToken['CrowdMemberCollection']
    if ($coll -ne $null) {
        foreach ($member in $coll) {
            $converted = Convert-Member $member
            if ($converted -ne $null) { [void]$members.Add($converted) }
        }
    }

    $def = @{
        Name    = $name
        Order   = $order
        Members = $members.ToArray()
    }

    $sp = $crowdToken['SavedPositions']
    if ($sp -ne $null -and $sp.Count -gt 0) {
        $def['SavedPositions'] = $sp
    }

    return $def
}

# ---------------------------------------------------------------------------
# Process all crowds
# ---------------------------------------------------------------------------
Write-Host "Processing crowds ..."
$crowdDefinitions = New-Object System.Collections.ArrayList

foreach ($crowdToken in $token) {
    $def = Convert-Crowd $crowdToken
    if ($def -eq $null) { continue }
    [void]$crowdDefinitions.Add($def)
    $memberCount = if ($def['Members']) { $def['Members'].Count } else { 0 }
    Write-Host ("  Crowd: {0}  ({1} members)" -f $def['Name'], $memberCount)
}

$totalChars  = $script:charactersSaved.Count
$totalCrowds = $crowdDefinitions.Count

Write-Host ""
Write-Host "Characters extracted : $totalChars"
Write-Host "Crowds processed     : $totalCrowds"

if ($totalChars -eq 0) {
    Write-Host "ERROR: No characters found. Is this already the new format? Aborting." -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------------------
# Backup (only needed if migrating from the live file, not already from .bak)
# ---------------------------------------------------------------------------
if ($sourceFile -eq $repoFile -and -not (Test-Path $repoBackup)) {
    Copy-Item $repoFile $repoBackup
    Write-Host "Backup created: $repoBackup"
} elseif (Test-Path $repoBackup) {
    Write-Host "Backup already exists: $repoBackup"
}

# ---------------------------------------------------------------------------
# Write lean CrowdRepo.data
# Serialize via Newtonsoft so IMemoryElementPosition types round-trip correctly
# ---------------------------------------------------------------------------
$leanToken = [Newtonsoft.Json.Linq.JToken]::FromObject($crowdDefinitions.ToArray())
$leanJson  = $leanToken.ToString([Newtonsoft.Json.Formatting]::Indented)
[System.IO.File]::WriteAllText($repoFile, $leanJson, [System.Text.Encoding]::UTF8)

Write-Host ""
Write-Host "=== Migration complete ===" -ForegroundColor Green
Write-Host "New CrowdRepo.data : $repoFile"
Write-Host "Character files    : $charDir"

# ---------------------------------------------------------------------------
# Verify
# ---------------------------------------------------------------------------
Write-Host ""
Write-Host "--- Verification ---" -ForegroundColor Yellow

$charFiles = @(Get-ChildItem $charDir -Filter "*.json" -ErrorAction SilentlyContinue)
Write-Host ("Character files on disk : {0}" -f $charFiles.Count)
if ($charFiles.Count -ne $totalChars) {
    Write-Host ("WARNING: expected {0} character files but found {1}" -f $totalChars, $charFiles.Count) -ForegroundColor Yellow
}

$newJson     = [System.IO.File]::ReadAllText($repoFile, [System.Text.Encoding]::UTF8)
$newToken    = [Newtonsoft.Json.Linq.JToken]::Parse($newJson)
$newCrowdCnt = $newToken.Count
Write-Host ("Crowds in new file      : {0} (was {1})" -f $newCrowdCnt, $token.Count)
if ($newCrowdCnt -ne $token.Count) {
    Write-Host ("WARNING: crowd count mismatch -- old: {0}, new: {1}" -f $token.Count, $newCrowdCnt) -ForegroundColor Yellow
}

Write-Host ""
Write-Host "Next steps:" -ForegroundColor Cyan
Write-Host "  1. Inspect a few files in: $charDir"
Write-Host "  2. Inspect new CrowdRepo.data -- should contain Names and crowd structure only"
Write-Host "  3. Once satisfied, launch City of Heroes and start HeroVirtualTabletop"
Write-Host ""
