#addin nuget:?package=Cake.Docker&version=0.9.0
#addin nuget:?package=Cake.Figlet&version=1.1.0

var target          = Argument("target", "Default");
var solutionDir     = System.IO.Directory.GetCurrentDirectory();
var testDirectory   = Argument("testDirectory", System.IO.Path.Combine(solutionDir, "test"));  
var artifactDir     = Argument("artifactDir", "./artifacts"); 		
var apiKey          = Argument<string>("apiKey", null);                                                                       
var testSln         = System.IO.Path.Combine(testDirectory, "CakeTest");							   
var testSlnLinux    = System.IO.Path.Combine(testDirectory, "CakeTestLinux");	

Information(Figlet("CakeApp Template"));

Task("Clean")
	.Does(() =>
	{
        var settings = new DeleteDirectorySettings {
            Recursive = true,
            Force = true
        };

		if(DirectoryExists(testDirectory))
			DeleteDirectory(testDirectory, settings);

		if(DirectoryExists(artifactDir))
			DeleteDirectory(artifactDir, settings);
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
        /************************************************
        *             Linux build.sh test
        *************************************************/
        DotNetNew("caketest", testSlnLinux);

        var dockerSettings = new DockerContainerRunSettings 
        {
            Volume = new string[]{$"{testSlnLinux}:/data"},
            Rm = true,
            Name = "cakeapptest",
            Interactive = true,
            Tty = true
        };

        // Test "publish" task
        DockerRun(dockerSettings, "secana/cakeapp", "bin/bash -c \"cd /data && chmod u+x build.sh && dos2unix build.sh && ./build.sh --target publish\"");

        var outputDllLinux = System.IO.Path.Combine(testSlnLinux, "artifacts", "CakeTestLinux.Console", "CakeTestLinux.Console.dll");
        if(!System.IO.File.Exists(outputDllLinux))
            throw new Exception($"\"Publish\" task of Linux template failed. Could not find {outputDllLinux}");
        else
            Information("\"Publish\" task of Linux template ran successfully");

        // Test "pack" task
        DockerRun(dockerSettings, "secana/cakeapp", "bin/bash -c \"cd /data && chmod u+x build.sh && dos2unix build.sh && ./build.sh --target pack\"");

        var outputPackageLinux = System.IO.Path.Combine(testSlnLinux, "artifacts", "CakeTestLinux.Console.0.0.0.nupkg");
        if(!System.IO.File.Exists(outputPackageLinux))
            throw new Exception($"\"Pack\" task of Linux template failed. Could not find {outputPackageLinux}");
        else
            Information("\"Pack\" task of Linux template ran successfully");

        
        // /************************************************
        // *             Windows build.ps1 test
        // *************************************************/
        DotNetNew("caketest", testSln);
        //Test "publish" task
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

        /************************************************
        *        Test Docker Container build task
        *************************************************/
        RunPowerShellScript(testSln, @"build.ps1", "-Target Build-Container");
        DockerRmi(new string[] {"local/caketest", "local/caketest:0.0.0-0"});

        /************************************************
        *   Uninstall the test template from the target
        *************************************************/

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