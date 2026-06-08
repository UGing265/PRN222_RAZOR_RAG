# Kill any process listening on port 5000
$procs = Get-NetTCPConnection -LocalPort 5000 -State Listen -ErrorAction SilentlyContinue |
         Select-Object -ExpandProperty OwningProcess |
         Sort-Object -Unique

if ($procs) {
    foreach ($procId in $procs) {
        Write-Host "      Killing stale process on port 5000 (PID $procId)..."
        Stop-Process -Id $procId -Force -ErrorAction SilentlyContinue
    }
} else {
    Write-Host "      Port 5000 is free."
}
