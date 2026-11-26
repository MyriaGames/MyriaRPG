<#
Provenance checks for releases and deployments (security review 2026-11-26, A11 F3).
Dot-sourced by release.ps1 (and mirrored in Myria.Website/deploy.ps1 and Setup/deploy-admin.ps1).

A release used to be built from whatever was in the folders: uncommitted edits, untracked files and
unpushed commits all shipped, and nothing recorded which commits a release contained. The release
signature (A08 F2) proves who built it - BUILDINFO.txt, listed in the signed SHA256SUMS, says from what.
#>

function Get-RepoReleaseState {
    <#
    .SYNOPSIS
    Checks that a repository folder is fit to ship: no uncommitted or untracked files, and HEAD contained
    in origin/<Branch> (pushed, and not a side branch). Returns Name, Path, Commit, Branch, Problems.
    #>
    param(
        [Parameter(Mandatory)][string]$Path,
        [string]$Branch = "main",
        [switch]$NoFetch
    )
    $state = [pscustomobject]@{
        Name = Split-Path (Resolve-Path $Path) -Leaf; Path = (Resolve-Path $Path).Path
        Commit = ""; Branch = ""; Problems = [System.Collections.Generic.List[string]]::new()
    }
    $top = & git -C $Path rev-parse --show-toplevel 2>$null
    if ($LASTEXITCODE -ne 0) { $state.Problems.Add("not a git repository"); return $state }

    $state.Commit = (& git -C $Path rev-parse HEAD).Trim()
    $state.Branch = (& git -C $Path rev-parse --abbrev-ref HEAD).Trim()

    $status = @(& git -C $Path status --porcelain --untracked-files=all)
    $tracked = @($status | Where-Object { $_ -and -not $_.StartsWith("??") })
    $untracked = @($status | Where-Object { $_ -and $_.StartsWith("??") } | ForEach-Object { $_.Substring(3) })
    if ($tracked.Count -gt 0) {
        $state.Problems.Add("$($tracked.Count) uncommitted change(s): " + (($tracked | Select-Object -First 5 | ForEach-Object { $_.Substring(3) }) -join ", "))
    }
    if ($untracked.Count -gt 0) {
        $state.Problems.Add("$($untracked.Count) untracked file(s) (commit or .gitignore them): " + (($untracked | Select-Object -First 5) -join ", "))
    }

    if (-not $NoFetch) {
        & git -C $Path fetch -q origin $Branch 2>$null
        if ($LASTEXITCODE -ne 0) { $state.Problems.Add("could not fetch origin/$Branch"); return $state }
    }
    & git -C $Path rev-parse --verify -q "origin/$Branch" *> $null
    if ($LASTEXITCODE -ne 0) { $state.Problems.Add("no origin/$Branch to compare with"); return $state }
    & git -C $Path merge-base --is-ancestor HEAD "origin/$Branch"
    if ($LASTEXITCODE -ne 0) {
        $state.Problems.Add("HEAD $($state.Commit.Substring(0, 7)) is not on origin/$Branch (unpushed, or a side branch)")
    }
    return $state
}

function New-BuildInfo {
    <# .SYNOPSIS Text of BUILDINFO.txt: what this release was built from. #>
    param(
        [Parameter(Mandatory)][string]$Version,
        [Parameter(Mandatory)][object[]]$States,
        [string]$Sdk = "",
        [string]$Runtime = "",
        [switch]$UnreleasedCode
    )
    $lines = @(
        "Myria release $Version",
        "built:   $((Get-Date).ToUniversalTime().ToString('yyyy-MM-dd HH:mm:ss')) UTC",
        "sdk:     $Sdk",
        "runtime: $Runtime"
    )
    if ($UnreleasedCode) {
        $lines += "WARNING: built with -AllowUnreleasedCode - this build contains code that is not on origin/main."
    }
    $lines += ""
    foreach ($s in $States) {
        $mark = if ($s.Problems.Count -gt 0) { "  [" + ($s.Problems -join "; ") + "]" } else { "" }
        $lines += ("{0,-22} {1}  ({2}){3}" -f $s.Name, $s.Commit, $s.Branch, $mark)
    }
    return ($lines -join "`n") + "`n"
}
