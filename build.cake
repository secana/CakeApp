var target = Argument("target", "Default");
var solutionDir = System.IO.Directory.GetCurrentDirectory();

var testDirectory = Argument("testDirectory", System.IO.Path.Combine(solutionDir, "test"));     // ./build.ps1 --target publish -testDirectory="somedir"
var artifactDir = Argument("artifactDir", "./artifacts"); 		
var apiKey = Argument<string>("apiKey", null);                                                  // ./build.ps1 --target push -apiKey="your nuget.org api key"                                            
var testSln = System.IO.Path.Combine(testDirectory, "CakeTest");							   

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

Task("InstallTestVersion")
    .IsDependentOn("PackageTestVersion")
    .Does(() => {
        var testPackage = GetFiles("**/*-test.nupkg").ElementAt(0);
        Information($"Found test package at {testPackage}");
        InstallTemplate(testPackage.FullPath);
    });

Task("Test")
    .IsDependentOn("InstallTestVersion")
    .Does(() => {
        
        // Create new sln from template
        DotNetNew("caketest", testSln);

        // Test "publish" task
        RunPowerShellScript(testSln, @"build.ps1", "-Target publish");
        var outputDll = System.IO.Path.Combine(testSln, "artifacts", "CakeTest.Console", "CakeTest.Console.dll");
        if(!System.IO.File.Exists(outputDll))
            throw new Exception($"\"Publish\" task of template failed. Could not find {outputDll}");
        else
            Information("\"Publish\" task of template ran successfully");

        // Test "pack" task
        RunPowerShellScript(testSln, @"build.ps1", "-Target pack");
        var outputPackage = System.IO.Path.Combine(testSln, "artifacts", "CakeTest.Console.0.0.0.nupkg");
        if(!System.IO.File.Exists(outputPackage))
            throw new Exception($"\"Pack\" task of template failed. Could not find {outputPackage}");
        else
            Information("\"Pack\" task of template ran successfully");

        // Uninstall the test template from the target.
        var testPackage = GetFiles("**/*-test.nupkg").ElementAt(0);
        var testPackageName = testPackage.GetFilename().FullPath.Split(new [] {".nupkg"}, StringSplitOptions.None)[0];
        UninstallTemplate(testPackageName);
    });

Task("Pack")
    .IsDependentOn("Test")
    .Does(() => {
        var settings = new NuGetPackSettings  
        {
            ArgumentCustomization = args=>args.Append("-NoDefaultExcludes"),
            OutputDirectory = artifactDir
        };

        var nuspec = System.IO.Path.Combine(solutionDir, "CakeApp.nuspec");
        NuGetPack(nuspec, settings);
    });

Task("Push")
    .IsDependentOn("Pack")
    .Does(() => {
        var package = GetFiles($"{artifactDir}/CakeApp.*.nupkg").ElementAt(0);
        var source = "https://www.nuget.org/api/v2/package";

        if(apiKey==null)
            throw new ArgumentNullException(nameof(apiKey), "The \"apiKey\" argument must be set for this task.");

        Information($"Push {package} to {source}");

        NuGetPush(package, new NuGetPushSettings {
            Source = source,
            ApiKey = apiKey
        });
    });

Task("Default")
	.IsDependentOn("Test")
	.Does(() =>
	{
		Information("Build and test the whole solution.");
		Information("To pack (nuget) the application use the cake build argument: -Target Pack");
		Information("To push the NuGet template to nuget.org use: -Target Push --apiKey=\"your nuget api key\"");
	});

void RunPowerShellScript(string workDir, string script, string arguments)
{
    var psCommand = $"\"& {System.IO.Path.Combine(workDir, script)} {arguments}\"";
    using(var process = StartAndReturnProcess(
        "powershell", 
        new ProcessSettings{ Arguments = psCommand, WorkingDirectory = workDir }))
    {
        process.WaitForExit();
        // This should output 0 as valid arguments supplied
        Information($"Run {script} {arguments} with Exit code: {process.GetExitCode()}");
    }
}

void InstallTemplate(string templatePackage)
{
    using(var process = StartAndReturnProcess("dotnet", new ProcessSettings{ Arguments = $"new -i {templatePackage}" }))
    {
        process.WaitForExit();
        // This should output 0 as valid arguments supplied
        Information($"Installed {templatePackage} with Exit code: {process.GetExitCode()}");
    }
}

void UninstallTemplate(string templatePackage)
{
    using(var process = StartAndReturnProcess("dotnet", new ProcessSettings{ Arguments = $"new -u {templatePackage}" }))
    {
        process.WaitForExit();
        // This should output 0 as valid arguments supplied
        Information($"Uninstalled {templatePackage} with Exit code: {process.GetExitCode()}");
    }
}

void DotNetNew(string template, string output)
{
    using(var process = StartAndReturnProcess("dotnet", new ProcessSettings{ Arguments = $"new {template} -o {output}" }))
    {
        process.WaitForExit();
        // This should output 0 as valid arguments supplied
        Information($"Created {template} in {output} with Exit code: {process.GetExitCode()}");
    }
}

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