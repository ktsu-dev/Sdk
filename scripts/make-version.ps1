param (
    [Parameter(Mandatory, Position=0)]
    [string]$github_sha = "" # SHA of the commit
)

Set-PSDebug -Trace 1

git config versionsort.suffix "-alpha"
git config versionsort.suffix "-beta"
git config versionsort.suffix "-rc"
git config versionsort.suffix "-pre"

# Get the first commit in the repo for use as a fallback
$FIRST_COMMIT = (git rev-list HEAD)[-1]
$LAST_COMMIT = $github_sha

# find the last version that was released
$ALL_TAGS = git tag --list --sort=-v:refname
$USING_FALLBACK_TAG = $false
if ($null -eq $ALL_TAGS) {
    $LAST_TAG = 'v1.0.0-pre.0'
    $USING_FALLBACK_TAG = $true
} elseif ($ALL_TAGS -is [array]) {
    $LAST_TAG = $ALL_TAGS[0]
} else {
    $LAST_TAG = $ALL_TAGS
}

Write-Host $LAST_TAG

$LAST_VERSION = $LAST_TAG -replace 'v', ''

Write-Host $LAST_VERSION

$IS_PRERELEASE = $LAST_VERSION.Contains('-')

$LAST_VERSION = $LAST_VERSION -replace '-alpha', ''
$LAST_VERSION = $LAST_VERSION -replace '-beta', ''
$LAST_VERSION = $LAST_VERSION -replace '-rc', ''
$LAST_VERSION = $LAST_VERSION -replace '-pre', ''

Write-Host $LAST_VERSION

$LAST_VERSION_COMPONENTS = $LAST_VERSION -split '\.'
$LAST_VERSION_MAJOR = [int]$LAST_VERSION_COMPONENTS[0]
$LAST_VERSION_MINOR = [int]$LAST_VERSION_COMPONENTS[1]
$LAST_VERSION_PATCH = [int]$LAST_VERSION_COMPONENTS[2]
$LAST_VERSION_PRERELEASE = 0
if ($LAST_VERSION_COMPONENTS.Length -gt 3) {
    $LAST_VERSION_PRERELEASE = [int]$LAST_VERSION_COMPONENTS[3]
}

# calculate which increment is needed

$EXCLUDE_BOTS = '^(?!.*(\[bot\]|github|ProjectDirector|SyncFileContents)).*$'
$EXCLUDE_HIDDEN_FILES = ":(icase,exclude)*/.*"
$EXCLUDE_MARKDOWN_FILES = ":(icase,exclude)*/*.md"
$EXCLUDE_TEXT_FILES = ":(icase,exclude)*/*.txt"
$EXCLUDE_SOLUTIONS_FILES = ":(icase,exclude)*/*.sln"
$EXCLUDE_PROJECTS_FILES = ":(icase,exclude)*/*.*proj"
$EXCLUDE_URL_FILES = ":(icase,exclude)*/*.url"
$EXCLUDE_BUILD_FILES = ":(icase,exclude)*/Directory.Build.*"
$EXCLUDE_CI_FILES = ":(icase,exclude).github/workflows/*"
$EXCLUDE_PS_FILES = ":(icase,exclude)*/*.ps1"

$EXCLUDE_PRS = @'
^.*(Merge pull request|Merge branch 'main'|Updated packages in|Update.*package version).*$
'@

$INCLUDE_ALL_FILES = "*/*.*"

# Get all relevant commits since the last tag to the current commit
# If we're using a fallback tag, use the first commit instead
if ($USING_FALLBACK_TAG) {
    $LAST_TAG_COMMIT = $FIRST_COMMIT
    Write-Host "No tags found. Using first commit as starting point: $FIRST_COMMIT"
} else {
    $LAST_TAG_COMMIT = git rev-list -n 1 $LAST_TAG
}
$COMMIT_RANGE = "$LAST_TAG_COMMIT..$LAST_COMMIT"

# Find all non-merge commits in the range
$ALL_NON_MERGE_COMMITS = git log --perl-regexp --regexp-ignore-case --format=format:%H --grep="$EXCLUDE_PRS" --invert-grep $COMMIT_RANGE

# Default version increment starts at prerelease
$VERSION_INCREMENT = 'prerelease'

# Check all relevant commits for version markers
foreach ($COMMIT in $ALL_NON_MERGE_COMMITS) {
    $COMMIT_MESSAGE = git log -n 1 --format=format:%s $COMMIT

    if ($COMMIT_MESSAGE.Contains('[major]')) {
        $VERSION_INCREMENT = 'major'
        # No need to check further commits if we've found a major bump
        break
    } elseif ($COMMIT_MESSAGE.Contains('[minor]') -and $VERSION_INCREMENT -ne 'major') {
        $VERSION_INCREMENT = 'minor'
    } elseif ($COMMIT_MESSAGE.Contains('[patch]') -and $VERSION_INCREMENT -notin @('major', 'minor')) {
        $VERSION_INCREMENT = 'patch'
    } elseif ($COMMIT_MESSAGE.Contains('[pre]') -and $VERSION_INCREMENT -eq 'prerelease') {
        # Keep as prerelease, which is the default
    }
}

