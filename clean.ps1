cd $PSScriptRoot
if (Test-Path artifacts) {
	Remove-Item artifacts -Recurse
}
Remove-Item src*\*\project.lock.json
Remove-Item src*\*\bin -Recurse
Remove-Item src*\*\obj -Recurse