using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using VirtualDesktopServer;
using WindowsDesktop;

namespace VirtualDesktopManager
{
    /// <summary>
    /// Implements the <see cref="IVirtualDesktop"/> interface using methods from the <see cref="Utils.WindowsDesktop"/> class.
    /// </summary>
    internal class VirtualDesktopViaGeneralLibrary : IVirtualDesktop, IDisposable
    {

        #region Classes

        /// <summary>
        /// Defines how logging should be handled. This is used to handle the <see cref="IVirtualDesktop.Log"/> method which can be called by clients
        /// that uses this implementation as its server.
        /// 
        /// The <see cref="CallbackLogger"/> class is provided to make it easy to create a logger.
        /// </summary>
        public interface ILogger
        {
            Task Log(string message, CancellationToken cancellationToken);
        }

        public class CallbackLogger : ILogger
        {
            private Func<string, CancellationToken, Task> log;
            public CallbackLogger(Action<string> log) : this((message, cancel) => log(message))
            { }
            public CallbackLogger(Action<string, CancellationToken> log) : this((message, cancel) =>
            {
                log(message, cancel);
                return Task.CompletedTask;
            })
            { }
            public CallbackLogger(Func<string, Task> log) : this((message, cancel) => log(message))
            { }
            public CallbackLogger(Func<string, CancellationToken, Task> log)
            {
                this.log = log;
            }

            Task ILogger.Log(string message, CancellationToken cancellationToken)
            {
                return log(message, cancellationToken);
            }
        }

        private struct IndexOutOfBounds
        {
            public enum Values
            {
                Inbounds,
                NoDesktop,
                ToLow,
                ToHigh,
            }
            public Values Value;

            public bool HasValue()
            {
                return Value != Values.NoDesktop;
            }
            public bool Inbounds()
            {
                return Value == Values.Inbounds;
            }

            public bool OutOfBounds()
            {
                return !Inbounds();
            }

            public static implicit operator IndexOutOfBounds(Values v)
            {
                return new IndexOutOfBounds() { Value = v };
            }
        }

        #endregion Classes


        #region Member Variables

        /// <summary>
        /// Log a message. Scripts or other services could use this to get info to the user.
        /// </summary>
        public ILogger Logger { get; }

        private readonly object locker = new object();

        /// <summary>
        /// Useful to ensure there is a message pump.
        /// </summary>
        private volatile Utils.VirtualDesktopManager.InvisibleForm invisibleForm = null;

        #endregion Member Variables


        #region Constructors

        public VirtualDesktopViaGeneralLibrary(ILogger logger)
        {
            Logger = logger;

            try
            {
                // The VirtualDesktop library needs to be initiated on the main thread, otherwise calls like `VirtualDesktop.FromHwnd()` will fail on other threads.
                _ = VirtualDesktop.Current;
            }
            // Virtual Desktops might not be supported?
            catch
            { }
        }

        #endregion Constructors


        #region Methods

        void IDisposable.Dispose()
        {
            lock (locker)
            {
                invisibleForm?.Close();
                invisibleForm?.Dispose();
                invisibleForm = null;
            }
        }

        void openInvisibleForm()
        {
            if (this.invisibleForm == null)
            {
                lock (locker)
                {
                    if (this.invisibleForm == null)
                    {
                        this.invisibleForm = new Utils.VirtualDesktopManager.InvisibleForm();
                        invisibleForm.Show();
                    }
                }
            }
        }

        private static void SwitchToDesktop(VirtualDesktop target, bool smoothChange)
        {
            if (target == null || VirtualDesktop.Current == target)
                return;
            if (!smoothChange)
            {
                // Use libray to switch active desktop.
                target.Switch();
            }
            else
            {
                new Utils.VirtualDesktopManager().ChangeCurrentVirtualDesktop(target.Id);
            }
        }

