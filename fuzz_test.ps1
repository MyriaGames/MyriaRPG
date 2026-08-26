#Requires -Version 5.1
<#
.SYNOPSIS
    Monkey fuzzer for Myria.Wpf — random UI inputs, crash detection, and log capture.

.PARAMETER DurationSeconds
    How long to run before declaring success. Default: 300.

.PARAMETER ActionDelayMs
    Milliseconds between actions. Lower is faster but may miss UI transitions. Default: 400.

.PARAMETER HistorySize
    How many recent actions to include in crash reports. Default: 150.

.PARAMETER BuildFirst
    Run 'dotnet build' (Debug) before starting.

.PARAMETER ExePath
    Path to Myria.Wpf.exe. Auto-detected from bin\ if omitted.

.EXAMPLE
    .\fuzz_test.ps1 -DurationSeconds 600 -BuildFirst
    .\fuzz_test.ps1 -ExePath "bin\Release\net8.0-windows\Myria.Wpf.exe"
#>
param(
    [int]$DurationSeconds = 60,
    [string]$LogPath      = "",
    [int]$ActionDelayMs   = 10,
    [int]$HistorySize     = 150,
    [switch]$BuildFirst,
    [string]$ExePath      = ""
)

Set-StrictMode -Version Latest
$ErrorActionPreference = "Stop"

$ScriptDir = Split-Path $MyInvocation.MyCommand.Path -Parent
if (-not $LogPath) {
    $LogPath = Join-Path $ScriptDir "fuzz_log_$(Get-Date -Format 'yyyyMMdd_HHmmss').txt"
}

# ── Win32 P/Invoke ────────────────────────────────────────────────────────────
Add-Type -AssemblyName UIAutomationClient
Add-Type -AssemblyName UIAutomationTypes
Add-Type -AssemblyName System.Windows.Forms

Add-Type @'
using System;
using System.Runtime.InteropServices;
using System.Threading;

public static class Win32 {
    [DllImport("user32.dll")] public static extern bool SetCursorPos(int x, int y);
    [DllImport("user32.dll")] public static extern void mouse_event(uint f, int dx, int dy, uint d, IntPtr e);
    [DllImport("user32.dll")] public static extern bool SetForegroundWindow(IntPtr hwnd);
    [DllImport("user32.dll")] public static extern bool GetWindowRect(IntPtr hwnd, ref RECT r);
    [DllImport("user32.dll")] public static extern bool IsWindow(IntPtr hwnd);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT { public int Left, Top, Right, Bottom; }

    public static void Click(int x, int y) {
        SetCursorPos(x, y);
        Thread.Sleep(30);
        mouse_event(0x02, x, y, 0, IntPtr.Zero); // LEFTDOWN
        Thread.Sleep(30);
        mouse_event(0x04, x, y, 0, IntPtr.Zero); // LEFTUP
    }

    public static RECT GetRect(IntPtr hwnd) {
        var r = new RECT();
        GetWindowRect(hwnd, ref r);
        return r;
    }
}
'@

# ── Logging ───────────────────────────────────────────────────────────────────
$history = [System.Collections.Generic.Queue[string]]::new()

function Log([string]$Level, [string]$Msg) {
    $ts   = (Get-Date).ToString("yyyy-MM-dd HH:mm:ss.fff")
    $line = "[$ts][$Level] $Msg"
    Write-Host $line
    try { Add-Content -LiteralPath $LogPath -Value $line -ErrorAction Stop } catch {}
    $script:history.Enqueue($line)
    while ($script:history.Count -gt $HistorySize) { [void]$script:history.Dequeue() }
}

function CrashReport([string]$Reason, [string]$Extra = "") {
    $sep  = "=" * 72
    $snap = ($script:history.ToArray()) -join "`n"
    $rpt  = @"

$sep
CRASH DETECTED
Time   : $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')
Reason : $Reason
$Extra

=== Last $($script:history.Count) actions before crash ===
$snap
$sep

"@
    Add-Content -LiteralPath $LogPath -Value $rpt
    Write-Host $rpt -ForegroundColor Red
}

