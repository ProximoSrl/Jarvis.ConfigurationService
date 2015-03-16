Jarvis.ConfigurationService <img src="http://demo.prxm.it:8811/app/rest/builds/buildType:Jarvis_JarvisConfigurationService_Build/statusIcon" alt="build status">
===========================

###System components

1. A server that hosts configuration and read config from file system
2. A client class to ask configuration to the system

###Conventions

Client calls server using a convention: the client is considered to be composed by several services collaborating togheter, all of that service constitute an "application". Given this the url that the client call use this format

	BaseUrl/ApplicationName/ServiceName.config/MachineName

Base Url is specified in an Environment Variable and the variable name should be given in the constructor of the client configuration class. This permits to different software to use different variables and point to different configuration service.

The application name is found simply looking in all parent directory of the executable or site folder for a file with .application extension. The name of the file is the name of the application. Ex: jarvis source directory has a 

	jarvis.application

in the root directory and each service of the suite share that application name. This file should start with the line
	
	#jarvis-config

to distinguish this file from other .application files, and it can contain other configurations, ex:

	#jarvis-config
	application-name : TESTAPPLICATION
	base-server-address : http://localhost:55555/

application-name overrides default application name (name of the application file) while base-server-address overrides the location of configuration service specified in the environment variable.

Service name is the name of the folder that contains the executable or the site that is running. A convention discard all folders called bin, debug, release, to avoid having all services called Debug or Release when launched from Visual Studio.

###Initialization

The Client component should be initialized with a simple call to a static initialization function.

	 public static void AppDomainInitializer
     (
        Action<String, Boolean, Exception> loggerFunction,
        String baseServerAddressEnvironmentVariable
    )

The first parameter is a lambda to an action that should log info and failures. This will prevent ConfigurationServiceClient class to depend on any logging system (log4net, nlog) or any other library es: Castle. You will need to pass a lambda that logs with your logger of choice.

Parameter baseServerAddressEnvironmentVariable is the name of the environment variable that contains base address of the configuration service.

###Configuration File Structure

An example of expected structure for configuration file is contained in the Configuration.Sample directory of the ConfigurationService. Configuration server looks for configuration in a single directory specified in configuration file, if no configuration is present the service expect configurations to be placed inside ConfigurationStore subfolder.

Inside the configuration store folder you should place a directory for each application configuration file, each application must contain a directory named Default that contains all the configuration file in the form **ServiceName.config**

You can place a file called base.config at application level that contains configuration common to all service. Each service can overrid configuration in the base.config. 

You can also place a base.config file at configuration store level, this file contains configurations shared by all applications.

####overriding configurations for specific machines

If you need to override a configuration for a specific machine, you can create another directory inside the application directory with the name of the machine with overridden configuration. Then you place a corresponding **ServiceName.config** file in that directory specifying only the configuration settings you want to override. Is duty of configuration service to compose json files to create final configuration.

###redirecting folder

During development is natural to store a default configuration for each service in source control, but this scatters configurations for all application through your Hard Disk and each time a configuration is changed in source control you need to copy configuration in relative directory of configuration service.

If you place a file called **ApplicationName.redirect** that contains a single line pointing to a physical directory, configuration server will follow that redirect. This de facto permits you to create a soft link from configuration store to the real directory where the files are. This is expecially useful during development to point to source controlled configuration files.

###Encrypting credentials

If you need to store some sensitive data inside file that will be included in source control you can use basic form of encryption. First you need to call a function to generate an encryption key

	http://localhost:55555/support/encryption/generatekey

This will return a json text with a fresh generated key, now you need to copy response inside a file called *encryption.machinename.key* inside the root of configuration folder. This will constitute the base encryption key for configuration for machine *machinename*

When encryption.key file is stored, you can issue a post to

	http://localhost:55555/support/encryption/encrypt

with such payload

	{StringToEncrypt : 'string you want to encrypt'}

This will return encrypted string. Now if you want to include an encrypted string inside config you should simply enclose with Dollar Sign, ex

	'$ldapPassword$' : '061FC47C86934D2B3311CE094CA61BB9'