param($installPath, $toolsPath, $package, $project)

Write-Host $installPath
Write-Host $toolsPath
Write-Host $package
Write-Host $project

$packageRoot = $installPath + "\..\..\"

$cmdDest = $packageRoot + "build.cmd"
$fsxDest = $packageRoot + "build.fsx"
$nuspecDest = $packageRoot + "sample.nuspec"

Write-Host "Testing if " + $cmdDest " or " + $fsxDest + " exist."

$cmdNeeded = !(Test-Path($cmdDest))
$fsxNeeded = !(Test-Path($fsxDest))

if($cmdNeeded -and $fsxNeeded) {
 
	Write-Host "Copying files"

	$cmdPath = $toolsPath + "\build.cmd"
	$fsxPath = $toolsPath + "\build.fsx"
	$nuspecPath = $toolsPath + "\sample.nuspec.txt"

	write-host $cmdPath
	write-host $fsxPath

	Copy-Item $cmdPath $cmdDest
	Copy-Item $fsxPath $fsxDest

	if(!(Test-Path($nuspecDest))) {
		Write-Host "Writing nuspec sample"
		Copy-Item $nuspecPath $nuspecDest
	}
} else {
	Write-Host "Files already exist"
}