# If no explicit version markers were found, check file changes
if ($VERSION_INCREMENT -eq 'prerelease') {
    # Check if there are any commits that would warrant a patch increment
    $HAS_PATCH_COMMITS = $null -ne (git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer="$EXCLUDE_BOTS" --author="$EXCLUDE_BOTS" --grep="$EXCLUDE_PRS" --invert-grep $COMMIT_RANGE)

    # Check if there are any commits that would warrant a minor increment based on file content changes
    $HAS_MINOR_COMMITS = $null -ne (git log -n 1 --topo-order --perl-regexp --regexp-ignore-case --format=format:%H --committer="$EXCLUDE_BOTS" --author="$EXCLUDE_BOTS" --grep="$EXCLUDE_PRS" --invert-grep $COMMIT_RANGE `
        -- `
        $INCLUDE_ALL_FILES `
        $EXCLUDE_HIDDEN_FILES `
        $EXCLUDE_MARKDOWN_FILES `
        $EXCLUDE_TEXT_FILES `
        $EXCLUDE_SOLUTIONS_FILES `
        $EXCLUDE_PROJECTS_FILES `
        $EXCLUDE_URL_FILES `
        $EXCLUDE_BUILD_FILES `
        $EXCLUDE_PS_FILES `
        $EXCLUDE_CI_FILES)

    if ($HAS_MINOR_COMMITS) {
        $VERSION_INCREMENT = 'minor'
    } elseif ($HAS_PATCH_COMMITS) {
        $VERSION_INCREMENT = 'patch'
    }
}

# Calculate the new version based on the determined increment type
if ($IS_PRERELEASE) {
    if ($VERSION_INCREMENT -eq 'prerelease') {
        $NEW_PRERELEASE = $LAST_VERSION_PRERELEASE + 1
        $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$LAST_VERSION_PATCH-pre.$NEW_PRERELEASE"
    } elseif ($VERSION_INCREMENT -eq 'patch') {
        $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$LAST_VERSION_PATCH"
    } elseif ($VERSION_INCREMENT -eq 'minor') {
        $NEW_MINOR = $LAST_VERSION_MINOR + 1
        $VERSION = "$LAST_VERSION_MAJOR.$NEW_MINOR.0"
    } elseif ($VERSION_INCREMENT -eq 'major') {
        $NEW_MAJOR = $LAST_VERSION_MAJOR + 1
        $VERSION = "$NEW_MAJOR.0.0"
    }
} else {
    if ($VERSION_INCREMENT -eq 'prerelease') {
        $NEW_PATCH = $LAST_VERSION_PATCH + 1
        $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$NEW_PATCH-pre.1"
    } elseif ($VERSION_INCREMENT -eq 'patch') {
        $NEW_PATCH = $LAST_VERSION_PATCH + 1
        $VERSION = "$LAST_VERSION_MAJOR.$LAST_VERSION_MINOR.$NEW_PATCH"
    } elseif ($VERSION_INCREMENT -eq 'minor') {
        $NEW_MINOR = $LAST_VERSION_MINOR + 1
        $VERSION = "$LAST_VERSION_MAJOR.$NEW_MINOR.0"
    } elseif ($VERSION_INCREMENT -eq 'major') {
        $NEW_MAJOR = $LAST_VERSION_MAJOR + 1
        $VERSION = "$NEW_MAJOR.0.0"
    }
}

# Output the version information
Write-Host "LAST_VERSION: $LAST_VERSION"
Write-Host "LAST_VERSION_MAJOR: $LAST_VERSION_MAJOR"
Write-Host "LAST_VERSION_MINOR: $LAST_VERSION_MINOR"
Write-Host "LAST_VERSION_PATCH: $LAST_VERSION_PATCH"
Write-Host "LAST_VERSION_PRERELEASE: $LAST_VERSION_PRERELEASE"
Write-Host "IS_PRERELEASE: $IS_PRERELEASE"
Write-Host "FIRST_COMMIT: $FIRST_COMMIT"
Write-Host "LAST_COMMIT: $LAST_COMMIT"
Write-Host "LAST_TAG_COMMIT: $LAST_TAG_COMMIT"
Write-Host "USING_FALLBACK_TAG: $USING_FALLBACK_TAG"
Write-Host "COMMIT_RANGE: $COMMIT_RANGE"
Write-Host "VERSION_INCREMENT: $VERSION_INCREMENT"
Write-Host "VERSION: $VERSION"

# set the environment variables
"LAST_VERSION=$LAST_VERSION" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_MAJOR=$LAST_VERSION_MAJOR" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_MINOR=$LAST_VERSION_MINOR" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_PATCH=$LAST_VERSION_PATCH" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_VERSION_PRERELEASE=$LAST_VERSION_PRERELEASE" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"IS_PRERELEASE=$IS_PRERELEASE" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"FIRST_COMMIT=$FIRST_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_COMMIT=$LAST_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"LAST_TAG_COMMIT=$LAST_TAG_COMMIT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"USING_FALLBACK_TAG=$USING_FALLBACK_TAG" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"VERSION_INCREMENT=$VERSION_INCREMENT" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append
"VERSION=$VERSION" | Out-File -FilePath $Env:GITHUB_ENV -Encoding utf8 -Append

# output files
$VERSION | Out-File -FilePath VERSION.md -Encoding utf8

$global:LASTEXITCODE = 0
