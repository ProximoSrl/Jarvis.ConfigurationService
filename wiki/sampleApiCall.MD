###Api calls

Apis are documented with swagger, just navigate to http://localhost:55555/swagger to test api.

###PowerShell invokation

It is interesting to invoke some API with PowerShell, expecially for installation scripts. These are sample PS calls to accomplish various tasks with Configuration Service

####Configure a new application

This call configure a new application, if the application is already configured, it does nothing. Phisically this calls creates a .redirect file for the new application, so you need to specify location of folder where base configuration is located.

```
Invoke-RestMethod -Uri "http://localhost:55556/api/applications/xxxx" -Method "PUT" -Body '{"ApplicationName" : "jarvis", "RedirectFolder" : "C:\\Develop\\xxx\\Assets\\Configs"}' -ContentType "application/json"
```

####Upload default parameters for an application

This call upload a new definition for default parameters, configuration manager does composition, and if a parameter is already defined it is not changed, but all new settings are stored in parameters configuration.

```
$fileContent = Get-Content -Path "C:\Develop\xxx\assets\defaultconfiguration"
Invoke-RestMethod -Uri "http://localhost:55556/api/defaultparameters/jarvis/HOSTNAME" -Method "PUT" -Body $fileContent -ContentType "application/json"

```

