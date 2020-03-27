# Release Notes

Below are the release notes with the most important changes for the different versions.

## 3.2.0

* Updated to .NET Core 3.1 LTS
* Updated all dependencies
* Updated cake-tool to 0.37.0

## 3.1.0

* Updated to .NET Core 3.0
* Updated all dependencies
* Updated cake-tool to 0.35.0

## 3.0.2

* Updated Cake.Figlet to 1.2.0
* Updated Cake.Docker to 0.9.9
* Updated to "mcr.microsoft.com/dotnet/core/runtime:2.2-alpine3.9" as runtime image

## 3.0.0

* Updated to Cake 0.30.0
* Removed bootstrapper `build.ps1` and `build.sh` in favor of the Cake global tool
* Updated to Cake.Docker 0.9.6
* Updated to 2.1.3-runtime-alpine3.7
* Updated to Microsoft.NET.Test.Sdk 15.8.0
* Updated to Moq 4.9.0
* Updated to xunit 2.4.0
* Updated to xunit.runner.visualstudio 2.4.0

## 2.3.0

* Updated to Cake 0.28.1
* Updated to Cake.Docker 0.9.4
* Use Alpine as base image instead of Ubuntu
* Updated to .Net Core SDK 2.1.1
* Updated all NuGet dependencies to the latest versions

## 2.1.3

* Added Readme.md to the sln file to be listed in VS
* Configured the appsettings.json to be copied to the output folder
* Set the C# version to latest
* Better Docker Alias calls in build.cake
* Better testing of the whole template

## 2.1.2

* Removed manual download of Docker package from the build.sh script

## 2.1.1

* Fixed bug in "Build-Container" Task

## 2.1.0

* Updated to Cake Build version 0.26.0 which only depends on .Net Core 2.0 instead of .Net 1.x
* Updated Cake.Docker plugin from 0.8.3 to 0.9.0 which depends only on .Net Core 2.0 instead of .Net 1.x
* Changed the template tags to a common tag format
* Added a *Readme.md* to the template

## Before

No release note were written before version 2.1.0