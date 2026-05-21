namespace unicli;

public static class Constants
{
    public const string ApplicationName = "unicli2";
    public const string Version = "v0.0.1";

    public const string HelpText = """
                                   Usage:
                                     unicli help 
                                         Displays this help text.
                                         
                                     unicli version
                                         Displays the current version.
                                         
                                     unicli checkupdate
                                         Checks for updates.
                                         
                                     unicli editors
                                         List installed Unity editor versions.
                                   
                                     unicli projects
                                         List Unity projects.
                                         
                                     unicli open <project_name> [extra args]
                                         Open project.
                                         
                                     unicli open <project_name> <editor-version> [extra args]
                                         Open project with specific Unity editor.
                                   
                                     unicli add <folder>
                                         Add a project scan directory or project.
                                   
                                     unicli config global
                                         Edit global configuration.
                                   
                                     unicli config <editor-version>
                                         Edit editor-specific configuration.
                                   
                                   Examples:
                                     unicli editors
                                     unicli projects
                                     unicli open MyGame 6000.3.6f1
                                     unicli open MyGame
                                     unicli add ~/Games/Unity
                                     unicli add ~/Downloads/TestProject 
                                   """;
}