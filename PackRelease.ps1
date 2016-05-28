Param
(
    [String] $Configuration,
    [String] $DestinationDir = "",
    [String] $DeleteOriginalAfterZip = "$true",
    [String] $StandardZipFormat = "$false"
)

Write-Output "Configuration = $Configuration, DestinationDir = $DestinationDir, DeleteOriginalAfterZip = $DeleteOriginalAfterZip, StandardZipFormat = $StandardZipFormat"
#they are string because of a teamcity problem: http://stackoverflow.com/questions/26340157/how-to-set-a-powershell-switch-parameter-from-teamcity-build-configuration/35074980
$DeleteOriginalAfterZipBool = ($DeleteOriginalAfterZip -eq "$true") -or ($DeleteOriginalAfterZip -eq "true")
$StandardZipFormatBool =  ($StandardZipFormat -eq "$true") -or ($StandardZipFormat -eq "true")

Write-Output "standardZipFormatBool = $StandardZipFormatBool, DeleteOriginalAfterZipBool = $DeleteOriginalAfterZipBool"

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

if ($DestinationDir -eq "") 
{
    $DestinationDir = $runningDirectory + "\release"
}
elseif ($DestinationDir.StartsWith(".")) 
{
     $DestinationDir = $runningDirectory + "\" + $DestinationDir.Substring(1)
}

$DestinationDir = [System.IO.Path]::GetFullPath($DestinationDir)
$DestinationDirHost = $DestinationDir + "\Jarvis.ConfigurationService.Host"
$DestinationDirClient = $DestinationDir + "\Jarvis.ConfigurationService.Client"

Write-Host "Destination dir is $DestinationDir"

if(Get-Module -name jarvisUtils) 
{
    Remove-Module -Name "jarvisUtils"
}

if(-not(Get-Module -name jarvisUtils)) 
{
    Import-Module -Name ".\jarvisUtils"
}

if(Test-Path -Path $DestinationDir )
{
    Remove-Item $DestinationDir -Recurse
}

$DestinationDir = $DestinationDir.TrimEnd('/', '\')

New-Item -ItemType Directory -Path $DestinationDir
New-Item -ItemType Directory -Path $DestinationDirHost
New-Item -ItemType Directory -Path $DestinationDirClient

Copy-Item ".\src\Jarvis.ConfigurationService.Host\bin\$configuration\*.*" `
    $DestinationDirHost `
    -Force -Recurse

$appDir = $DestinationDirHost.ToString() + "\app"
New-Item -Force -ItemType directory -Path $appDir

Copy-Item ".\src\Jarvis.ConfigurationService.Host\app" `
    $DestinationDirHost `
    -Force -Recurse

Write-Host "Destination dir is  $DestinationDirHost"
$configFileName = $DestinationDirHost + "\Jarvis.ConfigurationService.Host.exe.config"
Write-Host "Changing configuration file $configFileName"
$xml = [xml](Get-Content $configFileName)
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='uri']/@value" -value "http://localhost:55555"
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='baseConfigDirectory']/@value" -value "..\ConfigurationStore"

$xml.save($configFileName)

Write-Host "Cleaning up $DestinationDirHost"
Get-ChildItem $DestinationDirHost -Include *.xml | foreach ($_) {
    if (!$_.fullname.EndsWith("Jarvis.ConfigurationService.Host.XML")) 
    {
         remove-item $_.fullname
    }

}


Write-Host "Copying file for client"
Copy-Item ".\src\Jarvis.ConfigurationService.Client\bin\$configuration\*.*" `
    $DestinationDirClient `
    -Force -Recurse
Write-Host "Cleaning up $DestinationDirClient"
Get-ChildItem $DestinationDirClient -Include *.xml | foreach ($_) {remove-item $_.fullname}

Write-Host "Compressing everything with 7z"
$sevenZipExe = "c:\Program Files\7-Zip\7z.exe"
if (-not (test-path $sevenZipExe)) 
{
    $sevenZipExe =  "C:\Program Files (x86)\7-Zip\7z.exe"
    if (-not (test-path $sevenZipExe)) 
    {
        throw "$env:ProgramFiles\7-Zip\7z.exe needed"
        Exit 
    }

} 
set-alias sz $sevenZipExe 

$extension = ".7z"
if ($StandardZipFormatBool -eq $true) 
{
    Write-Output "Choose standard ZIP format instead of 7Z"
    $extension = ".zip"
}
$Source = $DestinationDirHost + "\*"
$Target = $DestinationDir + "\Jarvis.ConfigurationService.Host" + $extension


sz a -mx=9 $Target $Source

$Source = $DestinationDirClient + "\*"
$Target = $DestinationDir + "\Jarvis.ConfigurationService.Client" + $extension

sz a -mx=9 $Target $Source

if ($DeleteOriginalAfterZipBool -eq $true) 
{
    Remove-Item $DestinationDirHost  -Recurse -Force
	Remove-Item $DestinationDirClient  -Recurse -Force
}