        internal static async Task MoveWindowToVirtualDesktop(IntPtr windowHandle, VirtualDesktop targetDesktop, MoveWindowToVirtualDesktopOptions options)
        {
            if (windowHandle == IntPtr.Zero) throw new ArgumentNullException(nameof(windowHandle));

            if (targetDesktop != null && options.DontMoveIfAlreadyAtTarget && VirtualDesktop.FromHwnd(windowHandle).Id == targetDesktop.Id)
                return;

            if (!options.StopWindowFlashing)
            {
                if (targetDesktop != null) VirtualDesktop.MoveToDesktop(windowHandle, targetDesktop);
                return;
            }

            // Simulate flashing (for testing purposes):
            // Utils.WindowManager.FlashWindow(WindowHandle, Utils.WindowManager.FlashWindowFlags.FLASHW_TIMERNOFG | Utils.WindowManager.FlashWindowFlags.FLASHW_TRAY, 0, 0);
            // await Task.Delay(2000);



            // This move might be canceled by later operations but that might take a while so move window to give the user immediate feedback:
            if (targetDesktop != null) VirtualDesktop.MoveToDesktop(windowHandle, targetDesktop);


            // Stop Taskbar Icon Flashing:
            Utils.WindowManager.FlashWindow(windowHandle, Utils.WindowManager.FlashWindowFlags.FLASHW_STOP, 0, 0);
            // After sending flash stop: wait for flashing to stop otherwise it is reapplied when window is shown.
            await Task.Delay(1000);


            async Task asyncWait(int waitTimeInMilliseconds)
            {
                if (waitTimeInMilliseconds < 50)
                    Thread.Sleep(waitTimeInMilliseconds);
                else
                    await Task.Delay(waitTimeInMilliseconds); // Less accurate but doesn't block thread. "Task.Delay" can take 15-30ms minimal.
            }

            bool wasVisible = true;
            try
            {
                // Hide (and Later Show) to update taskbar visibility (fixes allways visible taskbar icons):
                wasVisible = Utils.WindowManager.ChangeWindowVisibility(windowHandle, visible: false, asyncOperation: false);
            }
            finally
            {
                if (wasVisible)
                {
                    // Wait needed before showing window again to stop flashing windows:
                    // Wait time Minimum: 30 is quite relaible. Under 20 nearly always fails.
                    // 100 can fail if system is under heavy load.


                    #region Re-Show Helper Functions

                    async Task showWindow()
                    {
                        try
                        {
                            await Task.Run(() =>
                            {
                                Utils.WindowManager.ChangeWindowVisibility(windowHandle, visible: true, showWithoutActivation: true, asyncOperation: false);
                            });
                        }
                        catch { }
                    }
                    Task<bool> checkIfVisible()
                    {
                        return Task.Run(() =>
                        {
                            return Utils.WindowManager.GetWindowInfo(windowHandle).WindowStyle.HasFlag(Utils.WindowManager.WindowStyles.WS_VISIBLE);
                        });
                    }

                    #endregion Re-Show Helper Functions


                    #region Re-Show Window

                    try
                    {
                        // Wait for window to become hidden:
                        int[] retryTimes = new int[]{
                            100,        // 100    ms
                            400,        // 500    ms
                            500,        // 1_000   ms
                            1000,       // 2_000   ms
                            3000,       // 5_000   ms
                            5000,       // 10_000  ms
                            5000,       // 15_000  ms
                            5000,       // 20_000  ms
                            10000,      // 30_000  ms
                            30000,      // 60_000  ms
                            //60000,      // 120_000 ms
                            //120000,     // 240_000 ms
                            //120000,     // 360_000 ms
                        };
                        foreach (var time in retryTimes)
                        {
                            await asyncWait(time);
                            try
                            {
                                bool isShown = await checkIfVisible();
                                if (!isShown) break;
                            }
                            catch
                            { }
                        }
                        // Then re-show it:
                        await showWindow();
                    }
                    finally
                    {
                        // Safeguard to make absolutely sure the window is shown again:
                        _ = Task.Run(async () =>
                        {
                            await Task.Delay(1000);
                            await showWindow();
                        });
                    }

                    #endregion Re-Show Window
                }
            }


            if (wasVisible && targetDesktop != null)
            {
                // After hidding and showing the window it can either be visible on the taskbar for all virtual desktops or the window might have been moved to the current desktop.
                // Reapply virtual desktop info to ensure it is moved to the rigt place:

                // Note that too many attempts will cause windows taskbar and virtual desktop switching to slow down and maybe freeze. 
                // 1000 attempts per window for 15 windows will causes explorer to freeze.
                // 100 attempts per window for 15 windows will cause a slight slowdown.
                // On newer Windows versions this has gotten dramatically slower.

                int[] retryTimes = new int[]
                {
                    0,  // 0ms: if there is no lag then this might actually work.
                    25, // 25  ms: 20% of windows are shown before 25 ms.
                    25, // 50  ms: 75% of windows are showin before 50 ms.
                    50, // 100 ms
                    400,// 500 ms
                };

                foreach (var time in retryTimes)
                {
                    if (time != 0) await asyncWait(time);

                    var current = VirtualDesktop.FromHwnd(windowHandle);
                    if (current == null)
                    {
                        // Not shown yet...
                        continue;
                    }
                    if (current.Id == targetDesktop.Id)
                    {
                        // Is at the right place!
                        break;
                    }
                    try
                    {
                        // Move Window:
                        VirtualDesktop.MoveToDesktop(windowHandle, targetDesktop);    // Might be in incorrect state for some of these.
                    }
                    catch (OperationCanceledException) { throw; }
                    catch { }
                }
            }
        }
        private IndexOutOfBounds IndexToVirtualDesktop(int virtualDesktopIndex, out VirtualDesktop virtualDesktop)
        {
            VirtualDesktop[] desktops = null;
            return IndexToVirtualDesktop(virtualDesktopIndex, out virtualDesktop, ref desktops);
        }
        private IndexOutOfBounds IndexToVirtualDesktop(int virtualDesktopIndex, out VirtualDesktop virtualDesktop, ref VirtualDesktop[] allDesktops)
        {
            IndexOutOfBounds outOfBounds = IndexOutOfBounds.Values.Inbounds;
            if (allDesktops == null)
                allDesktops = VirtualDesktop.GetDesktops();
            if (allDesktops.Length == 0)
            {
                virtualDesktop = null;
                return IndexOutOfBounds.Values.NoDesktop;
            }
            if (virtualDesktopIndex < 0)
            {
                virtualDesktopIndex = 0;
                outOfBounds = IndexOutOfBounds.Values.ToLow;
            }
            else if (virtualDesktopIndex >= allDesktops.Length)
            {
                virtualDesktopIndex = allDesktops.Length - 1;
                outOfBounds = IndexOutOfBounds.Values.ToHigh;
            }

            virtualDesktop = allDesktops[virtualDesktopIndex];
            return outOfBounds;
        }

