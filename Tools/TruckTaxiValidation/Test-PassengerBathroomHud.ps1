param([string]$ProjectRoot = (Split-Path (Split-Path $PSScriptRoot -Parent) -Parent))
$ErrorActionPreference = 'Stop'
Add-Type -TypeDefinition (Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiModalPauseGuard.cs') -Raw)
$script:assertions = 0
function Assert-True([bool]$Value, [string]$Message) {
    if (!$Value) { throw $Message }
    $script:assertions++
}
$guardType = [LWS.TruckTaxi.TruckTaxiModalPauseGuard]
$guard = $guardType::new()
$owner = [object]::new()
Assert-True ($guard.TryAcquire($owner, 'ride', $true, $false, 1) -eq 0) 'Do not take an already-paused host.'
Assert-True ($guard.TryAcquire($owner, 'ride', $false, $true, 1) -eq 0) 'Do not overlap a foreign modal.'
Assert-True ($guard.TryAcquire($null, 'ride', $false, $false, 1) -eq 0) 'A claim requires an owner.'
Assert-True ($guard.Dispatch(1) -eq 0) 'No modal leaves the existing HUD in charge.'
$token = $guard.TryAcquire($owner, 'ride', $false, $false, 1)
Assert-True ($token -gt 0) 'Valid acquisition.'
Assert-True ($guard.TryAcquire([object]::new(), 'ride', $false, $false, 1) -eq 0) 'Only one modal owner.'
Assert-True ($guard.Dispatch(1) -eq -1) 'Opening input cannot also submit.'
Assert-True ($guard.Dispatch(2) -eq 1) 'Next frame dispatches once.'
Assert-True ($guard.Dispatch(2) -eq -1) 'Duplicate dispatcher call consumes without replay.'
Assert-True (!$guard.Release([object]::new(), $token, 'ride', $true, $false, 2)) 'Wrong owner cannot release.'
Assert-True $guard.IsHeld 'Wrong-owner release leaves ownership intact.'
Assert-True ($guard.Release($owner, $token, 'ride', $true, $false, 2)) 'Only an unchanged host resumes.'
Assert-True ($guard.Dispatch(2) -eq -1) 'Closing input cannot reach another panel.'
Assert-True ($guard.Dispatch(3) -eq 0) 'Next frame returns input to normal HUD.'
Assert-True (!$guard.Release($owner, $token, 'ride', $true, $false, 3)) 'Disposal is idempotent.'
$replacement = $guard.TryAcquire($owner, 'ride', $false, $false, 3)
Assert-True (!$guard.Release($owner, $token, 'ride', $true, $false, 4)) 'Stale token cannot remove replacement.'
Assert-True $guard.IsHeld 'Replacement remains owned.'
Assert-True ($guard.Release($owner, $replacement, 'ride', $true, $false, 4)) 'Current replacement releases.'
foreach ($kind in @('context', 'modal', 'resume', 'teardown')) {
    $guard = $guardType::new()
    $token = $guard.TryAcquire($owner, 'ride', $false, $false, 10)
    switch ($kind) {
        'context' { $guard.Observe('ride-result', $true, $false) }
        'modal' { $guard.Observe('ride', $true, $true) }
        'resume' { $guard.Observe('ride', $false, $false) }
        'teardown' { $guard.InvalidateResume() }
    }
    Assert-True (!$guard.Release($owner, $token, 'ride', $true, $false, 11)) "$kind interruption must never resume a newer pause."
    Assert-True (!$guard.IsHeld) "$kind cleanup must release bookkeeping."
}

# Static boundaries complement the executable ownership tests; these are not visual/runtime proof.
$bridge = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiHudModalBridge.cs') -Raw
$panel = Get-Content -LiteralPath (Join-Path $ProjectRoot 'Assets/LWS/TruckTaxi/Scripts/TruckTaxiPassengerBathroomPanel.cs') -Raw
Assert-True ($bridge -notmatch 'new InputAction|\.performed\s*\+=|Keyboard\.current|Gamepad\.current') 'Bridge must not introduce another input listener/binding authority.'
Assert-True ($panel -notmatch 'AddComponent<Canvas>|typeof\(Canvas\)|new TruckTaxiUIInput|AddComponent<EventSystem>') 'Panel must stay under existing HUD Canvas and EventSystem.'
Assert-True ($panel -match 'hud\.Button\(' -and $panel -match 'TruckTaxiHud\.Rect\(hud\.Root') 'Panel must use existing HUD root and Heat button factory.'
Assert-True ($bridge -match 'input\.SubmitSelected\(\)' -and $bridge -match 'input\.Cancel\.WasPressedThisFrame') 'Bridge must reuse existing submit/cancel dispatcher actions.'
"PASS: $script:assertions modal lease/input ownership and existing-HUD boundary assertions. No Unity launch or build."
