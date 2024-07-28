using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System.Drawing;
using WindowsDesktop;
using VirtualDesktopManager.Extensions;
using System.IO;
using System.Threading;
using System.ComponentModel;
using System.Threading.Channels;
using VirtualDesktopServer;

namespace VirtualDesktopManager
{
    public class VirtualDesktopApplicationContext : ApplicationContext
    {
        #region Classes

        [Serializable]
        public class SettingsData
        {
            public bool SmoothDesktopSwitching = false;
            public bool StartWithAdminRights = false;
            public bool PreventFlashingWindows = false;

            public Rectangle ConfigurationWindowLocation = new Rectangle(-1, -1, -1, -1);
            public bool ConfigurationWindowMaximized = false;

            public Filter.SaveFile ActiveFilters = null;
        }

        private class CommandLine
        {
            public bool silent = false;
            public Filter[] loadedData = null;
            public bool help = false;
            public bool asServer = false;
            public bool writeJavaScriptClient = false;
            public bool writeTypeScriptClientDefs = false;
            public string outputPath = null;

            public string OutputPathWithPreferredExtension(string fileExtension)
            {
                if (String.IsNullOrWhiteSpace(outputPath)) return outputPath;
                if (String.IsNullOrEmpty(Path.GetExtension(outputPath)))
                    return outputPath + (fileExtension.StartsWith(".") ? "" : ".") + fileExtension;
                else
                    return outputPath;
            }
        }

        #endregion Classes


        #region Member Variables

        private readonly object locker = new object();


        #region Settings

        private string savePath = Utils.IOManager.ExecutablesDirectory + "settings.txt";

        private SettingsData data = new SettingsData();
        private System.Windows.Forms.Timer saveTimer = new System.Windows.Forms.Timer()
        {
            Interval = 300,
        };
        private bool autoSavingEnabled = true;

        #endregion Settings


        #region NotifyIcon

        private NotifyIcon notificationIcon = new NotifyIcon()
        {
            ContextMenuStrip = new ContextMenuStrip(),
            Text = "Virtual Desktop Manager",
            Icon = Properties.Resources.triangleEmpty,
        };
        private bool useOpenCloseDesktopContextMenuItems = true;
        private bool useSmoothDesktopSwitchToggleContextMenuItem = true;
        private Action smoothDesktopToggleContextMenuItemNameSetter = null;
        private bool useStopFlashingWindowsContextMenuItem = true;


        private System.Windows.Forms.Timer cursorTrackerTimer = new System.Windows.Forms.Timer()
        {
            Interval = 1000 / 10,
        };
        private Point lastTrackedMousePos = new Point(-1 - 1);

        #endregion NotifyIcon


        private ConfigureForm configForm = null;
        // TODO: this type doesn't use the IVirtualDesktop implementation to gather its data.
        private WindowInfo.Holder windowsInfoHolder = new WindowInfo.Holder(true);
        private List<Filter> filters = new List<Filter>();

        /// <summary>
        /// An interface for overriding the default virtual desktop behavior. If this is null then the default virtual desktop code will be used.
        /// 
        /// Note: ensure this is using background threads to complete requests or there might be deadlocks.
        /// </summary>
        private readonly VirtualDesktopServer.IVirtualDesktop virtualDesktopImplementation = null;
        private readonly CancellationTokenSource cancelVirtualDesktopImplementationEvents = new CancellationTokenSource();

        // private readonly AsyncOperation syncObject = AsyncOperationManager.CreateOperation(null);    // Can be canceled / disposed.
        private readonly SynchronizationContext syncObject = SynchronizationContext.Current ?? new SynchronizationContext();

        #endregion Member Variables


        #region Constructors

