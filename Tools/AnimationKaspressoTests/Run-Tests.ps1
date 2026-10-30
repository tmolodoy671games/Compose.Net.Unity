param(
    [string]$UnityEditor = 'C:\Program Files\Unity\Hub\Editor\7000.0.0a12\Editor\Unity.exe',
    [string]$ComposeNetDirectory = 'C:\Users\nikit\RiderProjects\Compose.Net'
)
$ErrorActionPreference = 'Stop'
$projectRoot = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '../..'))
$probeRoot = Join-Path $projectRoot 'Temp/AnimationKaspressoProbe'
$resultsRoot = Join-Path $projectRoot 'Build/AnimationKaspressoTests'
$runtimeAssembly = Join-Path $ComposeNetDirectory 'Compose.Net/bin/Debug/netstandard2.1/Compose.Net.dll'
$editorDirectory = Split-Path $UnityEditor
$nunitPackage = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory -Filter 'com.unity.ext.nunit@*' | Select-Object -First 1
$testPackage = Get-ChildItem "$projectRoot/Library/PackageCache" -Directory -Filter 'com.unity.test-framework@*' | Select-Object -First 1
$nunitDll = Get-ChildItem $nunitPackage.FullName -Recurse -Filter nunit.framework.dll | Select-Object -First 1 -ExpandProperty FullName
foreach ($directory in @("$probeRoot/Assets/Tests", "$probeRoot/Assets/Runner", "$probeRoot/Assets/Plugins", "$probeRoot/Packages", "$probeRoot/ProjectSettings", $resultsRoot)) {
    New-Item -ItemType Directory -Force $directory | Out-Null
}
Push-Location $projectRoot
try {
    & dotnet build "$ComposeNetDirectory/Compose.Net/Compose.Net.csproj" --no-restore -m:1 -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Compose runtime compilation failed.' }
    # Compile adapter and tests against the same runtime that the probe will load.
    # Unity's generated project otherwise references the last imported binary.
    $runtimeReference = Join-Path $probeRoot 'RuntimeReference.targets'
    [IO.File]::WriteAllText($runtimeReference, @'
<Project>
  <Target Name="UseProbeRuntime" BeforeTargets="ResolveAssemblyReferences">
    <ItemGroup>
      <Reference Remove="Compose.Net" />
      <Reference Include="Compose.Net"><HintPath>$(ComposeRuntimeAssembly)</HintPath><Private>False</Private></Reference>
    </ItemGroup>
  </Target>
</Project>
'@)
    & dotnet build Compose.Net.Unity.csproj --no-restore -m:1 -p:UseSharedCompilation=false "-p:CustomAfterMicrosoftCommonTargets=$runtimeReference" "-p:ComposeRuntimeAssembly=$runtimeAssembly" -v:q '-clp:ErrorsOnly'
    if ($LASTEXITCODE -ne 0) { throw 'Unity adapter compilation failed.' }
    & dotnet build "$ComposeNetDirectory/Compose.Net.Kaspresso/Compose.Net.Kaspresso.csproj" --no-restore -m:1 -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Kaspresso compilation failed.' }
    Copy-Item "$ComposeNetDirectory/Compose.Net.Kaspresso/bin/Debug/netstandard2.1/Compose.Net.Kaspresso.dll" "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Editor/Compose.Net.Kaspresso.dll" -Force
    & dotnet build "$PSScriptRoot/AnimationKaspressoTests.csproj" "-p:NUnitPath=$nunitDll" "-p:UnityEditorDirectory=$editorDirectory" "-p:ComposeRuntimeAssembly=$runtimeAssembly" -m:1 -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Kaspresso tests compilation failed.' }
    & dotnet build "$ComposeNetDirectory/Compose.Net.Patch/Compose.Net.Patch.csproj" --no-restore -m:1 -p:UseSharedCompilation=false -v:q
    if ($LASTEXITCODE -ne 0) { throw 'Compose patcher compilation failed.' }
} finally { Pop-Location }
$assemblies = @(
    "$projectRoot/Temp/Bin/Debug/Compose.Net.Unity/Compose.Net.Unity.dll",
    "$ComposeNetDirectory/Compose.Net/bin/Debug/netstandard2.1/Compose.Net.dll",
    "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Compose.Net.Navigation.dll",
    "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Editor/Compose.Net.Kaspresso.dll",
    "$projectRoot/Assets/Packages/T.Molodoy.StableCollections.0.0.45/lib/netstandard2.1/StableCollections.dll",
    "$projectRoot/Assets/Packages/T.Molodoy.SharpExtensions.0.0.48/lib/netstandard2.1/Sharp Extensions.dll",
    "$projectRoot/Library/ScriptAssemblies/Unity.InputSystem.dll",
    "$projectRoot/Library/ScriptAssemblies/UnityEngine.UI.dll",
    "$projectRoot/Library/ScriptAssemblies/UnityEditor.UI.dll"
)
foreach ($assembly in $assemblies) { Copy-Item -LiteralPath $assembly -Destination "$probeRoot/Assets/Plugins" -Force }
Copy-Item "$PSScriptRoot/bin/Debug/netstandard2.1/Compose.Net.Unity.Animation.Kaspresso.Tests.dll" "$probeRoot/Assets/Tests" -Force
Copy-Item "$PSScriptRoot/bin/Debug/netstandard2.1/Compose.Net.Unity.Animation.Kaspresso.Tests.pdb" "$probeRoot/Assets/Tests" -Force
$testMetadata = (Get-Content -Raw "$projectRoot/Assets/Packages/Compose.Net.Unity/Assemblies/Editor/Compose.Net.Kaspresso.dll.meta").Replace('49abcf66bbae4707acd6db6d7b9f11a2', '77f4cb624dbc4c8a8900d95414d9512b')
[IO.File]::WriteAllText("$probeRoot/Assets/Tests/Compose.Net.Unity.Animation.Kaspresso.Tests.dll.meta", $testMetadata)
$patcher = "$ComposeNetDirectory/Compose.Net.Patch/bin/Debug/net8.0/Compose.Net.Patch.dll"
foreach ($assembly in @("$probeRoot/Assets/Plugins/Compose.Net.dll", "$probeRoot/Assets/Tests/Compose.Net.Unity.Animation.Kaspresso.Tests.dll")) {
    & dotnet $patcher $assembly --search-dir "$probeRoot/Assets/Plugins" --search-dir "$editorDirectory/Data/Managed/UnityEngine"
    if ($LASTEXITCODE -ne 0) { throw "Compose patch failed: $assembly" }
}
$utf8 = New-Object Text.UTF8Encoding($false)
$runnerDefinition = @{
    name = 'Animation.Kaspresso.Runner'
    includePlatforms = @('Editor')
    overrideReferences = $true
    precompiledReferences = @('Compose.Net.Unity.Animation.Kaspresso.Tests.dll', 'nunit.framework.dll')
    optionalUnityReferences = @('TestAssemblies')
    autoReferenced = $false
}
[IO.File]::WriteAllText("$probeRoot/Assets/Runner/Runner.asmdef", ($runnerDefinition | ConvertTo-Json -Depth 4), $utf8)
[IO.File]::WriteAllText("$probeRoot/Assets/Runner/Scenarios.cs", "public class SizeScenarios : AnimateContentSizeKaspressoTests {}`npublic class VisibilityScenarios : AnimatedVisibilityKaspressoTests {}`npublic class ContentScenarios : AnimatedContentKaspressoTests {}`npublic class IncomingScenarios : IncomingContentKaspressoTests {}`npublic class ClickScenarios : ClickIndicationKaspressoTests {}", $utf8)
$manifest = @{ dependencies = @{
    'com.unity.test-framework' = 'file:' + $testPackage.FullName.Replace('\', '/')
    'com.unity.ext.nunit' = 'file:' + $nunitPackage.FullName.Replace('\', '/')
    'com.unity.modules.jsonserialize' = '1.0.0'
    'com.unity.modules.imgui' = '1.0.0'
    'com.unity.modules.ui' = '1.0.0'
    'com.unity.modules.uielements' = '1.0.0'
} }
[IO.File]::WriteAllText("$probeRoot/Packages/manifest.json", ($manifest | ConvertTo-Json -Depth 8), $utf8)
Copy-Item "$projectRoot/ProjectSettings/ProjectVersion.txt" "$probeRoot/ProjectSettings" -Force
$arguments = @('-batchmode', '-nographics', '-projectPath', ('"' + $probeRoot + '"'),
    '-runTests', '-testPlatform', 'EditMode', '-assemblyNames', 'Animation.Kaspresso.Runner',
    '-testResults', ('"' + "$resultsRoot/results.xml" + '"'), '-logFile', ('"' + "$resultsRoot/editor.log" + '"'))
$process = Start-Process -FilePath $UnityEditor -ArgumentList $arguments -WindowStyle Hidden -Wait -PassThru
if ($process.ExitCode -ne 0) { throw "Unity exited with $($process.ExitCode). See $resultsRoot/editor.log" }
[xml]$results = Get-Content -Raw "$resultsRoot/results.xml"
$run = $results.'test-run'
Write-Output "Animation Kaspresso: $($run.passed) passed, $($run.failed) failed, $($run.skipped) skipped."
if ([int]$run.total -ne 54 -or [int]$run.passed -ne 54) { throw 'Expected exactly 54 passing Kaspresso tests.' }
