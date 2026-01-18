Set-PSDebug -Trace 1

# Read the version from VERSION.md
if (Test-Path VERSION.md) {
    $VERSION = (Get-Content VERSION.md -Raw).Trim()
    Write-Host "VERSION: $VERSION"
} else {
    Write-Host "VERSION.md not found, skipping analyzer releases update"
    exit 0
}

# Update AnalyzerReleases.Shipped.md if it exists and contains the placeholder
$SHIPPED_FILE = "Sdk.Analyzers/AnalyzerReleases.Shipped.md"
if (Test-Path $SHIPPED_FILE) {
    $CONTENT = Get-Content $SHIPPED_FILE -Raw
    if ($CONTENT -match '\{version\}') {
        Write-Host "Updating $SHIPPED_FILE with version $VERSION"
        $UPDATED_CONTENT = $CONTENT -replace '\{version\}', $VERSION
        $UPDATED_CONTENT | Out-File -FilePath $SHIPPED_FILE -Encoding utf8 -NoNewline
        Write-Host "Updated $SHIPPED_FILE"
    } else {
        Write-Host "No {version} placeholder found in $SHIPPED_FILE, skipping update"
    }
} else {
    Write-Host "$SHIPPED_FILE not found, skipping analyzer releases update"
}

# Update Sdk.targets files to replace {version} placeholder with actual SDK version
Get-ChildItem -Recurse -Filter "Sdk.targets" | ForEach-Object {
    $TARGET_FILE = $_.FullName
    $CONTENT = Get-Content $TARGET_FILE -Raw
    if ($CONTENT -match '\{version\}') {
        Write-Host "Updating $TARGET_FILE with version $VERSION"
        $UPDATED_CONTENT = $CONTENT -replace '\{version\}', $VERSION
        $UPDATED_CONTENT | Out-File -FilePath $TARGET_FILE -Encoding utf8 -NoNewline
        Write-Host "Updated $TARGET_FILE"
    } else {
        Write-Host "No {version} placeholder found in $TARGET_FILE, skipping update"
    }
}

$global:LASTEXITCODE = 0
