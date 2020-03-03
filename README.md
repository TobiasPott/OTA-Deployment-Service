# OTA-Deployment-Service
An ASP.NET Core web service to provide iOS OTA deployment in local network or internet environments.


This ASP.NET Core web service can be used to provide OTA deployment of ad-hoc developer or enterprise applications for iOS. It can be used as a service running on machine in a local network or as a hosted service in Azure (or other cloud services which can run .NET Core applications).

## To-Do
* There are no authentication or security measures to prevent access to the service without permission
* There is no check for validity of the uploaded files and provided metadata (IPA file, size, identifier and name)
* There is no size-check to prevent the server's storage to overflow on large IPA files
* There is no check agains duplicates, uploads for the same identifier replace previous files
* There is no automatic listing of available IPA files on the web service (needs consideration on how in general and how visibility is managed)
* There is no method to delete previously uploaded files
* There is no method to edit or change uploaded files (needs consideration if this is required aside of deletion)
