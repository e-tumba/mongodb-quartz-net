#tool "nuget:?package=GitVersion.CommandLine"

#load "./build/parameters.cake"
#load "./build/credentials.cake"
#load "./build/paths.cake"

BuildParameters parameters = BuildParameters.Load(Context);
BuildPaths paths = BuildPaths.Load(Context);
Credentials credentials = Credentials.New(Context);

var version = GitVersion();

Task("Clean")
    .Does(() =>
{
    CleanDirectories(paths.SrcFolder + "/**/bin/" + parameters.Configuration);
	CleanDirectories(paths.SrcFolder + "/**/obj/" + parameters.Configuration);

    if(DirectoryExists(paths.PackagesFolder))
        DeleteDirectory(paths.PackagesFolder, true);

    CreateDirectory(paths.PackagesFolder);
});

Task("Restore")
    .Does(() =>
{
    NuGetRestore(paths.Solution);
});

Task("SetVersion")
	.Does(() => 
{
    printVersion(version);

    var files = GetFiles(MakeAbsolute(Directory(paths.RootFolder)).FullPath + "/**/AssemblyInfo.cs");
    foreach(var file in files)
    {
        // parse file
        var assemblyInfo = ParseAssemblyInfo(file);
        
        // update file
        CreateAssemblyInfo(file, new AssemblyInfoSettings {
            Company = assemblyInfo.Company,
            Product = assemblyInfo.Product,
            Copyright = string.Format("Copyright (c) 2015 - {0}", DateTime.Now.Year),
            Version = string.Format("{0}.{1}.0.0", version.Major, version.Minor),
            FileVersion = string.Format("{0}.0", version.MajorMinorPatch),
            InformationalVersion = version.InformationalVersion
        });
    }
});

Task("Build")
    .IsDependentOn("Clean")
    .IsDependentOn("Restore")
    .Does(() =>
{
	MSBuild(paths.Solution, settings => settings
        .SetConfiguration(parameters.Configuration)
        .SetPlatformTarget(PlatformTarget.MSIL)
        // force 32bit msbuild version call cause of Microsoft.NET.Sdk.Publish.Tasks.dll compiled in 32 bits, see https://github.com/dotnet/sdk/issues/1073
        .SetMSBuildPlatform(Cake.Common.Tools.MSBuild.MSBuildPlatform.x86) 
    );
});

Task("CreateNugetPackages")
	.IsDependentOn("Build")
	.Does(() => 
{
    Func<IFileSystemInfo, bool> exclude_tests = fi =>
    {
        return !fi.Path.FullPath.EndsWith("tests", StringComparison.OrdinalIgnoreCase);
    };
    
    var projects = GetFiles(paths.SrcFolder + "**/*.csproj", exclude_tests);

    NuGetPack(projects, new NuGetPackSettings
    {
        Properties = new Dictionary<string, string> {{"Configuration", parameters.Configuration}},
        Version = version.NuGetVersion,
        Symbols = true,
        OutputDirectory = paths.PackagesFolder,
		IncludeReferencedProjects = true        
    });
});

Task("PublishNugetPackages")
    .IsDependentOn("CreateNugetPackages")
    .Does(() => 
{
    var packages = GetFiles(paths.PackagesFolder + "/**/*.nupkg")
        .Where(_ => !_.ToString().EndsWith("symbols.nupkg", StringComparison.OrdinalIgnoreCase));
    
    NuGetPush(packages, new NuGetPushSettings
    {
       Source = credentials.NugetUrl,
       ApiKey =  credentials.NugetApiKey
    });
});

//////////////////////////////////////////////////////////////////////
// TASK TARGETS
//////////////////////////////////////////////////////////////////////

Task("Release")
    .IsDependentOn("PublishNugetPackages")
	.Does(() => 
	{
		printEnd();
	});

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

Action printEnd = () => {
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
};

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(parameters.Target);