        public VirtualDesktopApplicationContext()
        {
            var cliInfo = CheckFlags();
            bool exit = true;
            try
            {
                if (cliInfo.help)
                {
                    MessageBox.Show(
                        Utils.TextManager.CombineStringCollection(new[] {
                        "\"--help\", \"-h\" or \"/?\": open this help window and then exit immediately after the help window is closed." ,
                        "" ,
                        "\"--silent\" or \"-s\": run the program in silent mode (don't show taskbar icon). This will apply the loaded filters and then immediately exit. " +
                        "If a \"--filter\" flag is also specified and that specified file is loaded correctly then the default settings won't be loaded at all." ,
                        "" ,
                        "\"--filter\": the argument after this flag is a path to an exported filter. If the file is successfully loaded then auto saving of program settings will be disabled." ,
                        "",
                        "\"--as-server\": run the program as a virtual destkop server (won't show taskbar icon or run start the normal program.). It will attempt to communicate via stdin and stdout.",
                        "",
                        "\"--write-javascript-client\": writes code for a javascript client for the virtual destkop server to stdout.",
                        "",
                        "\"--write-javascript-client-types\": writes the contents of a typescript definition file for the javascript client to stdout and then exits.",
                        "",
                        "\"--output-path\": the argument after this flag is a file path. If the write javascript client flag is specified then this will redirect the output to a file instead of stdout.",
                        "",
                        "Note: this is a GUI program and therefore stdout and stdin can't be interacted with from a terminal, instead you need to use another program to pipe the data. For writing client " +
                        "files you can use the \"--output-path\" flag as a simple workaround.",
                        "",
                        }),
                        "VirtualDesktop - Command Line Options Help",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information
                    );
                }
                else if (cliInfo.asServer)
                {
                    var impl = new VirtualDesktopViaGeneralLibrary(new VirtualDesktopViaGeneralLibrary.CallbackLogger(message => throw new NotImplementedException()));
                    var server = new VirtualDesktopServer.Server(Console.OpenStandardInput(), Console.OpenStandardOutput(), impl);

                    bool showingError = false;

                    void handleError(Exception e)
                    {
                        syncObject.Post(s =>
                        {
                            if (showingError) return;
                            try
                            {
                                if (e != null && !(e is OperationCanceledException))
                                {
                                    string errorMessage = "Server crashed:\n" + e.ToString();
                                    showingError = true;
                                    using (var stderr = Console.OpenStandardError())
                                    {
                                        var buffer = Encoding.UTF8.GetBytes(errorMessage);
                                        stderr.Write(buffer, 0, buffer.Length);
                                    }
#if DEBUG
                                    MessageBox.Show(errorMessage, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
#endif
                                }
#if DEBUG
                                else
                                {
                                    MessageBox.Show("Exiting server");
                                }
#endif
                            }
                            finally
                            {
                                if (showingError)
                                {
                                    Environment.ExitCode = 1;
                                    Environment.Exit(1);
                                }
                                else
                                {
                                    Environment.Exit(0);
                                }
                                Exit();
                            }
                        }, null);
                    }
                    server.OnInputClosed += (sender, e) => handleError(e);
                    server.OnOutputClosed += (sender, e) => handleError(e);

                    // These errors are usually sent to the client as messages, so dont exit instead let it handle them:
                    // server.OnServerError += (sender, e) => handleError(new Exception("Something went wrong with message handling."));

                    // Keep running until server exits:
                    return;
                }
                else if (cliInfo.writeJavaScriptClient)
                {
                    // Test echoing stdin to stdout (works for programs that pipe but not from console!):
                    //using (var output = Console.OpenStandardOutput())
                    //using (var input = Console.OpenStandardInput())
                    //{
                    //    var data = Encoding.UTF8.GetBytes("Hello world!");
                    //    output.Write(data, 0, data.Length);

                    //    input.CopyTo(output);
                    //}

                    using (Stream output = cliInfo.outputPath != null ? File.Create(cliInfo.OutputPathWithPreferredExtension("js")) : Console.OpenStandardOutput())
                    {
                        if (cliInfo.outputPath != null)
                        {
                            var bom = Encoding.UTF8.GetPreamble();
                            output.Write(bom, 0, bom.Length);
                        }
                        using (var denoClient = Permissions.EmbeddedResourceCode.GetStreamForResource("deno-client.zip"))
                        {
                            using (var zip = new System.IO.Compression.ZipArchive(denoClient))
                            {
                                var entry = zip.GetEntry("index.js");
                                using (var embeded = entry.Open())
                                {
                                    embeded.CopyTo(output);
                                }
                            }
                        }
                    }
                }
                else if (cliInfo.writeTypeScriptClientDefs)
                {
                    using (Stream output = cliInfo.outputPath != null ? File.Create(cliInfo.OutputPathWithPreferredExtension("d.ts")) : Console.OpenStandardOutput())
                    {
                        if (cliInfo.outputPath != null)
                        {
                            var bom = Encoding.UTF8.GetPreamble();
                            output.Write(bom, 0, bom.Length);
                        }
                        using (var denoClient = Permissions.EmbeddedResourceCode.GetStreamForResource("deno-client.zip"))
                        {
                            using (var zip = new System.IO.Compression.ZipArchive(denoClient))
                            {
                                var entry = zip.GetEntry("index.d.ts");
                                using (var embeded = entry.Open())
                                {
                                    embeded.CopyTo(output);
                                }
                            }
                        }
                    }
                }
                else
                    exit = false;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Failed to handle command line flag.\n" + ex.ToString(), "VirtualDesktop - Failed to handle command line flag", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            if (exit)
            {
                syncObject.Post((data) => Exit(), null);
                return;
            }

            if (!(cliInfo.silent && cliInfo.loadedData != null))
            {
                LoadSettings();
            }
            if (cliInfo.loadedData != null)
            {
                autoSavingEnabled = false;
                filters = cliInfo.loadedData.ToList();
            }

            if (Data.StartWithAdminRights && !Permissions.PermissionsCode.CheckIfElevated())
            {
                if (Permissions.PermissionsCode.RestartExecutableWithElevatedStatus())
                {
                    syncObject.Post((data) => Exit(), null);
                    return;
                }
            }

            SetUpNotificationIcon();
            HookIntoVirtualDesktopEvents();

            if (!cliInfo.silent)
            {
                saveTimer.Tick += (sender, e) =>
                    {
                        saveTimer.Enabled = false;
                        SaveSettings();
                    };

                if (notificationIcon != null)
                {
                    notificationIcon.Visible = true;
                    cursorTrackerTimer.Tick += CursorTrackerTimer_Tick;
                    cursorTrackerTimer.Enabled = true;
                }
            }
            else if (filters.Count > 0)
            {
                windowsInfoHolder.Invalidate((collectedData) => syncObject.Post(s =>
                {
                    collectedData.ApplyFilters(filters.ToArray(), Data.PreventFlashingWindows);
                    Exit();
                }, null));
            }
            else syncObject.Post((data) => Exit(), null);
        }

        #endregion Constructors


        #region Methods

        #region Startup Configuration

        private CommandLine CheckFlags()
        {
            var info = new CommandLine();

            bool nextIsFilterFile = false;
            bool nextIsOutputPath = false;
            foreach (string arg in Environment.GetCommandLineArgs().Skip(1))
            {
                if (nextIsOutputPath)
                {
                    nextIsOutputPath = false;
                    info.outputPath = arg;
                }
                if (nextIsFilterFile)
                {
                    nextIsFilterFile = false;
                    try
                    {
                        string path = null;
                        try
                        {
                            path = Path.GetFullPath(arg);
                            if (!File.Exists(path))
                                path = null;
                        }
                        catch { }
                        path = Path.GetFullPath(Utils.IOManager.ExecutablesDirectory + arg);

                        Filter.SaveFile savedData = Utils.SaveManager.Deserialize<Filter.SaveFile>(File.ReadAllText(path, Encoding.UTF8));
                        info.loadedData = savedData.RecreateData();
                        continue;
                    }
                    catch { }
                }
                if (arg.StartsWith("--filter"))
                {
                    nextIsFilterFile = true;
                    continue;
                }
                if (arg == "-s" || arg == "--silent")
                    info.silent = true;  // Don't show icon. Exit after completing tasks.
                if (arg == "-h" || arg == "--help" || arg == "/?")
                    info.help = true;

                if (arg == "--as-server")
                    info.asServer = true;
                if (arg == "--write-javascript-client")
                    info.writeJavaScriptClient = true;
                if (arg == "--write-javascript-client-types")
                    info.writeTypeScriptClientDefs = true;
                if (arg == "--output-path")
                    nextIsOutputPath = true;
            }

            return info;
        }

        private void SetUpNotificationIcon()
        {
            if (notificationIcon == null)
                return;

            notificationIcon.MouseClick += (sender, args) =>
            {
                if (args.Button == MouseButtons.Left)
                {
                    if (IsConfigFormOpen)
                        CloseConfigure();
                    else
                        OpenConfigure();
                }
                else if (args.Button == MouseButtons.Middle)
                {
                    ApplyFilters();
                }
            };

            notificationIcon.ContextMenuStrip.Opening += (sender, args) =>
            {
                try
                {
                    if (lastTrackedMousePos.X >= 0 && lastTrackedMousePos.Y >= 0)
                        Cursor.Position = lastTrackedMousePos;
                }
                catch { }
            };

            BuildNotificationContextMenu();
            UpdateNotificationIcon();
        }

        private void HookIntoVirtualDesktopEvents()
        {
            if (virtualDesktopImplementation != null)
            {
                async void handleChannel<T>(ChannelReader<T> channel, bool indexChanged = false)
                {
                    while (await channel.WaitToReadAsync())
                        while (channel.TryRead(out T item))
                        {
                            if (indexChanged)
                                UpdateNotificationIcon();
                            else
                                BuildNotificationContextMenu();
                        }
                }
                handleChannel(virtualDesktopImplementation.ListenForVirtualDesktopCreated(cancelVirtualDesktopImplementationEvents.Token));
                handleChannel(virtualDesktopImplementation.ListenForVirtualDesktopDeleted(cancelVirtualDesktopImplementationEvents.Token));
                handleChannel(virtualDesktopImplementation.ListenForVirtualDesktopChanged(cancelVirtualDesktopImplementationEvents.Token), indexChanged: true);
                return;
            }
            VirtualDesktop.Created += (sender, args) => syncObject.Post(s =>
            {
                BuildNotificationContextMenu();
            }, null);
            VirtualDesktop.Destroyed += (sender, args) => syncObject.Post(s =>
            {
                BuildNotificationContextMenu();
            }, null);
            VirtualDesktop.CurrentChanged += (sender, args) => syncObject.Post(s =>
            {
                UpdateNotificationIcon();
            }, null);
        }

        #endregion Startup Configuration


        public void Exit()
        {
            notificationIcon.Dispose();
            notificationIcon = null;
            this.ExitThread();
        }


        #region Config Window

        public void OpenConfigure()
        {
            if (configForm == null)
            {
                ConfigureForm form = new ConfigureForm(this);
                form.FormClosed += (sender, args) =>
                {
                    configForm = null;
                };
                configForm = form;
                configForm.Show();
            }
        }

        public void CloseConfigure()
        {
            if (configForm != null)
            {
                try
                {
                    configForm.Close();
                }
                catch (Exception)
                {
                    configForm = null;
                }
            }
        }

        #endregion Config Window


        #region Manipulate Windows

        public void ApplyFilters()
        {
            windowsInfoHolder.Invalidate((collectedData) => syncObject.Post(s =>
            {
                // TODO: Use IVirtualDesktop implementation here
                collectedData.ApplyFilters(filters.ToArray(), Data.PreventFlashingWindows);
            }, null));
        }

        public void StopFlashingForAllWindows()
        {
            if (virtualDesktopImplementation != null)
            {
                virtualDesktopImplementation.StopWindowFlashing(new QueryOpenWindowsFilter() { HasVirtualDesktopInfo = true }, CancellationToken.None);
                return;
            }

            windowsInfoHolder.Invalidate((collectedData) => syncObject.Post(s =>
            {
                foreach (var window in collectedData.WindowInfo)
                {
                    window.MoveToDesktop(window.Desktop, stopFlashing: true, lazyMoving: false);
                }
            }, null));
        }

        #endregion Manipulate Windows


        #region Manipulate Virtual Desktops

        public void CloseCurrentDesktop()
        {
            try
            {
                if (virtualDesktopImplementation != null)
                {
                    virtualDesktopImplementation.DeleteVirtualDesktop(false, CancellationToken.None);
                    return;
                }
                VirtualDesktop current = VirtualDesktop.Current;
                VirtualDesktop target = current.GetRight();
                if (target == null)
                    target = current.GetLeft();
                if (target != null)
                {
                    target.Switch();
                    current.Remove();
                }
            }
            catch { }
        }

        public void CreateNewDesktop()
        {
            try
            {
                if (virtualDesktopImplementation != null)
                {
                    virtualDesktopImplementation.CreateVirtualDesktop(true, CancellationToken.None);
                    return;
                }
                VirtualDesktop.Create()/**/.Switch()/**/;
            }
            catch { }
        }

        public void SwitchToDesktop(VirtualDesktop target)
        {
            try
            {
                if (VirtualDesktop.Current != target)
                {
                    if (!data.SmoothDesktopSwitching)
                    {
                        target.Switch();    // Libray switch active desktop.
                    }
                    else
                    {
                        new Utils.VirtualDesktopManager().ChangeCurrentVirtualDesktop(target.Id);
                    }
                }
            }
            catch { }
        }
        public void SwitchToDesktop(int desktopIndex)
        {
            try
            {
                if (virtualDesktopImplementation != null)
                {
                    virtualDesktopImplementation.ChangeCurrentVirtualDesktopToIndex(data.SmoothDesktopSwitching, desktopIndex, CancellationToken.None);
                    return;
                }
                var desktops = VirtualDesktop.GetDesktops();
                if (desktops.Length != 0)
                {
                    if (desktopIndex < 0)
                        return;
                    else if (desktopIndex >= desktops.Length)
                        desktopIndex = desktops.Length - 1;

                    SwitchToDesktop(desktops[desktopIndex]);
                }
            }
            catch { }
        }

        #endregion Manipulate Virtual Desktops


        #region Manage NotifyIcon

        public void BuildNotificationContextMenu()
        {
            int desktopCount = 0;
            try
            {
                if (virtualDesktopImplementation != null)
                {
                    var task = virtualDesktopImplementation.GetCurrentVirtualDesktopIndex(CancellationToken.None);
                    // Possible deadlock, should be fine if implementation is using background threads.
                    task.Wait();
                    desktopCount = task.Result;
                }
                else
                {
                    desktopCount = VirtualDesktop.GetDesktops().Length;
                }
            }
            catch { }

            if (notificationIcon == null || notificationIcon.ContextMenuStrip == null)
                return;
            var items = notificationIcon.ContextMenuStrip.Items;

            items.Clear();
            // Possible item types: ToolStripMenuItem, ToolStripComboBox, ToolStripSeparator, ToolStripTextBox.

            #region Quick Input Box

            if (desktopCount > 1)
            {
                ToolStripTextBox textBox = new ToolStripTextBox();
                string lastText = "";
                int selStart = 0;
                int selLength = 0;
                textBox.KeyDown += (sender, args) =>
                {
                    if (args.KeyCode == Keys.Enter || args.KeyCode == Keys.Space)
                    {
                        if (int.TryParse(textBox.Text, out int parsedInt))
                            SwitchToDesktop(parsedInt - 1);
                        args.SuppressKeyPress = true;
                        args.Handled = true;
                        textBox.Text = "";
                        notificationIcon.ContextMenuStrip.Hide();
                    }
                    else
                    {
                        // Before keyboard text change:
                        selStart = textBox.SelectionStart;
                        selLength = textBox.SelectionLength;
                    }
                };
                textBox.LostFocus += (sender, args) => { textBox.Text = ""; };
                textBox.TextChanged += (sender, args) =>
                {
                    // After text change:
                    bool moved = false;

                    if (textBox.Text.ToLowerInvariant().Any(c => new char[] { '§', ',' }.Contains(c)))
                    {
                        SwitchToDesktop(0);
                        moved = true;
                    }
                    if (textBox.Text.ToLowerInvariant().Any(c => new char[] { 's', '*', '/' }.Contains(c)))
                    {
                        data.SmoothDesktopSwitching = !data.SmoothDesktopSwitching;
                        InvalidateSavedSettings();
                    }

                    if (textBox.Text.Contains("-"))
                    {
                        if (virtualDesktopImplementation != null)
                            virtualDesktopImplementation.ChangeCurrentVirtualDesktopToLeft(data.SmoothDesktopSwitching, 1, CancellationToken.None);
                        else
                            SwitchToDesktop(VirtualDesktop.Current.GetLeft());
                        moved = true;
                    }
                    else if (textBox.Text.Contains("+"))
                    {
                        if (virtualDesktopImplementation != null)
                            virtualDesktopImplementation.ChangeCurrentVirtualDesktopToRight(data.SmoothDesktopSwitching, 1, CancellationToken.None);
                        else
                            SwitchToDesktop(VirtualDesktop.Current.GetRight());
                        moved = true;
                    }
                    else if ((int.TryParse(textBox.Text, out int parsedInt) && !textBox.Text.StartsWith("-") && !textBox.Text.StartsWith("+") && textBox.Text.Trim().Length == textBox.Text.Length) || textBox.Text == "")
                    {
                        // Accept text change:
                        lastText = textBox.Text;

                        // Check if possiblities are 1:
                        int diff = lastText.Length - (parsedInt == 0 ? 0 : parsedInt.ToString().Length);   // Indicates number of 0 in beginning of textBox
                        int allowedNumbers = desktopCount.ToString().Length - diff;
                        int max = diff == 0 ? desktopCount : allowedNumbers * 10 - 1;
                        if (parsedInt > max || parsedInt * 10 > max || desktopCount.ToString().Length <= diff)
                        {
                            // AutoSwitch (no other possibilities then current.)
                            SwitchToDesktop(parsedInt - 1);
                            moved = true;
                        }
                    }

                    if (moved)
                    {
                        // Accept text change:
                        lastText = textBox.Text;
                    }
                    else if (lastText != textBox.Text)
                    {
                        // Reset to last accepted text:
                        textBox.Text = lastText;
                        textBox.SelectionStart = selStart;
                        textBox.SelectionLength = selLength;
                    }

                    if (moved)
                    {
                        textBox.Text = "";
                        notificationIcon.ContextMenuStrip.Hide();
                    }
                };
                items.Add(textBox);
                items.Add(new ToolStripSeparator());
            }

            #endregion Quick Input Box


            #region Open / Close Desktop Items

            if (useOpenCloseDesktopContextMenuItems)
            {
                if (desktopCount > 1) items.Add(new ToolStripMenuItem("Close Current Desktop", null, (sender, args) => CloseCurrentDesktop()));
                items.Add(new ToolStripMenuItem("New Desktop", null, (sender, args) => CreateNewDesktop()));

                items.Add(new ToolStripSeparator());
            }

            #endregion Open / Close Desktop Items


            #region Global Items

            smoothDesktopToggleContextMenuItemNameSetter = null;
            if (useSmoothDesktopSwitchToggleContextMenuItem)
            {
                ToolStripMenuItem smoothDesktopSwitchToggleContextMenuItem = new ToolStripMenuItem("", null, (sender, args) =>
                {
                    data.SmoothDesktopSwitching = !data.SmoothDesktopSwitching;
                    InvalidateSavedSettings();
                });
                bool showsOn = !data.SmoothDesktopSwitching;
                smoothDesktopToggleContextMenuItemNameSetter = () =>
                {
                    if (showsOn == data.SmoothDesktopSwitching)
                        return;

                    showsOn = !showsOn;
                    smoothDesktopSwitchToggleContextMenuItem.Text = "Smooth Desktop Switch (" + (showsOn ? "On" : "Off") + ")";
                };
                smoothDesktopToggleContextMenuItemNameSetter();

                items.Add(smoothDesktopSwitchToggleContextMenuItem);
            }
            if (useStopFlashingWindowsContextMenuItem)
            {
                items.Add(new ToolStripMenuItem("Stop Flashing Windows", null, (sender, args) => StopFlashingForAllWindows()));
            }
            if (!(items.Cast<ToolStripItem>().Last() is ToolStripSeparator))
            {
                items.Add(new ToolStripSeparator());
            }

            #endregion Global Items


            #region Desktop Switch Items

            if (desktopCount > 1)
            {
                for (int iii = desktopCount - 1; iii >= 0; iii--)
                {
                    int desktopIndex = iii;
                    items.Add(new ToolStripMenuItem("Desktop " + (desktopIndex + 1), null, (sender, args) => SwitchToDesktop(desktopIndex)));
                }
                items.Add(new ToolStripSeparator());
            }

            #endregion Desktop Switch Items


            #region Filters

            items.AddRange(new ToolStripItem[] {

                new ToolStripMenuItem("Configure Filters", null, (sender, args) => OpenConfigure()),
                new ToolStripMenuItem("Apply Filters", null, (sender, args) => ApplyFilters()),
                new ToolStripSeparator(),
            });

            #endregion Filters


            items.Add(new ToolStripMenuItem("Exit", null, (sender, args) => Exit()));
        }

        public void UpdateNotificationIcon()
        {
            try
            {
                if (notificationIcon == null)
                    return;

                int desktopNumber;
                if (virtualDesktopImplementation != null)
                {
                    var task = virtualDesktopImplementation.GetCurrentVirtualDesktopIndex(CancellationToken.None);
                    task.Wait();
                    desktopNumber = task.Result + 1;
                }
                else
                {
                    desktopNumber = Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.Current) + 1;
                }
#if DEBUG
                desktopNumber *= -1;
#endif
                notificationIcon.Icon = IconManager.CreateIconWithNumber(desktopNumber);
            }
            catch
            {
                notificationIcon.Icon = Properties.Resources.triangleEmpty;
            }
        }


        private void CursorTrackerTimer_Tick(object sender, EventArgs e)
        {
            try
            {
                lastTrackedMousePos = Cursor.Position;
            }
            catch
            {
                lastTrackedMousePos = new Point(-1, -1);
            }
        }

        #endregion Manage NotifyIcon


        #region Settings

        public void SaveSettings()
        {
            if (!autoSavingEnabled)
                return;

            try
            {
                data.ActiveFilters = Filter.SaveFile.CreateSaveFile(filters.ToArray());
                File.WriteAllText(savePath, Utils.SaveManager.Serialize(data), Encoding.UTF8);
            }
            catch
            {

            }
        }

        public void LoadSettings()
        {
            while (true)
            {
                try
                {
                    if (!File.Exists(savePath))
                        return;

                    SettingsData loadedData = Utils.SaveManager.Deserialize<SettingsData>(File.ReadAllText(savePath, Encoding.UTF8));
                    data = loadedData;
                    filters = loadedData.ActiveFilters.RecreateData().ToList();
                    break;
                }
                catch (Exception ex)
                {
                    DialogResult result = MessageBox.Show("Failed to load settings!" + Environment.NewLine + "Exception info: " + Environment.NewLine + ex.ToString(), "Error when Loading Settings", MessageBoxButtons.AbortRetryIgnore, MessageBoxIcon.Error, MessageBoxDefaultButton.Button1);
                    if (result == DialogResult.Ignore)
                        break;
                    if (result == DialogResult.Abort)
                        throw;
                }
            }
        }

        public void InvalidateSavedSettings()
        {
            saveTimer.Enabled = true;
            smoothDesktopToggleContextMenuItemNameSetter?.Invoke();
            if (configForm != null)
                configForm.SaveDataUpdated();
        }

        #endregion Settings

        #endregion Methods


        #region Properties

        public SettingsData Data
        {
            get
            {
                return data;
            }
        }

        public bool IsConfigFormOpen
        {
            get
            {
                return configForm != null;
            }
        }

        public WindowInfo.Holder WindowsInfoHolder
        {
            get
            {
                return windowsInfoHolder;
            }
        }

        public List<Filter> FilterListReference
        {
            get
            {
                return filters;
            }
        }

        #endregion Properties
    }
}
