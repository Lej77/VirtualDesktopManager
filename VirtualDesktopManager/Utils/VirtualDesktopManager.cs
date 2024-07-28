using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Threading;

namespace VirtualDesktopManager.Utils
{
    /// <summary>
    /// Handles virutal desktops.
    /// Base code retrived from the internet!
    /// Link: https://blogs.msdn.microsoft.com/winsdk/2015/09/10/virtual-desktop-switching-in-windows-10/
    /// </summary>
    public class VirtualDesktopManager
    {
        #region classes
        [ComImport, InterfaceType(ComInterfaceType.InterfaceIsIUnknown), Guid("a5cd92ff-29be-454c-8d04-d82879fb3f1b")]
        [System.Security.SuppressUnmanagedCodeSecurity]
        private interface IVirtualDesktopManager
        {
            [PreserveSig]
            int IsWindowOnCurrentVirtualDesktop(
                [In] IntPtr TopLevelWindow,
                [Out] out int OnCurrentDesktop
                );
            [PreserveSig]
            int GetWindowDesktopId(
                [In] IntPtr TopLevelWindow,
                [Out] out Guid CurrentDesktop
                );

            [PreserveSig]
            int MoveWindowToDesktop(
                [In] IntPtr TopLevelWindow,
                [MarshalAs(UnmanagedType.LPStruct)]
            [In]Guid CurrentDesktop
                );
        }

        [ComImport, Guid("aa509086-5ca9-4c25-8f95-589d3c07b48a")]
        private class CVirtualDesktopManager
        {

        }


        /// <summary>
        /// An invsible window. Useful to for example gather information or manipulate virtual desktops.
        /// </summary>
        public class InvisibleForm : Form
        {
            private bool PreventActivation { get; } = false;
            private bool PreventClickAndTabSwitch { get; } = false;
            private bool Transparent { get; } = false;
            private bool ClickThrough { get; } = false;

            /// <summary>
            /// Initialize a new invisibile window.
            /// </summary>
            /// <param name="showInTaskbar">Determines if the window is shown in the taskbar.</param>
            /// <param name="preventActivation">Prevent the window from gaining focus when shown.</param>
            /// <param name="hideFromAltTab">Hide the window from alt-tab window switching. Also prevents the window from gaining focus when shown.</param>
            /// <param name="topMost">A topmost form is a form that overlaps all the other (non-topmost) forms even if it is not the active or foreground form. Topmost forms are always displayed at the highest point in the z-order of the windows on the desktop. You can use this property to create a form that is always displayed in your application, such as a Find and Replace tool window.</param>
            /// <param name="transparent">Make the form transparent.</param>
            /// <param name="clickThrough">Make the form transparent and allow clicks and user inputs through the form. Mouse movement will not be tracked by the form. (More prone to strange behaviour and graphical artifacts than setting opacity to zero)</param>
            /// <param name="opacityZero">Make the form transparent and allow clicks and user inputs through the form. Mouse movement will not be tracked by the form.</param>
            public InvisibleForm(bool showInTaskbar = false, bool preventActivation = true, bool hideFromAltTab = true, bool topMost = false, 
                bool transparent = false, bool clickThrough = false, bool opacityZero = true)
            {
                this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
                if (!showInTaskbar) this.ShowInTaskbar = false;
                this.Load += (sender, e) => this.Size = new System.Drawing.Size(0, 0);

                if (transparent)
                {
                    SetStyle(System.Windows.Forms.ControlStyles.SupportsTransparentBackColor, true);
                    this.BackColor = System.Drawing.Color.Transparent;
                }
                if (transparent || clickThrough)
                    this.TransparencyKey = System.Drawing.Color.Transparent;
                if (opacityZero)
                    Opacity = 0;    // Can also be set to an extremely low value such as 0.01 to make the form invisible but not allow mouse movements through.


                TopMost = topMost;
                PreventClickAndTabSwitch = hideFromAltTab;
                PreventActivation = preventActivation;
                Transparent = transparent;
                ClickThrough = clickThrough;
            }

            protected override void OnPaintBackground(PaintEventArgs e)
            {
                if (!Transparent && !ClickThrough)
                    base.OnPaintBackground(e);
                if (ClickThrough)
                    e.Graphics.FillRectangle(System.Drawing.Brushes.Transparent, e.ClipRectangle);
            }

            /// <summary>
            /// Close this window after the specified time has passed.
            /// </summary>
            /// <param name="timeToDelayClosingWindowInMilliseconds">Time in milliseconds until the window should be closed.</param>
            public void CloseWindowAfterTimeSpan(int timeToDelayClosingWindowInMilliseconds)
            {
                CloseWindowAfterTimeSpan(this, timeToDelayClosingWindowInMilliseconds);
            }
            /// <summary>
            /// Close a window after the specified time has passed.
            /// </summary>
            /// <param name="timeToDelayClosingWindowInMilliseconds">Time in milliseconds until the window should be closed.</param>
            public static void CloseWindowAfterTimeSpan(Form form, int timeToDelayClosingWindowInMilliseconds)
            {
                if (form == null)
                    return;

                System.Windows.Forms.Timer timer = new System.Windows.Forms.Timer();
                FormClosedEventHandler disableTimer = (sender, e) =>
                {
                    timer.Enabled = false;
                    timer.Dispose();
                };
                timer.Tick += (sender, e) =>
                {
                    form.Close();
                    timer.Enabled = false;
                    timer.Dispose();
                    form.FormClosed -= disableTimer;
                };
                form.FormClosed += disableTimer;
                timer.Interval = timeToDelayClosingWindowInMilliseconds;
                timer.Enabled = true;
            }

