namespace unicli;

public static class Constants
{
    public const string ApplicationName = "unicli";
    public const string Version = "v1.2";

    public const string HelpText = """
        Verbs:
             help, version, editors, projects, projectinfo,
             open, add, install, config, getcertfingerprint

        Usage:
             unicli help
                 Displays this help text.

             unicli version
                 Displays the current version.

             unicli editors
                 List installed Unity editor versions.

             unicli projects
                 List Unity projects.

             unicli projectinfo <project_name>
                 Lists information about the Unity project.

             unicli path <project_name>
                 Returns the path of a project.

             unicli open <project_name> [extra args]
                 Open project.

             unicli open <project_name> <editor_version> [extra args]
                 Open project with specific Unity editor.

             unicli install <editor_version>
                 Download and install a Unity editor. Prompts for modules to install.

             unicli install <editor_version> <module_id> [module_ids...]
                 Download and install a Unity editor with the given modules (e.g. android webgl).
                 Adds the modules if the editor is already installed.

             unicli add <folder>
                 Add a project scan directory or project.

             unicli config unicli
                 Edit unicli configuration.

             unicli config global
                 Edit global configuration.

             unicli config <editor_version>
                 Edit editor-specific configuration.

             unicli getcertfingerprint <project_name> <keystore_name> <alias>
                 Get the SHA256 fingerprint from a keystore. A password prompt is provided.

             unicli getcertfingerprint <project_name> <keystore_name> <alias> <keystore_and_alias_password>
                 Get the SHA256 fingerprint from a keystore. The same password is used for both the keystore and the key.

             unicli getcertfingerprint <project_name> <keystore_name> <alias> <keystore_password> <key_password>
                 Get the SHA256 fingerprint from a keystore.

        Examples:
          unicli editors
          unicli projects
          unicli open MyGame 6000.3.6f1
          unicli open MyGame
          unicli add ~/Games/Unity
          unicli add ~/Downloads/TestProject
        """;

    public const string releaseUrl =
        "https://services.api.unity.com/unity/editor/release/v1/releases?version={0}";
    public const string releasesUrl =
        "https://services.api.unity.com/unity/editor/release/v1/releases?limit={0}&offset={1}";
}
