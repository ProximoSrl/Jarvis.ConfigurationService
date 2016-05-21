param(
    [string] $deployFileName,
    [string] $installationRoot
)

if(-not(Get-Module -name jarvisUtils)) 
{
    Write-Output (Split-Path -Parent -Path $MyInvocation.MyCommand.Definition)
    $runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
    Write-Output "loading jarvis utils module from $runningDirectory"
    Import-Module -Name "$runningDirectory\\jarvisUtils"
}

if(!(Test-Path -Path $deployFileName ))
{
     Throw "Unable to find package file $deployFileName"
}

$file = Get-Item $deployFileName

Write-Output "Starting Configuration Service installation script"
Write-Output "deployFileName:  $deployFileName"
Write-Output "installationRoot: $installationRoot"

Write-Output 'Stopping Service Jarvis - Configuration Service'
$configurationDir = "$installationRoot\ConfigurationStore"
$installationRoot = "$installationRoot\ConfigurationManagerHost"
Write-Output "Installing configuration manager host in $installationRoot"
$service = Get-Service -Name "Jarvis - Configuration Service" -ErrorAction SilentlyContinue 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Configuration Service"
    $service.WaitForStatus("Stopped")
}

Write-Output 'Deleting actual directory'

if (Test-Path $installationRoot) 
{
    Remove-Item $installationRoot -Recurse -Force
}

Write-Output "Unzipping setup file"
Expand-WithShell -zipFile $file.FullName -destinationFolder $installationRoot

if ($service -eq $null) 
{
    Write-Output "Starting the service in $finalInstallDir\Jarvis.ConfigurationService.Host.exe"

    & "$installationRoot\Jarvis.ConfigurationService.Host.exe" install
} 

Write-Output "Changing configuration"

$configFileName = $installationRoot + "\Jarvis.ConfigurationService.Host.exe.config"
$xml = [xml](Get-Content $configFileName)
 
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='uri']/@value" -value "http://localhost:55555"
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='baseConfigDirectory']/@value" -value "..\ConfigurationStore"

$xml.save($configFileName)

if(!(Test-Path -Path $configurationDir))
{
    New-Item -ItemType directory -Path $configurationDir
}

Write-Output "Removing sample directories"
$configurationSampleDir = $installationRoot + "\Configuration.Sample"
if (Test-Path ($configurationSampleDir)) 
{
    Remove-Item -Force -Path $configurationSampleDir -Recurse
}

$configurationStoreDefaultDir = $installationRoot + "\ConfigurationStore"
if (Test-Path ($configurationStoreDefaultDir)) 
{
    Remove-Item -Force -Path $configurationStoreDefaultDir -Recurse
}

Write-Output 'Starting the service'
Start-Service "Jarvis - Configuration Service"
Write-Output "Jarvis Configuration Service Installed"

