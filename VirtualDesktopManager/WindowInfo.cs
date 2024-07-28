using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using VirtualDesktopManager.Extensions;
using WindowsDesktop;
using System.Diagnostics;
using System.Threading;

namespace VirtualDesktopManager
{
    public class WindowInfo
    {
        #region Classes

        public class Data : ICloneable
        {
            private WindowInfo[] windowInfo = new WindowInfo[0];
            private VirtualDesktop[] desktops = new VirtualDesktop[0];
            private IntPtr[] mainWindows = new IntPtr[0];

            public VirtualDesktop CurrentDesktop { get; } = null;
            public bool OnlyDesktopBoundWindows { get; } = false;

            private Data(WindowInfo[] windowInfo, VirtualDesktop[] desktops, IntPtr[] mainWindows, VirtualDesktop currentDesktop, bool onlyDesktopBoundWindows)
            {
                this.windowInfo = windowInfo;
                this.desktops = desktops;
                this.mainWindows = mainWindows;
                this.CurrentDesktop = currentDesktop;

                this.OnlyDesktopBoundWindows = onlyDesktopBoundWindows;
            }

            public static Data Gather(bool onlyDesktopBoundWindows)
            {
                List<IntPtr> mainWindows = Utils.WindowManager.GetAllMainWindows();
                VirtualDesktop[] desktops = VirtualDesktop.GetDesktops();
                VirtualDesktop current = VirtualDesktop.Current;
                List<WindowInfo> newInfo = onlyDesktopBoundWindows ? GetAllWindowsBoundToDesktops() : GetAllWindows();

                return new Data(newInfo.ToArray(), desktops, mainWindows.ToArray(), current, onlyDesktopBoundWindows);
            }

            public object Clone()
            {
                return new Data(WindowInfo, Desktops, MainWindows, CurrentDesktop, OnlyDesktopBoundWindows);
            }


            public void ApplyFilters(Filter[] filterList, bool stopFlashing)
            {
                foreach (WindowInfo window in WindowInfo)
                {
                    try
                    {
                        Filter.DesktopTarget desktopTarget = Filter.GetFirstDesktopTarget(Filter.GetApplicableFilters(window, this, filterList)) ?? new Filter.DesktopTarget();
                        if (desktopTarget.shouldPin)
                        {
                            try
                            {
                                VirtualDesktop.PinWindow(window.WindowHandle);
                            }
                            catch
                            { }
                        }
                        else
                        {
                            int desktopIndex = desktopTarget.targetDesktopIndex;

                            if (0 <= desktopIndex && desktopIndex < Desktops.Length)
                                window.MoveToDesktop(Desktops[desktopIndex], stopFlashing: stopFlashing, allowUnpin: desktopTarget.allowUnpin);
                        }
                    }
                    catch { }
                }
            }


            public int DetermineDesktopIndex(VirtualDesktop virtualDesktop)
            {
                if (virtualDesktop == null)
                    return -1;

                return Desktops.ToList().IndexOf(virtualDesktop);
            }


            public WindowInfo[] WindowInfo
            {
                get
                {
                    return windowInfo.ToArray();
                }
            }

            public VirtualDesktop[] Desktops
            {
                get
                {
                    return desktops.ToArray();
                }
            }

            public IntPtr[] MainWindows
            {
                get
                {
                    return mainWindows.ToArray();
                }
            }
        }

        public class Holder
        {
            #region Classes

            #endregion Classes


            #region Member Variables

            private readonly object locker = new object();

            private Data latestData = null;

            private Thread updateThread = null;
            private bool invalidated = false;
            private bool onlyDesktopBoundWindows = false;
            private bool updating = false;

            private List<Action<Data>> invalidateCallbacks = new List<Action<Data>>();

            public event EventHandler<EventArgs> InfoUpdated;
            public event EventHandler<EventArgs> InfoUpToDate;

            #endregion Member Variables


            #region Constructors

            public Holder(bool onlyDesktopBoundWindows = false)
            {
                this.onlyDesktopBoundWindows = onlyDesktopBoundWindows;
            }

            #endregion Constructors


            #region Methods

