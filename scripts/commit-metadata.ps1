git config --global user.name "Github Actions"
git config --global user.email "actions@users.noreply.github.com"

# Add standard metadata files
git add VERSION.md LICENSE.md AUTHORS.md COPYRIGHT.md CHANGELOG.md PROJECT_URL.url AUTHORS.url

# Add analyzer releases file if it exists
if (Test-Path "Sdk.Analyzers/AnalyzerReleases.Shipped.md") {
    git add Sdk.Analyzers/AnalyzerReleases.Shipped.md
}

# `git commit` exits non-zero when the index is clean, which fails the release on any run where
# the generated metadata happens to be identical to what is already committed (a re-run, or a
# release whose version and changelog did not move). Nothing to commit is a valid outcome here:
# skip straight to the push so RELEASE_HASH still resolves to the right commit.
git diff --cached --quiet
$NOTHING_TO_COMMIT = ($LASTEXITCODE -eq 0)

if ($NOTHING_TO_COMMIT) {
    Write-Host "No metadata changes to commit."
} else {
    git commit -m "[bot][skip ci] Update Metadata"
    if ($LASTEXITCODE -ne 0) {
        throw "git commit failed."
    }

    git push
    if ($LASTEXITCODE -ne 0) {
        throw "git push failed."
    }
}

$RELEASE_HASH = (git rev-parse HEAD)
Write-Host "RELEASE_HASH: $RELEASE_HASH"
"RELEASE_HASH=$RELEASE_HASH" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
