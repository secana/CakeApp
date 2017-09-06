
/*
1) clean old packages
2) ensure all needed directories exist
3) package test version of template
4) install test version of template
5) create new sln with test version
6) test test version
7) if successful: package to release version
8) publish package to nuget
*/

var target = Argument("target", "Default");
var testFailed = false;
var solutionDir = System.IO.Directory.GetCurrentDirectory();

var testDirectory = Argument("testDirectory", System.IO.Path.Combine(solutionDir, "test"));     // ./build.sh --target publish -testDirectory="somedir"
var artifactDir = Argument("artifactDir", "./artifacts"); 									    // ./build.sh --target publish -artifactDir="somedir"

Task("Clean")
	.Does(() =>
	{
		if(DirectoryExists(testDirectory))
			DeleteDirectory(testDirectory, recursive:true);

		if(DirectoryExists(artifactDir))
			DeleteDirectory(artifactDir, recursive:true);
	});


Task("PrepareDirectories")
	.Does(() =>
	{
		EnsureDirectoryExists(testDirectory);
		EnsureDirectoryExists(artifactDir);
	});


Task("Default")
	.IsDependentOn("PrepareDirectories")
	.Does(() =>
	{
		Information("Build and test the whole solution.");
		Information("To pack (nuget) the application use the cake build argument: --target Pack");
		Information("To publish (to run it somewhere else) the application use the cake build argument: --target Publish");
	});


RunTarget(target);