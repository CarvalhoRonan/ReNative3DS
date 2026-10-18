# Launcher smoke test, not a substitute for functional Windows/ARM64 tests.
$ErrorActionPreference = 'Stop'
$root = Split-Path -Parent $PSScriptRoot
$exe = Join-Path $root 'dist\portable\ReNative3D36S.exe'
$logDir = Join-Path $root 'logs'
New-Item -ItemType Directory -Force -Path $logDir | Out-Null
$log = Join-Path $logDir ('teste-abertura-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.txt')
function Note([string]$message) {
    Write-Host $message
    Add-Content -Path $log -Encoding UTF8 -Value $message
}
try {
    Note ('Executavel: ' + $exe)
    if (-not (Test-Path -LiteralPath $exe)) { throw 'O executavel nao foi encontrado. Execute COMPILAR-UM-CLIQUE.cmd primeiro.' }
    $proc = Start-Process -FilePath $exe -PassThru
    Note ('PID: ' + $proc.Id)
    Start-Sleep -Seconds 8
    $proc.Refresh()
    if ($proc.HasExited) {
        Note ('O programa fechou prematuramente. Codigo de saida: ' + $proc.ExitCode)
        Note ('Consulte o Visualizador de Eventos do Windows ou os logs em %LOCALAPPDATA%\ReNative3D36S\logs.')
        exit 1
    }
    Note 'O programa ainda esta em execucao depois de 8 segundos.'
    Note 'Verifique visualmente a janela. Este teste nao encerra o programa.'
    Note ('Relatorio: ' + $log)
    exit 0
} catch {
    Note ('ERRO: ' + $_.Exception.Message)
    exit 1
}