function Get-EventLogCrashDetail([int]$AppPid) {
    # .NET runtime logs unhandled exceptions to Application event log
    try {
        $entries = Get-EventLog -LogName Application -Source ".NET Runtime","Application Error" `
                                -EntryType Error -Newest 5 -ErrorAction SilentlyContinue
        $match = $entries | Where-Object { $_.Message -match $AppPid -or $_.Message -match "Myria.Wpf" } |
                            Select-Object -First 1
        if ($match) { return $match.Message }
    } catch {}
    return ""
}

# ── Optional build ────────────────────────────────────────────────────────────
if ($BuildFirst) {
    Log "INFO" "Building solution..."
    & dotnet build "$ScriptDir\Myria.Wpf.sln" -c Debug
    if ($LASTEXITCODE -ne 0) { Log "ERROR" "Build failed — aborting."; exit 1 }
    Log "INFO" "Build succeeded."
}

# ── Locate executable ─────────────────────────────────────────────────────────
if (-not $ExePath) {
    $ExePath = @(
        "$ScriptDir\bin\Debug\net8.0-windows\Myria.Wpf.exe",
        "$ScriptDir\bin\Release\net8.0-windows\Myria.Wpf.exe"
    ) | Where-Object { Test-Path $_ } | Select-Object -First 1
}
if (-not $ExePath -or -not (Test-Path $ExePath)) {
    Write-Error "Cannot find Myria.Wpf.exe. Pass -BuildFirst or -ExePath."
    exit 1
}

Log "INFO" "Exe      : $ExePath"
Log "INFO" "Log file : $LogPath"
Log "INFO" "Duration : $DurationSeconds s  |  Delay: $ActionDelayMs ms/action"

# ── Launch ────────────────────────────────────────────────────────────────────
# WinExe has no console so we don't redirect streams — they don't exist.
$proc = Start-Process -FilePath $ExePath -PassThru
Log "INFO" "Process started (PID $($proc.Id)). Waiting for main window..."

$deadline = (Get-Date).AddSeconds(30)
while ((Get-Date) -lt $deadline) {
    $proc.Refresh()
    if ($proc.HasExited) {
        $detail = Get-EventLogCrashDetail $proc.Id
        CrashReport "Process exited before window appeared (code $($proc.ExitCode))" $detail
        exit 1
    }
    if ($proc.MainWindowHandle -ne [IntPtr]::Zero) { break }
    Start-Sleep -Milliseconds 300
}

if ($proc.MainWindowHandle -eq [IntPtr]::Zero) {
    CrashReport "Window never appeared within 30 s"
    $proc.Kill()
    exit 1
}

Log "INFO" "Window ready (hwnd 0x$($proc.MainWindowHandle.ToString('X'))). Fuzzing..."

# ── UIAutomation setup ────────────────────────────────────────────────────────
$AE           = [System.Windows.Automation.AutomationElement]
$Scope        = [System.Windows.Automation.TreeScope]
$invoke       = [System.Windows.Automation.InvokePattern]::Pattern
$valuePattern = [System.Windows.Automation.ValuePattern]::Pattern

$invokeCond = [System.Windows.Automation.PropertyCondition]::new(
    $AE::IsInvokePatternAvailableProperty, $true)
$toggleCond = [System.Windows.Automation.PropertyCondition]::new(
    $AE::IsTogglePatternAvailableProperty, $true)
$selectCond = [System.Windows.Automation.PropertyCondition]::new(
    $AE::IsSelectionItemPatternAvailableProperty, $true)
$valueCond  = [System.Windows.Automation.PropertyCondition]::new(
    $AE::IsValuePatternAvailableProperty, $true)
$orCond1 = [System.Windows.Automation.OrCondition]::new($invokeCond, $toggleCond)
$orCond2 = [System.Windows.Automation.OrCondition]::new($orCond1, $selectCond)
$clickableCond = [System.Windows.Automation.OrCondition]::new($orCond2, $valueCond)

$rand = [System.Random]::new()

# SendKeys strings - meaningful game keys plus common UI keys
$keys = @("{ENTER}","{ESC}","{TAB}"," ","{UP}","{DOWN}","{LEFT}","{RIGHT}",
          "1","2","3","i","m","q","c","e","f","r")

function Get-RandomText {
    $chars = "abcdefghijklmnopqrstuvwxyz0123456789"
    $len   = $rand.Next(1, 13)
    return -join (1..$len | ForEach-Object { $chars[$rand.Next($chars.Length)] })
}

function Get-AppRoot {
    $proc.Refresh()
    if ($proc.HasExited) { return $null }
    $h = $proc.MainWindowHandle
    if ($h -eq [IntPtr]::Zero) { return $null }
    try { return $AE::FromHandle($h) } catch { return $null }
}

function Invoke-Control([System.Windows.Automation.AutomationElement]$root) {
    $all = $root.FindAll($Scope::Descendants, $clickableCond)
    if ($all.Count -eq 0) { return $false }

    # Only consider on-screen, enabled elements; skip quit/exit controls to avoid clean shutdowns
    $vis = @($all | Where-Object {
        -not $_.Current.IsOffscreen -and
        $_.Current.IsEnabled -and
        $_.Current.Name -notmatch "(?i)quit|exit|beenden|schliessen|schließen|verlassen"
    })
    if ($vis.Count -eq 0) { return $false }

    $el   = $vis[$rand.Next($vis.Count)]
    $name = $el.Current.Name
    $ctrl = $el.Current.ControlType.ProgrammaticName

    # "Close" only appears on in-app dialogs — run full detection first so the content
    # is logged before the dialog disappears, then dismiss via the proper path.
    if ($name -imatch "^close$") {
        Log "WARN" "Found 'Close' button — waiting for dialog (Wait-ForDialog)"
        $dlg = Wait-ForDialog -TimeoutMs 5000
        if ($dlg) {
            Log "WARN" "InAppDialog (via Close button): Title='$($dlg.Title)' Content='$($dlg.Content)'"
            if ($dlg.Element) {
                Dismiss-GameDialog $dlg.Element | Out-Null
                Start-Sleep -Milliseconds 300
            }
        } else {
            # Timeout — no secondary window appeared; Close belongs to an embedded panel
            try {
                $pt = $el.GetClickablePoint()
                [Win32]::Click([int]$pt.X, [int]$pt.Y)
                Log "ACTION" "CloseButton (direct) at ($([int]$pt.X),$([int]$pt.Y))"
                Start-Sleep -Milliseconds 300
            } catch {
                try { $el.GetCurrentPattern($invoke).Invoke() } catch {}
            }
        }
        return $true
    }

    # Text fields: use ValuePattern.SetValue so the app sees a proper TextChanged event
    try {
        $vp = $el.GetCurrentPattern($valuePattern)
        if (-not $vp.Current.IsReadOnly) {
            $text = Get-RandomText
            $vp.SetValue($text)
            Log "ACTION" "TextInput $ctrl '$name' <- '$text'"
            return $true
        }
    } catch {}

    try {
        $pt = $el.GetClickablePoint()
        [Win32]::Click([int]$pt.X, [int]$pt.Y)
        Log "ACTION" "Click  $ctrl '$name' at ($([int]$pt.X),$([int]$pt.Y))"
        if ($ctrl -eq "ControlType.ListItem") { $script:npcSelected = $true }
        return $true
    } catch {
        # Element has no click point (off-screen edge case) — fall back to Invoke
        try {
            $el.GetCurrentPattern($invoke).Invoke()
            Log "ACTION" "Invoke $ctrl '$name'"
            if ($ctrl -eq "ControlType.ListItem") { $script:npcSelected = $true }
            return $true
        } catch { return $false }
    }
}

function Invoke-TalkToNpc([System.Windows.Automation.AutomationElement]$root) {
    $nameCond = [System.Windows.Automation.PropertyCondition]::new($AE::NameProperty, "Talk to NPC")
    $btn = $root.FindFirst($Scope::Descendants, $nameCond)
    if ($null -eq $btn -or $btn.Current.IsOffscreen -or -not $btn.Current.IsEnabled) { return $false }
    try {
        $pt = $btn.GetClickablePoint()
        [Win32]::Click([int]$pt.X, [int]$pt.Y)
        Log "ACTION" "TalkToNPC (targeted)"
        return $true
    } catch {
        try {
            $btn.GetCurrentPattern($invoke).Invoke()
            Log "ACTION" "TalkToNPC (targeted invoke)"
            return $true
        } catch { return $false }
    }
}

function Invoke-Key {
    $k = $keys[$rand.Next($keys.Count)]
    [System.Windows.Forms.SendKeys]::SendWait($k)
    Log "ACTION" "Key    $k"
}

function Invoke-RawClick {
    # Hits random coordinates in the window — finds undiscovered hit areas
    $r = [Win32]::GetRect($proc.MainWindowHandle)
    if (($r.Right - 10) -le ($r.Left + 10) -or ($r.Bottom - 10) -le ($r.Top + 40)) { return }
    $x = $rand.Next($r.Left + 10, $r.Right  - 10)
    $y = $rand.Next($r.Top  + 40, $r.Bottom - 10)
    [Win32]::Click($x, $y)
    Log "ACTION" "RawClick ($x,$y)"
}

function Get-GameDialog {
    # Detects two kinds of dialog:
    #   - External crash/WER dialogs (not our process)
    #   - In-app dialogs spawned by the game (MessageBox, Window_InitError, etc.)
    # Returns a PSCustomObject {IsCrash, Title, Content, Element} or $null.
    $top = $AE::RootElement.FindAll($Scope::Children,
           [System.Windows.Automation.Condition]::TrueCondition)
    $mainHwnd = $proc.MainWindowHandle.ToInt64()
    foreach ($w in $top) {
        try {
            $pid   = $w.Current.ProcessId
            $title = $w.Current.Name
            if ($pid -ne $proc.Id) {
                if ($title -imatch "exception|unhandled|stopped working|error|crash|not responding") {
                    return [PSCustomObject]@{ IsCrash = $true; Title = $title; Content = ""; Element = $null }
                }
                continue
            }
            # Skip the main game window itself
            if ([int64]$w.Current.NativeWindowHandle -eq $mainHwnd) { continue }
            # Any other window from our process is an in-app dialog — capture its text content
            $textCond = [System.Windows.Automation.PropertyCondition]::new(
                $AE::ControlTypeProperty, [System.Windows.Automation.ControlType]::Text)
            $texts   = $w.FindAll($Scope::Descendants, $textCond)
            $content = ($texts | ForEach-Object { $_.Current.Name } | Where-Object { $_ -ne "" }) -join " | "
            return [PSCustomObject]@{ IsCrash = $false; Title = $title; Content = $content; Element = $w }
        } catch { continue }
    }
    return $null
}

function Dismiss-GameDialog([System.Windows.Automation.AutomationElement]$dialogRoot) {
    # Click the first available button (OK/Close/Yes/No) to unblock the game
    $btnCond = [System.Windows.Automation.PropertyCondition]::new($AE::IsInvokePatternAvailableProperty, $true)
    $btns = $dialogRoot.FindAll($Scope::Descendants, $btnCond)
    $btn  = @($btns | Where-Object { -not $_.Current.IsOffscreen -and $_.Current.IsEnabled }) | Select-Object -First 1
    if ($null -eq $btn) { return $false }
    try {
        $pt = $btn.GetClickablePoint()
        [Win32]::Click([int]$pt.X, [int]$pt.Y)
        Log "ACTION" "DialogDismiss: clicked '$($btn.Current.Name)' in '$($dialogRoot.Current.Name)'"
        return $true
    } catch {
        try { $btn.GetCurrentPattern($invoke).Invoke(); return $true } catch { return $false }
    }
}

function Wait-ForDialog([int]$TimeoutMs = 5000) {
    # Polls Get-GameDialog every 200 ms until a non-crash dialog appears or the timeout expires.
    # Mirrors the Wait-ForDialog used in test_login_detection.ps1 where detection was confirmed working.
    $deadline = (Get-Date).AddMilliseconds($TimeoutMs)
    while ((Get-Date) -lt $deadline) {
        $dlg = Get-GameDialog
        if ($dlg -and -not $dlg.IsCrash) { return $dlg }
        Start-Sleep -Milliseconds 200
    }
    return $null
}

function Stop-GameGracefully {
    # Try "Save and Quit" (in-game overlay) or "Quit" (main menu) before falling back to Kill.
    # If neither is visible, press ESC to navigate back toward the main menu and retry.
    Log "INFO" "GracefulQuit: attempting graceful shutdown..."
    $deadline = (Get-Date).AddSeconds(15)

    while ((Get-Date) -lt $deadline) {
        $proc.Refresh()
        if ($proc.HasExited) { Log "INFO" "GracefulQuit: process exited cleanly."; return }

        $root = Get-AppRoot
        if ($null -eq $root) { break }

        $found = $false
        # Prefer Save-and-Quit (saves game state); fall back to Quit (main menu).
        # EN then DE label variants.
        foreach ($label in @("Save and Quit", "Speichern und Beenden", "Quit", "Beenden")) {
            $cond = [System.Windows.Automation.PropertyCondition]::new($AE::NameProperty, $label)
            $btn  = $root.FindFirst($Scope::Descendants, $cond)
            if ($null -ne $btn -and -not $btn.Current.IsOffscreen -and $btn.Current.IsEnabled) {
                try {
                    $pt = $btn.GetClickablePoint()
                    [Win32]::Click([int]$pt.X, [int]$pt.Y)
                } catch {
                    try { $btn.GetCurrentPattern($invoke).Invoke() } catch {}
                }
                Log "INFO" "GracefulQuit: clicked '$label' — waiting up to 5s for exit"
                $exitBy = (Get-Date).AddSeconds(5)
                while ((Get-Date) -lt $exitBy) {
                    $proc.Refresh()
                    if ($proc.HasExited) { Log "INFO" "GracefulQuit: process exited cleanly."; return }
                    Start-Sleep -Milliseconds 200
                }
                $found = $true
                break
            }
        }

        if (-not $found) {
            # No quit button visible — press ESC to close overlays or navigate back toward main menu.
            [System.Windows.Forms.SendKeys]::SendWait("{ESC}")
            Log "INFO" "GracefulQuit: pressed ESC (navigating back)"
            Start-Sleep -Milliseconds 600
        }
    }

    $proc.Refresh()
    if (-not $proc.HasExited) {
        Log "WARN" "GracefulQuit: timed out — killing process"
        $proc.Kill()
    }
}

# ── Fuzz loop ─────────────────────────────────────────────────────────────────
$endTime     = (Get-Date).AddSeconds($DurationSeconds)
$actions     = 0
$script:npcSelected = $false

try {
    while ((Get-Date) -lt $endTime) {
        $proc.Refresh()
        if ($proc.HasExited) {
            if ($proc.ExitCode -eq 0) {
                Log "WARN" "Process exited cleanly (code 0) after $actions actions -- likely a quit button was pressed. Stopping."
                exit 0
            }
            Start-Sleep -Milliseconds 200  # let event log entry appear
            $detail = Get-EventLogCrashDetail $proc.Id
            CrashReport "Process exited unexpectedly (exit code $($proc.ExitCode))" $detail
            exit 1
        }

        # Check for dialogs every iteration BEFORE touching the main window.
        # A modal dialog disables the main window — catching it here avoids accidentally
        # dismissing it via a random click before it can be logged.
        $dlg = Get-GameDialog
        if ($dlg) {
            if ($dlg.IsCrash) {
                $detail = Get-EventLogCrashDetail $proc.Id
                CrashReport "Error dialog appeared: `"$($dlg.Title)`"" $detail
                exit 1
            } else {
                Log "WARN" "InAppDialog: Title='$($dlg.Title)' Content='$($dlg.Content)'"
                if ($dlg.Element) { Dismiss-GameDialog $dlg.Element | Out-Null }
                Start-Sleep -Milliseconds 200
                $actions++
                if ($actions % 10 -eq 0) {
                    $remaining = [int](($endTime - (Get-Date)).TotalSeconds)
                    Log "INFO" "Heartbeat: $actions actions performed, ${remaining}s remaining"
                }
                continue
            }
        }

        $root = Get-AppRoot
        if ($null -eq $root) {
            Log "WARN" "Window unreachable — waiting for redraw..."
            Start-Sleep -Milliseconds 600
            continue
        }

        [Win32]::SetForegroundWindow($proc.MainWindowHandle) | Out-Null

        # If an NPC was just selected, 70 % chance to immediately try Talk to NPC.
        # If the button isn't visible (navigated away) or the 30 % roll fires, fall
        # through to the normal weighted action so we don't stall.
        $actionDone = $false
        if ($script:npcSelected -and $rand.Next(100) -lt 70) {
            $script:npcSelected = $false
            $actionDone = Invoke-TalkToNpc $root
        }

        if (-not $actionDone) {
            # Weights: 60 % invoke a real control, 25 % keyboard, 15 % raw click
            $roll = $rand.Next(100)
            if ($roll -lt 60) {
                if (-not (Invoke-Control $root)) {
                    # Invoke-Control found no enabled controls — WPF disables the main
                    # window when ShowDialog() runs, so this is the reliable signal that
                    # a modal dialog just appeared. UIAutomation registration takes
                    # ~50-100 ms; sleep past that lag before checking and raw-clicking.
                    Start-Sleep -Milliseconds 100
                    $dlg2 = Get-GameDialog
                    if ($dlg2) {
                        if ($dlg2.IsCrash) {
                            $detail = Get-EventLogCrashDetail $proc.Id
                            CrashReport "Error dialog appeared: `"$($dlg2.Title)`"" $detail
                            exit 1
                        }
                        Log "WARN" "InAppDialog (modal wait): Title='$($dlg2.Title)' Content='$($dlg2.Content)'"
                        if ($dlg2.Element) { Dismiss-GameDialog $dlg2.Element | Out-Null }
                        Start-Sleep -Milliseconds 200
                    } else {
                        Invoke-RawClick
                    }
                }
            } elseif ($roll -lt 85) {
                Invoke-Key
            } else {
                # Raw click with a quick pre-check — dialog may have appeared since
                # the loop-top check and we don't want to blindly click through it.
                $dlg2 = Get-GameDialog
                if ($dlg2) {
                    if ($dlg2.IsCrash) {
                        $detail = Get-EventLogCrashDetail $proc.Id
                        CrashReport "Error dialog appeared: `"$($dlg2.Title)`"" $detail
                        exit 1
                    }
                    Log "WARN" "InAppDialog (pre-rawclick): Title='$($dlg2.Title)' Content='$($dlg2.Content)'"
                    if ($dlg2.Element) { Dismiss-GameDialog $dlg2.Element | Out-Null }
                    Start-Sleep -Milliseconds 200
                } else {
                    Invoke-RawClick
                }
            }
        }

        $actions++

        if ($actions % 10 -eq 0) {
            $remaining = [int](($endTime - (Get-Date)).TotalSeconds)
            Log "INFO" "Heartbeat: $actions actions performed, ${remaining}s remaining"
        }

        Start-Sleep -Milliseconds $ActionDelayMs
    }

    Log "INFO" "Fuzz run complete. $actions actions performed. No crash detected."
    Log "INFO" "Full log: $LogPath"

} catch {
    CrashReport "Fuzzer script error" $_.ToString()
    exit 1
} finally {
    if (-not $proc.HasExited) {
        Stop-GameGracefully
    }
}
