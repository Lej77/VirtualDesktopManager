using System;
using System.Collections.Generic;
using System.Text;

using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Security.Principal;
using System.Threading;
using System.ComponentModel;

namespace VirtualDesktopManager.Permissions
{
    /// <summary>
    ///  Code related to getting and changing a programs admin rights / elevated status.
    /// </summary>
    public class PermissionsCode
    {
        /// <summary>
        /// Path to executable.
        /// </summary>
        public static string ExecutablePath => Application.ExecutablePath;
        /// <summary>
        /// Path to directory that contains executable.
        /// </summary>
        public static string ExecutableDirectory => Path.GetDirectoryName(ExecutablePath);
        /// <summary>
        /// Executable's file name.
        /// </summary>
        public static string ExecutableName => Path.GetFileNameWithoutExtension(ExecutablePath);
        /// <summary>
        /// Command line args passed to the program.
        /// </summary>
        public static string CommandLineArgs => Environment.CommandLine.Remove(0, (Environment.GetCommandLineArgs()[0].Length + (Environment.CommandLine.StartsWith("\"") ? 2 : 0)));


        #region Temp Help Files

        private static string GetTaskSchedulerXMLImportText(string programPath, string arguments, string workingDirectory, bool runAsAdmin = false)
        {
            string xmlText = EmbeddedResourceCode.GetStringFromResource("Task_Scheduler_Import.txt", true)
                .Replace("[Insert Program Path Here]", XMLStringHelper.FromString(programPath))
                .Replace("[Insert Arguments Here]", XMLStringHelper.FromString(arguments))
                .Replace("[Insert Working Directory Here]", XMLStringHelper.FromString(workingDirectory));

            if (runAsAdmin)
                xmlText = xmlText.Replace("LeastPrivilege", "HighestAvailable");

            return xmlText;
        }

        /// <summary>
        /// Create a VBScript that starts a program.
        /// </summary>
        /// <param name="programPath">Path to the program that should be started.</param>
        /// <param name="arguments">Arguments to pass to the program. Null to pass on the arguments that were passed to the script.</param>
        /// <param name="workingDirectory">Working directory to use when starting the program. Null to not change working directory.</param>
        /// <param name="waitOnReturn">Determines if the script is kept running until the program exits.</param>
        /// <param name="deleteScriptFile">Delete script file when it is run.</param>
        /// <param name="runAsAdmin">Run the program with admin rights. If TRUE the script will not wait on the started program's exit. Also if TRUE the working directory if the started program will be reset to windows default (C:\Windows\System32\).</param>
        /// <returns>A string with the VBScript code that executes a program.</returns>
        private static string GetVBScript(string programPath, string arguments = null, string workingDirectory = null, bool waitOnReturn = false, bool deleteScriptFile = false, bool runAsAdmin = false)
        {
            string script = "";


            if (workingDirectory != null)
            {
                script += "CreateObject(\"WScript.Shell\").CurrentDirectory = ";

                if (workingDirectory == "") // Set script files directory as working directory.
                    script += "CreateObject(\"Scripting.FileSystemObject\").GetParentFolderName(WScript.ScriptFullName)";
                else
                    script += "\"" + workingDirectory + "\"";

                script += Environment.NewLine;
            }


            if (arguments == null)
            {
                script += String.Join(Environment.NewLine, new string[]
                {
                    "",
                    "dim Args",
                    "For Each strArg in Wscript.Arguments",
                    "  dim arg",
                    "  arg = strArg",
                    "  If InStr(strArg, \" \") > 0 Then",
                    "    ' arg contains a space",
                    "    arg = chr(34) & arg & chr(34)",
                    "  End If",
                    "  If Args = \"\" Then",
                    "    Args = arg",
                    "  Else",
                    "    Args = Args & \" \" & arg",
                    "  End If",
                    "Next",
                    "",
                    "",
                });
            }


            if (!String.IsNullOrEmpty(programPath))
            {
                string programFileNameOrPath = programPath; // File Name if run as admin
                string programDirectory = "";

                if (runAsAdmin)
                {
                    int lastSeparator = programPath.LastIndexOf("\\");
                    if (lastSeparator >= 0)
                    {
                        programFileNameOrPath = programPath.Remove(0, lastSeparator + 1);
                        programDirectory = programPath.Substring(0, lastSeparator);
                    }
                }



                if (runAsAdmin)
                    script += "CreateObject(\"Shell.Application\").ShellExecute";
                else
                    script += "CreateObject(\"Wscript.Shell\").Run";

                script += " \"\"\"" + programFileNameOrPath + "\"\"\"";

                if (runAsAdmin)
                    script += ", \"\""; // Run As Admin Script requires arguments to be a different parameter

                if (arguments == null)
                    script += " & Args";
                else if (arguments != "")
                    script += " & \"" + arguments.Replace("\"", "\" & chr(34) & \"") + "\"";

                if (runAsAdmin)
                {
                    script += ", \"\"\"" + programDirectory + "\"\"\"";
                    script += ", \"runas\"";
                }

                script += ", 0";    // intWindowStyle: Int value indicating the appearance of the program's window. Not all programs make use of this. (0: hide the window)

                if (!runAsAdmin)    // RunAsAdmin script does not have this parameter
                    script += ", " + (waitOnReturn ? "True" : "False");
            }


            if (deleteScriptFile)
                script += Environment.NewLine + "CreateObject(\"Scripting.FileSystemObject\").DeleteFile Wscript.ScriptFullName" + Environment.NewLine;


            return script;
        }

