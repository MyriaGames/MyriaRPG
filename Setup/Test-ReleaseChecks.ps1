<#
Self-test for ReleaseChecks.ps1 (security review 2026-11-26, A11 F3) against throw-away git repositories:
a bare "origin" and a clone. Run: .\Test-ReleaseChecks.ps1   (exit code 0 = all passed)
#>
$ErrorActionPreference = "Stop"
. (Join-Path $PSScriptRoot "ReleaseChecks.ps1")

$root = Join-Path ([IO.Path]::GetTempPath()) ("myria-relcheck-" + [Guid]::NewGuid().ToString("N"))
$failures = 0
function Check([string]$name, [bool]$ok) {
    if ($ok) { Write-Host "  ok    $name" -ForegroundColor Green }
    else     { Write-Host "  FAIL  $name" -ForegroundColor Red; $script:failures++ }
}
function G { param([string]$repo) & git -C $repo @args 2>&1 | Out-Null; if ($LASTEXITCODE -ne 0) { throw "git $args failed" } }

try {
    New-Item -ItemType Directory -Path $root | Out-Null
    $origin = Join-Path $root "origin.git"; $work = Join-Path $root "work"
    & git init -q --bare -b main $origin
    & git clone -q $origin $work 2>&1 | Out-Null
    G $work config user.email "test@example.invalid"; G $work config user.name "test"; G $work config commit.gpgsign false
    Set-Content (Join-Path $work "a.cs") "class A {}"
    G $work add -A; G $work commit -q -m one; G $work push -q origin main

    $s = Get-RepoReleaseState -Path $work
    Check "clean and pushed -> releasable" ($s.Problems.Count -eq 0 -and $s.Commit.Length -eq 40 -and $s.Branch -eq "main")

    Add-Content (Join-Path $work "a.cs") "// edit"
    $s = Get-RepoReleaseState -Path $work
    Check "uncommitted change -> refused" (($s.Problems -join " ") -match "uncommitted")
    G $work checkout -q -- a.cs

    Set-Content (Join-Path $work "New.cs") "class N {}"
    $s = Get-RepoReleaseState -Path $work
    Check "untracked source file -> refused" (($s.Problems -join " ") -match "untracked file.*New\.cs")
    Remove-Item (Join-Path $work "New.cs")

    Set-Content (Join-Path $work "b.cs") "class B {}"
    G $work add -A; G $work commit -q -m two
    $s = Get-RepoReleaseState -Path $work
    Check "commit not pushed -> refused" (($s.Problems -join " ") -match "not on origin/main")

    G $work checkout -q -b side; G $work push -q origin side
    $s = Get-RepoReleaseState -Path $work
    Check "pushed, but only to a side branch -> refused" (($s.Problems -join " ") -match "not on origin/main")

    G $work checkout -q main; G $work push -q origin main
    $s = Get-RepoReleaseState -Path $work
    Check "after pushing to main -> releasable again" ($s.Problems.Count -eq 0)

    $plain = Join-Path $root "plain"; New-Item -ItemType Directory -Path $plain | Out-Null
    $s = Get-RepoReleaseState -Path $plain -NoFetch
    Check "not a repository -> refused" (($s.Problems -join " ") -match "not a git repository")

    $good = Get-RepoReleaseState -Path $work
    $info = New-BuildInfo -Version "9.9.9" -States @($good) -Sdk "10.0.404" -Runtime "10.0.15"
    Check "BUILDINFO names version, commit, SDK, runtime" (
        $info -match "Myria release 9\.9\.9" -and $info.Contains($good.Commit) -and $info -match "sdk:\s+10\.0\.404" -and $info -match "runtime: 10\.0\.15")
    Check "BUILDINFO has no warning for a clean build" ($info -notmatch "WARNING")

    Add-Content (Join-Path $work "a.cs") "// edit"
    $dirty = Get-RepoReleaseState -Path $work
    $info = New-BuildInfo -Version "9.9.9" -States @($dirty) -UnreleasedCode
    Check "BUILDINFO of an override build says so and lists the problem" ($info -match "WARNING" -and $info -match "uncommitted")
}
finally {
    Remove-Item $root -Recurse -Force -ErrorAction SilentlyContinue
}

if ($failures -gt 0) { Write-Host "$failures check(s) failed" -ForegroundColor Red; exit 1 }
Write-Host "All release-check tests passed." -ForegroundColor Green
exit 0
