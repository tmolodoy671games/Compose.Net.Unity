param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\7000.0.0a12\Editor\Unity.exe',
    [string]$ResultsDirectory
)

$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
if (-not $ResultsDirectory) { $ResultsDirectory = Join-Path $projectRoot 'Build/AnimatedVisibilityTests' }
$ResultsDirectory = [IO.Path]::GetFullPath($ResultsDirectory)
$probeRoot = Join-Path $projectRoot 'Temp/AnimatedVisibilityProbe'
foreach ($directory in @($ResultsDirectory, "$probeRoot/Assets/Plugins", "$probeRoot/Assets/Tests/Editor", "$probeRoot/Packages", "$probeRoot/ProjectSettings")) {
    New-Item -ItemType Directory -Force -Path $directory | Out-Null
}

# Build the actual adapter; leave the open project's imported DLLs and scene alone.
Push-Location $projectRoot
try {
    & dotnet build Compose.Net.Unity.csproj --no-restore -m:1 -p:UseSharedCompilation=false -v minimal
    if ($LASTEXITCODE -ne 0) { throw 'Unity adapter build failed. Open the project in Unity first to generate its C# projects.' }
} finally { Pop-Location }

$assemblies = @(
    "$projectRoot/Temp/Bin/Debug/Compose.Net.Unity/Compose.Net.Unity.dll",
    "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Compose.Net.dll",
    "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Editor/Compose.Net.Kaspresso.dll",
    "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Compose.Net.Navigation.dll",
    "$projectRoot/Assets/Packages/T.Molodoy.SharpExtensions.0.0.48/lib/netstandard2.1/Sharp Extensions.dll",
    "$projectRoot/Assets/Packages/T.Molodoy.StableCollections.0.0.45/lib/netstandard2.1/StableCollections.dll",
    "$projectRoot/Library/ScriptAssemblies/Unity.InputSystem.dll",
    "$projectRoot/Library/ScriptAssemblies/UnityEngine.UI.dll",
    "$projectRoot/Library/ScriptAssemblies/UnityEditor.UI.dll"
)
foreach ($assembly in $assemblies) { Copy-Item -LiteralPath $assembly -Destination "$probeRoot/Assets/Plugins" -Force }
Copy-Item -LiteralPath "$projectRoot/Assets/Tests/Editor/AnimatedVisibility/AnimatedVisibilityLayoutTests.cs" -Destination "$probeRoot/Assets/Tests/Editor" -Force
Copy-Item -LiteralPath "$projectRoot/Assets/Tests/Editor/csc.rsp" -Destination "$probeRoot/Assets/Tests/Editor" -Force
$definition = Get-Content -Raw "$projectRoot/Assets/Tests/Editor/AnimatedVisibility/Compose.Net.Unity.AnimatedVisibility.Tests.asmdef" | ConvertFrom-Json
$definition.references = @() # The isolated project imports the adapter as a precompiled DLL.
$definition.precompiledReferences += 'Compose.Net.Unity.dll'
$utf8 = New-Object Text.UTF8Encoding($false)
[IO.File]::WriteAllText("$probeRoot/Assets/Tests/Editor/Compose.Net.Unity.AnimatedVisibility.Tests.asmdef", ($definition | ConvertTo-Json -Depth 8), $utf8)
$testFramework = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory -Filter 'com.unity.test-framework@*' | Select-Object -First 1
$nunit = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory -Filter 'com.unity.ext.nunit@*' | Select-Object -First 1
if (-not $testFramework -or -not $nunit) { throw 'Install Unity Test Framework in the main project first.' }
$manifest = @{ dependencies = @{
    'com.unity.test-framework' = 'file:' + $testFramework.FullName.Replace('\', '/')
    'com.unity.ext.nunit' = 'file:' + $nunit.FullName.Replace('\', '/')
    'com.unity.modules.jsonserialize' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.ui' = '1.0.0'
    'com.unity.modules.uielements' = '1.0.0'
} }
[IO.File]::WriteAllText("$probeRoot/Packages/manifest.json", ($manifest | ConvertTo-Json -Depth 8), $utf8)
Copy-Item -LiteralPath "$projectRoot/ProjectSettings/ProjectVersion.txt" -Destination "$probeRoot/ProjectSettings" -Force

# The test runner exits on completion. Do not pass -quit, which can prevent tests from running.
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $probeRoot + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-testFilter', 'AnimatedVisibilityLayoutTests',
    '-testResults', ('"' + "$ResultsDirectory/results.xml" + '"'), '-logFile', ('"' + "$ResultsDirectory/editor.log" + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity tests failed (exit $($process.ExitCode)). See $ResultsDirectory/editor.log" }
[xml]$results = Get-Content -Raw "$ResultsDirectory/results.xml"
$run = $results.'test-run'
Write-Output "AnimatedVisibility: $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped."
if ([int]$run.total -ne 50 -or [int]$run.passed -ne 50) { throw 'Expected exactly 50 passing tests.' }