        /// <summary>
        /// Writes some text to a file using the default text encoding.
        /// </summary>
        /// <param name="fileName">Wanted file name for temp file. Include file extension. Will be changed to a new unused name.</param>
        /// <param name="text">Text to write to temp file.</param>
        /// <returns>Path to the new temporary file.</returns>
        private static string WriteTextToTempFile(string fileName, string text)
        {
            if (text == null)
                return null;

            if (fileName == null)
                fileName = "";

            string extension = "";
            extension = Path.GetExtension(fileName);
            fileName = Path.ChangeExtension(fileName, null);

            string tempDirectory = Path.GetTempPath();
            string tempPath = "";
            do
            {
                tempPath = tempDirectory + fileName + (fileName == "" ? "" : " - ") + "Temp [" + Guid.NewGuid().ToString() + "]" + extension;
            } while (File.Exists(tempPath));

            // Write data:
            File.WriteAllText(tempPath, text, System.Text.Encoding.Default);
            return tempPath;
        }

        #endregion Temp Help Files

        #region Admin Rights

        /// <summary>
        /// Check if the current program has administrator access.
        /// </summary>
        /// <returns>True if program has elevated premissions otherwise false.</returns>
        public static bool CheckIfElevated()
        {
            using (WindowsIdentity identity = WindowsIdentity.GetCurrent())
            {
                WindowsPrincipal principal = new WindowsPrincipal(identity);
                return principal.IsInRole(WindowsBuiltInRole.Administrator);
            }
        }

        /// <summary>
        /// Restart the current executalbe without elevated (admin) permissions.
        /// </summary>
        public static void RestartExecutableWithoutElevatedStatus()
        {
            StartExecutableWithoutElevatedStatus(ExecutablePath, CommandLineArgs, ExecutableDirectory);
        }
        /// <summary>
        /// Start an executalbe without elevated (admin) permissions.
        /// </summary>
        public static void StartExecutableWithoutElevatedStatus(string filePath, string arguments, string workingDirectory)
        {
            string tempImportFilePath = null;
            try
            {
                string name = ExecutableName + " - Drop admin";

                tempImportFilePath = WriteTextToTempFile(name + ".xml",
                    GetTaskSchedulerXMLImportText(filePath, arguments, workingDirectory, false));

                string taskName = name + " - Temp[" + Guid.NewGuid().ToString() + "]";

                Process.Start(new ProcessStartInfo()
                {
                    FileName = "schtasks",
                    Arguments = "/create /tn \"" + taskName + "\" /xml \"" + tempImportFilePath + "\"",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }).WaitForExit();
                Process.Start(new ProcessStartInfo()
                {
                    FileName = "schtasks",
                    Arguments = "/delete /tn \"" + taskName + "\" /f",
                    UseShellExecute = true,
                    WindowStyle = ProcessWindowStyle.Hidden,
                }).WaitForExit();
            }
            finally
            {
                if (tempImportFilePath != null)
                    File.Delete(tempImportFilePath);
            }
        }

        /// <summary>
        /// Restart the program with admin rights.
        /// </summary>
        /// <param name="useTempVBScript">Use a temporary VBScript file to start the program.</param>
        /// <param name="tempFileName">Prefix for the name of the temporary VBScript file.</param>
        /// <returns>Will be false if the user cancles the prompt; otherwise true.</returns>
        public static bool RestartExecutableWithElevatedStatus(bool useTempVBScript = false, string tempFileName = null)
        {
            return StartExecutableWithElevatedStatus(ExecutablePath, CommandLineArgs, ExecutableDirectory, useTempVBScript, tempFileName);
        }
        /// <summary>
        /// Start an executable with admin rights.
        /// </summary>
        /// <param name="filePath">The file path to the executable that should be started.</param>
        /// <param name="arguments">The arguments to use when starting the program.</param>
        /// <param name="workingDirectory">The working directory to use for the started program.</param>
        /// <param name="useTempVBScript">Use a temporary VBScript file to start the program.</param>
        /// <param name="tempFileName">Prefix for the name of the temporary VBScript file.</param>
        /// <returns>Will be false if the user cancles the prompt; otherwise true.</returns>
        public static bool StartExecutableWithElevatedStatus(string filePath, string arguments, string workingDirectory, bool useTempVBScript = false, string tempFileName = null)
        {
            string tempPath = null;
            try
            {
                try
                {
                    if (useTempVBScript)
                    {
                        string script = GetVBScript(filePath, arguments, workingDirectory, deleteScriptFile: true, runAsAdmin: true);
                        string filePrefix = "";
                        if (tempFileName != null)
                        {
                            filePrefix += tempFileName + " - ";
                        }
                        filePrefix += "Request Admin Rights";
                        try
                        {
                            tempPath = WriteTextToTempFile(filePrefix + " - " + ExecutableName + ".vbs", script);
                        }
                        catch (PathTooLongException)
                        {
                            try
                            {
                                if (tempPath != null && File.Exists(tempPath))
                                {
                                    File.Delete(tempPath);
                                    tempPath = null;
                                }
                            }
                            catch { }

                            tempPath = WriteTextToTempFile(filePrefix + ".vbs", script);
                        }


                        filePath = "wscript.exe";
                        arguments = "\"" + tempPath + "\"";
                    }
                    Process.Start(new ProcessStartInfo
                    {
                        // If this is ever false then the "Verb" does nothing and the program is started without admin rights:
                        UseShellExecute = true,
                        Verb = "RunAs",
                        FileName = filePath,
                        Arguments = arguments,
                        WorkingDirectory = workingDirectory,
                    });
                }
                catch
                {
                    if (tempPath != null && File.Exists(tempPath))
                    {
                        File.Delete(tempPath);
                        tempPath = null;
                    }
                    throw;
                }
            }
            catch (Win32Exception)
            {
                // This will be thrown if the user cancels the prompt
                return false;
            }
            return true;
        }

        #endregion Admin Rights
    }
}
