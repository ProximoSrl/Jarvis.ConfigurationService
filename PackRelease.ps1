function PackRelease
{
    Param
    (
        [String]$Configuration
    )

    if(Test-Path -Path ".\release\" )
    {
        Remove-Item ".\release\" -Recurse
    }

    Copy-Item ".\src\Jarvis.ConfigurationService.Host\bin\$configuration\*.*" `
        ".\release\" `
         -Force 

    New-Item -Force -ItemType directory -Path ".\release\app"

    Copy-Item ".\src\Jarvis.ConfigurationService.Host\app" `
        ".\release\" `
         -Force -Recurse
}

PackRelease -Configuration "Release"