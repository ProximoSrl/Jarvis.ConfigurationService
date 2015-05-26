param(
    [string] $BranchName = 'master',
    [string] $InstallDir = '',
    [string] $teamCityBuildId = 'Jarvis_JarvisConfigurationService_Build'
)
Remove-Module teamCity

Import-Module -Name ".\teamCity.psm1"

if ($InstallDir -eq '') 
{
    $InstallDir = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition
}


Write-Host "Installing in:$finalInstallDir"
$user  = Read-Host 'What is your username?' 
$pass = Read-Host 'What is your password?' -AsSecureString
$plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass))

$lastBuildNumber = Get-LatestBuildNumber -url "demo.prxm.it:8811" -buildId $teamCityBuildId -branch "master" -username $user  -password $plainPassword
$baseBuildUri = Get-LatestBuildUri -url "demo.prxm.it:8811" -buildId $teamCityBuildId -latestBuildId $lastBuildNumber

if ($baseBuildUri -eq $null) 
{
    Write-Host "Error calling team city city server";
    return;
}

$targetpath = [System.IO.Path]::GetFullPath($InstallDir + "\Jarvis.ConfigurationService.Host.build-$lastBuildNumber.zip ") 
$finalInstallDir = [System.IO.Path]::GetFullPath($InstallDir + "\ConfigurationManagerHost")

$hostUri = "$baseBuildUri/Jarvis.ConfigurationService.Host.zip"

Write-Host "Download Host Url $hostUri"
Get-Artifact $hostUri $targetpath $user $plainPassword 

Write-Host 'Stopping Service Jarvis - Configuration Service'

$service = Get-Service -Name "Jarvis - Configuration Service" -ErrorAction SilentlyContinue 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Configuration Service"
}

Write-Host 'Deleting actual directory'

if (Test-Path $finalInstallDir) 
{
    Remove-Item $finalInstallDir -Recurse -Force
}
Write-Host "Unzipping host zip file"

$shell = new-object -com shell.application
$fullpath = [System.IO.Path]::GetFullPath($targetpath)

New-Item $finalInstallDir -type directory
$zip = $shell.NameSpace($fullpath)
foreach($item in $zip.items())
{
    Write-Host "unzipping " + $item.Name
    $shell.Namespace($finalInstallDir).copyhere($item)
}

if ($service -eq $null) 
{
    Write-Host "Starting the service in $finalInstallDir\Jarvis.ConfigurationService.Host.exe"

    & "$finalInstallDir\Jarvis.ConfigurationService.Host.exe" install
} 

#Write-Host 'Starting the service'
#Start-Service "Jarvis - Configuration Service"
Write-Host "Jarvis Configuration Service Installed"

