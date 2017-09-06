
/*
1) clean old packages DONE
2) ensure all needed directories exist DONE
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
    .IsDependentOn("Clean")
	.Does(() =>
	{
		EnsureDirectoryExists(testDirectory);
		EnsureDirectoryExists(artifactDir);
	});

Task("PrepareTestNuSpec")
    .IsDependentOn("PrepareDirectories")
    .Does(() =>
    {
        var testSpecPath = System.IO.Path.Combine(testDirectory, "CakeApp-test.nuspec");
        CopyFile("CakeApp.nuspec", testSpecPath);
    });

Task("PrepareTestTemplate")
    .IsDependentOn("PrepareTestNuSpec")
    .Does(() => {
        var testTemplatePath = System.IO.Path.Combine(testDirectory, "CakeApp");
        CopyDirectory("CakeApp", testTemplatePath);

        // Replace the identity and shortname in the json template.json with test values
        var jsonPath = System.IO.Path.Combine(testTemplatePath, "Content", ".template.config", "template.json");
        ReplaceStringInFile(jsonPath, "\"identity\": \"Core.Cake.Template\"", "\"identity\": \"Core.Cake-Test.Template\"");
        ReplaceStringInFile(jsonPath, "\"shortName\": \"cake\"", "\"shortName\": \"caketest\"");
    });

Task("PackageTestVersion")
    .IsDependentOn("PrepareTestTemplate")
    .Does(() => {
        var testSpecPath = System.IO.Path.Combine(testDirectory, "CakeApp-test.nuspec");
        var version = GetNuSpecVersion(testSpecPath);
        var testVersion = $"{version}-test";
        PackTemplate(testSpecPath, testVersion, testDirectory, testDirectory);
    });

Task("Default")
	.IsDependentOn("Clean")
	.Does(() =>
	{
		Information("Build and test the whole solution.");
		Information("To pack (nuget) the application use the cake build argument: --target Pack");
		Information("To publish (to run it somewhere else) the application use the cake build argument: --target Publish");
	});

void PackTemplate(string nuspecFile, string version, string workdingDirectory, string outputDirectory)
{
    var settings = new NuGetPackSettings {
        OutputDirectory = outputDirectory,
        ArgumentCustomization = args=>args.Append("-NoDefaultExcludes"),
        WorkingDirectory = workdingDirectory,
        Version = version
    };

    NuGetPack(nuspecFile, settings);
}

void ReplaceStringInFile(string file, string oldString, string newString)
{
    var text = System.IO.File.ReadAllText(file);
    var newText = text.Replace(oldString, newString);
    System.IO.File.WriteAllText(file, newText);
}

string GetNuSpecVersion(string nuspecPath)
{
	var doc = System.Xml.Linq.XDocument.Load(nuspecPath);
	var version = doc.Descendants().First(p => p.Name.LocalName == "version").Value;
	Information($"Extrated version {version} from {nuspecPath}");
	return version;
}

RunTarget(target);