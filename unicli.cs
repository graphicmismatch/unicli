using System.Text.Json;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Net;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
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
    
    public ReturnObject path(ArgContext ctx)
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
            {
                return new ReturnObject
                {
                    Code = ReturnCode.Error,
                    ReturnType = typeof(string),
                    ReturnData = $"Project {projectName} not found."
                };
            }

            if (projectPath.Contains(' '))
            {
                Console.WriteLine($"\"{projectPath}\"");
            }
            else
            {
                Console.WriteLine(projectPath);
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
           

            var ro = new ReturnObject();

            if (ctx.Args == null || ctx.Args.Length < 1)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    "Please provide a path to a unity project or folder containing projects.";

                return ro;
            }

            

            if (!Directory.Exists(ctx.Args[0]))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    $"Directory {ctx.Args[0]} not found.";

                return ro;
            }
            var conf = loadUnicliConfig();
            var paths = new HashSet<string>();

            foreach (var path in conf.projectPaths) paths.Add(fixPath(path));
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
            Console.WriteLine("\n"+sha256);

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

    public ReturnObject create(ArgContext ctx)
    {
        try
        {
            ReturnObject ro =  new ReturnObject();
            if (ctx.Args == null || ctx.Args.Length == 0)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    "Please provide a name and unity version";

                return ro;
            }

            if (ctx.Args.Length == 1)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    "Please provide a unity version";

                return ro;
            }

            if (!isVersionText(ctx.Args[1]))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    $"{ctx.Args[1]} is not a valid unity version.";

                return ro;
            }
            
            if (!isVersionInstalled(ctx.Args[1]))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    $"Unity {ctx.Args[1]} is not installed.";

                return ro;
            }

            var currentDir = ctx.executionPath;
            if (Directory.Exists(Path.Combine(currentDir, ctx.Args[0])))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData =
                    $"Folder {Path.Combine(currentDir, ctx.Args[0])} already exists.";

                return ro;
            }
            DirectoryInfo projectDir = Directory.CreateDirectory(Path.Combine(currentDir, ctx.Args[0]));
            projectDir.CreateSubdirectory("Assets");
            projectDir.CreateSubdirectory("ProjectSettings");
            File.Create(Path.Combine(projectDir.FullName, "ProjectSettings","ProjectVersion.txt")).Close();
            File.WriteAllText(Path.Combine(projectDir.FullName, "ProjectSettings","ProjectVersion.txt"),$"m_EditorVersion: {ctx.Args[1]}");
            Console.WriteLine("Project Created.");
            if (!isInPath(currentDir))
            {
                ArgContext ctxNew =  new ArgContext();
                ctxNew.Args = [projectDir.FullName];
                ReturnObject ro2 = add(ctxNew);
                if (!ro2.IsSuccess)
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnData = "Unable to Add directory to unicli project paths: "+(string)ro2.ReturnData;
                    ro.ReturnType = typeof(string);
                }
                else
                {
                    Console.WriteLine("Added Project to unicli project paths.");
                }
            }
            if (ctx.Args.Length >= 3)
            {
                var projects = getUnityProjects();
                if (!projects.IsSuccess || (Dictionary<string, string>?)projects.ReturnData == null ||
                    ((Dictionary<string, string>)projects.ReturnData).Count == 0)
                {
                    ro.ReturnType = typeof(string);
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnData =
                        $"No Projects Found. Manifest could not be copied.";
                    return ro;
                }

                Dictionary<string, string> projs = (Dictionary<string, string>)projects.ReturnData;
                if (!projs.ContainsKey(ctx.Args[2]))
                {
                    ro.ReturnType = typeof(string);
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnData =
                        $"Project {ctx.Args[2]} cannot be found. Manifest could not be copied.";
                    return ro;
                }
                string manifestPath = Path.Combine(projs[ctx.Args[2]],"Packages","manifest.json");
                if (!File.Exists(manifestPath))
                {
                    ro.ReturnType = typeof(string);
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnData =
                        $"Manifest file for project {ctx.Args[2]} cannot be found. Manifest could not be copied.";
                    return ro;
                }
                string s = File.ReadAllText(manifestPath);
                DirectoryInfo pkgs = projectDir.CreateSubdirectory("Packages");
                File.Create(Path.Combine(pkgs.FullName, "manifest.json")).Close();
                File.WriteAllText(Path.Combine(pkgs.FullName, "manifest.json"),s);
                Console.WriteLine("Manifest Copied.");
            }

            return new ReturnObject()
            {
                Code = ReturnCode.Success,
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

    
    public ReturnObject install(ArgContext ctx)
    {
        //https://services.api.unity.com/unity/editor/release/v1/releases?version=6000.4.9f1
        try
        {
            ReturnObject ro = new ReturnObject();
            if (ctx.Args == null || ctx.Args.Length == 0)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = "Please provide a unity version";

                return ro;
            }

            string version = ctx.Args[0];
            if (!isVersionText(version))
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = $"{version} is not a valid unity version.";

                return ro;
            }

            bool editorInstalled = isVersionInstalled(version);

            using HttpClient client = new HttpClient { Timeout = Timeout.InfiniteTimeSpan };
            UnityReleaseResponse? response = client
                .GetFromJsonAsync<UnityReleaseResponse>(String.Format(Constants.releaseUrl, version))
                .GetAwaiter().GetResult();
            if (response == null)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = "Unable to fetch data. Check your Internet.";
                return ro;
            }

            List<Result> releases = response.Value.results.Where(r => r.version == version).ToList();
            if (releases.Count == 0)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = $"Unity {version} not found.";
                return ro;
            }

            string platform = OperatingSystem.IsWindows() ? "WINDOWS" : OperatingSystem.IsMacOS() ? "MAC_OS" : "LINUX";
            string arch = RuntimeInformation.OSArchitecture == Architecture.Arm64 ? "ARM64" : "X86_64";
            List<Download> downloads = releases[0].downloads
                .Where(d => d.platform == platform && d.architecture == arch).ToList();
            if (downloads.Count == 0)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = $"Unity {version} is not available for {platform} {arch}.";
                return ro;
            }

            Download dl = downloads[0];
            string versionDir = Path.Combine(fixPath(loadUnicliConfig().editorInstallationPath), version);

            // DMGs and modules without a destination are external apps (Visual Studio, VS Code).
            List<Module> available = dl.modules
                .Where(m => !m.hidden && m.destination != null && m.type != "DMG").ToList();
            List<Module> selected = new List<Module>();
            if (ctx.Args.Length >= 2)
            {
                foreach (string id in ctx.Args[1..])
                {
                    List<Module> match = available.Where(m => m.id.Equals(id, StringComparison.OrdinalIgnoreCase)).ToList();
                    if (match.Count == 0)
                    {
                        ro.ReturnType = typeof(string);
                        ro.Code = ReturnCode.Error;
                        ro.ReturnData = $"Module {id} not found. Available modules: " +
                                        string.Join(", ", available.Select(m => m.id));
                        return ro;
                    }

                    selected.Add(match[0]);
                }
            }
            else
            {
                selected = promptModules(available);
            }

            List<Module> toInstall = new List<Module>();
            foreach (Module m in selected) collectModules(m, toInstall);

            if (editorInstalled && toInstall.Count == 0)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.SuccessWithMessage;
                ro.ReturnData = $"Unity {version} is already installed.";

                return ro;
            }

            if (!editorInstalled)
            {
                // Fail before downloading if the install path isn't writable (e.g. Program Files without elevation).
                try
                {
                    Directory.CreateDirectory(versionDir);
                }
                catch (UnauthorizedAccessException)
                {
                    ro.ReturnType = typeof(string);
                    ro.Code = ReturnCode.Error;
                    ro.ReturnData = $"Cannot write to {versionDir}. Run unicli from an elevated terminal.";
                    return ro;
                }

                try
                {
                    installEditor(client, dl, version, versionDir);
                }
                catch
                {
                    if (Directory.Exists(versionDir)) Directory.Delete(versionDir, true);
                    throw;
                }

                Console.WriteLine($"Unity {version} installed to {versionDir}.");
            }

            foreach (Module m in toInstall)
            {
                installModule(client, m, versionDir);
                Console.WriteLine($"{m.name} installed.");
            }

            return new ReturnObject()
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
    
    
    public ReturnObject releases(ArgContext ctx)
    {
        //https://services.api.unity.com/unity/editor/release/v1/releases
        try
        {
            using HttpClient client = new HttpClient();
            ReturnObject ro = new ReturnObject();
            unicliConfig conf = loadUnicliConfig();
            int limit = conf.defaultReleasesLimit;
            int offset = 0;
            
            if (ctx.ArgCount >= 1)
            {
                if (!int.TryParse(ctx.Args[0], out limit))
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData =  $"Limit replaced with default value ({conf.defaultReleasesLimit}).";
                }

                if (limit > 25 || limit <= 0)
                {
                    limit = conf.defaultReleasesLimit;
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData = $"Limit replaced with default value ({conf.defaultReleasesLimit}).";
                }
            }

            if (ctx.ArgCount >= 2)
            {
                if (!int.TryParse(ctx.Args[1], out offset))
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData = "Offset replaced with default value (0).";
                }

                if (offset < 0)
                {
                    ro.Code = ReturnCode.SuccessWithWarning;
                    ro.ReturnType = typeof(string);
                    ro.ReturnData = "Offset replaced with default value (0).";
                    offset = 0;
                }
            }

            string q = String.Format(Constants.releasesUrl, limit, offset);
            
            Console.WriteLine(q);
            UnityReleaseResponse? response = client.GetFromJsonAsync< UnityReleaseResponse>(q).GetAwaiter().GetResult();
            if (response == null)
            {
                ro.ReturnType = typeof(string);
                ro.Code = ReturnCode.Error;
                ro.ReturnData = "Unable to fetch data. Check your Internet.";
                return ro;
            }

            foreach (Result res in response.Value.results)
            {
                Console.WriteLine(res);
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
    #endregion

    #region private helpers

    private List<Module> promptModules(List<Module> available)
    {
        List<Module> selected = new List<Module>();
        if (available.Count == 0) return selected;

        Console.WriteLine("Available modules:");
        for (int i = 0; i < available.Count; i++)
        {
            long.TryParse(available[i].downloadSize.value?.ToString(), out long size);
            Console.WriteLine($"  [{i + 1}] {available[i].name} ({available[i].id}) {size / 1024 / 1024} MB");
        }

        while (true)
        {
            Console.Write("Select modules to install (e.g. 1 3 5), or press Enter for none: ");
            string? line = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(line)) return selected;

            selected.Clear();
            bool valid = true;
            foreach (string s in line.Split([' ', ','], StringSplitOptions.RemoveEmptyEntries))
            {
                if (!int.TryParse(s, out int n) || n < 1 || n > available.Count)
                {
                    Console.WriteLine($"{s} is not a valid selection.");
                    valid = false;
                    break;
                }

                if (!selected.Contains(available[n - 1])) selected.Add(available[n - 1]);
            }

            if (valid) return selected;
        }
    }

    // Adds a module and its required submodules, parents first so children extract into them.
    private void collectModules(Module module, List<Module> into)
    {
        if (module.eula != null && module.eula.Count > 0)
        {
            foreach (Eula eula in module.eula)
                Console.WriteLine($"\n{eula.label}\n{eula.message}\n{eula.url}");

            Console.Write($"Accept the license terms for {module.name}? [y/N]: ");
            if (Console.ReadLine()?.Trim().ToLower() != "y")
            {
                Console.WriteLine($"Skipping {module.name}.");
                return;
            }
        }

        into.Add(module);
        if (module.subModules == null) return;

        foreach (Module sub in module.subModules)
            if (sub.required)
                collectModules(sub, into);
    }

    private void installEditor(HttpClient client, Download dl, string version, string versionDir)
    {
        string file = downloadFile(client, dl.url, dl.integrity, $"Unity {version}");
        try
        {
            Console.WriteLine("Installing...");
            Directory.CreateDirectory(versionDir);

            if (OperatingSystem.IsWindows())
            {
                runWindowsInstaller(file, versionDir);
            }
            else if (OperatingSystem.IsMacOS())
            {
                string expandDir = Path.Combine(Path.GetTempPath(), Constants.ApplicationName + "-" + version);
                if (Directory.Exists(expandDir)) Directory.Delete(expandDir, true);
                try
                {
                    runProcess("pkgutil", "--expand-full", file, expandDir);
                    string[] apps = Directory.GetDirectories(expandDir, "Unity.app", SearchOption.AllDirectories);
                    if (apps.Length == 0) throw new Exception("Unity.app not found in package.");

                    Directory.Move(apps[0], Path.Combine(versionDir, "Unity.app"));
                }
                finally
                {
                    if (Directory.Exists(expandDir)) Directory.Delete(expandDir, true);
                }
            }
            else
            {
                runProcess("tar", "-xJf", file, "-C", versionDir);
            }

            // modules.json is what isUnityEditor looks for.
            File.WriteAllText(Path.Combine(versionDir, "modules.json"), JsonSerializer.Serialize(dl.modules));
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    private void installModule(HttpClient client, Module module, string versionDir)
    {
        string dest = module.destination.Replace("{UNITY_PATH}", versionDir);
        string file = downloadFile(client, module.url, module.integrity, module.name);
        try
        {
            Directory.CreateDirectory(dest);
            switch (module.type)
            {
                case "TAR_XZ":
                    // Module tarballs are rooted at the editor folder, not at the destination.
                    runProcess("tar", "-xJf", file, "-C", versionDir);
                    break;
                case "ZIP":
                    // ZipFile doesn't restore symlinks, which the Android NDK relies on.
                    if (OperatingSystem.IsWindows())
                        ZipFile.ExtractToDirectory(file, dest, true);
                    else
                        runProcess("unzip", "-q", "-o", file, "-d", dest);
                    break;
                case "PO":
                    File.Copy(file, Path.Combine(dest, Path.GetFileName(new Uri(module.url).LocalPath) + ".po"), true);
                    break;
                case "PKG":
                    extractPkg(file, dest);
                    break;
                case "EXE":
                    runWindowsInstaller(file, dest);
                    break;
                default:
                    throw new Exception($"Unsupported module type {module.type} for {module.name}.");
            }

            if (module.extractedPathRename != null)
            {
                string from = module.extractedPathRename.Value.from.Replace("{UNITY_PATH}", versionDir);
                string to = module.extractedPathRename.Value.to.Replace("{UNITY_PATH}", versionDir);
                // Move the contents rather than the folder, since "to" can be the parent of "from".
                if (Directory.Exists(from) && from != to)
                {
                    Directory.CreateDirectory(to);
                    foreach (string d in Directory.GetDirectories(from))
                        Directory.Move(d, Path.Combine(to, Path.GetFileName(d)));
                    foreach (string f in Directory.GetFiles(from))
                        File.Move(f, Path.Combine(to, Path.GetFileName(f)), true);
                    Directory.Delete(from);
                }
            }
        }
        finally
        {
            if (File.Exists(file)) File.Delete(file);
        }
    }

    // Unity module pkgs hold a gzipped cpio Payload whose paths are relative to the destination.
    private void extractPkg(string file, string dest)
    {
        string expandDir = Path.Combine(Path.GetTempPath(), Constants.ApplicationName + "-pkg");
        if (Directory.Exists(expandDir)) Directory.Delete(expandDir, true);
        try
        {
            if (OperatingSystem.IsMacOS())
                runProcess("pkgutil", "--expand", file, expandDir);
            else
                runProcess("7z", "x", file, "-o" + expandDir, "-y", "-bso0");

            string[] payloads = Directory.GetFiles(expandDir, "Payload", SearchOption.AllDirectories);
            if (payloads.Length == 0) throw new Exception("Payload not found in package.");

            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = "cpio",
                WorkingDirectory = dest,
                RedirectStandardInput = true,
                UseShellExecute = false
            };
            startInfo.ArgumentList.Add("-idmu");
            startInfo.ArgumentList.Add("--quiet");

            using Process process = Process.Start(startInfo) ?? throw new Exception("Failed to start cpio.");
            using (FileStream fs = File.OpenRead(payloads[0]))
            using (GZipStream gz = new GZipStream(fs, CompressionMode.Decompress))
            {
                gz.CopyTo(process.StandardInput.BaseStream);
            }

            process.StandardInput.Close();
            process.WaitForExit();
            if (process.ExitCode != 0) throw new Exception($"cpio exited with code {process.ExitCode}.");
        }
        finally
        {
            if (Directory.Exists(expandDir)) Directory.Delete(expandDir, true);
        }
    }

    private string downloadFile(HttpClient client, string url, string? integrity, string label)
    {
        string file = Path.Combine(Path.GetTempPath(), Path.GetFileName(new Uri(url).LocalPath));
        using (HttpResponseMessage msg = client
                   .GetAsync(url, HttpCompletionOption.ResponseHeadersRead)
                   .GetAwaiter().GetResult())
        {
            msg.EnsureSuccessStatusCode();
            long? total = msg.Content.Headers.ContentLength;
            using Stream src = msg.Content.ReadAsStream();
            using FileStream dst = File.Create(file);
            byte[] buffer = new byte[81920];
            long done = 0;
            long lastPercent = -1;
            int n;
            while ((n = src.Read(buffer)) > 0)
            {
                dst.Write(buffer, 0, n);
                done += n;
                long percent = total > 0 ? done * 100 / total.Value : -1;
                if (percent != lastPercent)
                {
                    Console.Write($"\rDownloading {label}... {percent}%");
                    lastPercent = percent;
                }
            }

            Console.WriteLine();
        }

        if (!string.IsNullOrEmpty(integrity) && integrity.StartsWith("md5-"))
        {
            string hash;
            using (FileStream fs = File.OpenRead(file))
            {
                hash = Convert.ToBase64String(MD5.HashData(fs));
            }

            if (hash != integrity.Substring(4))
            {
                File.Delete(file);
                throw new Exception($"{label} failed the integrity check.");
            }
        }

        return file;
    }

    private void runProcess(string fileName, params string[] args)
    {
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = fileName,
            UseShellExecute = false
        };
        foreach (string arg in args) startInfo.ArgumentList.Add(arg);

        using Process process = Process.Start(startInfo) ?? throw new Exception($"Failed to start {fileName}.");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception($"{fileName} exited with code {process.ExitCode}.");
    }

    private void runWindowsInstaller(string file, string dest)
    {
        // NSIS wants /D last and unquoted, so ArgumentList can't be used here.
        // ShellExecute lets the installer raise its own UAC prompt.
        ProcessStartInfo startInfo = new ProcessStartInfo
        {
            FileName = file,
            Arguments = $"/S /D={dest.Replace('/', '\\')}",
            UseShellExecute = true
        };

        using Process process = Process.Start(startInfo) ?? throw new Exception($"Failed to start {file}.");
        process.WaitForExit();
        if (process.ExitCode != 0) throw new Exception($"Installer exited with code {process.ExitCode}.");
    }

    private bool isInPath(string path)
    {
        var conf = loadUnicliConfig();
        var fpath = fixPath(path);
        var paths = new HashSet<string>();
        foreach (var tpath in conf.projectPaths) paths.Add(fixPath(tpath));
        return paths.Contains(fpath);
    }

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