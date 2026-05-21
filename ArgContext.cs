namespace unicli;
public struct ArgContext
{
    public string[]? Args;
    public string[]? Flags;
    public int ArgCount;
    public int FlagCount;
    public string executionPath;
    public ArgContext()
    {
        Args = null;
        Flags = null;
        FlagCount = 0;
        ArgCount = 0;
        executionPath = Environment.CurrentDirectory;
    }
    public ArgContext(string[]? args)
    {
        List<string> _args = new List<string>();
        List<string> _flags = new List<string>();
        foreach (var arg in args)
        {
            if (arg.StartsWith("--"))
            {
                _flags.Add(arg.ToLower().Trim());
                FlagCount++;
            }
            else
            {
                _args.Add(arg.ToLower().Trim());
                ArgCount++;
            }
        }
        Args = args.ToArray();
        Flags = _flags.ToArray();
        executionPath = Environment.CurrentDirectory;
    }
}