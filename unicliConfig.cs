using System.Text.Json;
using System.Text.Json.Serialization;

namespace unicli;

[Serializable]
public struct unicliConfig
{
    public string editorInstallationPath { get; set; }
    public string textEditorCmd { get; set; }
    public string[] projectPaths { get; set; }
    public string shell { get; set; }
    public string shellExecArgs { get; set; }
    public string shellSourceCommand { get; set; }
    
    public string keyToolPath { get; set; }
    public override string ToString()
    {
        return JsonSerializer.Serialize(this);
    }

    public static unicliConfig getDefaultConfig()
    {
        var def = new unicliConfig();
        if (OperatingSystem.IsLinux())
        {
            def.editorInstallationPath = "~/Unity/Hub/Editor";
            def.textEditorCmd = "vim %s";
            def.projectPaths = [];
            def.shell = "bash";
            def.shellExecArgs = "-c";
            def.shellSourceCommand = "source";
            def.keyToolPath = "";
            return def;
        }
        else if (OperatingSystem.IsMacOS())
        {
            def.editorInstallationPath = "/Applications/Unity/Hub/Editor";
            def.textEditorCmd = "open -e %s";
            def.projectPaths = [];
            def.shell = "zsh";
            def.shellExecArgs = "-c";
            def.shellSourceCommand = "source";
            def.keyToolPath = "";
            return def;
        }
        else if (OperatingSystem.IsWindows())
        {
            def.editorInstallationPath = "C:/Program Files/Unity/Hub/Editor";
            def.textEditorCmd = "notepad %s";
            def.projectPaths = [];
            def.shell = "powershell.exe";
            def.shellExecArgs = "-Command";
            def.shellSourceCommand = ".";
            def.keyToolPath = "";
            return def;
        }
        else
        {
            Console.Error.WriteLine("Unsupported Platform: " + Environment.OSVersion.Platform);
            Environment.Exit(1);
        }

        return def;
    }
}