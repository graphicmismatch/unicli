using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace unicli;

public class unicli
{
    #region exposed methods

    public ReturnObject help(ArgContext? ctx = null)
    {
        try
        {
            version();
            Console.WriteLine("\n" + Constants.HelpText);

            return new ReturnObject
            {
                Code = ReturnCode.Success
            };
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject version(ArgContext? ctx = null)
    {
        try
        {
            Console.WriteLine(Constants.ApplicationName + " @" + Constants.Version);

            return new ReturnObject
            {
                Code = ReturnCode.Success
            };
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject projects(ArgContext ctx)
    {
        try
        {
            ReturnObject ro = getUnityProjects();

            if (ro.Code == ReturnCode.Success)
            {
                if (ro.ReturnData == null ||
                    ((Dictionary<string, string>)ro.ReturnData).Count == 0)
                {
                    Console.WriteLine("No project files found.");
                    return ro;
                }

                Dictionary<string, string> proj =
                    (Dictionary<string, string>)ro.ReturnData;

                foreach (string key in proj.Keys)
                {
                    Console.WriteLine($"{key} -> {proj[key]}");
                }
            }

            return ro;
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject editors(ArgContext ctx)
    {
        try
        {
            ReturnObject ro = getInstalledEditors();

            if (ro.Code == ReturnCode.Success)
            {
                Console.WriteLine(ro.ReturnData);
            }

            return ro;
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject config(ArgContext ctx)
    {
        try
        {
            string configPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                Constants.ApplicationName
            );

            ReturnObject ro = new ReturnObject
            {
                Code = ReturnCode.Success
            };

            if (ctx.Args == null || ctx.Args.Length < 1)
            {
                ro.Code = ReturnCode.SuccessWithMessage;
                ro.ReturnType = typeof(string);
                ro.ReturnData =
                    $"config path is {configPath}." +
                    $"\nuse config unicli, config global, config <editor_version> to open the configuration file directly.";

                return ro;
            }

            unicliConfig conf = loadUnicliConfig();

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = conf.textEditorCmd,
                UseShellExecute = false
            };

            string farg = ctx.Args[0].ToLower();

            bool valid =
                farg == "global" ||
                farg == Constants.ApplicationName.ToLower() ||
                isVersionText(farg);

            if (!valid)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Invalid config target: {farg}"
                };
            }

            string argument = Path.Combine(
                configPath,
                $"{Constants.ApplicationName}.config"
            );

            if (farg.Equals("global"))
            {
                argument = Path.Combine(configPath, "global.config");
            }

            if (isVersionText(farg))
            {
                if (!isVersionInstalled(farg.ToLower()))
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData =
                        $"Configuration for Unity {farg} written. However, version {farg} is not installed.";
                }

                string editorsDir = Path.Combine(configPath, "editors");

                if (!Directory.Exists(editorsDir))
                {
                    Directory.CreateDirectory(editorsDir);
                }

                argument = Path.Combine(
                    editorsDir,
                    $"{farg}.config"
                );
            }

            startInfo.ArgumentList.Add(argument);

            Process? process = Process.Start(startInfo);

            process?.WaitForExit();

            return ro;
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject open(ArgContext ctx)
    {
        try
        {

            if (ctx.Args == null || ctx.Args.Length < 1)
            {
                help();

                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Please provide a project name."
                };
            }

            ReturnObject projectResult = getUnityProjects();

            if (projectResult.Code != ReturnCode.Success ||
                projectResult.ReturnData is not Dictionary<string, string> projects)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Failed to retrieve Unity projects."
                };
            }

            if (projects.Count == 0)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "No Projects Found."
                };
            }

            string projectName = ctx.Args[0];

            if (!projects.TryGetValue(projectName, out string? projectPath))
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Project {projectName} not found."
                };
            }

            string? projectVersion =
                getExpectedEditorVersion(projectName);

            int unityArgStart = 1;

            if (ctx.Args.Length >= 2 &&
                isVersionText(ctx.Args[1]))
            {
                projectVersion = ctx.Args[1];
                unityArgStart = 2;
            }

