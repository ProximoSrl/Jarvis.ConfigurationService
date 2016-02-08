Write-Output "Installing Jarvis.ConfigurationManager service. Script running from $PSScriptRoot"

$packageParameters = $env:chocolateyPackageParameters
Write-Output "Passed packageParameters: $packageParameters"

$arguments = @{}

# Script used to parse parameters is taken from https://github.com/chocolatey/chocolatey/wiki/How-To-Parse-PackageParameters-Argument

# Now we can use the $env:chocolateyPackageParameters inside the Chocolatey package
$packageParameters = $env:chocolateyPackageParameters

# Default the values
$installationRoot = "c:\jarvis\setup\ConfigurationManager\ConfigurationManagerHost"

# Now parse the packageParameters using good old regular expression
if ($packageParameters) {
    $match_pattern = "\/(?<option>([a-zA-Z]+)):(?<value>([`"'])?([a-zA-Z0-9- _\\:\.]+)([`"'])?)|\/(?<option>([a-zA-Z]+))"
    $option_name = 'option'
    $value_name = 'value'

    Write-Output "Parameters found, parsing with regex";
    if ($packageParameters -match $match_pattern )
    {
       $results = $packageParameters | Select-String $match_pattern -AllMatches
       $results.matches | % {
            
            $arguments.Add(
                $_.Groups[$option_name].Value.Trim(),
                $_.Groups[$value_name].Value.Trim())
       }
    }
    else
    {
        Throw "Package Parameters were found but were invalid (REGEX Failure)"
    }

    if ($arguments.ContainsKey("installationRoot")) {

        $installationRoot = $arguments["installationRoot"]
        Write-Output "installationRoot Argument Found: $installationRoot"
    }
} else 
{
    Write-Output "No Package Parameters Passed in"
}

Write-Output "Installing ConfigurationManager in folder $installationRoot"

$artifactFile = "$PSScriptRoot\..\Artifacts\Jarvis.ConfigurationService.Host.zip"

if(!(Test-Path -Path $artifactFile ))
{
     Throw "Unable to find package file $artifactFile"
}

Write-Output "Installing from artifacts: $artifactFile"

if(!(Test-Path -Path "$PSScriptRoot\ConfigurationManagerSetup.ps1" ))
{
     Throw "Unable to find package file $PSScriptRoot\ConfigurationManagerSetup.ps1"
}


if(-not(Get-Module -name jarvisUtils)) 
{
    Import-Module -Name "$PSScriptRoot\jarvisUtils"
}

& $PSScriptRoot\ConfigurationManagerSetup.ps1 -deployFileName $artifactFile -installationRoot $installationRoot