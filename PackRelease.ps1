Param
(
    [String] $Configuration,
    [String] $DestinationDir = ""
)

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
Get-ChildItem $DestinationDirHost -Include *.xml | foreach ($_) {remove-item $_.fullname}


Write-Host "Copying file for client"
Copy-Item ".\src\Jarvis.ConfigurationService.Client\bin\$configuration\*.*" `
    $DestinationDirClient `
    -Force -Recurse
Write-Host "Cleaning up $DestinationDirClient"
Get-ChildItem $DestinationDirClient -Include *.xml | foreach ($_) {remove-item $_.fullname}

Write-Host "Compressing everything with 7z"
if (-not (test-path "$env:ProgramFiles\7-Zip\7z.exe")) {throw "$env:ProgramFiles\7-Zip\7z.exe needed"} 
set-alias sz "$env:ProgramFiles\7-Zip\7z.exe"  

$Source = $DestinationDirHost 
$Target = $DestinationDir + "\Jarvis.ConfigurationService.Host.7z"

sz a -mx=9 $Target $Source
Remove-Item $DestinationDirHost  -Recurse -Force

$Source = $DestinationDirClient 
$Target = $DestinationDir + "\Jarvis.ConfigurationService.Client.7z"

sz a -mx=9 $Target $Source
Remove-Item $DestinationDirClient  -Recurse -Force
