using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Security.Principal;

namespace VirtualDesktopManager.Utils
{
    public static class IOManager
    {

        // Methods:

        #region path manipulations

        /// <summary>
        /// Finds formatting errors in path strings.
        /// </summary>
        /// <param name="path">Path string to test.</param>
        /// <param name="rootedPath">Indicates if the path must be rooted.</param>
        /// <returns>Indicates if the given string is formatted as a path.</returns>
        public static bool CheckPathFormatting(string path, bool rootedPath)
        {
            try
            {
                string fullPath = Path.GetFullPath(path);
                return fullPath == path && (rootedPath ? Path.IsPathRooted(path) : true);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if a file or folder exist at the specified path. Also checks so that the path is correctly formatted. The path must be rooted.
        /// </summary>
        /// <param name="path">Path to check.</param>
        /// <param name="folder">Indicates if the path is to a file or a folder.</param>
        /// <returns>Indicates if the specified path is valid.</returns>
        public static bool IsPathValid(string path, bool folder)
        {
            bool pathIsValid;
            try
            {
                pathIsValid = CheckPathFormatting(path, true) && (folder ? Directory.Exists(path) : File.Exists(path));
            }
            catch
            {
                pathIsValid = false;
            }

            return pathIsValid;
        }


        public class FolderBrowserDialogSettings
        {
            public bool UseDescription { get { return Description != null; } }
            public string Description { get; }

            public bool UseRootFolder { get; } = false;
            public Environment.SpecialFolder RootFolder { get; }

            public bool UseSelectedPath { get { return SelectedPath != null; } }
            public string SelectedPath { get; }

            public bool UseShowNewFolderButton { get; } = false;
            public bool ShowNewFolderButton { get; }


            public FolderBrowserDialogSettings(string description = null, string selectedPath = null)
            {
                Description = description;
                SelectedPath = selectedPath;
            }
            public FolderBrowserDialogSettings(Environment.SpecialFolder rootFolder, string description = null, string selectedPath = null)
            {
                RootFolder = rootFolder;
                UseRootFolder = true;

                Description = description;
                SelectedPath = selectedPath;
            }
            public FolderBrowserDialogSettings(bool showNewFolderButton, string description = null, string selectedPath = null)
            {
                ShowNewFolderButton = showNewFolderButton;
                UseShowNewFolderButton = true;

                Description = description;
                SelectedPath = selectedPath;
            }
            public FolderBrowserDialogSettings(Environment.SpecialFolder rootFolder, bool showNewFolderButton, string description = null, string selectedPath = null)
            {
                RootFolder = rootFolder;
                UseRootFolder = true;

                ShowNewFolderButton = showNewFolderButton;
                UseShowNewFolderButton = true;



                Description = description;
                SelectedPath = selectedPath;
            }


            public FolderBrowserDialog CreateFolderBrowser()
            {
                FolderBrowserDialog fbd = new FolderBrowserDialog();
                if (UseDescription) fbd.Description = Description;
                if (UseRootFolder) fbd.RootFolder = RootFolder;
                if (UseSelectedPath) fbd.SelectedPath = SelectedPath;
                if (UseShowNewFolderButton) fbd.ShowNewFolderButton = ShowNewFolderButton;

                return fbd;
            }

            public string PromptUser()
            {
                return PromptUserForFolderPath(this);
            }
        }

        /// <summary>
        /// Shows the user a window where they can select a folder.
        /// </summary>
        /// <param name="settings">Settings for dialog window. Set to null to use default values.</param>
        /// <returns>Path to folder selected by user.</returns>
        public static string PromptUserForFolderPath(FolderBrowserDialogSettings settings = null)
        {
            if (settings == null) settings = new FolderBrowserDialogSettings();
            FolderBrowserDialog fbd = settings.CreateFolderBrowser();
            fbd.ShowDialog();

            return fbd.SelectedPath + "\\";
        }


        /// <summary>
        /// Create a filter for OpenFileDialog objects such as the one used in the "PromptUserForFilePath()" function.
        /// </summary>
        /// <param name="filterName">Name of the filter (text that will be shown to the user).</param>
        /// <param name="filterExtensions">Extension that will be filtered. Formatted: "[Text].[Extension]". [Text] or [Extension] can be replace with '*' for any file.</param>
        /// <param name="includeExtensionInName">Include the filter extensions in the filter name.</param>
        /// <returns>The created filter.</returns>
        public static string CreateFileDialogFilter(string filterName, string filterExtensions, bool includeExtensionInName = false)
        {
            return CreateFileDialogFilter(filterName, new string[] { filterExtensions }, includeExtensionInName);
        }
        /// <summary>
        /// Create a filter for OpenFileDialog objects such as the one used in the "PromptUserForFilePath()" function.
        /// </summary>
        /// <param name="filterName">Name of the filter (text that will be shown to the user).</param>
        /// <param name="filterExtensions">Extensions that will be filtered. Formatted: "[Text].[Extension]". [Text] or [Extension] can be replace with '*' for any file.</param>
        /// <param name="includeExtensionInName">Include the filter extensions in the filter name.</param>
        /// <returns>The created filter.</returns>
        public static string CreateFileDialogFilter(string filterName, string[] filterExtensions, bool includeExtensionInName = false)
        {
            if (filterExtensions == null || filterExtensions.Length == 0)
            {
                return "";
            }

            string filter = filterExtensions[0];

            for (int iii = 1; iii < filterExtensions.Length; iii++)
            {
                filter += ";" + filterExtensions[iii];
            }

            return filterName + (includeExtensionInName ? " (" + filter + ")" : "") + "|" + filter;
        }

        /// <summary>
        /// Shows the user a window where they can select a file.
        /// </summary>
        /// <param name="filter">A string with filters to apply. Should be formatted: "[Filter name]|[Filter]|[Filter name]|[Filter]" and so on. 
        /// [Filter] contains a '.' with text before and after, that text can be replaced with a '*' to allow for any text. 
        /// For mutliple files write a ';' between filters (like: [Filter];[Filter];[Filter]) Send null to set to all files.
        /// 
        /// Can create filters with CreateFileDialogFilter() and then append filter to each other with TextManager.CombineStringCollection(filters, "|").
        /// </param>
        /// <param name="startingFilterIndex">Select which of the given filters should be used by default.</param>
        /// <param name="directoryToBrowseFrom">Used to indicated the directory the window start on. 
        /// Invalid path will lead to default directory being used. Send null to use default.</param>
        /// <returns>Path to file selcted by user. Will be empty if the user cancel.</returns>
        public static string PromptUserForFilePath(string filter = null, int startingFilterIndex = 0, string directoryToBrowseFrom = null)
        {
            // Create an instance of the open file dialog box.
            OpenFileDialog openFileDialog = new OpenFileDialog();

            // Set filter options and filter index.
            if (filter == null) filter = "All Files (*.*)|*.*";
            openFileDialog.Filter = filter;
            openFileDialog.FilterIndex = startingFilterIndex;

            if (directoryToBrowseFrom != null)
            {
                if (IsPathValid(directoryToBrowseFrom, true))
                {
                    openFileDialog.InitialDirectory = directoryToBrowseFrom;
                }
            }
            openFileDialog.Multiselect = false;

            // Call the ShowDialog method to show the dialog box.
            bool userClickedOK = DialogResult.OK == openFileDialog.ShowDialog();

            // Process input if the user clicked OK.
            if (userClickedOK == true)
            {
                return openFileDialog.FileName;
            }
            else return "";

        }

        /// <summary>
        /// Show the user a windows where they can select a save file.
        /// </summary>
        /// <param name="title">Title of the window.</param>
        /// <param name="fileName">Default name of the save file.</param>
        /// <param name="filter">Filters for the file explorer. Follows the same rules as the "PromptUserForFilePath()" function.
        /// Can create filters with CreateFileDialogFilter() and then append filter to each other with TextManager.CombineStringCollection(filters, "|").
        /// </param>
        /// <param name="startingFilterIndex">Filter to be used when window is opened. Send null to set to all files.</param>
        /// <param name="directoryToBrowseFrom">Used to indicated the directory the window start on. Send null to use default.</param>
        /// <param name="warnIfOverwrite">Show a prompt if the user is trying to overwrite another file.</param>
        /// <returns>Path to save file. Empty if user canceled.</returns>
        public static string PromtUserForSaveFile(string title = null, string fileName = "file", string filter = null, int startingFilterIndex = 0, string directoryToBrowseFrom = null, bool warnIfOverwrite = true)
        {
            SaveFileDialog sfd = new SaveFileDialog();

            if (title != null) sfd.Title = title;
            sfd.FileName = fileName;
            if (filter == null) filter = "All Files|*.*";
            sfd.Filter = filter;
            sfd.OverwritePrompt = warnIfOverwrite;
            if (directoryToBrowseFrom != null) sfd.InitialDirectory = directoryToBrowseFrom;
            sfd.FilterIndex = startingFilterIndex;

            bool userPressedOK = sfd.ShowDialog() == DialogResult.OK;

            string savePath = "";
            if (userPressedOK) savePath = sfd.FileName;

            return savePath;
        }



        /// <summary>
        /// Searches through a folder and finds all files with specific extensions. (This is a recursive function)
        /// </summary>
        /// <param name="folderPath">Path of the directory to scan.</param>
        /// <param name="includeSubdirectories">Specifies whether files in subdirectories should also be added.</param>
        /// <param name="acceptableExtensions">Extensions (with or without a dot before) on the files that are to be added to the list. Set to null to accept all.</param>
        /// <returns>A list that all accepted files' paths will be added to.</returns>
        public static List<string> GetFilesInFolder(string folderPath, bool includeSubdirectories, string[] acceptableExtensions)
        {
            List<string> files = new List<string>();
            string[] filesInDirectory = Directory.GetFiles(folderPath);

            if (acceptableExtensions == null)
            {
                files.AddRange(filesInDirectory);                                   // No specified extension. Assuming all are acceptable and adding all files paths to list.
            }
            else
            {
                foreach (string fileInDirectory in filesInDirectory)                // Check all files to see if they are acceptable
                {
                    string extension = Path.GetExtension(fileInDirectory);          // Get the files extension.

                    foreach (string acceptableExtension in acceptableExtensions)    // Check it against all the acceptable extensions.
                    {
                        if (("." + acceptableExtension).Contains(extension))        // If the extension (with or without a '.') is the same as the acceptable extension (the acceptable extension have at least on dot in the beginning so if the extension has one or not doesn't matter it will still be a substring of the acceptable extension string.)
                        {
                            files.Add(fileInDirectory);                             // Then add the file path to the list.
                            break;                                                  // Then stop checking if the extension is acceptable.
                        }
                    }
                }
            }


            if (includeSubdirectories)
            {
                string[] subdirectories = Directory.GetDirectories(folderPath);

                foreach (string subdirectory in subdirectories)
                {
                    files.AddRange(GetFilesInFolder(subdirectory, true, acceptableExtensions));
                }
            }


            return files;
        }



        /// <summary>
        /// Searches for an unused path, a path that no file currently uses.
        /// </summary>
        /// <param name="path">Original path, search will start with this path and change it to find an unused one.</param>
        /// <param name="beforeCounter">Text before counter value.</param>
        /// <param name="afterCounter">Text after counter value.</param>
        /// <param name="counterStartValue">Value to start counter at.</param>
        /// <param name="afterExtension">Determines if counter text is placed after or before extension.</param>
        /// <param name="skipOriginalPath">Determines if the path supplied as argument should be skipped or if is an acceptable path.</param>
        /// <returns>An unused path found in the same directory as the original path.</returns>
        public static string FindFreeFilePath(string path, int counterStartValue = 2, string beforeCounter = " (", string afterCounter = ")", bool afterExtension = false, bool skipOriginalPath = false)
        {
            return FindFreeFilePath(path, ref counterStartValue, beforeCounter, afterCounter, afterExtension, skipOriginalPath);
        }

        /// <summary>
        /// Searches for an unused path, a path that no file currently uses.
        /// </summary>
        /// <param name="path">Original path, search will start with this path and change it to find an unused one.</param>
        /// <param name="beforeCounter">Text before counter value.</param>
        /// <param name="afterCounter">Text after counter value.</param>
        /// <param name="counterStartValue">Value to start counter at. Will be changed to reflect the value used by the path. (will be less then start value if original path was used)</param>
        /// <param name="afterExtension">Determines if counter text is placed after or before extension.</param>
        /// <param name="skipOriginalPath">Determines if the path supplied as argument should be skipped or if is an acceptable path.</param>
        /// <returns>An unused path found in the same directory as the original path.</returns>
        public static string FindFreeFilePath(string path, ref int counterStartValue, string beforeCounter = " (", string afterCounter = ")", bool afterExtension = false, bool skipOriginalPath = false)
        {
            return findFreePath(path, ref counterStartValue, beforeCounter, afterCounter, afterExtension, skipOriginalPath, false);
        }

        /// <summary>
        /// Searches for an unused path, a path that no directory currently uses.
        /// </summary>
        /// <param name="path">Original path, search will start with this path and change it to find an unused one.</param>
        /// <param name="beforeCounter">Text before counter value.</param>
        /// <param name="afterCounter">Text after counter value.</param>
        /// <param name="counterStartValue">Value to start counter at.</param>
        /// <param name="skipOriginalPath">Determines if the path supplied as argument should be skipped or if is an acceptable path.</param>
        /// <returns>An unused directory path found in the same directory as the original path.</returns>
        public static string FindFreeDirectoryPath(string path, int counterStartValue = 2, string beforeCounter = " (", string afterCounter = ")", bool skipOriginalPath = false)
        {
            return FindFreeDirectoryPath(path, ref counterStartValue, beforeCounter, afterCounter, skipOriginalPath);
        }

        /// <summary>
        /// Searches for an unused path, a path that no directory currently uses.
        /// </summary>
        /// <param name="path">Original path, search will start with this path and change it to find an unused one.</param>
        /// <param name="beforeCounter">Text before counter value.</param>
        /// <param name="afterCounter">Text after counter value.</param>
        /// <param name="counterStartValue">Value to start counter at. Will be changed to reflect the value used by the path. (will be less then start value if original path was used)</param>
        /// <param name="skipOriginalPath">Determines if the path supplied as argument should be skipped or if is an acceptable path.</param>
        /// <returns>An unused directory path found in the same directory as the original path.</returns>
        public static string FindFreeDirectoryPath(string path, ref int counterStartValue, string beforeCounter = " (", string afterCounter = ")", bool skipOriginalPath = false)
        {
            return findFreePath(path, ref counterStartValue, beforeCounter, afterCounter, true, skipOriginalPath, true);
        }

        private static string findFreePath(string path, ref int counterStartValue, string beforeCounter, string afterCounter, bool afterExtension, bool skipOriginalPath, bool isFolder)
        {
            path = Path.GetFullPath(path);

            string modifiedPath = path;
            bool nextPath = skipOriginalPath;
            do
            {
                if (nextPath)
                {
                    string text = beforeCounter + counterStartValue.ToString() + afterCounter;

                    if (isFolder)
                    {
                        modifiedPath = path.TrimEnd(new char[] { '\\' }) + text + "\\";
                    }
                    else
                    {
                        if (afterExtension)
                        {
                            modifiedPath = path + text;
                        }
                        else modifiedPath = Path.ChangeExtension(path, null) + text + Path.GetExtension(path);    
                    }

                    counterStartValue++;
                }
                nextPath = true;
            } while (isFolder ? Directory.Exists(modifiedPath) : File.Exists(modifiedPath));

            counterStartValue--;
            return modifiedPath;
        }


        /// <summary>
        /// Finds the executables directory. (Includes ending '\' sign.)
        /// </summary>
        /// <returns>Path to the executable for this program.</returns>
        public static string ExecutablesDirectory
        {
            get { return AppDomain.CurrentDomain.BaseDirectory; }
        }

        /// <summary>
        /// Gives the user name of the person running the program.
        /// </summary>
        public static string UserName
        {
            get { return Environment.UserName; }
        }

        #endregion




        #region input/output tasks

        /// <summary>
        /// Determines if a file can be opened without causing an exception.
        /// </summary>
        /// <param name="filePath">Path to file that is to be tested.</param>
        /// <param name="accessType">What type of access to the file is required.</param>
        /// <returns>A value indicating if the file can be accessed.</returns>
        public static bool FileAccessible(string filePath, FileAccess accessType)
        {
            try
            {
                File.Open(filePath, FileMode.Open, accessType).Dispose();
                return true;
            }
            catch (IOException)
            {
                return false;
            }
        }


        /// <summary>
        /// Read data as byte array from file.
        /// </summary>
        /// <param name="path">Read data from file at this path. Send null to abort reading without warning.</param>
        /// <param name="warnIfNotRead">Show messagebox with warning if file can't be read.</param>
        /// <param name="readData">Byte array with the read data.</param>
        /// <returns>Indicates if the data was read successfully.</returns>
        public static bool ReadFromFile(string path, bool warnIfNotRead, out byte[] readData)
        {
            List<string> stringList = null;
            byte[] byteArray = new byte[0];
            bool readSuccessful = ReadFromFile(path, warnIfNotRead, ref stringList, ref byteArray);

            readData = byteArray;
            return readSuccessful;
        }

        /// <summary>
        /// Reads text from a file.
        /// </summary>
        /// <param name="path">Path to file that is to be read.</param>
        /// <param name="warnIfNotRead">Indicates if a warning message should be shown if the path can't be read.</param>
        /// <param name="readData">List with the text from the file.</param>
        /// <returns>Indicates if the file could be read.</returns>
        public static bool ReadFromFile(string path, bool warnIfNotRead, out List<string> readData)
        {
            List<string> stringList = new List<string>();
            byte[] byteArray = null;
            bool readSuccessful = ReadFromFile(path, warnIfNotRead, ref stringList, ref byteArray);

            readData = stringList;
            return readSuccessful;
        }

        private static bool ReadFromFile(string path, bool warnIfNotRead, ref List<string> readDataAsStringList, ref byte[] readDataAsByteArray)
        {
            if (path == null) return false;

            if (File.Exists(path))
            {
                if (readDataAsStringList != null)
                {
                    using (StreamReader sr = new StreamReader(path, Encoding.UTF8))
                    {
                        string readLine = "";
                        while ((readLine = sr.ReadLine()) != null)                      // So long as there is text copy it into the readLine string.
                        {
                            readDataAsStringList.Add(readLine);
                        }
                    }
                }
                if (readDataAsByteArray != null) readDataAsByteArray = File.ReadAllBytes(path);

                return true;
            }
            else if (warnIfNotRead)
            {
                MessageBox.Show("File doesn't exist or cannot be accessed!", "Error opening file!", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button2, MessageBoxOptions.DefaultDesktopOnly, false);
            }

            return false;
        }


        /// <summary>
        /// Wirte text to a file.
        /// </summary>
        /// <param name="path">Path to create text file at.</param>
        /// <param name="output">String with text to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new text file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, string output, bool overwrite, bool changeExistingFilesFilename, bool warnIfNotWrite)
        {
            List<string> list = new List<string>();
            list.Add(output);
            return WriteToFile(path, list, overwrite, changeExistingFilesFilename, warnIfNotWrite);
        }

        /// <summary>
        /// Wirte text to a file and get notified of the location it was written to.
        /// </summary>
        /// <param name="path">Path to create text file at.</param>
        /// <param name="output">String with text to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new text file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="writenFilesPath">Path to the writen file. Might not be the wanted path if that was already in use and the function wasn't allowed to change that files name.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, string output, bool overwrite, bool changeExistingFilesFilename, out string writenFilesPath, bool warnIfNotWrite)
        {
            List<string> list = new List<string>();
            list.Add(output);
            return WriteToFile(path, list, overwrite, changeExistingFilesFilename, out writenFilesPath, warnIfNotWrite);
        }

        /// <summary>
        /// Wirte text to a file.
        /// </summary>
        /// <param name="path">Path to create text file at.</param>
        /// <param name="output">List with text to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new text file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, List<string> output, bool overwrite, bool changeExistingFilesFilename, bool warnIfNotWrite)
        {
            if (output == null) output = new List<string>();
            string writenFilesPath;
            return WriteToFile(path, output, null, overwrite, changeExistingFilesFilename, out writenFilesPath, warnIfNotWrite);
        }

        /// <summary>
        /// Wirte text to a file and get notified of the location it was written to.
        /// </summary>
        /// <param name="path">Path to create text file at.</param>
        /// <param name="output">List with text to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new text file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="writenFilesPath">Path to the writen file. Might not be the wanted path if that was already in use and the function wasn't allowed to change that files name.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, List<string> output, bool overwrite, bool changeExistingFilesFilename, out string writenFilesPath, bool warnIfNotWrite)
        {
            if (output == null) output = new List<string>();
            return WriteToFile(path, output, null, overwrite, changeExistingFilesFilename, out writenFilesPath, warnIfNotWrite);
        }

        /// <summary>
        /// Wirte byte array to a file.
        /// </summary>
        /// <param name="path">Path to create file at.</param>
        /// <param name="output">Byte array to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, byte[] output, bool overwrite, bool changeExistingFilesFilename, bool warnIfNotWrite)
        {
            if (output == null) output = new byte[0];
            string writenFilesPath;
            return WriteToFile(path, null, output, overwrite, changeExistingFilesFilename, out writenFilesPath, warnIfNotWrite);
        }

        /// <summary>
        /// Wirte byte array to a file and get notified of the location it was written to.
        /// </summary>
        /// <param name="path">Path to create file at.</param>
        /// <param name="output">Byte array to write.</param>
        /// <param name="overwrite">Indicates if any already existing files at the specified path should be deleted.</param>
        /// <param name="changeExistingFilesFilename">Indicates if existing file's name should be changed so that the new file can be placed there instead. Otherwise the new file's filename will be changed to an unused one.</param>
        /// <param name="writenFilesPath">Path to the writen file. Might not be the wanted path if that was already in use and the function wasn't allowed to change that files name.</param>
        /// <param name="warnIfNotWrite">Indicates if a warning should be shown if the file couldn't be writen.</param>
        /// <returns>Indicates if the file was successfully written.</returns>
        public static bool WriteToFile(string path, byte[] output, bool overwrite, bool changeExistingFilesFilename, out string writenFilesPath, bool warnIfNotWrite)
        {
            if (output == null) output = new byte[0];
            return WriteToFile(path, null, output, overwrite, changeExistingFilesFilename, out writenFilesPath, warnIfNotWrite);
        }

        private static bool WriteToFile(string path, List<string> outputAsStringList, byte[] outputAsByteArray, bool overwrite, bool changeExistingFilesFilename, out string writenFilesPath, bool warnIfNotWrite)
        {
            if (overwrite) changeExistingFilesFilename = true;  // want to keep it until new file is done.

            writenFilesPath = ""; // Not writen!

            if (path == null) return false;


            string oldFile = "";

            if (!changeExistingFilesFilename)
            {
                // change path to first available:
                path = FindFreeFilePath(path, 2, " (", ")", false, false);
            }
            else
            {
                // Change old files names:                    
                oldFile = FindFreeFilePath(path, 1, ".old", "", false, false);
                if (Path.GetFullPath(oldFile) == Path.GetFullPath(path)) oldFile = "";
                else File.Move(path, oldFile); // rename old file so that we can use its path for our new file.
            }


            // Create a file and start writing to it.
            if (!File.Exists(path))
            {
                // Create a file to write to:

                if (outputAsStringList != null)
                {
                    using (StreamWriter sw = File.CreateText(path))
                    {

                        for (int iii = 0; iii < outputAsStringList.Count; iii++)                       // Select each individual line to be outputed
                        {
                            sw.WriteLine(outputAsStringList.ElementAt(iii));                           // Write that line to a text file
                        }

                    }
                }
                else if (outputAsByteArray != null)
                {
                    using (FileStream fs = File.Create(path))
                    {
                        fs.Write(outputAsByteArray, 0, outputAsByteArray.Length);
                    }
                }


                // Delete old file that were kept temporary while we wrote the new one.
                if (overwrite && File.Exists(oldFile))
                {
                    File.Delete(oldFile);
                }
                writenFilesPath = path;
                return true;
            }
            else if (warnIfNotWrite)
            {
                MessageBox.Show("Could not access file at location!" + "\n" + "Could not save!", "Error writing to disk", MessageBoxButtons.OK, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1, MessageBoxOptions.DefaultDesktopOnly, false);
            }


            return false;
        }



        /// <summary>
        /// Draw the whole screen to a bitmap.
        /// </summary>
        /// <returns>Bitmap with the whole screen drawn to it.</returns>
        public static Bitmap DrawFromScreen()
        {
            Size bmpSize = Screen.PrimaryScreen.Bounds.Size;
            Bitmap bmp = new Bitmap(bmpSize.Width, bmpSize.Height);
            Graphics g = Graphics.FromImage(bmp);
            DrawFromScreen(g, new Rectangle(0, 0, bmpSize.Width, bmpSize.Height), new Point(0, 0));
            g.Dispose();
            return bmp;
        }

        /// <summary>
        /// Draw an area of the screen with a graphics object.
        /// </summary>
        /// <param name="graphicsObject">Graphics object to draw with. Will determine where sceen data is drawn to.</param>
        /// <param name="screenAreaToDraw">The area of the screen to draw given in pixels.</param>
        /// <param name="pointToStartDrawingTo">Where the graphics object should start drawing the screen data to.</param>
        public static void DrawFromScreen(Graphics graphicsObject, Rectangle screenAreaToDraw, Point pointToStartDrawingTo)
        {
            graphicsObject.CopyFromScreen(screenAreaToDraw.Left, screenAreaToDraw.Top, pointToStartDrawingTo.X, pointToStartDrawingTo.Y, screenAreaToDraw.Size, CopyPixelOperation.SourceCopy);
        }

        #endregion



        /// <summary>
        /// Check if current program has administrator access.
        /// </summary>
        /// <returns>True if program has elevated premissions otherwise false.</returns>
        public static bool CheckIfElevated()
        {
            return Permissions.PermissionsCode.CheckIfElevated();
        }
    }
}
