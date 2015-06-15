param(
    [string] $branchName = "master",
    [string] $InstallDir = "",
    [string] $user = '',
    [string] $plainPassword = ''
)

cls

if(-not(Get-Module -name teamCity)) 
{
    Import-Module -Name ".\teamCity"
}

if(-not(Get-Module -name jarvisUtils)) 
{
    Import-Module -Name ".\jarvisUtils"
}

$runningDirectory = Split-Path -Parent -Path $MyInvocation.MyCommand.Definition

$installDir =  $teamCityUrl = Get-StringAnswer "Enter Installation Dir [$runningDirectory]:" `
        -defaultValue $runningDirectory

$localInstall = Get-YesNoAnswer -question "Do you want to install with local artifacts? [Y/n]" -default "y"

if ($localInstall -eq 'n') 
{
    $teamCityUrl = Get-StringAnswer "Enter Team City Url [demo.prxm.it:8181]:" `
        -defaultValue "demo.prxm.it:8181"

    Write-Host "Team City Url is http://$teamCityUrl"

    if ($user -eq '') 
    {
        $user  = Read-Host "http://$teamCityUrl - What is your username?" 
        $pass = Read-Host "http://$teamCityUrl . what is your password?" -AsSecureString
        $plainPassword = [Runtime.InteropServices.Marshal]::PtrToStringAuto([Runtime.InteropServices.Marshal]::SecureStringToBSTR($pass))
    }
}


    if ($localInstall -eq 'y') 
    {
        $configurationManagerInstallFile = "$InstallDir\artifacts\Jarvis.ConfigurationService.Host.Zip" 
        if (!(Test-Path($configurationManagerInstallFile))) 
        {
            Write-Host "Installation File $configurationManagerInstallFile Not Present"
            return;
        }
    }
    else
    {
        $configurationManagerInstallFile = Get-ArtifactFromName `
            -teamCityBuildId "Jarvis_JarvisConfigurationService_Build" `
            -artifactName "Jarvis.ConfigurationService.Host.zip" `
            -targetPath "$installDir\artifacts" `
            -branchName $branchName `
            -user $user -plainPassword $plainPassword 
    }
    Write-Host "Installing from file $configurationManagerInstallFile"

    .\ConfigurationManagerSetup.ps1 `
        -deployFileName $configurationManagerInstallFile `
        -installationRoot "$InstallDir\ConfigurationManager"
