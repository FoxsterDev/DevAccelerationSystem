# DevAccelerationSystem for Unity projects
The DevAccelerationSystem tool helps to enable features to speed up development iteration from code perspective.
It includes ProjectCompilation checks for all your target platforms with different scripting define symbols combinations in your project.

## Benefits
### You can verify scripts compilation state of
- your project without actually switching the platform in Unity Editor.
- your project without setting scripting define symbols with PlayerSettings.SetScriptingDefineSymbolsForGroup.
- all build target configurations **at once**
- It supports EditorMode and BatchMode. You can easily integrate it into your CI/CD. Example bash script is provided in the package.
- You can configure the compilation configs from your custom scripts or with using the **provided Editor.** 
### Significantly reduce build time, build license usage because you reduce failed builds due to scripting define symbols issues.

## Table of contents
<!-- TOC -->
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
  * [Features](#features)
  * [How to contribute](#how-to-contribute)

<!-- TOC -->


## Getting Started
### Prerequisites Unity 2020.3+
ProjectCompilation checks are only available for Unity 2020.3 and higher.
It is tested with 2020.3.38, 2021.3.31, 2022.3.13 Unity versions.

### Installation

#### Install via UPM (using Git URL)
1. Open the Package Manager window.
2. Click on the + icon in the top-left corner of the window.
3. Select "Add package from git URL".
4. Paste the following URL: https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.0

#### Install manually (using Git URL)
1. Navigate to your project's Packages folder and open the manifest.json file.
2. Add this line below the "dependencies":

- ```json title="Packages/manifest.json"
    "com.foxsterdev.devaccelerationsystem": "https://github.com/FoxsterDev/DevAccelerationSystem.git?path=DevAccelerationSystem/Assets/DevAccelerationSystem#1.0.0",
  ```
### In Unity
#### Creating project compilation config
1. Find and select any available Editor folder inside Assets folder.
2. Right-click on the Editor folder and select Create > Assets > DevAccelerationSystem > Create ProjectCompilationConfig.
   ![Example1](Docs/Img1.png)
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

#### Compilation output viewer
1. You can see the compilation output in the Compilation Output Viewer window.
   ![Example5](Docs/Img5.png)

#### Compilation output known cases
1. if you specified config for a build target but the module is not installed you see error about it 
[ProjectCompilationCheck][Error] Compilation failed for WebGLNotDevelopment with errors:[1]: Unity module WebGL is not installed. Try to install the module and restart the unity.
2. if a config is not enabled it will be skipped during run

## Features
1. Support configurations for player script compilation with build target and scripting define symbols.
2. Simplified scriptable object editor to run compilation checks from Unity
2. Supports EditorMode and BatchMode. You can easily integrate it into your CI/CD. Example bash script is provided in the package.
3. Compilation output viewer window to see the previous compilation results. 

## How to contribute
Don't hesitate to [create an Issue ](https://github.com/FoxsterDev/DevAccelerationSystem/issues/new)