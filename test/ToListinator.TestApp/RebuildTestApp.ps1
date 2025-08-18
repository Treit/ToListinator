# Rebuilding the test app
# The test application in this folder is a playground for verifying the analyzers actually work in real code, not unit test code.
# Because the analyzers are consumed from a local build of the NuGet package, the following steps are necessary:
# 1. Remove the cached NuGet package from the NuGet cache
# 2. Rebuild the local debug build of the NuGet package
# 3. Rebuild the test application
#
# NOTE: Initial build/restore failures after clearing the cache are expected and will auto-resolve.

Write-Host "Starting ToListinator test app rebuild process..." -ForegroundColor Green

# Navigate to the repository root
$repoRoot = (Get-Item $PSScriptRoot).Parent.Parent.FullName
Write-Host "Repository root: $repoRoot" -ForegroundColor Yellow
Set-Location $repoRoot

# Step 1: Clear NuGet package cache for ToListinator
Write-Host "`n1. Clearing NuGet cache for ToListinator..." -ForegroundColor Cyan
$packageName = "ToListinator"
$packageCachePaths = @(
    "$env:USERPROFILE\.nuget\packages\$packageName",
    "$env:NUGET_PACKAGES\$packageName"
)

foreach ($path in $packageCachePaths) {
    if (Test-Path $path) {
        Write-Host "   Removing cached package from: $path" -ForegroundColor Gray
        Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
    }
}

# Also clear the global packages folder if different
$globalPackagesPath = dotnet nuget locals global-packages --list 2>$null | ForEach-Object { if ($_ -match "global-packages: (.*)") { $matches[1] } }
if ($globalPackagesPath -and (Test-Path "$globalPackagesPath\$packageName")) {
    Write-Host "   Removing cached package from global packages: $globalPackagesPath\$packageName" -ForegroundColor Gray
    Remove-Item -Path "$globalPackagesPath\$packageName" -Recurse -Force -ErrorAction SilentlyContinue
}

# Step 2: Clean and rebuild the main ToListinator project (which packages the analyzers)
Write-Host "`n2. Rebuilding the ToListinator NuGet package..." -ForegroundColor Cyan
Set-Location "$repoRoot\src\ToListinator"

Write-Host "   Cleaning previous build..." -ForegroundColor Gray
dotnet clean --configuration Debug --verbosity quiet

Write-Host "   Building ToListinator package..." -ForegroundColor Gray
$buildResult = dotnet build --configuration Debug --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ERROR: Failed to build ToListinator package!" -ForegroundColor Red
    exit 1
}

Write-Host "   Creating NuGet package..." -ForegroundColor Gray
$packResult = dotnet pack --configuration Debug --no-build --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   ERROR: Failed to create NuGet package!" -ForegroundColor Red
    exit 1
}

# Step 3: Clean and rebuild the test application
Write-Host "`n3. Rebuilding the test application..." -ForegroundColor Cyan
Set-Location "$repoRoot\test\ToListinator.TestApp"

Write-Host "   Cleaning test app..." -ForegroundColor Gray
dotnet clean --verbosity quiet

Write-Host "   Restoring packages for test app..." -ForegroundColor Gray
dotnet restore --force --verbosity quiet
if ($LASTEXITCODE -ne 0) {
    Write-Host "   Note: Initial restore failure is expected when package cache was cleared. Continuing..." -ForegroundColor Yellow
}

Write-Host "   Performing final build. Should show analyzer diagnostics..."

dotnet clean
dotnet build

Write-Host "`nRebuild complete! The test application has been rebuilt with the latest analyzers." -ForegroundColor Green
Write-Host "Check the build output above for analyzer warnings (TL001, TL002, TL003, TL004, TL005, TL006, TL009)." -ForegroundColor Yellow
