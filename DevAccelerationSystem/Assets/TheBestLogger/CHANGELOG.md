# Changelog

## [2.2.4] - 2024-12-06
Fix
UnityException: get_isDebugBuild can only be called from the main thread.
Constructors and field initializers will be executed from the loading thread when loading a scene.
Don't use this function in the constructor or field initializers, instead move initialization code to the Awake or Start function.

## [1.0.0] - 2024-10-06
Added stacktraces configuration for Unity Application Log Types and for a specific LogTarget. You can remove your custom Application.SetStackTrace if any
Added option to inherit Unity Editor Console Log Target and customize print messages.
Added a property to LogTarget about getting in explicit way a right name for log target configuration
Fixed issues with applying remote configs. Added a clear method to merge new config into existed one. It will apply only known properties.

## [0.0.9] - 2024-09-22
This is the first release of TheBestLogger as a package.
