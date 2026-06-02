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
        var _args = new List<string>();
        var _flags = new List<string>();

        if (args == null)
        {
            Args = _args.ToArray();
            Flags = _flags.ToArray();
            ArgCount = 0;
            FlagCount = 0;
            executionPath = Environment.CurrentDirectory;
            return;
        }


        foreach (var arg in args)
            if (arg.StartsWith("--"))
            {
                _flags.Add(arg.ToLower().Trim());
                FlagCount++;
            }
            else
            {
                _args.Add(arg.Trim());
                ArgCount++;
            }

        Args = _args.ToArray();
        Flags = _flags.ToArray();
        executionPath = Environment.CurrentDirectory;
    }
}