public class BuildParameters
{
    public string Target { get; private set; }
    public string Configuration { get; private set; }

    public static BuildParameters Load(ICakeContext context)
    {
        if (context == null)
        {
            throw new ArgumentNullException("context");
        }

        return new BuildParameters 
        {
            Target = context.Argument("target", "Default"),
            Configuration = context.Argument("configuration", "Release")
        };
    }
}
