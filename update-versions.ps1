# get all .props and .targets files and replace <Sdk Name="ktsu.Sdk" Version=".*" /> with the version number from VERSION.md

# get the version number from VERSION.md
$version = (Get-Content -Path VERSION.md).Trim()

# get all .props and .targets files
$files = Get-ChildItem -Path . -Recurse -Include *.props, *.targets
# loop through each file
foreach ($file in $files) {
	# read the file content
	$content = Get-Content -Path $file.FullName
	# replace the version number
	$newContent = $content -replace "<Sdk Name=`"ktsu\.Sdk`" Version=`".*`" \/>", "<Sdk Name=`"ktsu.Sdk`" Version=`"$version`" />"
	# write the new content to the file
	Set-Content -Path $file.FullName -Value $newContent
	# print the file name
	Write-Host "Updated version in $($file.FullName)"
}
