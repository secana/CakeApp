# Release Notes

Below are the release notes with the most important changes for the different versions.

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