param([string]$Configuration = "Release")
$ErrorActionPreference = "Stop"
Set-Location $PSScriptRoot
function Check-Exit([string]$Step) { if ($LASTEXITCODE -ne 0) { throw "$Step failed: exit $LASTEXITCODE" } }
# Run from a VS 2022 x64 Native Tools PowerShell with Desktop C++ and MASM installed.
cmake -S . -B artifacts/native -A x64 -DCESI_STRUCTURAL_TEST_BUILD=ON
Check-Exit "CMake configure"
cmake --build artifacts/native --config $Configuration
Check-Exit "Native build"
ctest --test-dir artifacts/native -C $Configuration --output-on-failure
Check-Exit "Native tests"
dotnet run --project tests/Cesi.SmokeTests -c $Configuration -- "artifacts/native/$Configuration/Syntax.dll"
Check-Exit "Managed tests"
dotnet publish ui/RblxExecutorUI.csproj -c $Configuration -r win-x64 --self-contained true -p:Platform=x64 -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o artifacts/cesi
Check-Exit "UI build"
Copy-Item "artifacts/native/$Configuration/Syntax.dll" "artifacts/cesi/Syntax.dll" -Force
Write-Host "Built UI and native core together: artifacts/cesi/cesi.exe"
Write-Host "Structural test: DataModel/account/avatar. Script execution remains pending profile validation."
