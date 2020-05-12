/* *** READ ME CAREFULLY
before using this script, you must register the following environment variables:
PRIVATE_NUGET_URL, PRIVATE_NUGET_API_KEY
***  */

// Install addins.

// Install tools.
#tool "nuget:https://api.nuget.org/v3/index.json?package=GitVersion.CommandLine&version=3.6.2"


#load "./build/parameters.cake"
#load "./build/credentials.cake"
#load "./build/paths.cake"


//////////////////////////////////////////////////////////////////////
// PARAMETERS
//////////////////////////////////////////////////////////////////////
GitVersion version;
DotNetCoreMSBuildSettings msBuildSettings;
BuildParameters parameters = BuildParameters.Load(Context);
BuildPaths paths = BuildPaths.Load(Context);
Credentials nugetCredentials = Credentials.New(Context);

string assemblyVersion, fileVersion, informationalVersion, nugetVersion;

//////////////////////////////////////////////////////////////////////
// SETUP/TEARDOWN
//////////////////////////////////////////////////////////////////////
Setup(context =>
{
    version = GitVersion();
    printVersion(version);

    assemblyVersion = $"{version.Major}.{version.Minor}.0.0";
    fileVersion = $"{version.MajorMinorPatch}.0";
    informationalVersion = version.InformationalVersion;
    nugetVersion = version.NuGetVersion;

    msBuildSettings = new DotNetCoreMSBuildSettings()
        .WithProperty("Version", nugetVersion)
        .WithProperty("AssemblyVersion", assemblyVersion)
        .WithProperty("FileVersion", fileVersion)
        .WithProperty("InformationalVersion", informationalVersion);
});

Teardown(context =>
{
    Information("Task completed at {0}", DateTime.Now.ToString("s"));
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write(@"
                           *     .--.
                                / /  `
               +               | |
                      '         \ \__,
                  *          +   '--'  *
                      +   /\
         +              .'  '.   *
                *      /======\      +
                      ;:.  _   ;
                      |:. (_)  |
                      |:.  _   |
            +         |:. (_)  |          *
                      ;:.      ;
                    .' \:.    / `.
                   / .-'':._.'`-. \
                   |/    /||\    \|
             jgs _..--""""""""""""""""""""--.._
            _.-'``                    ``'-._
                -'                                '-
    ");
});

//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("Clean")
    .Does(() =>
    {
        CleanDirectories(paths.SrcFolder + "/**/bin/" + parameters.Configuration);
        CleanDirectories(paths.SrcFolder + "/**/obj/" + parameters.Configuration);

        if(!DirectoryExists(paths.PackagesFolder))
            CreateDirectory(paths.PackagesFolder);

        CleanDirectory(paths.PackagesFolder);
    });

Task("Restore")
    .Does(() =>
    {
        NuGetRestore(paths.Solution);
    });

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
    {
        var settings = new DotNetCoreBuildSettings
        {
            Configuration = parameters.Configuration,
            MSBuildSettings = msBuildSettings
        };

        DotNetCoreBuild(paths.Solution, settings);
    });

Task("Create-NuGet-Packages")
    .IsDependentOn("Build")
    .Does(() =>
    {
		var projects = GetFiles(paths.SrcFolder + "/**/*Quartz.Spi.MongoDbJobStore*.csproj");

        foreach(var project in projects)
        {
            // .NET Core
            DotNetCorePack(project.FullPath, new DotNetCorePackSettings {
                Configuration = parameters.Configuration,
                OutputDirectory = paths.PackagesFolder,
                NoBuild = true,
                NoRestore = true,
                IncludeSymbols = true,
                MSBuildSettings = msBuildSettings
            });
        }
    });

Task("Publish-NuGet")
    .IsDependentOn("Create-NuGet-Packages")
    .Does(() =>
    {
        var packages = GetFiles(paths.PackagesFolder + "/**/*.nupkg")
            .Where(_ => !_.ToString().EndsWith("symbols.nupkg", StringComparison.OrdinalIgnoreCase));

        NuGetPush(packages, new NuGetPushSettings
        {
            Source = nugetCredentials.NugetUrl,
            ApiKey =  nugetCredentials.NugetApiKey
        });
    });

//////////////////////////////////////////////////////////////////////
// TARGETS
//////////////////////////////////////////////////////////////////////
Task("Release")
    .IsDependentOn("Publish-NuGet");

Task("Default")
   .Does(() => 
    {
        Information("Please select a build target from the following list:");
        Information("------------------------------------------------------------");
        foreach(var task in Tasks) 
        {
            Information("\t{0}", task.Name);
        }
        Information("------------------------------------------------------------");
    });

Action<GitVersion> printVersion = (version) => {
    // Information("AssemblySemFileVer: {0}", version.AssemblySemFileVer);
    Information("AssemblySemVer: {0}", version.AssemblySemVer);
    Information("BranchName: {0}", version.BranchName);
    Information("BuildMetaData: {0}", version.BuildMetaData);
    Information("BuildMetaDataPadded: {0}", version.BuildMetaDataPadded);
    Information("CommitDate: {0}", version.CommitDate);
    Information("CommitsSinceVersionSource: {0}", version.CommitsSinceVersionSource);
    Information("CommitsSinceVersionSourcePadded: {0}", version.CommitsSinceVersionSourcePadded);
    Information("FullBuildMetaData: {0}", version.FullBuildMetaData);
    Information("FullSemVer: {0}", version.FullSemVer);
    Information("InformationalVersion: {0}", version.InformationalVersion);
    Information("LegacySemVer: {0}", version.LegacySemVer);
    Information("LegacySemVerPadded: {0}", version.LegacySemVerPadded);
    Information("Major: {0}", version.Major);
    Information("MajorMinorPatch: {0}", version.MajorMinorPatch);
    Information("Minor: {0}", version.Minor);
    Information("NuGetVersion: {0}", version.NuGetVersion);
    Information("NuGetVersionV2: {0}", version.NuGetVersionV2);
    Information("Patch: {0}", version.Patch);
    Information("PreReleaseLabel: {0}", version.PreReleaseLabel);
    Information("PreReleaseNumber: {0}", version.PreReleaseNumber);
    Information("PreReleaseTag: {0}", version.PreReleaseTag);
    Information("PreReleaseTagWithDash: {0}", version.PreReleaseTagWithDash);
    Information("SemVer: {0}", version.SemVer);
    Information("Sha: {0}", version.Sha);
};

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////
RunTarget(parameters.Target);