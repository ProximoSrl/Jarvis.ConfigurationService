#Installation with scripts

Simply copy all these scripts in the folder you want to install ConfigurationService, then from a PowerShell console, with administration rights (you need rights to install services) you can simply launch

	.\InstallFromTeamCity.ps1

You will be prompted for credentials for the build server, then it will download the latest build from selected branch (default is master) and install everything.

#Installation Layout

Base configuration install the host in subdirectory ConfigurationManagerHost, and rewrite the config file to use configuration store in a subfolder named ConfigurationStore. This will avoid further update to overwrite the ConfigurationStore directory

After the installation you should have this directory layout

	ConfigurationManagerHost
	--app
	--Logs
	ConfigurationStore
	InstallFromTeamCity.ps1
	Jarvis.ConfigurationService.Hos.build-xxxx.zip
	....
	....