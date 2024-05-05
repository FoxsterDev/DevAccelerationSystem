# DevAccelerationSystem
The DevAccelerationSystem package helps to enable features to speed up development iteration from code perspective.
It includes ProjectCompilation checks for all your target platforms with different scripting define symbols combinations in your project.

## Benefits
- You can verify compilation state of your project without actually switching the platform in Unity Editor.
- You can verify compilation state of your project without setting scripting define symbols with PlayerSettings.SetScriptingDefineSymbolsForGroup.
- You can verify compilation state of all build target configurations **at once**
- It supports EditorMode and BatchMode. You can easily integrate it into your CI/CD. Example bash script is provided in the package.
- You can configure the compilation configs from your custom scripts or with using the **provided Editor.** 
### Significantly reduce build time, build license usage because you reduce failed builds due to scripting define symbols issues.

## Prerequisites
### Unity >=2020.3
ProjectCompilation checks are only available for Unity 2020.3 and higher.
It is tested with 2020.3.38, 2021.3.31, 2022.3.13 Unity versions.

## Getting Started