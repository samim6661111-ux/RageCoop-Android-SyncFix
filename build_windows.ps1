$ErrorActionPreference = 'Stop'
$Root = Split-Path -Parent $MyInvocation.MyCommand.Path
$Src = Join-Path $Root 'source\RageCoop.Client'
$Refs = Join-Path $Root 'build_refs'
$Out = Join-Path $Root 'install\games\gtav\scripts\RageCoop'
$LogDir = Join-Path $Root 'build_logs'
New-Item -ItemType Directory -Force -Path $Out, $LogDir | Out-Null

$cscExeCandidates = @(
    (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\Roslyn\csc.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Professional\MSBuild\Current\Bin\Roslyn\csc.exe'),
    (Join-Path $env:ProgramFiles 'Microsoft Visual Studio\2022\Enterprise\MSBuild\Current\Bin\Roslyn\csc.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2019\BuildTools\MSBuild\Current\Bin\Roslyn\csc.exe'),
    (Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\2019\Community\MSBuild\Current\Bin\Roslyn\csc.exe')
)
$cscExe = $cscExeCandidates | Where-Object { $_ -and (Test-Path $_) } | Select-Object -First 1
$dotnetCmd = Get-Command 'dotnet.exe' -ErrorAction SilentlyContinue
$cscDll = $null
if (-not $cscExe -and $dotnetCmd) {
    $sdkLines = & $dotnetCmd.Source --list-sdks 2>$null
    foreach ($line in ($sdkLines | Sort-Object -Descending)) {
        if ($line -match '^([^ ]+) \[(.+)\]$') {
            $sdkVersion = $Matches[1]
            $sdkRoot = $Matches[2]
            $candidate = Join-Path (Join-Path $sdkRoot $sdkVersion) 'Roslyn\bincore\csc.dll'
            if (Test-Path $candidate) { $cscDll = $candidate; break }
        }
    }
    if (-not $cscDll) {
        $dotnetRoots = @($env:DOTNET_ROOT, $env:LocalAppData, $env:ProgramFiles) | Where-Object { $_ }
        foreach ($base in $dotnetRoots) {
            $sdkRoot = if ($base -eq $env:LocalAppData) { Join-Path $base 'Microsoft\dotnet\sdk' } else { Join-Path $base 'dotnet\sdk' }
            if (-not (Test-Path $sdkRoot)) { continue }
            $sdkDirs = Get-ChildItem -LiteralPath $sdkRoot -Directory -ErrorAction SilentlyContinue
            foreach ($sdk in ($sdkDirs | Sort-Object Name -Descending)) {
                $candidate = Join-Path $sdk.FullName 'Roslyn\bincore\csc.dll'
                if (Test-Path $candidate) { $cscDll = $candidate; break }
            }
            if ($cscDll) { break }
        }
    }
}
if (-not $cscExe -and -not $cscDll) {
    throw 'Roslyn C# compiler not found. Install Visual Studio 2022 Build Tools with Roslyn, or the .NET 8 SDK.'
}

$frameworkRefs = @(
    'mscorlib.dll','System.dll','System.Core.dll','System.Drawing.dll','System.Xml.dll',
    'System.Xml.Linq.dll','System.Windows.Forms.dll','System.Runtime.Serialization.dll',
    'System.Runtime.Serialization.Formatters.Soap.dll','System.Net.Http.dll',
    'System.IO.Compression.dll','System.IO.Compression.FileSystem.dll','System.Numerics.dll',
    'System.Configuration.dll','System.ComponentModel.Composition.dll','System.Data.dll',
    'System.Transactions.dll','System.Runtime.dll','netstandard.dll'
)
$extraRefs = @(
    'ScriptHookVDotNet.dll','ScriptHookVDotNet3.dll','LemonUI.SHVDN3.dll',
    'Lidgren.Network.dll','Newtonsoft.Json.dll','RageCoop.Core.dll'
)
foreach ($name in ($frameworkRefs + $extraRefs)) {
    $path = Join-Path $Refs $name
    if (-not (Test-Path $path)) { throw "Missing build reference: $path" }
}

$sourceFiles = @(Get-ChildItem -LiteralPath $Src -Recurse -File -Filter '*.cs' | Sort-Object FullName | ForEach-Object { $_.FullName })
if ($sourceFiles.Count -eq 0) { throw "No C# sources found in $Src" }
$clientOut = Join-Path $Out 'RageCoop.Client.dll'
$compilerArgs = New-Object 'System.Collections.Generic.List[string]'
foreach ($arg in @('/nologo','/noconfig','/target:library','/unsafe+','/optimize+','/langversion:latest','/nostdlib+','/platform:anycpu','/out:' + $clientOut)) {
    $compilerArgs.Add($arg)
}
foreach ($name in ($frameworkRefs + $extraRefs)) {
    $compilerArgs.Add('/reference:' + (Join-Path $Refs $name))
}
foreach ($source in $sourceFiles) { $compilerArgs.Add($source) }
$log = Join-Path $LogDir 'RageCoop.Client.log'
$compilerArgArray = $compilerArgs.ToArray()
if ($cscExe) {
    $compilerOutput = & $cscExe @compilerArgArray 2>&1
}
else {
    $compilerOutput = & $dotnetCmd.Source exec $cscDll @compilerArgArray 2>&1
}
$compilerExit = $LASTEXITCODE
$compilerOutput | Tee-Object -FilePath $log
if ($compilerExit -ne 0) { throw "RageCoop.Client compilation failed; see $log" }
if (-not (Test-Path $clientOut) -or (Get-Item $clientOut).Length -lt 4096) {
    throw 'Client DLL output missing or unexpectedly small.'
}
Write-Host "BUILT $clientOut ($((Get-Item $clientOut).Length) bytes)"
Write-Host ''
Write-Host 'BUILD COMPLETE. Copy ONLY this DLL after backing up the old one:'
Write-Host '  /storage/emulated/0/games/gtav/scripts/RageCoop/RageCoop.Client.dll'
Write-Host 'Keep the existing RageCoop.Core.dll, APK, and Stage 2.6 native bridge unchanged.'
