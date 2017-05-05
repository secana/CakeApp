var target = Argument("target", "Default");
var testFailed = false;
var solutionDir = System.IO.Directory.GetCurrentDirectory();
var testResultDir = System.IO.Path.Combine(solutionDir, "test-results");
var artifactDir = "./artifacts";

Information("Solution Directory: {0}", solutionDir);
Information("Test Results Directory: {0}", testResultDir);

Task("PrepareDirectories")
	.Does(() =>
	{
		EnsureDirectoryExists(testResultDir);
		EnsureDirectoryExists(artifactDir);
	});

Task("Clean")
	.IsDependentOn("PrepareDirectories")
	.Does(() =>
	{
		CleanDirectory(testResultDir);
		CleanDirectory(artifactDir);

		var binDirs = GetDirectories("./**/bin");
		var objDirs = GetDirectories("./**/obj");
		var testResDirs = GetDirectories("./**/TestResults");
		
		DeleteDirectories(binDirs, true);
		DeleteDirectories(objDirs, true);
		DeleteDirectories(testResDirs, true);
	});

Task("Restore")
	.Does(() =>
	{
		Exception exception = null;
		
		// We have to retry the restore a few
		// times since on docker it does not work 
		// on the first time :(

		for(var i = 0; i < 5; i++)
		{
			try
			{
				exception = null;
				DotNetCoreRestore();	  
			}
			catch(Exception e)
			{
				exception = e;
				continue;
			}

			break;
		}

		if(exception != null)
		{
			throw exception;
		}		
	});

Task("Build")
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
	.IsDependentOn("Clean")
	.IsDependentOn("Build")
	.ContinueOnError()
	.Does(() =>
	{
		var tests = GetFiles("./test/**/*Test/*.csproj");
		
		foreach(var test in tests)
		{
			var projectFolder = System.IO.Path.GetDirectoryName(test.FullPath);
			try
			{
				DotNetCoreTest(test.FullPath, new DotNetCoreTestSettings
				{
					ArgumentCustomization = args => args.Append("-l trx"),
					WorkingDirectory = projectFolder
				});
			}
			catch(Exception e)
			{
				testFailed = true;
				Error(e.Message.ToString());
			}
		}

		// Copy test result files.
		var tmpTestResultFiles = GetFiles("./**/*.trx");
		CopyFiles(tmpTestResultFiles, testResultDir);
	});

Task("Pack")
	.IsDependentOn("Clean")
	.IsDependentOn("Test")
	.Does(() =>
	{
		if(testFailed)
		{
			Information("Do not pack because tests failed");
			return;
		}

		var projects = GetFiles("./src/**/*.csproj");
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
	.IsDependentOn("Clean")
	.IsDependentOn("Test")
	.Does(() =>
	{
		if(testFailed)
		{
			Information("Do not publish because tests failed");
			return;
		}
		var projects = GetFiles("./src/**/*.csproj");

		foreach(var project in projects)
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

			// Check if an appsettings.json exits and copy it to the
			// output directory.
			var appsettingsFile = System.IO.Path.Combine(projectDir, "appsettings.json");
			var outAppsettingsFile = System.IO.Path.Combine(outputDir, "appsettings.json");
			if(FileExists(appsettingsFile))
			{
				Information("Copy {0} to {1}", appsettingsFile, outAppsettingsFile);
				CopyFile(appsettingsFile, outAppsettingsFile);
			}

		}
	});

Task("Default")
	.IsDependentOn("Test")
	.Does(() =>
	{
		Information("Build and test the whole solution.");
		Information("To pack (nuget) the application use the cake build argument: --target Pack");
		Information("To publish (to run it somewhere else) the application use the cake build argument: --target Publish");
	});

RunTarget(target);