        /// <summary>
        /// Get a virtual desktop that is offset from a specified desktop.
        /// </summary>
        /// <param name="offsetToRight">Number of desktops to the right of the specified one. Negative to offset to the left.</param>
        /// <param name="referenceDesktop">The desktop to return for offset 0.</param>
        /// <param name="virtualDesktop">The desktop at the specified offset.</param>
        /// <returns>Info about out of bounds issues.</returns>
        private IndexOutOfBounds OffsetToVirtualDesktop(int offsetToRight, VirtualDesktop referenceDesktop, out VirtualDesktop virtualDesktop)
        {
            if (referenceDesktop == null)
            {
                virtualDesktop = null;
                return IndexOutOfBounds.Values.NoDesktop;
            }

            IndexOutOfBounds bounds = IndexOutOfBounds.Values.Inbounds;
            if (offsetToRight >= 0)
            {
                for (int iii = 0; iii < offsetToRight; iii++)
                {
                    var next = referenceDesktop.GetRight();
                    if (next == null)
                    {
                        bounds = IndexOutOfBounds.Values.ToHigh;
                        break;
                    }
                    referenceDesktop = next;
                }
            }
            else
            {
                for (int iii = 0; iii < -offsetToRight; iii++)
                {
                    var next = referenceDesktop.GetLeft();
                    if (next == null)
                    {
                        bounds = IndexOutOfBounds.Values.ToLow;
                        break;
                    }
                    referenceDesktop = next;
                }
            }
            virtualDesktop = referenceDesktop;
            return bounds;
        }

        private static ChannelReader<T> EventViaChannel<T, E>(Action<EventHandler<E>> subscribe, Action<EventHandler<E>> unsubscribe, Func<E, T> mapEventToMessage, CancellationToken cancellationToken)
        {
            CancellationTokenSource cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            try
            {
                // Unbounded since their is no way to apply backpressure to an event anyway, so better to store all data inside a channel than inside queued tasks.
                Channel<T> channel = Channel.CreateUnbounded<T>();
                void handler(object sender, E e)
                {
                    try
                    {
                        _ = channel.Writer.WriteAsync(mapEventToMessage(e));
                    }
                    catch (Exception ex)
                    {
                        unsubscribe(handler);
                        cancellationTokenSource.Cancel();
                        channel.Writer.TryComplete(ex);
                    }
                }

                cancellationTokenSource.Token.ThrowIfCancellationRequested();
                subscribe(handler);

                void cancel()
                {
                    unsubscribe(handler);
                    cancellationTokenSource.Cancel();
                    channel.Writer.TryComplete(new OperationCanceledException());
                }
                cancellationTokenSource.Token.Register(cancel);
                if (cancellationTokenSource.Token.IsCancellationRequested)
                    cancel();

                return channel;
            }
            catch (Exception)
            {
                cancellationTokenSource.Cancel();
                throw;
            }
        }


        #region IVirtualDesktop implementation

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToIndex(bool smoothChange, int index, CancellationToken cancellationToken)
        {
            if (IndexToVirtualDesktop(index, out var target).HasValue())
                SwitchToDesktop(target, smoothChange);

            return Task.CompletedTask;
        }

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToLeft(bool smoothChange, int count, CancellationToken cancellationToken)
        {
            if (count <= 0)
                return Task.CompletedTask;

            if (OffsetToVirtualDesktop(-count, VirtualDesktop.Current, out var target).HasValue())
                SwitchToDesktop(target, smoothChange);

            return Task.CompletedTask;
        }

        Task IVirtualDesktop.ChangeCurrentVirtualDesktopToRight(bool smoothChange, int count, CancellationToken cancellationToken)
        {
            if (count <= 0)
                return Task.CompletedTask;

            if (OffsetToVirtualDesktop(count, VirtualDesktop.Current, out var target).HasValue())
                SwitchToDesktop(target, smoothChange);

            return Task.CompletedTask;
        }

        Task IVirtualDesktop.CreateVirtualDesktop(bool switchToTheCreatedDesktop, CancellationToken cancellationToken)
        {
            var desktop = VirtualDesktop.Create();
            if (switchToTheCreatedDesktop)
                desktop.Switch();

            return Task.CompletedTask;
        }