            if (string.IsNullOrWhiteSpace(projectVersion))
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData =
                        "Cannot extract Unity version. Please provide the version manually."
                };
            }

            string? unityBin =
                getEditorBinary(projectVersion);

            if (unityBin == null)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData =
                        $"Version {projectVersion} is not installed."
                };
            }

            unicliConfig conf = loadUnicliConfig();

            string appConfigDir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                Constants.ApplicationName
            );

            Directory.CreateDirectory(appConfigDir);

            string globalConfigPath =
                Path.Combine(appConfigDir, "global.config");

            if (!File.Exists(globalConfigPath))
            {
                File.WriteAllText(globalConfigPath, "");
            }

            string editorsDir =
                Path.Combine(appConfigDir, "editors");

            Directory.CreateDirectory(editorsDir);

            string editorConfigPath = Path.Combine(
                editorsDir,
                $"{projectVersion}.config"
            );

            if (!File.Exists(editorConfigPath))
            {
                File.WriteAllText(editorConfigPath, "");
            }

            ProcessStartInfo startInfo;

            if (OperatingSystem.IsWindows())
            {
                string unityArgs =
                    $"-projectPath \"{projectPath}\"";

                for (int i = unityArgStart; i < ctx.Args.Length; i++)
                {
                    unityArgs +=
                        $" \"{ctx.Args[i].Replace("\"", "\\\"")}\"";
                }

                string script =
                    $"{conf.shellSourceCommand} \"{globalConfigPath}\" && " +
                    $"{conf.shellSourceCommand} \"{editorConfigPath}\" && " +
                    $"\"{unityBin}\" {unityArgs}";

                startInfo = new ProcessStartInfo
                {
                    FileName = conf.shell,
                    UseShellExecute = false
                };

                startInfo.ArgumentList.Add(conf.shellExecArgs);
                startInfo.ArgumentList.Add(script);
            }
            else
            {
                string escapedUnity =
                    unityBin.Replace("\"", "\\\"");

                string escapedProject =
                    projectPath.Replace("\"", "\\\"");

                string script =
                    $"{conf.shellSourceCommand} \"{globalConfigPath}\"\n" +
                    $"{conf.shellSourceCommand} \"{editorConfigPath}\"\n" +
                    $"exec \"{escapedUnity}\" -projectPath \"{escapedProject}\"";

                for (int i = unityArgStart; i < ctx.Args.Length; i++)
                {
                    string escapedArg =
                        ctx.Args[i].Replace("\"", "\\\"");

                    script += $" \"{escapedArg}\"";
                }

                startInfo = new ProcessStartInfo
                {
                    FileName = conf.shell,
                    UseShellExecute = false
                };

                startInfo.ArgumentList.Add(conf.shellExecArgs);
                startInfo.ArgumentList.Add(script);
            }

            Process? process = Process.Start(startInfo);

            if (process == null)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Failed to start Unity process."
                };
            }

            return new ReturnObject
            {
                Code = ReturnCode.Success
            };
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    public ReturnObject add(ArgContext ctx)
    {
        try
        {
            unicliConfig conf = loadUnicliConfig();

            ReturnObject ro = new ReturnObject();

            if (ctx.Args == null || ctx.Args.Length < 1)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    "Please provide a path to a unity project or folder containing projects.";

                return ro;
            }

            HashSet<string> paths = new HashSet<string>();

            foreach (string path in conf.projectPaths)
            {
                paths.Add(fixPath(path));
            }

            if (!Directory.Exists(ctx.Args[0]))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    $"Directory {ctx.Args[0]} not found.";

                return ro;
            }

            paths.Add(fixPath(ctx.Args[0]));

            conf.projectPaths = paths.ToArray();

            writeToUnicliConfig(conf);

            ro.Code = ReturnCode.Success;

            return ro;
        }
        catch (Exception e)
        {
            return new ReturnObject
            {
                Code = ReturnCode.Error,
                ReturnType = typeof(string),
                ReturnData = e.Message
            };
        }
    }

    #endregion

    #region private helpers

    private bool isUnityProject(string folderPath)
    {
        return File.Exists(Path.Combine(folderPath, "ProjectSettings", "ProjectVersion.txt"));
    }

    private bool isUnityEditor(string folderPath)
    {
        return File.Exists(Path.Combine(folderPath, "modules.json")) &&
               Directory.Exists(Path.Combine(folderPath, "Editor"));
    }

    private bool isVersionInstalled(string version)
    {
        string? versions = (string?)getInstalledEditors().ReturnData;
        if (versions == null)
        {
            return false;
        }

        return versions
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(v => v.Trim() == version);
    }

    private unicliConfig loadUnicliConfig()
    {
        string appConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.ApplicationName);
        string configFile = Path.Combine(appConfigDir, Constants.ApplicationName + ".config");
        if (!Directory.Exists(appConfigDir))
        {
            Directory.CreateDirectory(appConfigDir);
        }

        if (!File.Exists(configFile))
        {
            writeToUnicliConfig(unicliConfig.getDefaultConfig());
        }

        unicliConfig? conf =
            JsonSerializer.Deserialize<unicliConfig>(
                File.ReadAllText(configFile));

        if (conf == null)
        {
            throw new Exception("Failed to load configuration.");
        }

        return conf.Value;
    }

    private void writeToUnicliConfig(unicliConfig conf)
    {
        string appConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.ApplicationName);
        string configFile = Path.Combine(appConfigDir, Constants.ApplicationName + ".config");
        if (!File.Exists(configFile))
        {
            File.Create(configFile).Close();
        }

        File.WriteAllText(configFile, conf.ToString());
    }

    private string? getEditorBinary(string version)
    {
        string path = Path.Combine(loadUnicliConfig().editorInstallationPath, version, "Editor",
            (OperatingSystem.IsWindows()) ? "Unity.exe" : "Unity");
        path = fixPath(path);
        return File.Exists(path) ? path : null;
    }

    private ReturnObject getUnityProjects()
    {
        try
        {
            unicliConfig config = loadUnicliConfig();
            Dictionary<string, string> s = new Dictionary<string, string>();
            foreach (string path in config.projectPaths)
            {
                string fpath = fixPath(path);
                if (!Directory.Exists(fpath))
                {
                    continue;
                }

                if (isUnityProject(fpath))
                {
                    string prefix = "";
                    int n = -1;

                    while (s.ContainsKey(((new DirectoryInfo(fpath)).Name + prefix)))
                    {
                        n++;
                        prefix = $"[{n}]";
                    }

                    s.Add(((new DirectoryInfo(fpath)).Name + prefix), fpath);
                }

                foreach (var dir in (new DirectoryInfo(fpath)).GetDirectories())
                {
                    if (isUnityProject(dir.FullName))
                    {
                        string prefix = "";
                        int n = -1;

                        while (s.ContainsKey((dir.Name + prefix)))
                        {
                            n++;
                            prefix = $"[{n}]";
                        }

                        s.Add((dir.Name + prefix), dir.FullName);
                    }
                }
            }

            ReturnObject ro = new ReturnObject(ReturnCode.Success,typeof(Dictionary<string, string>),s);
            return ro;
        }
        catch (Exception e)
        {
            ReturnObject ro = new ReturnObject(ReturnCode.Error,typeof(string),e.Message);
            return ro;
        }
    }

    private ReturnObject getInstalledEditors()
    {
        try
        {
            unicliConfig config = loadUnicliConfig();
            string instPath = fixPath(config.editorInstallationPath);

            if (!Directory.Exists(instPath))
            {
                throw new Exception("Editor installation directory not found.");
            }

            string s = "";
            foreach (DirectoryInfo versions in (new DirectoryInfo(instPath)).GetDirectories())
            {
                if (isUnityEditor(versions.FullName))
                {
                    s += "\n" + versions.Name;
                }
            }

            if (string.IsNullOrWhiteSpace(s))
            {
                s = "No Editors Installed.";
            }


            ReturnObject ro = new ReturnObject(ReturnCode.Success,typeof(string),s);
            return ro;
        }
        catch (Exception e)
        {
            ReturnObject ro = new ReturnObject(ReturnCode.Error,typeof(string),e.Message);
            return ro;
        }
    }

    private bool isVersionText(string version)
    {
        return Regex.Match(version, @"\s*([\d\.]+[a-z]\d+)").Success;
    }

    private string? getExpectedEditorVersion(string projectName)
    {
        ReturnObject ro = getUnityProjects();
        if (ro.Code == ReturnCode.Success)
        {
            if (ro.ReturnData == null || ((Dictionary<string, string>)ro.ReturnData).Count == 0)
            {
                return null;
            }

            Dictionary<string, string> proj = (Dictionary<string, string>)ro.ReturnData;
            if (!proj.ContainsKey(projectName))
            {
                return null;
            }

            string versionFile = Path.Combine(
                proj[projectName],
                "ProjectSettings",
                "ProjectVersion.txt"
            );

            string s = File.ReadAllText(versionFile);

            Match match = Regex.Match(
                s,
                @"m_EditorVersion:\s*([\d\.]+[a-z]\d+)"
            );

            if (match.Success)
            {
                return match.Groups[1].Value;
            }
        }

        return null;
    }

    private string fixPath(string path)
    {
        string execPath = Environment.CurrentDirectory;
        if (string.IsNullOrWhiteSpace(path))
        {
            return execPath;
        }

        path = path.Replace("\\\\", "/");
        path = path.Replace('\\', '/');

        string executionPath = Path.GetFullPath(execPath);
        string home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (path == "." || path == "./")
        {
            return executionPath;
        }

        if (path == ".." || path == "../")
        {
            return Directory.GetParent(executionPath)?.FullName ?? executionPath;
        }

        if (path.StartsWith("~/"))
        {
            path = Path.Combine(home, path.Substring(2));
        }
        else if (path == "~")
        {
            path = home;
        }


        path = path.Replace("$HOME", home);


        if (!Path.IsPathRooted(path))
        {
            path = Path.Combine(executionPath, path);
        }


        return Path.GetFullPath(path).TrimEnd('/');
    }

    #endregion
}