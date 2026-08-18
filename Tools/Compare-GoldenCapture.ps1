<#
.SYNOPSIS
    Compares two Eclipse game-index golden captures, ignoring the ordering of tied entries.

.DESCRIPTION
    Temporary scaffolding for the game-index refactor. Delete along with
    Eclipse/Diagnostics/GameIndexDiagnostics.cs once the refactor is complete.

    Eclipse builds its index with Parallel.ForEach into a ConcurrentBag, and every sort in
    the pipeline is a stable LINQ sort. Entries that tie on the sort key therefore come out
    in bag insertion order, which varies run to run. A raw diff of two captures of the SAME
    build reports differences that are not behaviour changes.

    This script canonicalises those ties before comparing: within each block it finds runs
    of consecutive entries sharing the same sort key and sorts each run by game id. The
    overall sequence is untouched, so a genuine reordering - an entry moving past one with a
    different key - still shows up as a difference.

    Sort key per block type:
      category list   the game title       (lines are "<guid> <title>")
      voice phrase    the "NN% MatchType"  (lines are "NN% MatchType <guid> <title>")

.PARAMETER Before
    Golden capture taken before the change.

.PARAMETER After
    Golden capture taken after the change.

.PARAMETER ShowTies
    Also list the blocks whose tied entries were reordered. Informational only.

.EXAMPLE
    .\Compare-GoldenCapture.ps1 -Before golden-A.txt -After golden-B.txt
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory = $true)][string] $Before,
    [Parameter(Mandatory = $true)][string] $After,
    [switch] $ShowTies
)

$ErrorActionPreference = 'Stop'

function Get-SortKey {
    param([string] $Line)

    $trimmed = $Line.Trim()

    # voice result: "61% FullTitleContains <guid> <title>" - key is percentage + match type
    if ($trimmed -match '^(\d+%\s+\S+)\s') { return $Matches[1] }

    # category entry: "<guid> <title>" - key is the title
    if ($trimmed -match '^[0-9a-fA-F-]{36}\s+(.*)$') { return $Matches[1] }

    # anything else (headers, settings, grammar phrases) is its own key, never tied
    return "`0$trimmed"
}

function Get-EntryId {
    param([string] $Line)

    if ($Line -match '([0-9a-fA-F-]{36})') { return $Matches[1] }
    return $Line
}

# Sort each run of consecutive equal-key entries by game id, leaving the sequence alone.
function ConvertTo-Canonical {
    param([string[]] $Lines)

    $result = New-Object System.Collections.ArrayList
    $run    = New-Object System.Collections.ArrayList
    $runKey = $null

    function Flush {
        if ($run.Count -gt 1) {
            foreach ($item in ($run | Sort-Object { Get-EntryId $_ })) { [void]$result.Add($item) }
        }
        elseif ($run.Count -eq 1) { [void]$result.Add($run[0]) }
        $run.Clear()
    }

    foreach ($line in $Lines) {
        $key = Get-SortKey $line
        if ($key -ne $runKey) { Flush; $runKey = $key }
        [void]$run.Add($line)
    }
    Flush

    return $result.ToArray()
}

function Get-Blocks {
    param([string[]] $Lines)

    $blocks = [ordered]@{}
    $order  = New-Object System.Collections.ArrayList
    $key    = '(preamble)'
    $buffer = New-Object System.Collections.ArrayList

    foreach ($line in $Lines) {
        if ($line -match '^\[' -or $line -match '^## ' -or $line -match '^- ') {
            $blocks[$key] = $buffer.ToArray(); [void]$order.Add($key)
            $key = $line; $buffer = New-Object System.Collections.ArrayList
        }
        else { [void]$buffer.Add($line) }
    }
    $blocks[$key] = $buffer.ToArray(); [void]$order.Add($key)

    return [pscustomobject]@{ Blocks = $blocks; Order = $order.ToArray() }
}

$beforeLines = Get-Content -LiteralPath $Before
$afterLines  = Get-Content -LiteralPath $After

$b = Get-Blocks $beforeLines
$a = Get-Blocks $afterLines

Write-Host ""
Write-Host "before : $Before"  -ForegroundColor DarkGray
Write-Host "after  : $After"   -ForegroundColor DarkGray
Write-Host ""

$failures = New-Object System.Collections.ArrayList
$tieOnly  = New-Object System.Collections.ArrayList

# 1. block headers must match exactly, in order - this covers list names, list order,
#    game counts, match counts and the set of categories.
$headerDiff = Compare-Object $b.Order $a.Order -SyncWindow 0
if ($headerDiff) {
    [void]$failures.Add("Block headers differ ($($headerDiff.Count) lines). List names, order or counts changed.")
    $headerDiff | Select-Object -First 20 | ForEach-Object {
        $side = if ($_.SideIndicator -eq '<=') { 'before' } else { 'after ' }
        Write-Host "  [$side] $($_.InputObject)" -ForegroundColor Yellow
    }
    Write-Host ""
}

# 2. block contents must match once tied entries are canonicalised
foreach ($key in $b.Order) {
    if (-not $a.Blocks.Contains($key)) { continue }

    $bBody = $b.Blocks[$key]
    $aBody = $a.Blocks[$key]

    $rawEqual = (($bBody -join "`n") -eq ($aBody -join "`n"))
    if ($rawEqual) { continue }

    $bCanon = ConvertTo-Canonical $bBody
    $aCanon = ConvertTo-Canonical $aBody

    if (($bCanon -join "`n") -eq ($aCanon -join "`n")) {
        [void]$tieOnly.Add($key)
    }
    else {
        [void]$failures.Add($key)
    }
}

Write-Host "blocks compared            : $($b.Order.Count)"
Write-Host "tied-entry reorder only    : $($tieOnly.Count)"   -ForegroundColor DarkGray

if ($ShowTies -and $tieOnly.Count -gt 0) {
    $tieOnly | ForEach-Object { Write-Host "    $_" -ForegroundColor DarkGray }
}

if ($failures.Count -eq 0) {
    Write-Host "genuine differences        : 0" -ForegroundColor Green
    Write-Host ""
    Write-Host "PASS - membership, counts, scoring and list order are unchanged." -ForegroundColor Green
    exit 0
}

Write-Host "genuine differences        : $($failures.Count)" -ForegroundColor Red
Write-Host ""
foreach ($key in ($failures | Select-Object -First 15)) {
    Write-Host "  $key" -ForegroundColor Red

    if ($b.Blocks.Contains($key) -and $a.Blocks.Contains($key)) {
        $delta = Compare-Object (ConvertTo-Canonical $b.Blocks[$key]) (ConvertTo-Canonical $a.Blocks[$key]) -SyncWindow 0
        $delta | Select-Object -First 6 | ForEach-Object {
            $side = if ($_.SideIndicator -eq '<=') { 'before' } else { 'after ' }
            Write-Host "      [$side] $($_.InputObject)"
        }
    }
}

Write-Host ""
Write-Host "FAIL - the capture changed in ways that are not tie ordering." -ForegroundColor Red
exit 1