        Task IVirtualDesktop.DeleteVirtualDesktop(bool preferFallbackToTheLeft, CancellationToken cancellationToken)
        {
            VirtualDesktop current = VirtualDesktop.Current;
            VirtualDesktop target = current.GetRight();
            if (target == null || preferFallbackToTheLeft)
                target = current.GetLeft() ?? target;
            if (target != null)
            {
                target.Switch();
                current.Remove();
            }

            return Task.CompletedTask;
        }

        Task IVirtualDesktop.DeleteVirtualDesktop(bool preferFallbackToTheLeft, int index, CancellationToken cancellationToken)
        {
            if (IndexToVirtualDesktop(index, out var target).OutOfBounds())
                return Task.CompletedTask;

            if (target == VirtualDesktop.Current)
            {
                VirtualDesktop fallback = target.GetRight();
                if (fallback == null || preferFallbackToTheLeft)
                    fallback = target.GetLeft() ?? fallback;
                if (fallback == null)
                    return Task.CompletedTask;

                fallback.Switch();
            }
            target.Remove();

            return Task.CompletedTask;
        }

        Task<int> IVirtualDesktop.GetCurrentVirtualDesktopIndex(CancellationToken cancellationToken)
        {
            var desktop = VirtualDesktop.Current;
            int index = Array.IndexOf(VirtualDesktop.GetDesktops(), desktop);
            if (index < 0)
                throw new Exception("Failed to find the index of the current virtual desktop.");

            return Task.FromResult(index);
        }

        Task IVirtualDesktop.Log(string message, CancellationToken cancellationToken)
        {
            return Logger.Log(message, cancellationToken);
        }

        ChannelReader<VirtualDesktopServer.VirtualDesktopChangedEventArgs> IVirtualDesktop.ListenForVirtualDesktopChanged(CancellationToken cancellationToken)
        {
            return EventViaChannel<VirtualDesktopServer.VirtualDesktopChangedEventArgs, WindowsDesktop.VirtualDesktopChangedEventArgs>(
                subscribe: handler => VirtualDesktop.CurrentChanged += handler,
                unsubscribe: handler => VirtualDesktop.CurrentChanged -= handler,
                mapEventToMessage: e =>
                {
                    var desktops = VirtualDesktop.GetDesktops();
                    return new VirtualDesktopServer.VirtualDesktopChangedEventArgs()
                    {
                        OldCurrentVirtualDesktopIndex = Array.IndexOf(desktops, e.OldDesktop),
                        NewCurrentVirtualDesktopIndex = Array.IndexOf(desktops, e.NewDesktop),
                    };
                },
                cancellationToken
            );
        }

        ChannelReader<VirtualDesktopCreatedEventArgs> IVirtualDesktop.ListenForVirtualDesktopCreated(CancellationToken cancellationToken)
        {
            return EventViaChannel<VirtualDesktopServer.VirtualDesktopCreatedEventArgs, VirtualDesktop>(
                subscribe: handler => VirtualDesktop.Created += handler,
                unsubscribe: handler => VirtualDesktop.Created -= handler,
                mapEventToMessage: e =>
                {
                    var desktops = VirtualDesktop.GetDesktops();
                    return new VirtualDesktopServer.VirtualDesktopCreatedEventArgs()
                    {
                        IndexOfCreatedVirtualDesktop = Array.IndexOf(desktops, e),
                    };
                },
                cancellationToken
            );
        }

        ChannelReader<VirtualDesktopDeletedEventArgs> IVirtualDesktop.ListenForVirtualDesktopDeleted(CancellationToken cancellationToken)
        {
            return EventViaChannel<VirtualDesktopServer.VirtualDesktopDeletedEventArgs, WindowsDesktop.VirtualDesktopDestroyEventArgs>(
                subscribe: handler => VirtualDesktop.Destroyed += handler,
                unsubscribe: handler => VirtualDesktop.Destroyed -= handler,
                mapEventToMessage: e =>
                {
                    var desktops = VirtualDesktop.GetDesktops();
                    return new VirtualDesktopServer.VirtualDesktopDeletedEventArgs()
                    {
                        // TODO: probably need to gather virtual desktop info earlier to still have desktop in collection.
                        IndexOfDeletedVirtualDesktop = Array.IndexOf(desktops, e.Destroyed),
                        IndexOfFallbackVirtualDesktop = Array.IndexOf(desktops, e.Fallback),
                    };
                },
                cancellationToken
            );
        }

        async Task IVirtualDesktop.PinWindowToAllVirtualDesktops(IntPtr windowHandle, bool stopWindowFlashing, CancellationToken cancellationToken)
        {
            VirtualDesktop.PinWindow(windowHandle);
            if (stopWindowFlashing)
            {
                await MoveWindowToVirtualDesktop(windowHandle, null, new MoveWindowToVirtualDesktopOptions()
                {
                    DontMoveIfAlreadyAtTarget = false,
                    StopWindowFlashing = true,
                });
            }
        }

