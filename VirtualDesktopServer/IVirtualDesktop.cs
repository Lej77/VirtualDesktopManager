using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace VirtualDesktopServer
{
    public class VirtualDesktopChangedEventArgs : EventArgs
    {
        public int OldCurrentVirtualDesktopIndex;
        public int NewCurrentVirtualDesktopIndex;
    }
    public class VirtualDesktopDeletedEventArgs : EventArgs
    {
        /// <summary>
        /// The index of the virtual desktop that was deleted
        /// </summary>
        public int IndexOfDeletedVirtualDesktop;
        /// <summary>
        /// The index of the virtual desktop that is to be displayed after specified virtual desktop
        /// is deleted.
        ///
        /// This index is for before the delete is preformed. So if it is after the deleted virtual
        /// desktop then it will be one less after the delete operation has been preformed.
        /// </summary>
        public int IndexOfFallbackVirtualDesktop;
    }
    public class VirtualDesktopCreatedEventArgs : EventArgs
    {
        /// <summary>
        /// The index of the created virtual desktop. The new virtual desktop should always be created
        /// after all the currently existing virtual desktops but this might change in the future.
        /// </summary>
        public int IndexOfCreatedVirtualDesktop;
    }

    public class ShellEventArgs : EventArgs
    {
        public Virtualdesktop.ShellEventType EventType;
        public IntPtr Data;
        public IntPtr SecondaryData;
        public int EventTypeCode;
    }

    /// <summary>
    /// Specifies a filter for what windows we are interested in.
    /// </summary>
    public class QueryOpenWindowsFilter
    {
        public bool? HasVirtualDesktopInfo = null;
        public IntPtr WindowHandle = IntPtr.Zero;
        public IntPtr ParentWindowHandle = IntPtr.Zero;
        public IntPtr RootParentWindowHandle = IntPtr.Zero;
        public string Title = null;
        public int? ProcessId = null;
        public bool? PinnedToAllVirtualDesktops = null;
        public int? VirtualDesktopIndex = null;
        public bool? IsMinimized = null;

        /// <summary>
        /// Create an empty filter that will match all windows. Can manually change the filter after it has been created.
        /// </summary>
        public QueryOpenWindowsFilter() { }
        /// <summary>
        /// Explicitly set all fields. Use named arguments to make the call site more readable.
        /// </summary>
        public QueryOpenWindowsFilter(bool? HasVirtualDesktopInfo, IntPtr WindowHandle, IntPtr ParentWindowHandle, IntPtr RootParentWindowHandle, string Title, int? ProcessId, bool? PinnedToAllVirtualDesktops, int? VirtualDesktopIndex, bool? IsMinimized)
        {
            this.HasVirtualDesktopInfo = HasVirtualDesktopInfo;
            this.WindowHandle = WindowHandle;
            this.ParentWindowHandle = ParentWindowHandle;
            this.RootParentWindowHandle = RootParentWindowHandle;
            this.Title = Title;
            this.ProcessId = ProcessId;
            this.PinnedToAllVirtualDesktops = PinnedToAllVirtualDesktops;
            this.VirtualDesktopIndex = VirtualDesktopIndex;
            this.IsMinimized = IsMinimized;
        }

        public static implicit operator QueryOpenWindowsFilter(Virtualdesktop.OpenWindowsFilter filter) => filter == null ? null : new QueryOpenWindowsFilter(
            HasVirtualDesktopInfo: filter.OptionalHasVirtualDesktopInfoCase == Virtualdesktop.OpenWindowsFilter.OptionalHasVirtualDesktopInfoOneofCase.None ? null : (bool?)filter.HasVirtualDesktopInfo,
            WindowHandle: filter.WindowHandle.ToLossyIntPtr(),
            ParentWindowHandle: filter.ParentWindowHandle.ToLossyIntPtr(),
            RootParentWindowHandle: filter.RootParentWindowHandle.ToLossyIntPtr(),
            Title: filter.OptionalTitleCase == Virtualdesktop.OpenWindowsFilter.OptionalTitleOneofCase.None ? null : filter.Title,
            ProcessId: filter.OptionalProcessIdCase == Virtualdesktop.OpenWindowsFilter.OptionalProcessIdOneofCase.None ? null : (int?)filter.ProcessId,
            PinnedToAllVirtualDesktops: filter.OptionalPinnedToAllVirtualDesktopsCase == Virtualdesktop.OpenWindowsFilter.OptionalPinnedToAllVirtualDesktopsOneofCase.None ? null : (bool?)filter.PinnedToAllVirtualDesktops,
            VirtualDesktopIndex: filter.OptionalVirtualDesktopIndexCase == Virtualdesktop.OpenWindowsFilter.OptionalVirtualDesktopIndexOneofCase.None ? null : (int?)filter.VirtualDesktopIndex,
            IsMinimized: filter.OptionalIsMinimizedCase == Virtualdesktop.OpenWindowsFilter.OptionalIsMinimizedOneofCase.None ? null : (bool?)filter.IsMinimized
        );
        public static implicit operator Virtualdesktop.OpenWindowsFilter(QueryOpenWindowsFilter filter) {
            var result = new Virtualdesktop.OpenWindowsFilter();

            if (filter.WindowHandle != IntPtr.Zero)
                result.WindowHandle = filter.WindowHandle.ToInt64();
            if (filter.ParentWindowHandle != IntPtr.Zero)
                result.ParentWindowHandle = filter.ParentWindowHandle.ToInt64();
            if (filter.RootParentWindowHandle != IntPtr.Zero)
                result.RootParentWindowHandle = filter.RootParentWindowHandle.ToInt64();

            if (filter.Title != null)
                result.Title = filter.Title;
            if (filter.ProcessId.HasValue)
                result.ProcessId = (uint)filter.ProcessId.Value;
            if (filter.IsMinimized.HasValue)
                result.IsMinimized = filter.IsMinimized.Value;

            if (filter.HasVirtualDesktopInfo.HasValue)
                result.HasVirtualDesktopInfo = filter.HasVirtualDesktopInfo.Value;
            if (filter.PinnedToAllVirtualDesktops.HasValue)
                result.PinnedToAllVirtualDesktops = filter.PinnedToAllVirtualDesktops.Value;
            if (filter.VirtualDesktopIndex.HasValue)
                result.VirtualDesktopIndex = filter.VirtualDesktopIndex.Value;

            return result;
        }
    }
    /// <summary>
    /// Specifies what properties of a window we are interested in.
    /// </summary>
    public class QueryOpenWindowsWantedData
    {
        public bool ParentWindowHandle = false;
        public bool RootParentWindowHandle = false;

        public bool Title = false;
        public bool ProcessId = false;
        public bool IsMinimized = false;

        public bool PinnedToAllVirtualDesktops = false;
        public bool VirtualDesktopIndex = false;

        private QueryOpenWindowsWantedData() { }
        /// <summary>
        /// Explicitly set all fields. Use named arguments to make the call site more readable.
        /// </summary>
        public QueryOpenWindowsWantedData(
            bool ParentWindowHandle, 
            bool RootParentWindowHandle, 
            bool Title, 
            bool ProcessId,
            bool IsMinimized,
            bool PinnedToAllVirtualDesktops, 
            bool VirtualDesktopIndex
        )
        {
            this.ParentWindowHandle = ParentWindowHandle;
            this.RootParentWindowHandle = RootParentWindowHandle;

            this.Title = Title;
            this.ProcessId = ProcessId;
            this.IsMinimized = IsMinimized;

            this.PinnedToAllVirtualDesktops = PinnedToAllVirtualDesktops;
            this.VirtualDesktopIndex = VirtualDesktopIndex;
        }

        /// <summary>
        /// Create a data specifier that requests all window data possible.
        /// </summary>
        public static QueryOpenWindowsWantedData WantAll()
        {
            return new QueryOpenWindowsWantedData(
                ParentWindowHandle: true,
                RootParentWindowHandle: true,

                Title: true,
                ProcessId: true,
                IsMinimized: true,

                PinnedToAllVirtualDesktops: true,
                VirtualDesktopIndex: true
            );
        }

        /// <summary>
        /// Create a data specifier that requests no window data. Manually set some fields to true to get some data about a window.
        /// </summary>
        public static QueryOpenWindowsWantedData WantNothing()
        {
            return new QueryOpenWindowsWantedData();
        }

        public static implicit operator QueryOpenWindowsWantedData(Virtualdesktop.QueryOpenWindowsRequestWantedDataSpecifier wanted) => wanted == null ? null : new QueryOpenWindowsWantedData(
            ParentWindowHandle: wanted.ParentWindowHandle,
            RootParentWindowHandle: wanted.RootParentWindowHandle,

            Title: wanted.Title,
            ProcessId: wanted.ProcessId,
            IsMinimized: wanted.IsMinimized,

            PinnedToAllVirtualDesktops: wanted.PinnedToAllVirtualDesktops,
            VirtualDesktopIndex: wanted.VirtualDesktopIndex
        );
        public static implicit operator Virtualdesktop.QueryOpenWindowsRequestWantedDataSpecifier(QueryOpenWindowsWantedData wanted) => new Virtualdesktop.QueryOpenWindowsRequestWantedDataSpecifier()
        {
            ParentWindowHandle = wanted.ParentWindowHandle,
            RootParentWindowHandle = wanted.RootParentWindowHandle,

            Title = wanted.Title,
            ProcessId = wanted.ProcessId,
            IsMinimized = wanted.IsMinimized,

            PinnedToAllVirtualDesktops = wanted.PinnedToAllVirtualDesktops,
            VirtualDesktopIndex = wanted.VirtualDesktopIndex,
        };
    }

    /// <summary>
    /// Info about an open window.
    /// </summary>
    public class QueryOpenWindowsInfo
    {
        public IntPtr WindowHandle = IntPtr.Zero;
        public IntPtr ParentWindowHandle = IntPtr.Zero;
        public IntPtr RootParentWindowHandle = IntPtr.Zero;
        public string Title = null;
        public int? ProcessId = null;
        public bool? PinnedToAllVirtualDesktops = null;
        public int? VirtualDesktopIndex = null;
        public bool? IsMinimized = null;
    }

    /// <summary>
    /// Options when moving a window to a virtual desktop.
    /// </summary>
    public class MoveWindowToVirtualDesktopOptions
    {
        /// <summary>
        /// Check if a window is already at a target desktop before trying to move it.
        /// </summary>
        public bool DontMoveIfAlreadyAtTarget = true;
        public bool StopWindowFlashing = false;

        public MoveWindowToVirtualDesktopOptions() { }
        public MoveWindowToVirtualDesktopOptions(bool DontMoveIfAlreadyAtTarget, bool StopWindowFlashing)
        {
            this.DontMoveIfAlreadyAtTarget = DontMoveIfAlreadyAtTarget;
            this.StopWindowFlashing = StopWindowFlashing;
        }


        /// <summary>
        /// Convert from protobuf representation.
        /// </summary>
        /// <param name="options">The protobuf data.</param>
        public static implicit operator MoveWindowToVirtualDesktopOptions(Virtualdesktop.MoveWindowToVirtualDesktopOptions options) => new MoveWindowToVirtualDesktopOptions()
        {
            DontMoveIfAlreadyAtTarget = options?.DontMoveIfAlreadyAtTarget ?? false,
            StopWindowFlashing = options?.StopWindowFlashing ?? false,
        };
        public static implicit operator Virtualdesktop.MoveWindowToVirtualDesktopOptions(MoveWindowToVirtualDesktopOptions options) => new Virtualdesktop.MoveWindowToVirtualDesktopOptions()
        {
            DontMoveIfAlreadyAtTarget = options.DontMoveIfAlreadyAtTarget,
            StopWindowFlashing = options.StopWindowFlashing,
        };
    }

    /// <summary>
    /// Something that can handle virtual desktop server commands and queries.
    /// </summary>
    public interface IVirtualDesktop
    {
        /// <summary>
        /// Log a message. This can be useful if the client is a sub-process of the server and therefore might want to defer log handling to the parent process.
        /// </summary>
        /// <param name="message">The message that should be logged.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task Log(string message, CancellationToken cancellationToken);

        /// <summary>
        /// Determine if the server has elevated permissions.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>True if the server has elevated permissions.</returns>
        Task<bool> GetIsElevated(CancellationToken cancellationToken);


        #region Handle Current Virtual Desktop

        /// <summary>
        /// Get the index of the current virtual desktop.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>The index of the current virtual desktop.</returns>
        Task<int> GetCurrentVirtualDesktopIndex(CancellationToken cancellationToken);
        Task ChangeCurrentVirtualDesktopToIndex(bool smoothChange, int index, CancellationToken cancellationToken);
        Task ChangeCurrentVirtualDesktopToRight(bool smoothChange, int count, CancellationToken cancellationToken);
        Task ChangeCurrentVirtualDesktopToLeft(bool smoothChange, int count, CancellationToken cancellationToken);

        #endregion Handle Current Virtual Desktop


        #region Create/Delete Virtual Desktops

        /// <summary>
        /// Create a new virtual desktop, this will always be created after all the current virtual desktops.
        /// </summary>
        /// <param name="switchToCreatedDesktop">True to immediately switch to the newly created virtual desktop.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task CreateVirtualDesktop(bool switchToCreatedDesktop, CancellationToken cancellationToken);
        /// <summary>
        /// Delete the currently selected virtual desktop.
        /// </summary>
        /// <param name="preferFallbackToTheLeft">If true then fallback to the virtual desktop to the left of the current one instead of the one the right.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task DeleteVirtualDesktop(bool preferFallbackToTheLeft, CancellationToken cancellationToken);
        /// <summary>
        /// Delete the virtual desktop at the specified index.
        /// </summary>
        /// <param name="preferFallbackToTheLeft">If true then fallback to the virtual desktop to the left of the current one instead of the one the right.</param>
        /// <param name="index">The index of the virtual desktop that should be deleted.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task DeleteVirtualDesktop(bool preferFallbackToTheLeft, int index, CancellationToken cancellationToken);

        #endregion Create/Delete Virtual Desktops


        #region Listen for Virtual Desktop Changed/Created/Removed

        /// <summary>
        /// Be notified when the current virtual desktop index is changed.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the event listener and close the returned channel.</param>
        /// <returns>A channel which will be sent the requested events.</returns>
        ChannelReader<VirtualDesktopChangedEventArgs> ListenForVirtualDesktopChanged(CancellationToken cancellationToken);
        /// <summary>
        /// Be notified when a virtual desktop is created.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the event listener and close the returned channel.</param>
        /// <returns>A channel which will be sent the requested events.</returns>
        ChannelReader<VirtualDesktopCreatedEventArgs> ListenForVirtualDesktopCreated(CancellationToken cancellationToken);
        /// <summary>
        /// Be notified when a virtual desktop is deleted/closed/destroyed.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the event listener and close the returned channel.</param>
        /// <returns>A channel which will be sent the requested events.</returns>
        ChannelReader<VirtualDesktopDeletedEventArgs> ListenForVirtualDesktopDeleted(CancellationToken cancellationToken);

        #endregion Listen for Virtual Desktop Changed/Created/Removed


        #region Query a window's virtual desktop info

        Task<int> GetWindowVirtualDesktopIndex(IntPtr windowHandle, CancellationToken cancellationToken);
        Task<bool> GetWindowIsPinnedToVirtualDesktop(IntPtr windowHandle, CancellationToken cancellationToken);

        #endregion Query a window's virtual desktop info


        #region Manage a window's virtual desktop

        Task PinWindowToAllVirtualDesktops(IntPtr windowHandle, bool stopWindowFlashing, CancellationToken cancellationToken);
        /// <summary>
        /// Unpin a window so that it is no longer shown on all virtual desktops.
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be unpinned.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, CancellationToken cancellationToken);
        /// <summary>
        /// Unpin a window so that it is no longer shown on all virtual desktops and then move it to a specified virtual desktop index.
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be unpinned.</param>
        /// <param name="targetVirtualDesktopIndex">The virtual desktop that the window should be moved to directly after it is unpinned.</param>
        /// <param name="moveEvenIfAlreadyUnpinned">Move the window even if it was already unpinned.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task UnpinWindowFromAllVirtualDesktops(IntPtr windowHandle, int targetVirtualDesktopIndex, bool moveEvenIfAlreadyUnpinned, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken);
        /// <summary>
        /// Move a window to a specific virtual desktop index.
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be moved.</param>
        /// <param name="targetVirtualDesktopIndex">The index of the virtual desktop that the window should be moved to.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task MoveWindowToVirtualDesktopIndex(IntPtr windowHandle, int targetVirtualDesktopIndex, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken);
        /// <summary>
        /// Move a window to a virtual desktop to the left of the one it is currently on.
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be moved.</param>
        /// <param name="count">The number of virtual desktops to move the window.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task MoveWindowToVirtualDesktopLeftOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken);
        /// <summary>
        /// Move a window to a virtual desktop to the right of the one it is currently on.
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be moved.</param>
        /// <param name="count">The number of virtual desktops to move the window.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task MoveWindowToVirtualDesktopRightOfCurrent(IntPtr windowHandle, int count, MoveWindowToVirtualDesktopOptions options, CancellationToken cancellationToken);

        #endregion Manage a window's virtual desktop


        #region Query Open Windows

        /// <summary>
        /// Query information about some open windows.
        /// </summary>
        /// <param name="filter">Filter what windows to collect information from.</param>
        /// <param name="wantedData">Specifies what data that should be collected about the windows.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>A channel which will be sent information about each opened window that wasn't filtered out.</returns>
        ChannelReader<QueryOpenWindowsInfo> QueryOpenWindows(QueryOpenWindowsFilter filter, QueryOpenWindowsWantedData wantedData, CancellationToken cancellationToken);

        /// <summary>
        /// Get the currently selected window.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>The window handle for the currently selected window.</returns>
        Task<IntPtr> GetForegroundWindow(CancellationToken cancellationToken);

        /// <summary>
        /// Get the desktop window.
        /// </summary>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>The window handle for desktop.</returns>
        Task<IntPtr> GetDesktopWindow(CancellationToken cancellationToken);

        /// <summary>
        /// Listen to shell events.
        /// </summary>
        /// <param name="getSecondEventArg">Use a more advanced event listener that captures the second event argument as well.</param>
        /// <param name="wantedEventType">If this is specified then all other events types will be filter out.</param>
        /// <param name="cancellationToken">Token that will cancel the event listener and close the returned channel.</param>
        /// <returns>A channel which will be sent the requested events.</returns>
        ChannelReader<ShellEventArgs> ListenToShellEvents(bool getSecondEventArg, Virtualdesktop.ShellEventType? wantedEventType, CancellationToken cancellationToken);

        #endregion Query Open Windows


        #region Get Window Info

        /// <summary>
        /// Determine if a window is minimized.
        /// </summary>
        /// <param name="windowHandle">The window that should be checked to see if it is minimized.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>True if the window is minimized.</returns>
        Task<bool> GetIsWindowMinimized(IntPtr windowHandle, CancellationToken cancellationToken);

        /// <summary>
        /// Determine a window's location.
        /// </summary>
        /// <param name="windowHandle">The window whose location should be determined.</param>
        /// <param name="extendedFrameBounds">Use extended frame bounds, this will exclude the window's drop shadow from the returned bounds.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>The bounds of the window.</returns>
        Task<Rectangle> GetWindowLocation(IntPtr windowHandle, bool extendedFrameBounds, CancellationToken cancellationToken);

        #endregion Get Window Info


        #region Manipulate Window

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="windowHandle">The window that should have its show state changed.</param>
        /// <param name="showState">The state that window should be changed to.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>True if the window was previously shown.</returns>
        Task<bool> SetWindowShowState(IntPtr windowHandle, Virtualdesktop.SetWindowShowStateRequest.Types.ShowState showState, CancellationToken cancellationToken);

        /// <summary>
        /// Close a window.
        /// </summary>
        /// <param name="windowHandle">The window that should be closed.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task CloseWindow(IntPtr windowHandle, CancellationToken cancellationToken);

        /// <summary>
        /// Set the foreground window (the currently select window).
        /// </summary>
        /// <param name="windowHandle">The handle for the window that should be moved to the front.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task SetForegroundWindow(IntPtr windowHandle, CancellationToken cancellationToken);

        /// <summary>
        /// Move a window.
        /// </summary>
        /// <param name="windowHandle">The window that should be moved.</param>
        /// <param name="wantedLocation">The location the window should be moved to.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task MoveWindow(IntPtr windowHandle, Rectangle wantedLocation, CancellationToken cancellationToken);

        /// <summary>
        /// Stop some windows from trying to grab the users attention by flashing their taskbar icons. The taskbar icon of flashing windows is visible on all virtual desktops.
        /// </summary>
        /// <param name="filter">Determines what windows should stop being flashed.</param>
        /// <param name="cancellationToken">Token that will cancel the request.</param>
        /// <returns>Completes once the operation has been preformed.</returns>
        Task StopWindowFlashing(QueryOpenWindowsFilter filter, CancellationToken cancellationToken);

        #endregion Manipulate Window
    }
}
