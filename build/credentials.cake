public class Credentials
{
    public string NugetUrl { get; private set; }
    public string NugetApiKey { get; private set; }

    public Credentials(string nugetUrl, string nugetApiKey)
    {
        NugetUrl = nugetUrl;
        NugetApiKey = nugetApiKey;
    }

    public static Credentials New(ICakeContext context)
    {
        return new Credentials(
            context.EnvironmentVariable("PRIVATE_NUGET_URL"),
            context.EnvironmentVariable("PRIVATE_NUGET_API_KEY")
        );
    }
}