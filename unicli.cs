using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Text;

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
            var ro = getUnityProjects();

            if (ro.Code == ReturnCode.Success)
            {
                if (ro.ReturnData == null ||
                    ((Dictionary<string, string>)ro.ReturnData).Count == 0)
                {
                    Console.WriteLine("No project files found.");
                    return ro;
                }

                var proj =
                    (Dictionary<string, string>)ro.ReturnData;

                foreach (var key in proj.Keys) Console.WriteLine($"{key} -> {proj[key]}");
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

    public ReturnObject projectinfo(ArgContext ctx)
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

            var projectResult = getUnityProjects();

            if (projectResult.Code != ReturnCode.Success ||
                projectResult.ReturnData is not Dictionary<string, string> projects)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Failed to retrieve Unity projects."
                };

            if (projects.Count == 0)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "No Projects Found."
                };

            var projectName = ctx.Args[0];

            if (!projects.TryGetValue(projectName, out var projectPath))
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Project {projectName} not found."
                };

            var projectVersion =
                getExpectedEditorVersion(projectName);

            var unityArgStart = 1;

            if (ctx.Args.Length >= 2 &&
                isVersionText(ctx.Args[1]))
            {
                projectVersion = ctx.Args[1];
                unityArgStart = 2;
            }

            if (string.IsNullOrWhiteSpace(projectVersion))
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData =
                        "Cannot extract Unity version."
                };
            Console.WriteLine("Project Name: " + projectName);
            Console.WriteLine("Project Version: " + projectVersion);
            Console.WriteLine("Project Path: " + projectPath);

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

    public ReturnObject editors(ArgContext ctx)
    {
        try
        {
            var ro = getInstalledEditors();

            if (ro.Code == ReturnCode.Success) Console.WriteLine(ro.ReturnData);

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
            var configPath = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                Constants.ApplicationName
            );

            var ro = new ReturnObject
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

            var conf = loadUnicliConfig();

            ProcessStartInfo startInfo;

            var farg = ctx.Args[0].ToLower();

            var valid =
                farg == "global" ||
                farg == Constants.ApplicationName.ToLower() ||
                isVersionText(farg);

            if (!valid)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Invalid config target: {farg}"
                };

            var argument = Path.Combine(
                configPath,
                $"{Constants.ApplicationName}.config"
            );

            if (farg.Equals("global")) argument = Path.Combine(configPath, "global.config");

            if (isVersionText(farg))
            {
                if (!isVersionInstalled(farg.ToLower()))
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData =
                        $"Configuration for Unity {farg} written. However, version {farg} is not installed.";
                }

                var editorsDir = Path.Combine(configPath, "editors");

                if (!Directory.Exists(editorsDir)) Directory.CreateDirectory(editorsDir);

                argument = Path.Combine(
                    editorsDir,
                    $"{farg}.config"
                );
            }

            if (conf.textEditorCmd.Contains("%s"))
            {
                var editorCommand =
                    conf.textEditorCmd.Replace(
                        "%s",
                        "\"" + argument.Replace("\"", "\\\"") + "\""
                    );

                startInfo = new ProcessStartInfo
                {
                    FileName = conf.shell,
                    UseShellExecute = false
                };

                startInfo.ArgumentList.Add(conf.shellExecArgs);
                startInfo.ArgumentList.Add(editorCommand);
            }
            else
            {
                startInfo = new ProcessStartInfo
                {
                    FileName = conf.textEditorCmd,
                    UseShellExecute = false
                };

                startInfo.ArgumentList.Add(argument);
            }


            var process = Process.Start(startInfo);

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

            var projectResult = getUnityProjects();

            if (projectResult.Code != ReturnCode.Success ||
                projectResult.ReturnData is not Dictionary<string, string> projects)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Failed to retrieve Unity projects."
                };

            if (projects.Count == 0)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "No Projects Found."
                };

            var projectName = ctx.Args[0];

            if (!projects.TryGetValue(projectName, out var projectPath))
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Project {projectName} not found."
                };

            var projectVersion =
                getExpectedEditorVersion(projectName);

            var unityArgStart = 1;

            if (ctx.Args.Length >= 2 &&
                isVersionText(ctx.Args[1]))
            {
                projectVersion = ctx.Args[1];
                unityArgStart = 2;
            }

            if (string.IsNullOrWhiteSpace(projectVersion))
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData =
                        "Cannot extract Unity version. Please provide the version manually."
                };

            var unityBin =
                getEditorBinary(projectVersion);

            if (unityBin == null)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData =
                        $"Version {projectVersion} is not installed."
                };

            var conf = loadUnicliConfig();

            var appConfigDir = Path.Combine(
                Environment.GetFolderPath(
                    Environment.SpecialFolder.ApplicationData
                ),
                Constants.ApplicationName
            );

            Directory.CreateDirectory(appConfigDir);

            var globalConfigPath =
                Path.Combine(appConfigDir, "global.config");

            if (!File.Exists(globalConfigPath)) File.WriteAllText(globalConfigPath, "");

            var editorsDir =
                Path.Combine(appConfigDir, "editors");

            Directory.CreateDirectory(editorsDir);

            var editorConfigPath = Path.Combine(
                editorsDir,
                $"{projectVersion}.config"
            );

            if (!File.Exists(editorConfigPath)) File.WriteAllText(editorConfigPath, "");

            ProcessStartInfo startInfo;

            if (OperatingSystem.IsWindows())
            {
                var unityArgs =
                    $"-projectPath \"{projectPath}\"";

                for (var i = unityArgStart; i < ctx.Args.Length; i++)
                    unityArgs +=
                        $" \"{ctx.Args[i].Replace("\"", "\\\"")}\"";

                var script =
                    $"{conf.shellSourceCommand} \"{globalConfigPath}\"; " +
                    $"{conf.shellSourceCommand} \"{editorConfigPath}\"; " +
                    $"& \"{unityBin}\" {unityArgs}";

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
                var escapedUnity =
                    unityBin.Replace("\"", "\\\"");

                var escapedProject =
                    projectPath.Replace("\"", "\\\"");

                var script =
                    $"{conf.shellSourceCommand} \"{globalConfigPath}\"\n" +
                    $"{conf.shellSourceCommand} \"{editorConfigPath}\"\n" +
                    $"exec \"{escapedUnity}\" -projectPath \"{escapedProject}\"";

                for (var i = unityArgStart; i < ctx.Args.Length; i++)
                {
                    var escapedArg =
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

            var process = Process.Start(startInfo);

            if (process == null)
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Failed to start Unity process."
                };

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
            var conf = loadUnicliConfig();

            var ro = new ReturnObject();

            if (ctx.Args == null || ctx.Args.Length < 1)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    "Please provide a path to a unity project or folder containing projects.";

                return ro;
            }

            var paths = new HashSet<string>();

            foreach (var path in conf.projectPaths) paths.Add(fixPath(path));

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

    public ReturnObject getcertfingerprint(ArgContext ctx)
    {
        //unicli getcertfingerprint project keystore alias storePass* keyPass*
        try
        {
            if (ctx.Args == null || ctx.Args.Length == 0)
            {
                help();

                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Please provide a project name."
                };
            }
            if (ctx.Args.Length == 1)
            {
                help();

                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Please provide a keystore name."
                };
            }
            if (ctx.Args.Length == 2)
            {
                help();

                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = "Please provide an alias name."
                };
            }
            var projectResult = getUnityProjects();

            if (projectResult.Code != ReturnCode.Success || projectResult.ReturnData is not Dictionary<string, string> projects)
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

            var projectName = ctx.Args[0];

            if (!projects.TryGetValue(projectName, out var projectPath))
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Project {projectName} not found."
                };
            }
            
            DirectoryInfo projectDirectory = new DirectoryInfo(projectPath);
            FileInfo? keystore = null;

            foreach (var file in projectDirectory.EnumerateFiles("*.keystore", SearchOption.AllDirectories))
            {
                if (string.Equals(Path.GetFileNameWithoutExtension(file.Name), ctx.Args[1], StringComparison.OrdinalIgnoreCase))
                {
                    keystore = file;
                    break;
                }
            }

            if (keystore == null)
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Keystore {ctx.Args[1]}.keystore not found."
                };
            }
            var conf = loadUnicliConfig();
            string keytoolPath = (string.IsNullOrEmpty(conf.keyToolPath))?"keytool":conf.keyToolPath;
            string storePass = "";
            string keypass = "";
            if (ctx.Args.Length < 5)
            {
                if (ctx.Args.Length == 4)
                {
                    storePass = ctx.Args[3];
                    keypass = ctx.Args[3];
                }
                else
                {
                    Console.Write($"Keystore Password ({ctx.Args[1]}.keystore): ");
                    StringBuilder ksp = new StringBuilder();
                    ConsoleKeyInfo key;
                    do
                    {
                        key = Console.ReadKey(intercept: true);

                        if (key.Key == ConsoleKey.Backspace && ksp.Length > 0)
                        {
                            ksp.Length--;
                            Console.Write("\b \b");
                        }
                        else if (key.Key != ConsoleKey.Enter)
                        {
                            ksp.Append(key.KeyChar);
                            Console.Write("*");
                        }

                    } while (key.Key != ConsoleKey.Enter);

                    Console.WriteLine();
                    storePass = ksp.ToString();


                    Console.Write($"Key Password ({ctx.Args[2]}): ");
                    StringBuilder kp = new StringBuilder();
                    do
                    {
                        key = Console.ReadKey(intercept: true);

                        if (key.Key == ConsoleKey.Backspace && kp.Length > 0)
                        {
                            kp.Length--;
                            Console.Write("\b \b");
                        }
                        else if (key.Key != ConsoleKey.Enter)
                        {
                            kp.Append(key.KeyChar);
                            Console.Write("*");
                        }

                    } while (key.Key != ConsoleKey.Enter);

                    Console.WriteLine();
                    keypass = kp.ToString();
                }
            }
            else
            {
                storePass = ctx.Args[3];
                keypass = ctx.Args[4];
            }

            string arguments =
                "-list -v " +
                $"-keystore \"{keystore.FullName}\" " +
                $"-alias \"{ctx.Args[2]}\" " +
                $"-storepass \"{storePass}\" " +
                $"-keypass \"{keypass}\"";
            
            var processStartInfo = new ProcessStartInfo
            {
                FileName = keytoolPath,
                Arguments = arguments,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            
            using var process = new Process
            {
                StartInfo = processStartInfo
            };

            process.Start();

            string output = process.StandardOutput.ReadToEnd();
            string error = process.StandardError.ReadToEnd();

            process.WaitForExit();
            if (!string.IsNullOrWhiteSpace(error) && (string.IsNullOrWhiteSpace(output)||string.IsNullOrEmpty(output)) )
            {
                return new ReturnObject(ReturnCode.Error, typeof(string),error);
            }
            var sha256Match = Regex.Match(
                output,
                @"SHA256:\s*([A-Fa-f0-9:]+)",
                RegexOptions.IgnoreCase);

            if (!sha256Match.Success)
            {
                return new ReturnObject(ReturnCode.Error, typeof(string),"SHA256 fingerprint not found. (Check Entered Password)");
            }
            
            string sha256 = sha256Match.Groups[1].Value;
            Console.WriteLine(sha256);

            if (!string.IsNullOrWhiteSpace(error))
            {
                return new ReturnObject
                {
                    Code = ReturnCode.SuccessWithWarning,
                    ReturnData = error,
                    ReturnType = typeof(string)
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
                ReturnData = e.Message,
                ReturnType = typeof(string)
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
        if (!File.Exists(Path.Combine(folderPath, "modules.json"))) return false;

        if (OperatingSystem.IsMacOS())
            return File.Exists(Path.Combine(folderPath, "Unity.app", "Contents", "MacOS", "Unity"));

        return Directory.Exists(Path.Combine(folderPath, "Editor"));
    }

    private bool isVersionInstalled(string version)
    {
        var versions = (string?)getInstalledEditors().ReturnData;
        if (versions == null) return false;

        return versions
            .Split('\n', StringSplitOptions.RemoveEmptyEntries)
            .Any(v => v.Trim() == version);
    }

    private unicliConfig loadUnicliConfig()
    {
        var appConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.ApplicationName);
        var configFile = Path.Combine(appConfigDir, Constants.ApplicationName + ".config");
        if (!Directory.Exists(appConfigDir)) Directory.CreateDirectory(appConfigDir);

        if (!File.Exists(configFile)) writeToUnicliConfig(unicliConfig.getDefaultConfig());

        unicliConfig? conf =
            JsonSerializer.Deserialize<unicliConfig>(
                File.ReadAllText(configFile));

        if (conf == null) throw new Exception("Failed to load configuration.");

        return conf.Value;
    }

    private void writeToUnicliConfig(unicliConfig conf)
    {
        var appConfigDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            Constants.ApplicationName);
        var configFile = Path.Combine(appConfigDir, Constants.ApplicationName + ".config");
        if (!File.Exists(configFile)) File.Create(configFile).Close();

        File.WriteAllText(configFile, conf.ToString());
    }

    private string? getEditorBinary(string version)
    {
        var installPath = loadUnicliConfig().editorInstallationPath;
        var path = OperatingSystem.IsMacOS()
            ? Path.Combine(installPath, version, "Unity.app", "Contents", "MacOS", "Unity")
            : Path.Combine(installPath, version, "Editor",
                OperatingSystem.IsWindows() ? "Unity.exe" : "Unity");
        path = fixPath(path);
        return File.Exists(path) ? path : null;
    }

    private ReturnObject getUnityProjects()
    {
        try
        {
            var config = loadUnicliConfig();
            var s = new Dictionary<string, string>();
            foreach (var path in config.projectPaths)
            {
                var fpath = fixPath(path);
                if (!Directory.Exists(fpath)) continue;

                if (isUnityProject(fpath))
                {
                    var prefix = "";
                    var n = -1;

                    while (s.ContainsKey(new DirectoryInfo(fpath).Name + prefix))
                    {
                        n++;
                        prefix = $"[{n}]";
                    }

                    s.Add(new DirectoryInfo(fpath).Name + prefix, fpath);
                }

                foreach (var dir in new DirectoryInfo(fpath).GetDirectories())
                    if (isUnityProject(dir.FullName))
                    {
                        var prefix = "";
                        var n = -1;

                        while (s.ContainsKey(dir.Name + prefix))
                        {
                            n++;
                            prefix = $"[{n}]";
                        }

                        s.Add(dir.Name + prefix, dir.FullName);
                    }
            }

            var ro = new ReturnObject(ReturnCode.Success, typeof(Dictionary<string, string>), s);
            return ro;
        }
        catch (Exception e)
        {
            var ro = new ReturnObject(ReturnCode.Error, typeof(string), e.Message);
            return ro;
        }
    }

    private ReturnObject getInstalledEditors()
    {
        try
        {
            var config = loadUnicliConfig();
            var instPath = fixPath(config.editorInstallationPath);

            if (!Directory.Exists(instPath)) throw new Exception("Editor installation directory not found.");

            var s = "";
            foreach (var versions in new DirectoryInfo(instPath).GetDirectories())
                if (isUnityEditor(versions.FullName))
                    s += "\n" + versions.Name;

            if (string.IsNullOrWhiteSpace(s)) s = "No Editors Installed.";


            var ro = new ReturnObject(ReturnCode.Success, typeof(string), s);
            return ro;
        }
        catch (Exception e)
        {
            var ro = new ReturnObject(ReturnCode.Error, typeof(string), e.Message);
            return ro;
        }
    }

    private bool isVersionText(string version)
    {
        return Regex.Match(version, @"\s*([\d\.]+[a-z]\d+)").Success;
    }

    private string? getExpectedEditorVersion(string projectName)
    {
        var ro = getUnityProjects();
        if (ro.Code == ReturnCode.Success)
        {
            if (ro.ReturnData == null || ((Dictionary<string, string>)ro.ReturnData).Count == 0) return null;

            var proj = (Dictionary<string, string>)ro.ReturnData;
            if (!proj.ContainsKey(projectName)) return null;

            var versionFile = Path.Combine(
                proj[projectName],
                "ProjectSettings",
                "ProjectVersion.txt"
            );

            var s = File.ReadAllText(versionFile);

            var match = Regex.Match(
                s,
                @"m_EditorVersion:\s*([\d\.]+[a-z]\d+)"
            );

            if (match.Success) return match.Groups[1].Value;
        }

        return null;
    }

    private string fixPath(string path)
    {
        var execPath = Environment.CurrentDirectory;
        if (string.IsNullOrWhiteSpace(path)) return execPath;

        path = path.Replace("\\\\", "/");
        path = path.Replace('\\', '/');

        var executionPath = Path.GetFullPath(execPath);
        var home = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

        if (path == "." || path == "./") return executionPath;

        if (path == ".." || path == "../") return Directory.GetParent(executionPath)?.FullName ?? executionPath;

        if (path.StartsWith("~/"))
            path = Path.Combine(home, path.Substring(2));
        else if (path == "~") path = home;


        path = path.Replace("$HOME", home);


        if (!Path.IsPathRooted(path)) path = Path.Combine(executionPath, path);


        return Path.GetFullPath(path).TrimEnd('/');
    }

    #endregion
}