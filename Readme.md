# CakeApp
A .Net Core solution template using the [Cake](http://cakebuild.net/) build system. It provides an initial console project and a corresponding [XUnit](https://xunit.github.io/) test project. Furthermore it adds cake build scripts for Windows and Linux.

[![NuGet](https://img.shields.io/nuget/v/CakeApp.svg)](https://www.nuget.org/packages/CakeApp/)
[![NuGet](https://img.shields.io/nuget/dt/CakeApp.svg)](https://www.nuget.org/packages/CakeApp/)

## Template layout
This template creates the following structure on your disk, where *CakeApp* is replaced by the name of your solution.

```
|-> CakeApp.sln
|-> src
|   |-> CakeApp.Console
|   |   |-> CakeApp.Console.csproj
|   |   |-> Program.cs
|   |   |-> appsettings.json
|-> test
|   |-> CakeApp.Console_Test
|   |   |-> CakeApp.Console_Test.csproj
|   |   |-> UnitTest1.cs
|-> .gitignore
|-> build.cake
|-> build.ps1
|-> build.sh
|-> Dockerfile
|-> Readme.md
```

## Prerequisite

Since [Cake 0.30.0 ](https://cakebuild.net/blog/2018/08/cake-v0.30.0-released) the script runner is available as a [.Net Core Global Tool](https://docs.microsoft.com/en-us/dotnet/core/tools/global-tools). This make the bootstrap scripts `build.ps1` and `build.sh` obsolete.

* Make sure you have at least the .Net Core SDK version 2.1.400 installed
* Install the Cake global tool with:  `dotnet tool install -g Cake.Tool --version 0.30.0`

## Installation
On Windows PowerShell or Linux Shell:
```
dotnet new -i CakeApp
```
To uninstall the template run:
```
dotnet new -u CakeApp
```

## Usage
This section will shortly describe how to use the template.

### Create a new solution
To create a new solution based on the template use

`dotnet new cake -o [your solution name]`

This will create a corresponding Visual Studio solution with the structure described above on your disk. You can now proceed to write your code in the given projects or at more projects if you need so.

### Build the solution
To build the solution the [Cake](http://cakebuild.net/) build system is used. This template comes with a pre-defined Cake build script `build.cake` with some default build targets. Of course feel free to alter the script if it doesn't fit all your needs.

To run a build on the PowerShell type:
`build.ps1 -Target [target name]` 
or just `build.ps1` to run the default target `test`.

On Linux run:
```
dos2unix build.sh
build.sh --target [target name]
```
or just `build.sh` to run the default target `test`. The *dos2unix* command is needed since Windows changes the line endings in the bash script when the NuGet package is created. You have to to the command only once.

| Build Target | Description | Depends on |
| ------------ | ----------- | ---------- |
| PrepareDirectories | Ensures that all needed directories for the build are available in your solution directory. | - |
| Clean | Cleans your last Cake build and deletes all build artifacts. | PrepareDirectories |
| Restore | Restores all NuGet packages in your projects. It will try it up to five times, since sometimes the restore does not work on the first try. | - |
| Build | Builds your whole solution with the *Release* configuration. | Restore |
| Test | Runs all Unit test projects in the *test* folder which project names are ending with **Test**. Other projects are ignored. The test results **.trx* files are put into the *testResults* folder. | Clean, Build |
| Pack | Packages all projects from the *src* folder into corresponding NuGet packages. The packages are placed in the *artifacts* folder. | Clean, Test |
| Publish | Publishes all projects from *src* to the *artifacts* folder. You can use the published projects to run them every where else. | Clean, Test |
| Build-Container | Builds a Docker container with the main application and tags the container based on the \"Version\" tag in the *.csproj file and a given build number (default 0). Futhermore the container gets a \"latest\" tag. | publish |
| Push-Container | Pushes the two container tags (version and latest) into a Docker registry which you have to specify with `-dockerRegistry="yourregistry"` | Build-Container |
| Default | The same as *Test*. If no target is given, this one is used. | Clean, Build |

## Build the project
To build a usable NuGet package from the template source run:
`build.ps1 -Target Pack` from a Windows machine. You find the built Nuget package under *artifacts*.

## Release-Notes

Release notes can be found here: [Release-Notes](Release-Notes.md)