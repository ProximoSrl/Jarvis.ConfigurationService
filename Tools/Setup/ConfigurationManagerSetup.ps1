param(
    [string] $deployFileName,
    [string] $installationRoot
)

Write-Host 'Stopping Service Jarvis - Configuration Service'

$configurationDir = "$installationRoot\ConfigurationStore"
$installationRoot = "$installationRoot\ConfigurationManagerHost"
Write-Host "Installing configuration manager host in $installationRoot"
$service = Get-Service -Name "Jarvis - Configuration Service" -ErrorAction SilentlyContinue 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Configuration Service"
    $service.WaitForStatus("Stopped")
}

Write-Host 'Deleting actual directory'

if (Test-Path $installationRoot) 
{
    Remove-Item $installationRoot -Recurse -Force
}

Write-Host "Unzipping setup file"
Expand-WithShell -zipFile $deployFileName -destinationFolder $installationRoot

if ($service -eq $null) 
{
    Write-Host "Starting the service in $finalInstallDir\Jarvis.ConfigurationService.Host.exe"

    & "$installationRoot\Jarvis.ConfigurationService.Host.exe" install
} 

Write-Host "Changing configuration"

$configFileName = $installationRoot + "\Jarvis.ConfigurationService.Host.exe.config"
$xml = [xml](Get-Content $configFileName)
 
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='uri']/@value" -value "http://localhost:55555"
Edit-XmlNodes $xml -xpath "/configuration/appSettings/add[@key='baseConfigDirectory']/@value" -value "..\ConfigurationStore"

$xml.save($configFileName)

if(!(Test-Path -Path $configurationDir))
{
    New-Item -ItemType directory -Path $configurationDir
}

Write-Host "Removing sample directories"
$configurationSampleDir = $installationRoot + "\Configuration.Sample"
Remove-Item -Force -Path $configurationSampleDir -Recurse
$configurationStoreDefaultDir = $installationRoot + "\ConfigurationStore"
Remove-Item -Force -Path $configurationStoreDefaultDir -Recurse

Write-Host 'Starting the service'
Start-Service "Jarvis - Configuration Service"
Write-Host "Jarvis Configuration Service Installed"