            public void Invalidate(Action<Data> callback = null)
            {
                lock (locker)
                {
                    invalidated = true;

                    if (updateThread == null || !updateThread.IsAlive)
                    {
                        updateThread = new Thread(Main);
                        updateThread.Name = "WindowInfo Holder - Updater thread";
                        updateThread.Start();
                    }

                    if (callback != null)
                        invalidateCallbacks.Add(callback);
                }
            }

            private void Main()
            {
                try
                {
                    bool error = false;
                    while (Invalidated && !error)
                    {
                        try
                        {
                            Action<Data>[] callbacks = null;
                            Data newData = null;
                            try
                            {
                                bool bound;
                                lock (locker)
                                {
                                    Updating = true;
                                    Invalidated = false;
                                    bound = OnlyDesktopBoundWindows;
                                    callbacks = invalidateCallbacks.ToArray();
                                    invalidateCallbacks.Clear();
                                }
                                newData = Data.Gather(bound);

                                lock (locker)
                                {
                                    latestData = newData;
                                }
                            }
                            catch { error = true; Invalidated = true; }
                            finally
                            {
                                if (callbacks != null && callbacks.Length > 0)
                                {
                                    Data clonedData;
                                    lock (locker)
                                    {
                                        clonedData = newData == null ? null : newData.Clone() as Data;
                                    }

                                    foreach (Action<Data> callback in callbacks)
                                    {
                                        try
                                        {
                                            callback(clonedData == null ? null : clonedData.Clone() as Data);
                                        }
                                        catch { }
                                    }
                                }
                            }

                            if (!error)
                            {
                                try
                                {
                                    InfoUpdated.Raise(this, EventArgs.Empty);
                                }
                                catch { }
                            }
                        }
                        finally
                        {
                            Updating = false;
                        }
                    }
                }
                finally
                {
                    lock (locker)
                    {
                        if (updateThread == Thread.CurrentThread)
                            updateThread = null;
                    }
                }
            }

            #endregion Methods


            #region Properties

            /// <summary>
            /// Indicates that a new update is needed. If the updater thread is working it will need to redo its work after it is done.
            /// Is set to false at start of an update.
            /// </summary>
            public bool Invalidated
            {
                get
                {
                    lock (locker)
                    {
                        return invalidated;
                    }
                }
                private set
                {
                    bool upToDate;
                    lock (locker)
                    {
                        if (Invalidated == value)
                            return;

                        invalidated = value;

                        upToDate = UpToDate;
                    }

                    if (upToDate)
                        InfoUpToDate.Raise(this, EventArgs.Empty);
                }
            }

            /// <summary>
            /// Indicates that the updater thread is working. This means that the "InfoUpdated" event might be raised one time before a new invalidates data is ready.
            /// </summary>
            public bool Updating
            {
                get
                {
                    lock (locker)
                    {
                        return updating;
                    }
                }
                set
                {
                    bool upToDate;
                    lock (locker)
                    {
                        if (Updating == value)
                            return;

                        updating = value;

                        upToDate = UpToDate;
                    }

                    if (upToDate)
                        InfoUpToDate.Raise(this, EventArgs.Empty);
                }
            }

            /// <summary>
            /// LatestInfo has not been invalidated.
            /// </summary>
            public bool UpToDate
            {
                get
                {
                    lock (locker)
                    {
                        return !Updating && !Invalidated;
                    }
                }
            }

            public Data LatestInfo
            {
                get
                {
                    lock (locker)
                    {
                        return latestData == null ? null : latestData.Clone() as Data;
                    }
                }
            }

            public bool OnlyDesktopBoundWindows
            {
                get
                {
                    lock (locker)
                    {
                        return onlyDesktopBoundWindows;
                    }
                }
                set
                {
                    lock (locker)
                    {
                        if (OnlyDesktopBoundWindows == value)
                            return;

                        onlyDesktopBoundWindows = value;
                    }
                    Invalidate();
                }
            }

            #endregion Properties
        }

        #endregion Classes


        #region Member Variables

        public IntPtr WindowHandle { get; private set; } = IntPtr.Zero;
        public IntPtr ParentWindowHandle { get; private set; } = IntPtr.Zero;
        public IntPtr RootParentWindowHandle { get; private set; } = IntPtr.Zero;

        public string Title { get; private set; } = "";
        public Process Process { get; private set; } = null;
        public VirtualDesktop Desktop { get; private set; } = null;

        #endregion Member Variables


        #region Constructors

