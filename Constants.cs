namespace unicli;

public static class Constants
{
    public const string ApplicationName = "unicli";
    public const string Version = "v1.0";

    public const string HelpText = """
                                   Verbs: 
                                        help, version, editors, projects, projectinfo, 
                                        open, add, config, getcertfingerprint
                                        
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
                                         
                                        unicli open <project_name> [extra args]
                                            Open project.
                                         
                                        unicli open <project_name> <editor_version> [extra args]
                                            Open project with specific Unity editor.

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
}