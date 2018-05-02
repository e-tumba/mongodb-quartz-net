public class BuildPaths
{
    public string RootFolder { get; private set; }
    public string SrcFolder { get; private set; }
    public string Solution { get; private set; }
    public string PackagesFolder { get; private set; }

    public static BuildPaths Load(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

		var rootFolder = "./";

        return new BuildPaths {
            RootFolder = rootFolder,
            SrcFolder = rootFolder,
            Solution = System.IO.Path.Combine(rootFolder, "mongodb-quartznet.sln"),
            PackagesFolder = System.IO.Path.Combine(rootFolder, "dist"),
        };
    }

	public string CombineFromRoot(string path) 
    {
		return System.IO.Path.Combine(RootFolder, path);
    }
}
