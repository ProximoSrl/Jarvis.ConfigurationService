param(
    [string]$DestinationFolder = '..\..\ConfigurationService'
)

Import-Module -Name ".\Invoke-MsBuild.psm1"
Write-Host 'Cleaning and compiling solution'
$compileResult = Invoke-MsBuild -Path '..\src\Jarvis.ConfigurationService.sln' -MsBuildParameters "/target:Clean;Build /p:Configuration=Release"

if ($compileResult -eq $false) 
{
    Write-Error 'Compile failed'
    Exit 1
}

Write-Host 'Stopping Service Jarvis - Configuration Service'

$service = Get-Service -Name "Jarvis - Configuration Service" 
if ($service -ne $null) 
{
    Stop-Service "Jarvis - Configuration Service"
}

Write-Host 'Deleting actual directory'

#No need to remove old installation for configuration service
#Remove-Item -Recurse $DestinationFolder -Exclude ConfigurationStore

Write-Host 'Copy new deploy to destination folder'

$rco = robocopy '..\src\Jarvis.ConfigurationService.Host\bin\Release' $DestinationFolder /e
$rco = robocopy '..\src\Jarvis.ConfigurationService.Host\app' "$DestinationFolder\app" /e 

if ($service -eq $null) 
{
    Write-Host "Starting the service in $DestinationFolder\Jarvis.ConfigurationService.Host.exe"
    $ps = Start-Process "$DestinationFolder\Jarvis.ConfigurationService.Host.exe" -ArgumentList 'install' -Wait -NoNewWindow
    Write-Host "installing service exited with: $ps.ExitCode"
} 

Write-Host 'Starting the service'
Start-Service "Jarvis - Configuration Service"