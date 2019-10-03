#addin nuget:?package=Cake.Docker&version=0.9.9
#addin nuget:?package=Cake.Figlet&version=1.2.0

var target = Argument("target", "Default");
var testFailed = false;
var solutionDir = System.IO.Directory.GetCurrentDirectory();


var testResultDir = Argument("testResultDir", System.IO.Path.Combine(solutionDir, "test-results"));     // ./build.sh --target Build-Container -testResultsDir="somedir"
var artifactDir = Argument("artifactDir", "./artifacts"); 												// ./build.sh --target Build-Container -artifactDir="somedir"
var buildNumber = Argument<int>("buildNumber", 0); 														// ./build.sh --target Build-Container -buildNumber=5
var dockerRegistry = Argument("dockerRegistry", "local");												// ./build.sh --target Build-Container -dockerRegistry="local"
var slnName = Argument("slnName", "CakeApp");

Information(Figlet("CakeApp"));

Task("Clean")
	.Does(() =>
	{
		var settings = new DeleteDirectorySettings {
    		Recursive = true,
    		Force = true
		};

		if(DirectoryExists(testResultDir))
		{
			CleanDirectory(testResultDir);	
			DeleteDirectory(testResultDir, settings);
		}
			

		if(DirectoryExists(artifactDir))
		{
			CleanDirectory(artifactDir);
			DeleteDirectory(artifactDir, settings);
		}
			

		var binDirs = GetDirectories("./**/bin");
		var objDirs = GetDirectories("./**/obj");
		var testResDirs = GetDirectories("./**/TestResults");
		
		CleanDirectories(binDirs);
		CleanDirectories(objDirs);
		CleanDirectories(testResDirs);

		DeleteDirectories(binDirs, settings);
		DeleteDirectories(objDirs, settings);
		DeleteDirectories(testResDirs, settings);
	});


Task("PrepareDirectories")
	.Does(() =>
	{
		EnsureDirectoryExists(testResultDir);
		EnsureDirectoryExists(artifactDir);
	});


Task("Restore")
	.Does(() =>
	{
		DotNetCoreRestore();	  
	});


Task("Build")
	.IsDependentOn("Clean")
	.IsDependentOn("PrepareDirectories")
	.IsDependentOn("Restore")
	.Does(() =>
	{
		var solution = GetFiles("./*.sln").ElementAt(0);
		Information("Build solution: {0}", solution);

		var settings = new DotNetCoreBuildSettings
		{
			Configuration = "Release"
		};

		DotNetCoreBuild(solution.FullPath, settings);
	});


Task("Test")
	.IsDependentOn("Build")
	.ContinueOnError()
	.DoesForEach(GetTestProjectFiles(), (testProject) =>
	{
		var projectFolder = System.IO.Path.GetDirectoryName(testProject.FullPath);
		DotNetCoreTest(testProject.FullPath, new DotNetCoreTestSettings
		{
			ArgumentCustomization = args => args.Append("-l trx"),
			WorkingDirectory = projectFolder
		});
	})
	.Does(() =>
	{
		var tmpTestResultFiles = GetFiles("./**/*.trx");
		CopyFiles(tmpTestResultFiles, testResultDir);
	})
	.DeferOnError();


Task("Pack")
	.IsDependentOn("Test")
	.Does(() =>
	{
		if(testFailed)
		{
			Information("Do not pack because tests failed");
			return;
		}

		var projects = GetSrcProjectFiles();
		var settings = new DotNetCorePackSettings
		{
			Configuration = "Release",
			OutputDirectory = artifactDir
		};
		
		foreach(var project in projects)
		{
			Information("Pack {0}", project.FullPath);
			DotNetCorePack(project.FullPath, settings);
		}
	});


Task("Publish")
	.IsDependentOn("Test")
	.DoesForEach(GetSrcProjectFiles(), (project) => 
	{
		var projectDir = System.IO.Path.GetDirectoryName(project.FullPath);
		var projectName = new System.IO.DirectoryInfo(projectDir).Name;
		var outputDir = System.IO.Path.Combine(artifactDir, projectName);
		EnsureDirectoryExists(outputDir);

		Information("Publish {0} to {1}", projectName, outputDir);

		var settings = new DotNetCorePublishSettings
		{
			OutputDirectory = outputDir,
			Configuration = "Release"
		};

		DotNetCorePublish(project.FullPath, settings);
	});


Task("Build-Container")
	.IsDependentOn("Publish")
	.Does(() => {
		var version = GetProjectVersion();
		var imageName = GetImageName();
		var tagVersion = $"{imageName}:{version}-{buildNumber}".ToLower();
		var tagLatest = $"{imageName}:latest".ToLower();

		Information($"Build docker image {tagVersion}");
		Information($"Build docker image {tagLatest}");

		var buildArgs = new DockerImageBuildSettings 
		{
			Tag = new List<string>() { tagVersion, tagLatest }.ToArray()
		};

		DockerBuild(buildArgs, solutionDir);
	});


Task("Push-Container")
	.IsDependentOn("Build-Container")
	.Does(() => {
		var imageName = GetImageName().ToLower();
		DockerPush(imageName);
	});


Task("Default")
	.IsDependentOn("Test")
	.Does(() =>
	{
		Information("Build and test the whole solution.");
		Information("To pack (nuget) the application use the cake build argument: --target=Pack");
		Information("To publish (to run it somewhere else) the application use the cake build argument: --target=Publish");
		Information("To build a Docker container with the application use the cake build argument: --target=Build-Container");
		Information("To push the Docker container into an Docker registry use the cake build argument: --target=Push-Container -dockerRegistry=\"yourregistry\"");
	});


FilePathCollection GetSrcProjectFiles()
{
	return GetFiles("./src/**/*.csproj");
}

FilePathCollection GetTestProjectFiles()
{
	return GetFiles("./test/**/*Test/*.csproj");
}

FilePath GetSlnFile()
{
	return GetFiles("./**/*.sln").First();
}

FilePath GetMainProjectFile()
{
	foreach(var project in GetSrcProjectFiles())
	{
		if(project.FullPath.EndsWith($"{slnName}.Console.csproj"))
		{
			return project;
		}
	}

	Error("Cannot find main project file");
	return null;
}

string GetProjectVersion()
{
	var project = GetMainProjectFile();

	var doc = System.Xml.Linq.XDocument.Load(project.FullPath);
	var version = doc.Descendants().First(p => p.Name.LocalName == "Version").Value;
	Information($"Extrated version {version} from {project}");
	return version;
}

string GetImageName()
{
	return $"{dockerRegistry}/{slnName}";
}

RunTarget(target);