        private WindowInfo(IntPtr windowHandle)
        {
            this.WindowHandle = windowHandle;
        }

        #endregion Constructors


        #region Methods

        public bool CheckIfOnCurrentDesktop()
        {
            return VirtualDesktop.IsCurrentVirtualDesktop(WindowHandle);
        }

        public void MoveToDesktop(VirtualDesktop targetDesktop, bool stopFlashing = true, bool lazyMoving = true, bool allowUnpin = false, int timeoutInMilliseconds = 30000, bool waitForCompletion = false)
        {
            if (targetDesktop == null)
                return;

            var source = new CancellationTokenSource();
            var task = Task.Run(async () =>
            {
                try
                {
                    source.CancelAfter(TimeSpan.FromMilliseconds(timeoutInMilliseconds));
                    await MoveToDesktopAsync(targetDesktop, stopFlashing, lazyMoving, allowUnpin);
                }
                catch { }
            }, source.Token);

            if (waitForCompletion)
                task.Wait();
        }
        public async Task MoveToDesktopAsync(VirtualDesktop targetDesktop, bool stopFlashing = true, bool lazyMoving = true, bool allowUnpin = false)
        {
            if (targetDesktop == null)
                return;

            if (allowUnpin)
            {
                try
                {
                    VirtualDesktop.UnpinWindow(WindowHandle);
                }
                catch
                { }
            }

            await VirtualDesktopViaGeneralLibrary.MoveWindowToVirtualDesktop(WindowHandle, targetDesktop, new VirtualDesktopServer.MoveWindowToVirtualDesktopOptions()
            {
                DontMoveIfAlreadyAtTarget = lazyMoving,
                StopWindowFlashing = stopFlashing,
            });
        }


        /// <summary>
        /// Get WindowInfo for all windows whose VirtualDesktop can be retrieved.
        /// </summary>
        /// <returns>A list with the WindowInfo.</returns>
        public static List<WindowInfo> GetAllWindowsBoundToDesktops()
        {
            List<WindowInfo> windows = new List<WindowInfo>();
            var allWindows = Utils.WindowManager.GetAllWindows().GetEnumerator();
            bool done = false;
            Exception? ex = null;
            while (!done)
            {
                try
                {
                    while (allWindows.MoveNext())
                    {
                        IntPtr handle = allWindows.Current;
                        if (VirtualDesktop.IsPinnedWindow(handle) || VirtualDesktop.FromHwnd(handle) != null)
                        {
                            try
                            {
                                windows.Add(GetWindowInfo(handle));
                            }
                            catch (Exception e)
                            {
                                ex = e;
                                break;
                            }
                        }
                    }
                    done = true;
                } catch  {
                    // Ignore windows which we can't get virtual desktop info about...
                }
                // But re-throw other errors:
                if (ex != null) throw ex;
            }

            return windows;
        }

        /// <summary>
        /// Get WindowInfo for all windows.
        /// </summary>
        /// <returns>A list with the WindowInfo.</returns>
        public static List<WindowInfo> GetAllWindows()
        {
            List<WindowInfo> windows = new List<WindowInfo>();
            foreach (IntPtr handle in Utils.WindowManager.GetAllWindows())
            {
                windows.Add(GetWindowInfo(handle));
            }
            return windows;
        }

        /// <summary>
        /// Collect info about a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window to collect info about.</param>
        /// <returns>WindowInfo about the specified window.</returns>
        public static WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return null;

            WindowInfo info = new WindowInfo(windowHandle);

            int stage = 0;
            bool done = false;
            while (!done)
            {
                try
                {
                    while (!done)
                    {
                        stage++;

                        if (stage == 1) info.ParentWindowHandle = Utils.WindowManager.GetParentWindow(windowHandle);
                        else if (stage == 2) info.RootParentWindowHandle = Utils.WindowManager.GetRootParentWindow(windowHandle);
                        else if (stage == 3) info.Title = Utils.WindowManager.GetWindowTitle(windowHandle);
                        else if (stage == 4) info.Process = Utils.WindowManager.GetWindowProcess(windowHandle);
                        else if (stage == 5) info.Desktop = VirtualDesktop.FromHwnd(windowHandle);
                        else done = true;
                    }
                }
                catch { }
            }

            return info;
        }

        #endregion Methods


        #region Properties

        #endregion Properties
    }
}
