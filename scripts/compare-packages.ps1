#Requires -Version 7.0
<#
.SYNOPSIS
Compares two NuGet packages (or any ZIP archives) byte for byte and member by member (P9 release contract RL-03 A).
.DESCRIPTION
Exit 0 and IDENTICAL: the files are byte-identical.
Exit 3 and CONTENT-IDENTICAL: same member names and identical member bytes, but the archive bytes differ; one TIMESTAMP <member> line per member whose last-write time differs.
Exit 1: the members differ; one line per difference: ONLY-FIRST <member>, ONLY-SECOND <member>, DIFFERENT <member> (same name, different size or SHA-256).
Exit 2: a file is missing, is not a ZIP archive or contains a duplicate member name.
.PARAMETER First
The first package.
.PARAMETER Second
The second package.
#>
[CmdletBinding()]
param(
    [Parameter(Mandatory)][string]$First,
    [Parameter(Mandatory)][string]$Second
)

Add-Type -AssemblyName System.IO.Compression.FileSystem

function Get-ZipEntries {
    param([string]$Path)
    
    try {
        $zip = [IO.Compression.ZipFile]::OpenRead($Path)
        $seen = [System.Collections.Generic.HashSet[string]]::new([StringComparer]::Ordinal)
        $entries = @()
        foreach ($entry in $zip.Entries) {
            if (-not $seen.Add($entry.FullName)) {
                throw "duplicate member $($entry.FullName)"
            }
            $stream = $entry.Open()
            try {
                $hash = [Security.Cryptography.SHA256]::HashData($stream)
                $hashString = -join ($hash | ForEach-Object { $_.ToString('x2') })
                $entries += [PSCustomObject]@{
                    FullName = $entry.FullName
                    Length = $entry.Length
                    Hash = $hashString
                    LastWriteTime = $entry.LastWriteTime.ToUniversalTime()
                }
            }
            finally {
                $stream.Dispose()
            }
        }
        return $entries
    }
    catch {
        throw "Failed to read zip file '$Path': $_"
    }
    finally {
        if ($zip) { $zip.Dispose() }
    }
}

if (-not (Test-Path $First -PathType Leaf)) {
    [Console]::Error.WriteLine("Input error: '$First' does not exist.")
    exit 2
}

if (-not (Test-Path $Second -PathType Leaf)) {
    [Console]::Error.WriteLine("Input error: '$Second' does not exist.")
    exit 2
}

try {
    $firstEntries = Get-ZipEntries -Path $First
}
catch {
    [Console]::Error.WriteLine("Input error: '$First' cannot be read as a package: $_")
    exit 2
}

try {
    $secondEntries = Get-ZipEntries -Path $Second
}
catch {
    [Console]::Error.WriteLine("Input error: '$Second' cannot be read as a package: $_")
    exit 2
}

$firstHash = Get-FileHash -Algorithm SHA256 $First
$secondHash = Get-FileHash -Algorithm SHA256 $Second

if ($firstHash.Hash -eq $secondHash.Hash) {
    Write-Output "IDENTICAL"
    exit 0
}

$firstDict = [System.Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($entry in $firstEntries) {
    $firstDict[$entry.FullName] = $entry
}

$secondDict = [System.Collections.Generic.Dictionary[string,object]]::new([StringComparer]::Ordinal)
foreach ($entry in $secondEntries) {
    $secondDict[$entry.FullName] = $entry
}

$allNames = [System.Collections.Generic.SortedSet[string]]::new([StringComparer]::Ordinal)
foreach ($key in $firstDict.Keys) { [void]$allNames.Add($key) }
foreach ($key in $secondDict.Keys) { [void]$allNames.Add($key) }

$different = $false

foreach ($name in $allNames) {
    $firstEntry = if ($firstDict.ContainsKey($name)) { $firstDict[$name] } else { $null }
    $secondEntry = if ($secondDict.ContainsKey($name)) { $secondDict[$name] } else { $null }

    if (-not $firstEntry) {
        Write-Output "ONLY-SECOND $name"
        $different = $true
    }
    elseif (-not $secondEntry) {
        Write-Output "ONLY-FIRST $name"
        $different = $true
    }
    else {
        if ($firstEntry.Length -ne $secondEntry.Length -or $firstEntry.Hash -ne $secondEntry.Hash) {
            Write-Output "DIFFERENT $name"
            $different = $true
        }
    }
}

if ($different) {
    exit 1
}

Write-Output "CONTENT-IDENTICAL"

foreach ($name in $allNames) {
    $firstEntry = $firstDict[$name]
    $secondEntry = $secondDict[$name]

    if ($firstEntry.LastWriteTime -ne $secondEntry.LastWriteTime) {
        Write-Output "TIMESTAMP $name"
    }
}

# Exit 0 is reserved for byte-identical files (checked above); identical contents with different archive bytes are exit 3 even without a timestamp difference.
exit 3