            protected override bool ShowWithoutActivation
            {
                get { return PreventActivation ? true : base.ShowWithoutActivation; }
            }
            protected override CreateParams CreateParams
            {
                get
                {
                    CreateParams baseParams = base.CreateParams;

                    if (PreventClickAndTabSwitch)
                    {
                        const int WS_EX_NOACTIVATE = 0x08000000;
                        const int WS_EX_TOOLWINDOW = 0x00000080;

                        baseParams.ExStyle |= (int)(WS_EX_NOACTIVATE | WS_EX_TOOLWINDOW);
                    }

                    return baseParams;
                }
            }
        }


        [Serializable]
        public class IncorrectStateException : Exception
        {
            public IncorrectStateException(Exception innerException) : base(innerException.Message, innerException) { }
        }

        [Serializable]
        public class ElementNotFoundException : Exception
        {
            public ElementNotFoundException(Exception innerException) : base(innerException.Message, innerException) { }
        }

        #endregion classes


        #region member variables and constructor

        private CVirtualDesktopManager cmanager = null;
        private IVirtualDesktopManager manager;

        public VirtualDesktopManager()
        {
            cmanager = new CVirtualDesktopManager();
            manager = (IVirtualDesktopManager)cmanager;
        }

        #endregion member variables and constructor


        #region methods

        /// <summary>
        /// Indicates if a form is on the current virtual desktop.
        /// </summary>
        /// <param name="handleForWindow">Handle for the form.</param>
        /// <returns>Value indicating if the form is on the current virtual desktop.</returns>
        public bool IsWindowOnCurrentVirtualDesktop(IntPtr handleForWindow)
        {
            int hr;
            if ((hr = manager.IsWindowOnCurrentVirtualDesktop(handleForWindow, out int result)) != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return result != 0;
        }

        /// <summary>
        /// Gets the virtual desktop id for a specific form.
        /// </summary>
        /// <param name="handleForWindow">Handle for form.</param>
        /// <returns>ID of the virtual desktop the form is on.</returns>
        public Guid GetVirtualDesktopIdFromWindow(IntPtr handleForWindow)
        {
            int hr;
            if ((hr = manager.GetWindowDesktopId(handleForWindow, out Guid result)) != 0)
            {
                Marshal.ThrowExceptionForHR(hr);
            }
            return result;
        }

        /// <summary>
        /// Moves a form to a spcific virtual desktop.
        /// </summary>
        /// <param name="handleForWindow">Handle for the form.</param>
        /// <param name="virtualDesktopID">ID for the virtual desktop that the form will be moved to.</param>
        public void MoveWindowToVirtualDesktop(IntPtr handleForWindow, Guid virtualDesktopID)
        {
            try
            {
                int hr;
                if ((hr = manager.MoveWindowToDesktop(handleForWindow, virtualDesktopID)) != 0)
                {
                    Marshal.ThrowExceptionForHR(hr);
                }
            }
            catch (Exception exception)
            {
                if (exception.HResult == -2147319765)
                {
                    throw new ElementNotFoundException(exception);
                }
                else if (exception.HResult == -2147019873)
                {
                    throw new IncorrectStateException(exception);
                }
                else throw;
            }
        }

        /// <summary>
        /// Gets the ID for the current virtual desktop.
        /// </summary>
        /// <returns>ID of the current virtual desktop.</returns>
        public Guid GetCurrentVirtualDesktopId()
        {
            InvisibleForm form = null;
            Guid currentVirtualDesktopId;
            try
            {
                form = new InvisibleForm(true);
                ShowFormOnCurrentVirtualDesktop(form);
                Thread.Sleep(25);   // Prevent Element not found exceptions
                currentVirtualDesktopId = GetVirtualDesktopIdFromWindow(form.Handle);
            }
            finally
            {
                form.Close();
            }
            return currentVirtualDesktopId;
        }

        /// <summary>
        /// Change the current virtual desktop.
        /// </summary>
        /// <param name="virtualDesktopID">Id of the virtual desktop to set as current.</param>
        public void ChangeCurrentVirtualDesktop(Guid virtualDesktopID)
        {
            InvisibleForm windowToForceCurrentDesktopSwitch = null;
            try
            {
                windowToForceCurrentDesktopSwitch = new InvisibleForm(preventActivation: false, hideFromAltTab: false);
                MoveWindowToVirtualDesktop(windowToForceCurrentDesktopSwitch.Handle, virtualDesktopID);
                windowToForceCurrentDesktopSwitch.Show();
            }
            finally
            {
                if (windowToForceCurrentDesktopSwitch != null)
                    windowToForceCurrentDesktopSwitch.CloseWindowAfterTimeSpan(100);
            }
        }

        /// <summary>
        /// Shows a form on the current virtual desktop.
        /// (uses the show command with the parrent set to null)
        /// </summary>
        /// <param name="formToShow">The form to show.</param>
        public static void ShowFormOnCurrentVirtualDesktop(Form formToShow)
        {
            formToShow.Show(null);
        }

        #endregion methods

    }
}