        Task IVirtualDesktop.UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            VirtualDesktop.UnpinWindow(windowHandle);
            return Task.CompletedTask;
        }

        async Task IVirtualDesktop.UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, int targetVirtualDesktopIndex, bool moveEvenIfAlreadyUnpinned, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            // Not unconditional and already unpinned => return.
            if (!moveEvenIfAlreadyUnpinned && !VirtualDesktop.IsPinnedWindow(windowHandle))
                return;

            VirtualDesktop.UnpinWindow(windowHandle);

            if (IndexToVirtualDesktop(targetVirtualDesktopIndex, out var target).HasValue())
                await MoveWindowToVirtualDesktop(windowHandle, target, options);
        }

        async Task IVirtualDesktop.MoveWindowToVirtualDesktopIndex(IntPtr windowHandle, int targetVirtualDesktopIndex, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            if (IndexToVirtualDesktop(targetVirtualDesktopIndex, out var target).HasValue())
                await MoveWindowToVirtualDesktop(windowHandle, target, options);
        }

        async Task IVirtualDesktop.MoveWindowToVirtualDesktopLeftOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            if (count != 0 && OffsetToVirtualDesktop(-count, VirtualDesktop.FromHwnd(windowHandle), out var target).HasValue())
                await MoveWindowToVirtualDesktop(windowHandle, target, options);
        }

        async Task IVirtualDesktop.MoveWindowToVirtualDesktopRightOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken)
        {
            if (count != 0 && OffsetToVirtualDesktop(count, VirtualDesktop.FromHwnd(windowHandle), out var target).HasValue())
                await MoveWindowToVirtualDesktop(windowHandle, target, options);
        }

        Task<int> IVirtualDesktop.GetWindowVirtualDesktopIndex(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return Task.FromResult(Array.IndexOf(VirtualDesktop.GetDesktops(), VirtualDesktop.FromHwnd(windowHandle)));
        }

        Task<bool> IVirtualDesktop.GetWindowIsPinnedToVirtualDesktop(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return Task.FromResult(VirtualDesktop.IsPinnedWindow(windowHandle));
        }

        ChannelReader<QueryOpenWindowsInfo> IVirtualDesktop.QueryOpenWindows(QueryOpenWindowsFilter filter, QueryOpenWindowsWantedData wantedData, CancellationToken cancellationToken)
        {
            bool allowedByFilter(QueryOpenWindowsInfo info, bool hasVirtualDesktop)
            {
                //if (filter.WindowHandle != IntPtr.Zero && filter.WindowHandle != info.WindowHandle) return false;
                if (filter.ParentWindowHandle != IntPtr.Zero && filter.ParentWindowHandle != info.ParentWindowHandle) return false;
                if (filter.RootParentWindowHandle != IntPtr.Zero && filter.RootParentWindowHandle != info.RootParentWindowHandle) return false;
                if (filter.Title != null && filter.Title != info.Title) return false;
                //if (filter.ProcessId != null && filter.ProcessId != info.ProcessId) return false;
                //if (filter.PinnedToAllVirtualDesktops != null && filter.PinnedToAllVirtualDesktops != info.PinnedToAllVirtualDesktops) return false;
                //if (filter.VirtualDesktopIndex != null && filter.VirtualDesktopIndex != info.VirtualDesktopIndex) return false;
                //if (filter.RequireVirtualDesktopInfo && info.PinnedToAllVirtualDesktops != true && !hasVirtualDesktop) return false;
                //if (filter.IsMinimized != null && filter.IsMinimized != info.IsMinimized) return false;

                return true;
            }

            // Defaults to all true to get all data:
            if (wantedData == null)
                wantedData = QueryOpenWindowsWantedData.WantAll();
            else
            {
                // Ensure we get data that the filters need:
                if (filter.ParentWindowHandle != IntPtr.Zero)
                    wantedData.ParentWindowHandle = true;
                if (filter.RootParentWindowHandle != IntPtr.Zero)
                    wantedData.RootParentWindowHandle = true;

                if (filter.Title != null)
                    wantedData.Title = true;
                if (filter.ProcessId.HasValue)
                    wantedData.ProcessId = true;
                if (filter.IsMinimized.HasValue)
                    wantedData.IsMinimized = true;

                if (filter.PinnedToAllVirtualDesktops.HasValue)
                    wantedData.PinnedToAllVirtualDesktops = true;
                if (filter.VirtualDesktopIndex.HasValue)
                    wantedData.VirtualDesktopIndex = true;

                if (filter.HasVirtualDesktopInfo.HasValue)
                {
                    wantedData.PinnedToAllVirtualDesktops = true;
                    wantedData.VirtualDesktopIndex = true;
                }
            }

            Channel<QueryOpenWindowsInfo> channel = Channel.CreateBounded<QueryOpenWindowsInfo>(10);
            Task.Run(async () =>
            {
                Exception exception = null;
                try
                {
                    IEnumerable<IntPtr> windowHandles;

                    if (filter.WindowHandle != IntPtr.Zero)
                    {
                        windowHandles = new[] { filter.WindowHandle };
                    }
                    else if (filter.ParentWindowHandle != IntPtr.Zero)
                    {
                        windowHandles = Utils.WindowManager.GetAllDescendantWindows(filter.ParentWindowHandle);
                    }
                    else if (filter.RootParentWindowHandle != IntPtr.Zero)
                    {
                        windowHandles = Utils.WindowManager.GetAllDescendantWindows(filter.RootParentWindowHandle);
                    }
                    else if (filter.ProcessId.HasValue)
                    {
                        windowHandles = Utils.WindowManager.GetAllWindowsForProcess(filter.ProcessId.Value);
                    }
                    else
                    {
                        windowHandles = Utils.WindowManager.GetAllWindows();
                    }

                    using (IEnumerator<IntPtr> windows = windowHandles.GetEnumerator())
                    {
                        QueryOpenWindowsInfo info = null;
                        bool hasVirtualDesktop = false;
                        int step = 0;
                        while (!cancellationToken.IsCancellationRequested)
                        {
                            try
                            {
                                while (!cancellationToken.IsCancellationRequested)
                                {
                                    if (step == 0)
                                    {
                                        if (!windows.MoveNext()) return;
                                        info = new QueryOpenWindowsInfo()
                                        {
                                            WindowHandle = windows.Current
                                        };
                                        hasVirtualDesktop = false;
                                    }
                                    step++;
                                    switch (step)
                                    {
                                        case 1:
                                            if (filter.WindowHandle != IntPtr.Zero && filter.WindowHandle != info.WindowHandle)
                                            {
                                                step = 0;
                                                continue;
                                            }
                                            if (!wantedData.PinnedToAllVirtualDesktops) continue;
                                            info.PinnedToAllVirtualDesktops = VirtualDesktop.IsPinnedWindow(info.WindowHandle);
                                            break;
                                        case 2:
                                            if (filter.PinnedToAllVirtualDesktops.HasValue && filter.PinnedToAllVirtualDesktops != info.PinnedToAllVirtualDesktops)
                                            {
                                                step = 0;
                                                continue;
                                            }
                                            if (!wantedData.VirtualDesktopIndex) continue;
                                            var virtualDesktop = VirtualDesktop.FromHwnd(info.WindowHandle);
                                            if (virtualDesktop != null)
                                            {
                                                hasVirtualDesktop = true;
                                                var index = Array.IndexOf(VirtualDesktop.GetDesktops(), virtualDesktop);
                                                if (index >= 0)
                                                    info.VirtualDesktopIndex = index;
                                            }
                                            break;
                                        case 3:
                                            if (filter.VirtualDesktopIndex.HasValue && filter.VirtualDesktopIndex != info.VirtualDesktopIndex)
                                            {
                                                // Wrong virtual desktop index => skip window:
                                                step = 0;
                                                continue;
                                            }
                                            var windowHasVirtualDesktopInfo = info.PinnedToAllVirtualDesktops == true || hasVirtualDesktop;
                                            if (filter.HasVirtualDesktopInfo.HasValue && filter.HasVirtualDesktopInfo.Value != windowHasVirtualDesktopInfo)
                                            {
                                                // No virtual desktop info so skip collecting the rest of the info:
                                                step = 0;
                                                continue;
                                            }
                                            if (!wantedData.ProcessId) continue;
                                            info.ProcessId = Utils.WindowManager.GetWindowProcessID(info.WindowHandle);
                                            break;
                                        case 4:
                                            if (filter.ProcessId.HasValue && filter.ProcessId.Value != info.ProcessId)
                                            {
                                                // Incorrect process id => skip this window:
                                                step = 0;
                                                continue;
                                            }
                                            if (!wantedData.ParentWindowHandle) continue;
                                            info.ParentWindowHandle = Utils.WindowManager.GetParentWindow(info.WindowHandle);
                                            break;
                                        case 5:
                                            if (!wantedData.RootParentWindowHandle) continue;
                                            info.RootParentWindowHandle = Utils.WindowManager.GetRootParentWindow(info.WindowHandle);
                                            break;
                                        case 6:
                                            if (!wantedData.IsMinimized) continue;
                                            info.IsMinimized = Utils.WindowManager.IsMinimized(info.WindowHandle);
                                            break;
                                        case 7:
                                            if (filter.IsMinimized.HasValue && filter.IsMinimized != info.IsMinimized)
                                            {
                                                // Incorrect minimized status => skip window:
                                                step = 0;
                                                continue;
                                            }
                                            if (!wantedData.Title) continue;
                                            info.Title = Utils.WindowManager.GetWindowTitle(info.WindowHandle);
                                            break;
                                        default:
                                            step = 0;
                                            if (allowedByFilter(info, hasVirtualDesktop))
                                            {
                                                await channel.Writer.WriteAsync(info, cancellationToken);
                                            }
                                            break;
                                    }
                                }
                            }
                            catch (Exception)
                            { }
                            cancellationToken.ThrowIfCancellationRequested();
                        }
                    }
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
                finally
                {
                    channel.Writer.TryComplete(exception);
                }
            });
            return channel;
        }

        Task<bool> IVirtualDesktop.SetWindowShowState(IntPtr windowHandle, Virtualdesktop.SetWindowShowStateRequest.Types.ShowState showState, CancellationToken cancellationToken)
        {
            // Proto buffer enum uses the real constant's values.
            Utils.WindowManager.ShowCommand command = (Utils.WindowManager.ShowCommand)(int)showState;
            return Task.FromResult(Utils.WindowManager.SetWindowShowState(windowHandle, command, false));
        }

        Task IVirtualDesktop.CloseWindow(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            // Ensure we don't use a null handle since that would broadcast close to all windows.
            if (windowHandle == IntPtr.Zero) throw new Exception("No window handle specified so can't close a window");
            Utils.WindowManager.CloseWindow(windowHandle);
            return Task.CompletedTask;
        }

        Task<bool> IVirtualDesktop.GetIsWindowMinimized(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            return Task.FromResult(Utils.WindowManager.IsMinimized(windowHandle));
        }

        Task<IntPtr> IVirtualDesktop.GetForegroundWindow(CancellationToken cancellationToken)
        {
            return Task.FromResult(Utils.WindowManager.GetForegroundWindow());
        }

        Task<IntPtr> IVirtualDesktop.GetDesktopWindow(CancellationToken cancellationToken)
        {
            return Task.FromResult(Utils.WindowManager.GetDesktopWindow());
        }

        Task IVirtualDesktop.SetForegroundWindow(IntPtr windowHandle, CancellationToken cancellationToken)
        {
            Utils.WindowManager.SetForegroundWindow(windowHandle);
            return Task.CompletedTask;
        }

        Task<bool> IVirtualDesktop.GetIsElevated(CancellationToken cancellationToken)
        {
            return Task.FromResult(Permissions.PermissionsCode.CheckIfElevated());
        }

        Task<System.Drawing.Rectangle> IVirtualDesktop.GetWindowLocation(IntPtr windowHandle, bool extendedFrameBounds, CancellationToken cancellationToken)
        {
            return Task.FromResult(Utils.WindowManager.GetWindowLocation(windowHandle, extendedFrameBounds));
        }

        Task IVirtualDesktop.MoveWindow(IntPtr windowHandle, System.Drawing.Rectangle wantedLocation, CancellationToken cancellationToken)
        {
            Utils.WindowManager.MoveWindow(windowHandle, wantedLocation, true);
            return Task.CompletedTask;
        }

        ChannelReader<ShellEventArgs> IVirtualDesktop.ListenToShellEvents(bool getSecondEventArg, Virtualdesktop.ShellEventType? wantedEventType, CancellationToken cancellationToken)
        {
            Virtualdesktop.ShellEventType convertEventType(Utils.WindowManager.ShellEventType eventType)
            {
                switch (eventType)
                {
                    case Utils.WindowManager.ShellEventType.HSHELL_WINDOWCREATED:
                        return Virtualdesktop.ShellEventType.WindowCreated;
                    case Utils.WindowManager.ShellEventType.HSHELL_WINDOWDESTROYED:
                        return Virtualdesktop.ShellEventType.WindowDestroyed;
                    case Utils.WindowManager.ShellEventType.HSHELL_ACTIVATESHELLWINDOW:
                        return Virtualdesktop.ShellEventType.ActivateShellWindow;
                    case Utils.WindowManager.ShellEventType.HSHELL_WINDOWACTIVATED:
                        return Virtualdesktop.ShellEventType.WindowActivated;
                    case Utils.WindowManager.ShellEventType.HSHELL_GETMINRECT:
                        return Virtualdesktop.ShellEventType.GetMinRect;
                    case Utils.WindowManager.ShellEventType.HSHELL_REDRAW:
                        return Virtualdesktop.ShellEventType.Redraw;
                    case Utils.WindowManager.ShellEventType.HSHELL_TASKMAN:
                        return Virtualdesktop.ShellEventType.TaskMan;
                    case Utils.WindowManager.ShellEventType.HSHELL_LANGUAGE:
                        return Virtualdesktop.ShellEventType.Language;
                    case Utils.WindowManager.ShellEventType.HSHELL_SYSMENU:
                        return Virtualdesktop.ShellEventType.SysMenu;
                    case Utils.WindowManager.ShellEventType.HSHELL_ENDTASK:
                        return Virtualdesktop.ShellEventType.EndTask;
                    case Utils.WindowManager.ShellEventType.HSHELL_ACCESSIBILITYSTATE:
                        return Virtualdesktop.ShellEventType.AccessibilityState;
                    case Utils.WindowManager.ShellEventType.HSHELL_APPCOMMAND:
                        return Virtualdesktop.ShellEventType.AppCommand;
                    case Utils.WindowManager.ShellEventType.HSHELL_WINDOWREPLACED:
                        return Virtualdesktop.ShellEventType.WindowReplaced;
                    case Utils.WindowManager.ShellEventType.HSHELL_WINDOWREPLACING:
                        return Virtualdesktop.ShellEventType.WindowReplacing;
                    case Utils.WindowManager.ShellEventType.HSHELL_MONITORCHANGED:
                        return Virtualdesktop.ShellEventType.MonitorChanged;
                    case Utils.WindowManager.ShellEventType.HSHELL_RUDEAPPACTIVATED:
                        return Virtualdesktop.ShellEventType.RudeAppActivated;
                    case Utils.WindowManager.ShellEventType.HSHELL_FLASH:
                        return Virtualdesktop.ShellEventType.Flash;
                    default:
                        return Virtualdesktop.ShellEventType.Unknown;
                }
            }
            if (getSecondEventArg)
            {
                Utils.WindowManager.AdvancedShellEventListener listener = null;
                return EventViaChannel<VirtualDesktopServer.ShellEventArgs, Utils.WindowManager.AdvancedShellEventListener.AdvancedShellEventArgs>(
                    subscribe: handler =>
                    {
                        listener = new Utils.WindowManager.AdvancedShellEventListener();
                        if (wantedEventType.HasValue)
                        {
                            listener.UnsafeEvents.ShellEvent += (sender, e) =>
                            {
                                var eventType = convertEventType(e.EventType);
                                if (eventType == wantedEventType.Value)
                                {
                                    listener.SyncObject.Post(state =>
                                    {
                                        handler(sender, e);
                                    }, null);
                                }
                            };
                        }
                        else
                        {
                            listener.SafeEvents.ShellEvent += handler;
                        }
                    },
                    unsubscribe: handler =>
                    {
                        listener?.Dispose();
                    },
                    mapEventToMessage: e =>
                    {
                        return new VirtualDesktopServer.ShellEventArgs()
                        {
                            EventType = convertEventType(e.EventType),
                            EventTypeCode = (int)e.EventType,
                            Data = e.WParam,
                            SecondaryData = e.LParam,
                        };
                    },
                    cancellationToken
                );
            }
            else
            {
                Utils.WindowManager.ShellEventListener listener = null;
                return EventViaChannel<VirtualDesktopServer.ShellEventArgs, Utils.WindowManager.ShellEventListener.ShellEventArgs>(
                    subscribe: handler =>
                    {
                        listener = new Utils.WindowManager.ShellEventListener();
                        if (wantedEventType.HasValue)
                        {
                            listener.UnsafeEvents.ShellEvent += (sender, e) =>
                            {
                                var eventType = convertEventType(e.EventType);
                                if (eventType == wantedEventType.Value)
                                {
                                    listener.SyncObject.Post(state =>
                                    {
                                        handler(sender, e);
                                    }, null);
                                }
                            };
                        }
                        else
                        {
                            listener.SafeEvents.ShellEvent += handler;
                        }
                    },
                    unsubscribe: handler =>
                    {
                        listener?.Dispose();
                    },
                    mapEventToMessage: e =>
                    {
                        return new VirtualDesktopServer.ShellEventArgs()
                        {
                            EventType = convertEventType(e.EventType),
                            EventTypeCode = (int)e.EventType,
                            Data = e.EventData,
                            // No secondary data from this listener:
                            SecondaryData = IntPtr.Zero,
                        };
                    },
                    cancellationToken
                );
            }
        }

        async Task IVirtualDesktop.StopWindowFlashing(QueryOpenWindowsFilter filter, CancellationToken cancellationToken)
        {
            using (var cancelWindowsQuery = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
            {
                try
                {
                    var wantedData = QueryOpenWindowsWantedData.WantNothing();
                    wantedData.PinnedToAllVirtualDesktops = true;
                    var windows = (this as IVirtualDesktop).QueryOpenWindows(filter, wantedData, cancelWindowsQuery.Token);
                    while (await windows.WaitToReadAsync(cancellationToken))
                        while (windows.TryRead(out var item))
                        {
                            VirtualDesktop target = null;
                            if (item.PinnedToAllVirtualDesktops != true)
                            {
                                // Not pinned => Should have a virtual desktop:
                                try
                                {
                                    target = VirtualDesktop.FromHwnd(item.WindowHandle);
                                }
                                // Caller might be interested in windows that don't have virtual desktop info:
                                catch
                                { }
                            }
                            await MoveWindowToVirtualDesktop(item.WindowHandle, target, new MoveWindowToVirtualDesktopOptions()
                            {
                                DontMoveIfAlreadyAtTarget = false,
                                StopWindowFlashing = true,
                            });
                        }
                }
                finally
                {
                    cancelWindowsQuery.Cancel();
                }
            }
        }

        #endregion IVirtualDesktop implementation

        #endregion Methods


        #region Properties

        #endregion Properties
    }
}
