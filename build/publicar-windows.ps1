# ReNative3D36S - publicador automatico Windows (PowerShell 5.1+)
# Instala SDK somente para este projeto, sem privilegios administrativos.
[CmdletBinding()]
param([switch]$NaoAbrirPasta)
$ErrorActionPreference = 'Stop'
$ProgressPreference = 'SilentlyContinue'
$root = Split-Path -Parent $PSScriptRoot
$project = Join-Path $root 'src\ReNative3D36S\ReNative3D36S.csproj'
$toolsDir = Join-Path $root '.tools'
$sdkDir = Join-Path $toolsDir 'dotnet'
$installer = Join-Path $toolsDir 'dotnet-install.ps1'
$dest = Join-Path $root 'dist\portable'
$nugetConfig = Join-Path $root 'NuGet.Config'
$logDir = Join-Path $root 'logs'
New-Item -ItemType Directory -Force -Path $toolsDir,$logDir | Out-Null
$log = Join-Path $logDir ('publicacao-' + (Get-Date -Format 'yyyyMMdd-HHmmss') + '.log')
$script:logPath = $log
function Say([string]$msg) {
    $line = '[' + (Get-Date -Format 'HH:mm:ss') + '] ' + $msg
    Write-Host $line
    Add-Content -LiteralPath $script:logPath -Value $line -Encoding UTF8
}
function RunLogged([string]$exe, [string[]]$arguments) {
    # Nao encaminhar stderr nativo ao pipeline do PowerShell 5.1:
    # algumas ferramentas .NET escrevem no stderr e isso vira excecao
    # sem mensagem, escondendo o verdadeiro erro da compilacao.
    $stdout = Join-Path $logDir ('processo-' + [guid]::NewGuid().ToString('N') + '.out.log')
    $stderr = $stdout -replace '\.out\.log$', '.err.log'
    # Start-Process aceita uma unica command line. Escape argumentos com espacos.
    $quoted = foreach ($a in $arguments) {
        if ($a -match '[\s"]') { '"' + ($a -replace '"', '\"') + '"' } else { $a }
    }
    # -Wait faz o PowerShell aguardar o processo realmente terminar; sem
    # isso ExitCode pode vir vazio mesmo depois da instalacao ter concluido.
    $proc = Start-Process -FilePath $exe -ArgumentList ([string]::Join(' ', [string[]]$quoted)) -PassThru -Wait -RedirectStandardOutput $stdout -RedirectStandardError $stderr -WindowStyle Hidden
    $code = $proc.ExitCode
    foreach ($path in @($stdout,$stderr)) {
        if (Test-Path -LiteralPath $path) {
            Get-Content -LiteralPath $path -Encoding UTF8 | ForEach-Object {
                $line = [string]$_
                Write-Host $line
                Add-Content -LiteralPath $script:logPath -Value $line -Encoding UTF8
            }
        }
    }
    if ($null -eq $code) {
        Say "Saida detalhada preservada em: $stdout e $stderr"
        throw "O Windows nao informou o codigo de saida do processo: $exe"
    } elseif ($code -ne 0) {
        Say "Saida detalhada preservada em: $stdout e $stderr"
        throw "Processo falhou com codigo ${code}: $exe"
    }
}
function SDK10([string]$exe) {
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { return $false }
    try { $all = & $exe --list-sdks 2>$null; return [bool](@($all | Where-Object { $_ -match '^10\.' }).Count) }
    catch { return $false }
}
try {
    if (-not (Test-Path -LiteralPath $project)) { throw "Projeto nao encontrado: $project" }
    if (-not [Environment]::Is64BitOperatingSystem) { throw 'Este publicador precisa de Windows x64.' }
    [Net.ServicePointManager]::SecurityProtocol = [Net.ServicePointManager]::SecurityProtocol -bor [Net.SecurityProtocolType]::Tls12
    Say 'Iniciando publicacao automatica do ReNative3D36S v0.2.0...'
    # Check upstream RELEASE at build time without changing GUI code.
    # The GUI downloads the stable motor independently on first run and before builds.
    try {
        $pn = Invoke-RestMethod -Uri 'https://api.github.com/repos/whovian64/pocket-native/releases/latest' -TimeoutSec 12 -Headers @{'User-Agent'='ReNative3D36S-Publisher/0.2.0'}
        Say ('Pocket Native stable release available on GitHub: ' + $pn.tag_name + '. Engine files are installed/updated separately by the app.')
    } catch { Say ('AVISO: Nao foi possivel verificar Pocket Native no GitHub: ' + $_.Exception.Message) }

    $dotnet = Join-Path $sdkDir 'dotnet.exe'
    if (SDK10 $dotnet) {
        Say 'SDK .NET 10 local ja disponivel.'
    } else {
        $systemDotnet = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
        if ($systemDotnet -and (SDK10 $systemDotnet.Source)) {
            $dotnet = $systemDotnet.Source
            Say 'SDK .NET 10 detectado no Windows.'
        } else {
            Say 'SDK .NET 10 nao encontrado. Instalando SDK local, sem alterar o Windows...'
            Say 'Baixando instalador oficial da Microsoft (conexao com internet obrigatoria).'
            Invoke-WebRequest -UseBasicParsing -Uri 'https://dot.net/v1/dotnet-install.ps1' -OutFile $installer
            Say 'Instalando SDK .NET 10 na pasta .tools\dotnet...'
            # dotnet-install oferece instalacao por usuario, sem elevacao e sem mexer no PATH.
            RunLogged 'powershell.exe' @('-NoProfile','-ExecutionPolicy','Bypass','-File',$installer,'-Channel','10.0','-Architecture','x64','-InstallDir',$sdkDir,'-NoPath')
            if (-not (SDK10 $dotnet)) { throw 'SDK .NET 10 nao foi encontrado apos a instalacao.' }
            Say 'SDK instalado com sucesso.'
        }
    }
    # Evita que NuGet.Config global ou de empresa substitua nuget.org.
    # Pacotes runtime especificos de Windows x64 sao obtidos aqui na primeira publicacao.
    Say 'Verificando acesso ao catalogo oficial NuGet...'
    try {
        $req = [System.Net.HttpWebRequest]::Create('https://api.nuget.org/v3/index.json')
        $req.Timeout = 15000
        $req.ReadWriteTimeout = 15000
        $req.UserAgent = 'ReNative3D36S-AutoBuild/0.1.4'
        $response = $req.GetResponse()
        $response.Close()
        Say 'Fonte NuGet oficial acessivel.'
    } catch {
        Say ('AVISO: NuGet pode estar inacessivel (DNS, firewall, proxy ou internet): ' + $_.Exception.Message)
        Say 'Continuarei mesmo assim, pois os pacotes podem estar no cache local.'
    }
    $env:DOTNET_ROOT = Split-Path -Parent $dotnet
    $env:DOTNET_MULTILEVEL_LOOKUP = '0'
    $env:NUGET_PACKAGES = Join-Path $toolsDir 'nuget-packages'
    $env:NUGET_HTTP_CACHE_PATH = Join-Path $toolsDir 'nuget-http-cache'
    $env:NUGET_AUDIT_MODE = 'direct'
    New-Item -ItemType Directory -Force -Path $env:NUGET_PACKAGES | Out-Null
    Say 'Restaurando pacotes e runtime Windows x64 pelo NuGet oficial...'
    RunLogged $dotnet @('restore',$project,'--runtime','win-x64','--configfile',$nugetConfig,'--force','--disable-parallel','--verbosity','minimal','-p:PublishSelfContained=true','-p:PublishSingleFile=true','-p:PublishTrimmed=false')
    Say 'Validando estrutura XAML e o recurso R3D antes da publicacao...'
    foreach ($xaml in (Get-ChildItem -LiteralPath (Join-Path $root 'src\ReNative3D36S') -Filter '*.xaml')) {
        $xml = New-Object System.Xml.XmlDocument
        $xml.Load($xaml.FullName)
    }
    Add-Type -AssemblyName System.Drawing
    $iconPng = Join-Path $root 'src\ReNative3D36S\Assets\ReNative3D36S-icon.png'
    $iconIco = Join-Path $root 'src\ReNative3D36S\Assets\ReNative3D36S.ico'
    $bitmap = [System.Drawing.Image]::FromFile($iconPng)
    $bitmap.Dispose()
    $icon = New-Object System.Drawing.Icon -ArgumentList $iconIco
    $icon.Dispose()
    Say 'XAML bem formado e icones Windows decodificados com sucesso.'
    Say 'Publicando executavel standalone para Windows x64...' 
    New-Item -ItemType Directory -Force -Path $dest | Out-Null
    $previousExe = Join-Path $dest 'ReNative3D36S.exe'
    if (Test-Path -LiteralPath $previousExe) { Remove-Item -LiteralPath $previousExe -Force }
    RunLogged $dotnet @('publish',$project,'--no-restore','--configuration','Release','--runtime','win-x64','--self-contained','true','-p:PublishSingleFile=true','-p:PublishTrimmed=false','-p:EnableCompressionInSingleFile=true','-p:DebugType=None','--output',$dest)
    $exe = Join-Path $dest 'ReNative3D36S.exe'
    if (-not (Test-Path -LiteralPath $exe -PathType Leaf)) { throw 'Publicacao terminou, mas o executavel nao foi encontrado.' }
    $size = (Get-Item -LiteralPath $exe).Length
    if ($size -lt 1048576) { throw "O executavel foi gerado com tamanho inesperadamente pequeno ($size bytes)." }
    Say 'SUCESSO! ReNative3D36S.exe gerado.' 
    Say "Arquivo: $exe"
    Say "Log: $log"
    if (-not $NaoAbrirPasta) { Start-Process explorer.exe -ArgumentList ('"' + $dest + '"') }
    exit 0
} catch {
    Say ('ERRO: ' + ($_ | Out-String).Trim())
    Say 'Se ocorrer NU1100 ou NU1301: teste acesso a https://api.nuget.org/v3/index.json; revise firewall, proxy, antivirus e DNS.'
    Say 'O SDK local permanece instalado em .tools\dotnet para a proxima tentativa.'
    Say ('Consulte o log: ' + $log)
    exit 1
}
