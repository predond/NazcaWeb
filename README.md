# NazcaWeb

That repository contains simple web application written in ASP.NET Core MVC technology.
In order to run a solution you need to:
- change path in 'StartPath' field in IRC.cs file (line 18)
- comment line 'InitializeCommandsAsync().Wait();' in IRD.cs file (line 17)
- change the IP address of server machine (can be localhost) in corresponding secion (depends which profile do you launch the app with, eg. http, https, etc.) in launchSettings.json in properties directory
