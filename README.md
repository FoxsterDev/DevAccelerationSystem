<h1 align="center">
<img alt="logo" src="Docs/Img7.png" height="200px" />
<br/>
Development Acceleration System for Unity projects
</h1>

The Development Acceleration System helps to enable features to speed up development iteration from code perspective.
It includes ProjectCompilation checks for all your target platforms with different scripting define symbols combinations in your project.

About conditional compilation you can find [here](https://docs.unity3d.com/Manual/PlatformDependentCompilation.html).
More about scripting define symbols you can find [here](https://docs.unity3d.com/Manual/CustomScriptingSymbols.html).

## Table of contents
<!-- TOC -->
* [Features](#features)
* [Problem](#problem)
* [Getting Started](#getting-started)
  * [Prerequisites](#Prerequisites)
  * [Installation](#installation)
    *  [Install via UPM (using Git URL)](#install-via-upm--using-git-url-)
    *  [Install manually (using Git URL)](#install-manually--using-git-url-)
  * [In Unity](#in-unity)
    * [Creating project compilation config](#creating-project-compilation-config)
    * [Configuring project compilation config](#configuring-project-compilation-config)
    * [Available menuitems](#available-menuitems)
    * [Compilation output viewer](#compilation-output-viewer)
    * [Compilation output known cases](#compilation-output-known-cases)
* [Usage in Batchmode](#usage-in-batchmode)
  * [Compilation output](#compilation-output)
* [Limitations](#limitations)
* [Recommendations](#recommendations)
* [How to contribute](#how-to-contribute)

<!-- TOC -->

## Features
1. Support configurations for player script compilation with build target and scripting define symbols.
2. Simplified scriptable object editor to run script compilation checks from Unity
3. Supports EditorMode and BatchMode. 
4. Compilation output viewer window to see the previous compilation results.
5. Configuring the compilation configs from your custom editor scripts or with using the **provided Editor.**

## Problem
When you have a project with multiple scripting define symbols and multiple build targets, it is hard to verify the compilation state of the project for all the configurations.

Let's see simplified example

```csharp
using UnityEngine;

public class ErrorWhenDevelopmentBuild  
{
     void Start()
    {
#if DEVELOPMENT_BUILD
        Debug.Log(Application.version);
#endif
    }
}
```
in this example if you run full code cleanup in Rider it will remove the using UnityEngine; (by default)
```csharp

public class ErrorWhenDevelopmentBuild  
{
     void Start()
    {
#if DEVELOPMENT_BUILD
        Debug.Log(Application.version);
#endif
    }
}
```
You will get the compilation error when try to build the project as development build.

Another use case might be when you have several android builds per different stores. You might introduce AMAZON_STORE, GOOGLE_STORE, HUAWEI_STORE scripting define symbols.
But Unity has only Android build target. In the case you should change player scripting define symbols before each build per target store.

## Benefits
- You can verify scripts compilation state of your project without actually switching the platform in Unity Editor and setting PlayerSettings.SetScriptingDefineSymbolsForGroup - **it saves development time**
- All build target configurations **at once** - **it saves CI/CD build agents resource, time and less aggressive build license usage** 
- Significantly reduce build time to get successful build, because **you reduce failed builds due to scripting define symbols issues.**

## Getting Started
### Prerequisites
Unity 2020.3+ version is required to use this package.
It is tested with 2020.3.38, 2021.3.33, 2022.3.13 Unity versions on Mac/ and basically Windows.

### Installation

#### Install via UPM (using Git URL)
1. Open the Package Manager window.
2. Click on the + icon in the top-left corner of the window.
3. Select "Add package from git URL".
4. Paste the following URL:
- ```json title="Add package from git URL"
  https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.1
  ```
  
#### Install manually (using Git URL)
1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies":

- ```json title="Packages/manifest.json"
    "com.foxsterdev.devaccelerationsystem": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.1"
  ```
### In Unity
#### Creating project compilation config
1. Find and select any available Editor folder inside Assets folder.
2. Right-click on the Editor folder and select Create > Assets > DevAccelerationSystem > Create ProjectCompilationConfig.
   ![Example12](Docs/Img12.png)
3. By default it will create a new ProjectCompilationConfig asset in the Editor folder.
   ![Example2](Docs/Img2.png)

#### Configuring project compilation config
1. Select the asset and configure the compilation settings. By default it has 10 compilation configs. You can add more or remove them.
   ![Example3](Docs/Img3.png)
2. For any config you can specify your own list of scripting define symbols
3. Default Unity Option - **None** will compile scripts without DEVELOPMENT_BUILD, **Development Build** will compile scripts with DEVELOPMENT_BUILD

#### Available menuitems 
1. Open the DevAccelerationSystem menuitems from the top menu: **Window > DevAccelerationSystem**
   ![Example4](Docs/Img4.png)
2. **Run All compilation** - will run the compilation checks for all the configurations.
3. **Focus config** - will open the ProjectCompilationConfig asset in the Inspector window.
4. **Show Compilation Output Viewer Window**  - will open the editor window to see the any previous compilation result.

#### Compilation output
1. By default you will see results in console window
2. Also you can see the compilation output in the Compilation Output Viewer window.
      ![Example15](Docs/Img15.png)   

**In Batchmode**
1. You can find a folder ProjectCompilationCheck inside the Library folder with unity logs and compilation output json
2. More details you find in terminal output

#### Compilation output known cases
1. if you specified config for a build target but the module is not installed you see error about it 
[ProjectCompilationCheck][Error] Compilation failed for WebGLNotDevelopment with errors:[1]: Unity module WebGL is not installed. Try to install the module and restart the unity.
2. if a config is not enabled it will be skipped during run
3. If I missed somewhere describing some error please create an issue

## Usage in Batchmode
1. When you configured config and tested in UnityEditor
2. You can use for reference the [ShellScript](./DevAccelerationSystem/Assets/DevAccelerationSystem/ShellScripts/unity_compilation_runner_mac.sh) to run the compilation checks in batch mode. It is tested on Mac
3. Parameters to provide <project_path> [compile_config_name] [unity_version]. project_path is required, compile_config_name and unity_version are optional
Example
```bash
   ./unity_compilation_runner_mac.sh /Users/PUT_HERE_YOURUSER_IF_ANY/Projects/DevAccelerationSystem/DevAccelerationSystem.DemoProject RunAll 2022.3.13f1
``` 
### Compilation output
1. You can find a folder ProjectCompilationCheck inside the Library folder with unity logs and compilation output json
2. More details you find in terminal output

## Limitations
1. It is just high level build of your scripts into dll's. It doesn't check the actual build of the project!
   ![Example6](Docs/Img6.png)
2. It uses Unity editor compilation API, so it might not be 100% accurate as the actual build. Especially when your scripting backend is IL2CPP.

## Recommendations 
1. If you use NewtonJson and don't need XML you can leave the example define symbol JSONNET_XMLDISABLE in compilation settings. It will reduce the build size by cut off unused functionality. Otherwise just remove it from compilation settings.
2. I recommend reviewing your biggest third-party docs (for example, BestHttp) and discovering any provided in-build define symbols to remove unused functionality compiled now. That is good advice to achieve a smaller build size, which correlates with more organic installs.
3. You can check compilation states and find scripting define related issues related for iOS target on Windows. Just install iOS module on Windows for Unity Editor and run the compilation checks. It will show you the errors related to iOS target.

## Demo project
You can find the demo project in the [Demo](https://github.com/FoxsterDev/DevAccelerationSystem/tree/master/DevAccelerationSystem.DemoProject).
It has example of the ProjectCompilationConfig asset and how to run the compilation checks with scripts examples to throw errors.

## How to contribute
Don't hesitate to [create an Issue ](https://github.com/FoxsterDev/DevAccelerationSystem/issues/new)
