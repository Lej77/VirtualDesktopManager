using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Diagnostics;
using System.Runtime.InteropServices;
using VirtualDesktopManager.Extensions;
using System.Windows.Forms;
using System.ComponentModel;
using System.Windows.Automation;
using System.Threading;
using System.IO;
using System.Drawing;
using System.Windows;

namespace VirtualDesktopManager.Utils
{
    /// <summary>
    /// Contains functions to retrive info about different windows and to control them.
    /// </summary>
    public static class WindowManager
    {
        #region Classes

        #region Constants

        /// <summary>
        /// The following are the window styles. After the window has been created, these styles cannot be modified, except as noted.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms632600(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum WindowStyles : long
        {
            /// <summary>
            /// The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_TILED style.
            /// </summary>
            WS_OVERLAPPED = 0x00000000L
,
            /// <summary>
            /// The windows is a pop-up window. This style cannot be used with the WS_CHILD style.
            /// </summary>
            WS_POPUP = 0x80000000L
,
            /// <summary>
            /// The window is a child window. A window with this style cannot have a menu bar. This style cannot be used with the WS_POPUP style.
            /// </summary>
            WS_CHILD = 0x40000000L
,
            /// <summary>
            /// The window is initially minimized. Same as the WS_ICONIC style.
            /// </summary>
            WS_MINIMIZE = 0x20000000L
,
            /// <summary>
            /// <para>The window is initially visible.</para>
            /// <para>This style can be turned on and off by using the ShowWindow or SetWindowPos function.</para>
            /// </summary>
            WS_VISIBLE = 0x10000000L
,
            /// <summary>
            /// The window is initially disabled. A disabled window cannot receive input from the user. To change this after a window has been created, use the EnableWindow function.
            /// </summary>
            WS_DISABLED = 0x08000000L
,
            /// <summary>
            /// Clips child windows relative to each other; that is, when a particular child window receives a WM_PAINT message, the WS_CLIPSIBLINGS style clips all other overlapping child windows out of the region of the child window to be updated. If WS_CLIPSIBLINGS is not specified and child windows overlap, it is possible, when drawing within the client area of a child window, to draw within the client area of a neighboring child window.
            /// </summary>
            WS_CLIPSIBLINGS = 0x04000000L
,
            /// <summary>
            /// Excludes the area occupied by child windows when drawing occurs within the parent window. This style is used when creating the parent window.
            /// </summary>
            WS_CLIPCHILDREN = 0x02000000L
,
            /// <summary>
            /// The window is initially maximized.
            /// </summary>
            WS_MAXIMIZE = 0x01000000L
,
            /// <summary>
            /// The window has a title bar (includes the WS_BORDER style).
            /// </summary>
            WS_CAPTION = 0x00C00000L     /* WS_BORDER | WS_DLGFRAME  */
,
            /// <summary>
            /// The window has a thin-line border.
            /// </summary>
            WS_BORDER = 0x00800000L
,
            /// <summary>
            /// The window has a border of a style typically used with dialog boxes. A window with this style cannot have a title bar.
            /// </summary>
            WS_DLGFRAME = 0x00400000L
,
            /// <summary>
            /// The window has a vertical scroll bar.
            /// </summary>
            WS_VSCROLL = 0x00200000L
,
            /// <summary>
            /// The window has a horizontal scroll bar.
            /// </summary>
            WS_HSCROLL = 0x00100000L
,
            /// <summary>
            /// The window has a window menu on its title bar. The WS_CAPTION style must also be specified.
            /// </summary>
            WS_SYSMENU = 0x00080000L
,
            /// <summary>
            /// The window has a sizing border. Same as the WS_SIZEBOX style.
            /// </summary>
            WS_THICKFRAME = 0x00040000L
,
            /// <summary>
            /// <para> The window is the first control of a group of controls. The group consists of this first control and all controls defined after it, up to the next control with the WS_GROUP style. The first control in each group usually has the WS_TABSTOP style so that the user can move from group to group. The user can subsequently change the keyboard focus from one control in the group to the next control in the group by using the direction keys.</para>
            /// <para>You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function.</para>
            /// </summary>
            WS_GROUP = 0x00020000L
,
            /// <summary>
            /// <para>The window is a control that can receive the keyboard focus when the user presses the TAB key. Pressing the TAB key changes the keyboard focus to the next control with the WS_TABSTOP style.</para>
            /// <para>You can turn this style on and off to change dialog box navigation. To change this style after a window has been created, use the SetWindowLong function. For user-created windows and modeless dialogs to work with tab stops, alter the message loop to call the IsDialogMessage function.</para>
            /// </summary>
            WS_TABSTOP = 0x00010000L

,
            /// <summary>
            /// The window has a minimize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified. 
            /// </summary>
            WS_MINIMIZEBOX = 0x00020000L
,
            /// <summary>
            /// The window has a maximize button. Cannot be combined with the WS_EX_CONTEXTHELP style. The WS_SYSMENU style must also be specified. 
            /// </summary>
            WS_MAXIMIZEBOX = 0x00010000L


,
            /// <summary>
            /// The window is an overlapped window. An overlapped window has a title bar and a border. Same as the WS_OVERLAPPED style. 
            /// </summary>
            WS_TILED = WS_OVERLAPPED
,
            /// <summary>
            /// The window is initially minimized. Same as the WS_MINIMIZE style.
            /// </summary>
            WS_ICONIC = WS_MINIMIZE
,
            /// <summary>
            /// The window has a sizing border. Same as the WS_THICKFRAME style.
            /// </summary>
            WS_SIZEBOX = WS_THICKFRAME
,
            /// <summary>
            /// The window is an overlapped window. Same as the WS_OVERLAPPEDWINDOW style. 
            /// </summary>
            WS_TILEDWINDOW = WS_OVERLAPPEDWINDOW

/*
 * Common Window Styles
 */
,
            /// <summary>
            /// The window is an overlapped window. Same as the WS_TILEDWINDOW style. 
            /// </summary>
            WS_OVERLAPPEDWINDOW = (WS_OVERLAPPED |
                         WS_CAPTION |
                         WS_SYSMENU |
                         WS_THICKFRAME |
                         WS_MINIMIZEBOX |
                         WS_MAXIMIZEBOX)

,
            /// <summary>
            /// The window is a pop-up window. The WS_CAPTION and WS_POPUPWINDOW styles must be combined to make the window menu visible.
            /// </summary>
            WS_POPUPWINDOW = (WS_POPUP |
                         WS_BORDER |
                         WS_SYSMENU)

,
            /// <summary>
            /// Same as the WS_CHILD style.
            /// </summary>
            WS_CHILDWINDOW = (WS_CHILD)
        }

        /// <summary>
        /// The following are the extended window styles.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx
        /// </summary>
        [Flags]
        public enum ExtendedWindowStyles : long
        {
            /// <summary>
            /// The window has a double border; the window can, optionally, be created with a title bar by specifying the WS_CAPTION style in the dwStyle parameter.
            /// </summary>
            WS_EX_DLGMODALFRAME = 0x00000001L
,
            /// <summary>
            /// The child window created with this style does not send the WM_PARENTNOTIFY message to its parent window when it is created or destroyed.
            /// </summary>
            WS_EX_NOPARENTNOTIFY = 0x00000004L
,
            /// <summary>
            /// The window should be placed above all non-topmost windows and should stay above them, even when the window is deactivated. To add or remove this style, use the SetWindowPos function.
            /// </summary>
            WS_EX_TOPMOST = 0x00000008L
,
            /// <summary>
            /// The window accepts drag-drop files.
            /// </summary>
            WS_EX_ACCEPTFILES = 0x00000010L
,
            /// <summary>
            /// The window should not be painted until siblings beneath the window (that were created by the same thread) have been painted. The window appears transparent because the bits of underlying sibling windows have already been painted
            /// To achieve transparency without these restrictions, use the SetWindowRgn function.
            /// </summary>
            WS_EX_TRANSPARENT = 0x00000020L
//(WINVER >= 0x0400)
,
            /// <summary>
            /// The window is a MDI child window.
            /// </summary>
            WS_EX_MDICHILD = 0x00000040L
,
            /// <summary>
            /// The window is intended to be used as a floating toolbar. A tool window has a title bar that is shorter than a normal title bar, and the window title is drawn using a smaller font. A tool window does not appear in the taskbar or in the dialog that appears when the user presses ALT+TAB. If a tool window has a system menu, its icon is not displayed on the title bar. However, you can display the system menu by right-clicking or by typing ALT+SPACE. 
            /// </summary>
            WS_EX_TOOLWINDOW = 0x00000080L
,
            /// <summary>
            /// The window has a border with a raised edge.
            /// </summary>
            WS_EX_WINDOWEDGE = 0x00000100L
,
            /// <summary>
            /// The window has a border with a sunken edge.
            /// </summary>
            WS_EX_CLIENTEDGE = 0x00000200L
,
            /// <summary>
            /// The title bar of the window includes a question mark. When the user clicks the question mark, the cursor changes to a question mark with a pointer. If the user then clicks a child window, the child receives a WM_HELP message. The child window should pass the message to the parent window procedure, which should call the WinHelp function using the HELP_WM_HELP command. The Help application displays a pop-up window that typically contains help for the child window
            /// WS_EX_CONTEXTHELP cannot be used with the WS_MAXIMIZEBOX or WS_MINIMIZEBOX styles.
            /// </summary>
            WS_EX_CONTEXTHELP = 0x00000400L

// END /* WINVER >= 0x0400 */
//(WINVER >= 0x0400)

,
            /// <summary>
            /// The window has generic "right-aligned" properties. This depends on the window class. This style has an effect only if the shell language is Hebrew, Arabic, or another language that supports reading-order alignment; otherwise, the style is ignored
            /// Using the WS_EX_RIGHT style for static or edit controls has the same effect as using the SS_RIGHT or ES_RIGHT style, respectively. Using this style with button controls has the same effect as using BS_RIGHT and BS_RIGHTBUTTON styles. 
            /// </summary>
            WS_EX_RIGHT = 0x00001000L
,
            /// <summary>
            /// The window has generic left-aligned properties. This is the default.
            /// </summary>
            WS_EX_LEFT = 0x00000000L
,
            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading-order alignment, the window text is displayed using right-to-left reading-order properties. For other languages, the style is ignored.
            /// </summary>
            WS_EX_RTLREADING = 0x00002000L
,
            /// <summary>
            /// The window text is displayed using left-to-right reading-order properties. This is the default.
            /// </summary>
            WS_EX_LTRREADING = 0x00000000L
,
            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the vertical scroll bar (if present) is to the left of the client area. For other languages, the style is ignored.
            /// </summary>
            WS_EX_LEFTSCROLLBAR = 0x00004000L
,
            /// <summary>
            /// The vertical scroll bar (if present) is to the right of the client area. This is the default.
            /// </summary>
            WS_EX_RIGHTSCROLLBAR = 0x00000000L

,
            /// <summary>
            /// The window itself contains child windows that should take part in dialog box navigation. If this style is specified, the dialog manager recurses into children of this window when performing navigation operations such as handling the TAB key, an arrow key, or a keyboard mnemonic.
            /// </summary>
            WS_EX_CONTROLPARENT = 0x00010000L
,
            /// <summary>
            /// The window has a three-dimensional border style intended to be used for items that do not accept user input.
            /// </summary>
            WS_EX_STATICEDGE = 0x00020000L
,
            /// <summary>
            /// Forces a top-level window onto the taskbar when the window is visible. 
            /// </summary>
            WS_EX_APPWINDOW = 0x00040000L


,
            /// <summary>
            /// The window is an overlapped window.
            /// </summary>
            WS_EX_OVERLAPPEDWINDOW = (WS_EX_WINDOWEDGE | WS_EX_CLIENTEDGE)
,
            /// <summary>
            /// The window is palette window, which is a modeless dialog box that presents an array of commands. 
            /// </summary>
            WS_EX_PALETTEWINDOW = (WS_EX_WINDOWEDGE | WS_EX_TOOLWINDOW | WS_EX_TOPMOST)

// END /* WINVER >= 0x0400 */

//(_WIN32_WINNT >= 0x0500)
,
            /// <summary>
            /// The window is a layered window. This style cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC
            /// Windows 8:  The WS_EX_LAYERED style is supported for top-level windows and child windows. Previous Windows versions support WS_EX_LAYERED only for top-level windows.
            /// </summary>
            WS_EX_LAYERED = 0x00080000

// END /* _WIN32_WINNT >= 0x0500 */


//(WINVER >= 0x0500)
,
            /// <summary>
            /// The window does not pass its window layout to its child windows.
            /// </summary>
            WS_EX_NOINHERITLAYOUT = 0x00100000L // Disable inheritence of mirroring by children
                                                // END /* WINVER >= 0x0500 */

//(WINVER >= 0x0602)
,
            /// <summary>
            /// The window does not render to a redirection surface. This is for windows that do not have visible content or that use mechanisms other than surfaces to provide their visual.
            /// </summary>
            WS_EX_NOREDIRECTIONBITMAP = 0x00200000L
// END /* WINVER >= 0x0602 */

//(WINVER >= 0x0500)
,
            /// <summary>
            /// If the shell language is Hebrew, Arabic, or another language that supports reading order alignment, the horizontal origin of the window is on the right edge. Increasing horizontal values advance to the left. 
            /// </summary>
            WS_EX_LAYOUTRTL = 0x00400000L // Right to left mirroring
                                          // END /* WINVER >= 0x0500 */

//(_WIN32_WINNT >= 0x0501)
,
            /// <summary>
            ///  Paints all descendants of a window in bottom-to-top painting order using double-buffering. For more information, see Remarks. This cannot be used if the window has a class style of either CS_OWNDC or CS_CLASSDC
            ///  Windows 2000:  This style is not supported.
            /// </summary>
            WS_EX_COMPOSITED = 0x02000000L
// END /* _WIN32_WINNT >= 0x0501 */
//(_WIN32_WINNT >= 0x0500)
,
            /// <summary>
            ///  A top-level window created with this style does not become the foreground window when the user clicks it. The system does not bring this window to the foreground when the user minimizes or closes the foreground window
            ///  To activate the window, use the SetActiveWindow or SetForegroundWindow function.
            ///  The window does not appear on the taskbar by default. To force the window to appear on the taskbar, use the WS_EX_APPWINDOW style.
            /// </summary>
            WS_EX_NOACTIVATE = 0x08000000L
            // END /* _WIN32_WINNT >= 0x0500 */
        }

        /// <summary>
        /// Value indicating what shell event occurred.
        /// Links:
        /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644989(v=vs.85).aspx"/>
        /// <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644991(v=vs.85).aspx"/>
        /// </summary>
        public enum ShellEventType : int
        {
            /// <summary>
            /// A top-level, unowned window has been created. The window exists when the system calls this hook.
            /// Data is: A handle to the window being created.
            /// </summary>
            HSHELL_WINDOWCREATED = 1,
            /// <summary>
            /// A top-level, unowned window is about to be destroyed. The window still exists when the system calls this hook.
            /// Data is: A handle to the top-level window being destroyed.
            /// </summary>
            HSHELL_WINDOWDESTROYED = 2,
            /// <summary>
            /// The shell should activate its main window.
            /// Data is: Not used.
            /// </summary>
            HSHELL_ACTIVATESHELLWINDOW = 3,
            /// <summary>
            /// The activation has changed to a different top-level, unowned window. 
            /// Data is: A handle to the activated window.
            /// 2nd Data is: The value is TRUE if the window is in full-screen mode, or FALSE otherwise. 
            /// </summary>
            HSHELL_WINDOWACTIVATED = 4,
            /// <summary>
            /// A window is being minimized or maximized. The system needs the coordinates of the minimized rectangle for the window. 
            /// Data is: A pointer to a SHELLHOOKINFO structure.
            /// 2nd Data is: A pointer to a RECT structure. 
            /// </summary>
            HSHELL_GETMINRECT = 5,
            /// <summary>
            /// The title of a window in the task bar has been redrawn. 
            /// Data is: A handle to the window that needs to be redrawn.
            /// 2nd Data is: The value is TRUE if the window is flashing, or FALSE otherwise. 
            /// </summary>
            HSHELL_REDRAW = 6,
            /// <summary>
            /// The user has selected the task list. 
            /// Data is: Can be ignored.
            /// </summary>
            HSHELL_TASKMAN = 7,
            /// <summary>
            /// Keyboard language was changed or a new keyboard layout was loaded.
            /// Data is: A handle to the window.
            /// 2nd Data is: A handle to a keyboard layout.
            /// </summary>
            HSHELL_LANGUAGE = 8,
            HSHELL_SYSMENU = 9,
            /// <summary>
            /// Data is: A handle to the window that should be forced to exit.
            /// </summary>
            HSHELL_ENDTASK = 10,
            /// <summary>
            /// The accessibility state has changed. 
            /// Data is: The accessibility state has changed. 
            /// </summary>
            HSHELL_ACCESSIBILITYSTATE = 11,
            /// <summary>
            /// The user completed an input event (for example, pressed an application command button on the mouse or an application command key on the keyboard), and the application did not handle the WM_APPCOMMAND message generated by that input. 
            /// Data is: The APPCOMMAND which has been unhandled by the application or other hooks. See WM_APPCOMMAND and use the GET_APPCOMMAND_Data macro to retrieve this parameter.
            /// 2nd Data is: GET_APPCOMMAND_LPARAM(lParam) is the application command corresponding to the input event.
            ///             GET_DEVICE_LPARAM(lParam) indicates what generated the input event; for example, the mouse or keyboard. For more information, see the uDevice parameter description under WM_APPCOMMAND.
            ///             GET_FLAGS_LPARAM(lParam) depends on the value of cmd in WM_APPCOMMAND. For example, it might indicate which virtual keys were held down when the WM_APPCOMMAND message was originally sent. For more information, see the dwCmdFlags description parameter under WM_APPCOMMAND.
            /// </summary>
            HSHELL_APPCOMMAND = 12,
            /// <summary>
            /// A top-level window is being replaced. The window exists when the system calls this hook. 
            /// Data is: A handle to the window being replaced.
            /// 2nd Data is: A handle to the new window. 
            /// Windows 2000:  Not supported.
            /// </summary>
            HSHELL_WINDOWREPLACED = 13,
            /// <summary>
            /// Data is: A handle to the window replacing the top-level window.
            /// </summary>
            HSHELL_WINDOWREPLACING = 14,
            /// <summary>
            /// Data is: A handle to the window that moved to a different monitor.
            /// 2nd Data is: A handle to the window that moved to a different monitor.
            /// </summary>
            HSHELL_MONITORCHANGED = 16,
            /// <summary>
            /// Data is: A handle to the activated window.
            /// </summary>
            HSHELL_RUDEAPPACTIVATED = 0x8000 | HSHELL_WINDOWACTIVATED,
            /// <summary>
            /// Data is: A handle to the window that needs to be flashed.
            /// </summary>
            HSHELL_FLASH = 0x8000 | HSHELL_REDRAW,
        }

        #endregion Constants

        /// <summary>
        /// Contains window information.
        /// </summary>
        public class WindowInfo
        {
            /// <summary>
            /// The coordinates of the window. 
            /// </summary>
            public System.Drawing.Rectangle WindowRectangle;
            /// <summary>
            /// The coordinates of the client area. 
            /// </summary>
            public System.Drawing.Rectangle ClientRectangle;
            /// <summary>
            /// The window styles. For a table of window styles, see Window Styles <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632600(v=vs.85).aspx"/>. 
            /// </summary>
            public WindowStyles WindowStyle;
            /// <summary>
            /// The extended window styles. For a table of extended window styles, see Extended Window Styles <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx"/>. 
            /// </summary>
            public ExtendedWindowStyles ExtendedWindowStyle;
            /// <summary>
            /// Indicates if the window is active.
            /// </summary>
            public bool IsActive;
            /// <summary>
            /// The width of the window border, in pixels. 
            /// </summary>
            public uint WindowBorderWidth;
            /// <summary>
            /// The height of the window border, in pixels. 
            /// </summary>
            public uint WindowBorderHeight;
            /// <summary>
            /// The window class atom (see RegisterClass <see href="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633586(v=vs.85).aspx"/>). 
            /// </summary>
            public ushort AtomWindowType;
            /// <summary>
            /// The Windows version of the application that created the window. 
            /// </summary>
            public ushort CreatorWindowsVersion;

            /// <summary>
            /// EXPERIMENTAL: Does not work correctly.
            /// </summary>
            public bool IsVisibleInTaskbar
            {
                get
                {
                    if (ExtendedWindowStyle.HasFlag(ExtendedWindowStyles.WS_EX_APPWINDOW) && WindowStyle.HasFlag(WindowStyles.WS_VISIBLE))
                        return true;

                    if (ExtendedWindowStyle.HasFlag(ExtendedWindowStyles.WS_EX_NOACTIVATE) && !ExtendedWindowStyle.HasFlag(ExtendedWindowStyles.WS_EX_APPWINDOW))
                        return false;

                    if (ExtendedWindowStyle.HasFlag(ExtendedWindowStyles.WS_EX_TOOLWINDOW))
                        return false;

                    if (WindowStyle.HasFlag(WindowStyles.WS_VISIBLE))
                        return true;

                    return false;
                }
            }
        }

        // This is a Native binding but we want to expose the values so we moved it here:
        /// <summary>
        /// Value to pass to ShowWindow.
        /// https://msdn.microsoft.com/en-us/library/windows/desktop/ms633548(v=vs.85).aspx
        /// </summary>
        public enum ShowCommand : int
        {
            /// <summary>
            /// Hides the window and activates another window.
            /// </summary>
            SW_HIDE = 0,
            /// <summary>
            /// Activates and displays a window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when displaying the window for the first time.
            /// </summary>
            SW_SHOWNORMAL = 1,
            /// <summary>
            /// Activates the window and displays it as a minimized window.
            /// </summary>
            SW_SHOWMINIMIZED = 2,
            /// <summary>
            /// Maximizes the specified window.
            /// </summary>
            SW_MAXIMIZE = 3,
            /// <summary>
            /// Activates the window and displays it as a maximized window.
            /// </summary>
            SW_SHOWMAXIMIZED = 3,
            /// <summary>
            /// Displays a window in its most recent size and position. This value is similar to SW_SHOWNORMAL, except that the window is not activated.
            /// </summary>
            SW_SHOWNOACTIVATE = 4,
            /// <summary>
            /// Activates the window and displays it in its current size and position. 
            /// </summary>
            SW_SHOW = 5,
            /// <summary>
            /// Minimizes the specified window and activates the next top-level window in the Z order.
            /// </summary>
            SW_MINIMIZE = 6,
            /// <summary>
            /// Displays the window as a minimized window. This value is similar to SW_SHOWMINIMIZED, except the window is not activated.
            /// </summary>
            SW_SHOWMINNOACTIVE = 7,
            /// <summary>
            /// Displays the window in its current size and position. This value is similar to SW_SHOW, except that the window is not activated.
            /// </summary>
            SW_SHOWNA = 8,
            /// <summary>
            /// Activates and displays the window. If the window is minimized or maximized, the system restores it to its original size and position. An application should specify this flag when restoring a minimized window.
            /// </summary>
            SW_RESTORE = 9,
            /// <summary>
            /// Sets the show state based on the SW_ value specified in the STARTUPINFO structure passed to the CreateProcess function by the program that started the application. 
            /// </summary>
            SW_SHOWDEFAULT = 10,
            /// <summary>
            /// Minimizes a window, even if the thread that owns the window is not responding. This flag should only be used when minimizing windows from a different thread.
            /// </summary>
            SW_FORCEMINIMIZE = 11,
        }

        private static class Native
        {
            #region Data Structures

            /// <summary>
            /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
            /// 
            /// The Win32 RECT structure is not compatible with the .NET System.Drawing.Rectangle structure.
            /// The RECT structure has left, top, right and bottom members,
            /// but the System.Drawing.Rectangle structure has left, top, width and height members internally.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct RECT
            {
                public int Left, Top, Right, Bottom;

                public RECT(int left, int top, int right, int bottom)
                {
                    Left = left;
                    Top = top;
                    Right = right;
                    Bottom = bottom;
                }

                public RECT(System.Drawing.Rectangle r) : this(r.Left, r.Top, r.Right, r.Bottom) { }

                public int X
                {
                    get { return Left; }
                    set { Right -= (Left - value); Left = value; }
                }

                public int Y
                {
                    get { return Top; }
                    set { Bottom -= (Top - value); Top = value; }
                }

                public int Height
                {
                    get { return Bottom - Top; }
                    set { Bottom = value + Top; }
                }

                public int Width
                {
                    get { return Right - Left; }
                    set { Right = value + Left; }
                }

                public System.Drawing.Point Location
                {
                    get { return new System.Drawing.Point(Left, Top); }
                    set { X = value.X; Y = value.Y; }
                }

                public System.Drawing.Size Size
                {
                    get { return new System.Drawing.Size(Width, Height); }
                    set { Width = value.Width; Height = value.Height; }
                }

                public static implicit operator System.Drawing.Rectangle(RECT r)
                {
                    return new System.Drawing.Rectangle(r.Left, r.Top, r.Width, r.Height);
                }

                public static implicit operator RECT(System.Drawing.Rectangle r)
                {
                    return new RECT(r);
                }

                public static bool operator ==(RECT r1, RECT r2)
                {
                    return r1.Equals(r2);
                }

                public static bool operator !=(RECT r1, RECT r2)
                {
                    return !r1.Equals(r2);
                }

                public bool Equals(RECT r)
                {
                    return r.Left == Left && r.Top == Top && r.Right == Right && r.Bottom == Bottom;
                }

                public override bool Equals(object obj)
                {
                    if (obj is RECT)
                        return Equals((RECT)obj);
                    else if (obj is System.Drawing.Rectangle)
                        return Equals(new RECT((System.Drawing.Rectangle)obj));
                    return false;
                }

                public override int GetHashCode()
                {
                    return ((System.Drawing.Rectangle)this).GetHashCode();
                }

                public override string ToString()
                {
                    return string.Format(System.Globalization.CultureInfo.CurrentCulture, "{{Left={0},Top={1},Right={2},Bottom={3}}}", Left, Top, Right, Bottom);
                }
            }

            /// <summary>
            /// The WINDOWINFO structure contains window information.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct WINDOWINFO
            {
                /// <summary>
                /// The size of the structure, in bytes. The caller must set this member to sizeof(WINDOWINFO). 
                /// </summary>
                public uint cbSize;
                /// <summary>
                /// The coordinates of the window. 
                /// </summary>
                public RECT rcWindow;
                /// <summary>
                /// The coordinates of the client area. 
                /// </summary>
                public RECT rcClient;
                /// <summary>
                /// The window styles. For a table of window styles, see Window Styles <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms632600(v=vs.85).aspx"/>. 
                /// </summary>
                public uint dwStyle;
                /// <summary>
                /// The extended window styles. For a table of extended window styles, see Extended Window Styles <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ff700543(v=vs.85).aspx"/>. 
                /// </summary>
                public uint dwExStyle;
                /// <summary>
                /// The window status. If this member is WS_ACTIVECAPTION (0x0001), the window is active. Otherwise, this member is zero. 
                /// </summary>
                public uint dwWindowStatus;
                /// <summary>
                /// The width of the window border, in pixels. 
                /// </summary>
                public uint cxWindowBorders;
                /// <summary>
                /// The height of the window border, in pixels. 
                /// </summary>
                public uint cyWindowBorders;
                /// <summary>
                /// The window class atom (see RegisterClass <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms633586(v=vs.85).aspx"/>). 
                /// </summary>
                public ushort atomWindowType;
                /// <summary>
                /// The Windows version of the application that created the window. 
                /// </summary>
                public ushort wCreatorVersion;

                public WINDOWINFO(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
                {
                    cbSize = (UInt32)(Marshal.SizeOf(typeof(WINDOWINFO)));
                }

                public static implicit operator WindowInfo(WINDOWINFO w)
                {
                    WindowInfo info = new WindowInfo
                    {
                        WindowRectangle = w.rcWindow,
                        ClientRectangle = w.rcClient,
                        WindowStyle = (WindowStyles)w.dwStyle,
                        ExtendedWindowStyle = (ExtendedWindowStyles)w.dwExStyle,
                        IsActive = w.dwWindowStatus != 0,
                        WindowBorderWidth = w.cxWindowBorders,
                        WindowBorderHeight = w.cyWindowBorders,
                        AtomWindowType = w.atomWindowType,
                        CreatorWindowsVersion = w.wCreatorVersion
                    };

                    return info;
                }
            }

            /// <summary>
            /// Contains the flash status for a window and the number of times the system should flash the window.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            public struct FLASHWINFO
            {
                [Flags]
                public enum Flags : uint
                {
                    /// <summary>
                    /// Stop flashing. The system restores the window to its original state. 
                    /// </summary>    
                    FLASHW_STOP = 0,

                    /// <summary>
                    /// Flash the window caption 
                    /// </summary>
                    FLASHW_CAPTION = 1,

                    /// <summary>
                    /// Flash the taskbar button. 
                    /// </summary>
                    FLASHW_TRAY = 2,

                    /// <summary>
                    /// Flash both the window caption and taskbar button.
                    /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
                    /// </summary>
                    FLASHW_ALL = 3,

                    /// <summary>
                    /// Flash continuously, until the FLASHW_STOP flag is set.
                    /// </summary>
                    FLASHW_TIMER = 4,

                    /// <summary>
                    /// Flash continuously until the window comes to the foreground. 
                    /// </summary>
                    FLASHW_TIMERNOFG = 12
                }

                /// <summary>
                /// The size of the structure, in bytes.
                /// </summary>
                public uint cbSize;
                /// <summary>
                /// A handle to the window to be flashed. The window can be either opened or minimized.
                /// </summary>
                public IntPtr hwnd;
                /// <summary>
                /// The flash status. This parameter can be one or more of the "Flags" values. 
                /// </summary>
                public uint dwFlags;
                /// <summary>
                /// The number of times to flash the window.
                /// </summary>
                public uint uCount;
                /// <summary>
                /// The rate at which the window is to be flashed, in milliseconds. If dwTimeout is zero, the function uses the default cursor blink rate.
                /// </summary>
                public uint dwTimeout;

                public FLASHWINFO(Boolean? filler) : this()   // Allows automatic initialization of "cbSize" with "new WINDOWINFO(null/true/false)".
                {
                    cbSize = (UInt32)(Marshal.SizeOf(typeof(FLASHWINFO)));
                }
            }

            #endregion Data Structures


            #region Foreground Window

            [DllImport("user32.dll")]
            public static extern IntPtr GetForegroundWindow();

            [DllImport("User32.dll")]
            public static extern bool SetForegroundWindow(IntPtr handle);

            #endregion Foreground Window


            #region Move Window

            /// <summary>
            ///     The MoveWindow function changes the position and dimensions of the specified window. For a top-level window, the
            ///     position and dimensions are relative to the upper-left corner of the screen. For a child window, they are relative
            ///     to the upper-left corner of the parent window's client area.
            ///     <para>
            ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633534%28v=vs.85%29.aspx for more
            ///     information
            ///     </para>
            /// </summary>
            /// <param name="hWnd">C++ ( hWnd [in]. Type: HWND )<br /> Handle to the window.</param>
            /// <param name="X">C++ ( X [in]. Type: int )<br />Specifies the new position of the left side of the window.</param>
            /// <param name="Y">C++ ( Y [in]. Type: int )<br /> Specifies the new position of the top of the window.</param>
            /// <param name="nWidth">C++ ( nWidth [in]. Type: int )<br />Specifies the new width of the window.</param>
            /// <param name="nHeight">C++ ( nHeight [in]. Type: int )<br />Specifies the new height of the window.</param>
            /// <param name="bRepaint">
            ///     C++ ( bRepaint [in]. Type: bool )<br />Specifies whether the window is to be repainted. If this
            ///     parameter is TRUE, the window receives a message. If the parameter is FALSE, no repainting of any kind occurs. This
            ///     applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the
            ///     parent window uncovered as a result of moving a child window.
            /// </param>
            /// <returns>
            ///     If the function succeeds, the return value is nonzero.<br /> If the function fails, the return value is zero.
            ///     <br />To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            internal static extern bool MoveWindow(IntPtr hWnd, int X, int Y, int nWidth, int nHeight, bool bRepaint);

            #endregion Move Window


            #region Arrange Windows

            /// <summary>
            /// Tiles the specified child windows of the specified parent window.
            /// 
            /// Remarks:
            /// Calling TileWindows causes all maximized windows to be restored to their previous size. 
            /// </summary>
            /// <param name="hwndParent">A handle to the parent window. If this parameter is NULL, the desktop window is assumed.</param>
            /// <param name="wHow">The tiling flags. This parameter can be one of the following values—optionally combined with MDITILE_SKIPDISABLED to prevent disabled MDI child windows from being tiled.</param>
            /// <param name="lpRect">A pointer to a structure that specifies the rectangular area, in client coordinates, within which the windows are arranged. If this parameter is NULL, the client area of the parent window is used.</param>
            /// <param name="cKids">The number of elements in the array specified by the lpKids parameter. This parameter is ignored if lpKids is NULL.</param>
            /// <param name="lpKids">An array of handles to the child windows to arrange. If a specified child window is a top-level window with the style WS_EX_TOPMOST or WS_EX_TOOLWINDOW, the child window is not arranged. If this parameter is NULL, all child windows of the specified parent window (or of the desktop window) are arranged.</param>
            /// <returns>
            /// If the function succeeds, the return value is the number of windows arranged.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern ushort TileWindows(IntPtr hwndParent, ArrangeFlags wHow, IntPtr lpRect, uint cKids, IntPtr lpKids);

            /// <summary>
            /// Cascades the specified child windows of the specified parent window.
            /// 
            /// Remarks:
            /// By default, CascadeWindows arranges the windows in the order provided by the lpKids array, but preserves the Z-Order. If you specify the MDITILE_ZORDER flag, CascadeWindows arranges the windows in Z order.
            /// Calling CascadeWindows causes all maximized windows to be restored to their previous size. 
            /// </summary>
            /// <param name="hwndParent">A handle to the parent window. If this parameter is NULL, the desktop window is assumed.</param>
            /// <param name="wHow">A cascade flag. This parameter can be one or more of the following values.</param>
            /// <param name="lpRect">A pointer to a structure that specifies the rectangular area, in client coordinates, within which the windows are arranged. This parameter can be NULL, in which case the client area of the parent window is used.</param>
            /// <param name="cKids">The number of elements in the array specified by the lpKids parameter. This parameter is ignored if lpKids is NULL.</param>
            /// <param name="lpKids">An array of handles to the child windows to arrange. If a specified child window is a top-level window with the style WS_EX_TOPMOST or WS_EX_TOOLWINDOW, the child window is not arranged. If this parameter is NULL, all child windows of the specified parent window (or of the desktop window) are arranged.</param>
            /// <returns>
            /// If the function succeeds, the return value is the number of windows arranged.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern ushort CascadeWindows(IntPtr hwndParent, ArrangeFlags wHow, IntPtr lpRect, uint cKids, IntPtr lpKids);

            /// <summary>
            /// Used with TileWindows and CascadeWindows.
            /// For TileWindows: either vertical or horizontal, not both. ZOrder can't be used.
            /// For CascadeWindows: vertical or horizontal flags can't be used.
            /// </summary>
            public enum ArrangeFlags : uint
            {
                /// <summary>
                /// Tiles windows vertically.
                /// </summary>
                MDITILE_VERTICAL = 0x0000,
                /// <summary>
                /// Tiles windows horizontally.
                /// </summary>
                MDITILE_HORIZONTAL = 0x0001,
                /// <summary>
                /// Prevent disabled MDI child windows from being tiled. 
                /// </summary>
                MDITILE_SKIPDISABLED = 0x0002,
                /// <summary>
                /// Arranges the windows in Z order. If this value is not specified, the windows are arranged using the order specified in the lpKids array.
                /// </summary>
                MDITILE_ZORDER = 0x0004,
            }

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool BringWindowToTop(IntPtr hwnd);

            #endregion Arrange Windows


            #region Show/Hide Window

            /// <summary>
            /// <para>Sets the show state of a window without waiting for the operation to complete.</para>
            /// <para>Remarks:</para>
            /// <para>This function posts a show-window event to the message queue of the given window. An application can use this function to avoid becoming nonresponsive while waiting for a nonresponsive application to finish processing a show-window event.</para>
            /// </summary>
            /// <param name="hWnd">A handle to the window.</param>
            /// <param name="nCmdShow">Controls how the window is to be shown. For a list of possible values, see the description of the ShowWindow function.</param>
            /// <returns>If the operation was successfully started, the return value is nonzero.</returns>
            [DllImport("user32.dll")]
            public static extern bool ShowWindowAsync(IntPtr hWnd, ShowCommand nCmdShow);

            /// <summary>
            /// Sets the specified window's show state.
            /// Remarks:
            /// To perform certain special effects when showing or hiding a window, use AnimateWindow.
            /// The first time an application calls ShowWindow, it should use the WinMain function's nCmdShow parameter as its nCmdShow parameter. Subsequent calls to ShowWindow must use one of the values in the given list, instead of the one specified by the WinMain function's nCmdShow parameter.
            /// As noted in the discussion of the nCmdShow parameter, the nCmdShow value is ignored in the first call to ShowWindow if the program that launched the application specifies startup information in the structure. In this case, ShowWindow uses the information specified in the STARTUPINFO structure to show the window. On subsequent calls, the application must call ShowWindow with nCmdShow set to SW_SHOWDEFAULT to use the startup information provided by the program that launched the application. This behavior is designed for the following situations:
            /// # Applications create their main window by calling CreateWindow with the WS_VISIBLE flag set.
            /// # Applications create their main window by calling CreateWindow with the WS_VISIBLE flag cleared, and later call ShowWindow with the SW_SHOW flag set to make it visible.
            /// </summary>
            /// <param name="handle">A handle to the window.</param>
            /// <param name="nCmdShow">Controls how the window is to be shown. This parameter is ignored the first time an application calls ShowWindow, if the program that launched the application provides a STARTUPINFO structure. Otherwise, the first time ShowWindow is called, the value should be the value obtained by the WinMain function in its nCmdShow parameter. In subsequent calls, this parameter can be one of the following values.</param>
            /// <returns>
            /// If the window was previously visible, the return value is nonzero.
            /// If the window was previously hidden, the return value is zero. 
            /// </returns>
            [DllImport("User32.dll")]
            public static extern bool ShowWindow(IntPtr hWnd, ShowCommand nCmdShow);

            /// <summary>
            /// Enables you to produce special effects when showing or hiding windows. There are four types of animation: roll, slide, collapse or expand, and alpha-blended fade. 
            /// 
            /// Remarks:
            /// To show or hide a window without special effects, use ShowWindow.
            /// When using slide or roll animation, you must specify the direction. It can be either AW_HOR_POSITIVE, AW_HOR_NEGATIVE, AW_VER_POSITIVE, or AW_VER_NEGATIVE.
            /// You can combine AW_HOR_POSITIVE or AW_HOR_NEGATIVE with AW_VER_POSITIVE or AW_VER_NEGATIVE to animate a window diagonally.
            /// The window procedures for the window and its child windows should handle any WM_PRINT or WM_PRINTCLIENT messages. Dialog boxes, controls, and common controls already handle WM_PRINTCLIENT. The default window procedure already handles WM_PRINT.
            /// If a child window is displayed partially clipped, when it is animated it will have holes where it is clipped.
            /// AnimateWindow supports RTL windows.
            /// Avoid animating a window that has a drop shadow because it produces visually distracting, jerky animations. 
            /// 
            /// Comment:
            /// AnimateWindow doesn't actually show the window so make sure you call Show() or set this.Visible = true after calling AnimateWindow
            /// </summary>
            /// <param name="hwnd">A handle to the window to animate. The calling thread must own this window.</param>
            /// <param name="dwTime">The time it takes to play the animation, in milliseconds. Typically, an animation takes 200 milliseconds to play.</param>
            /// <param name="dwFlags">The type of animation. This parameter can be one or more of the following values. Note that, by default, these flags take effect when showing a window. To take effect when hiding a window, use AW_HIDE and a logical OR operator with the appropriate flags.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. The function will fail in the following situations:
            /// # If the window is already visible and you are trying to show the window.
            /// # If the window is already hidden and you are trying to hide the window.
            /// # If there is no direction specified for the slide or roll animation.
            /// # When trying to animate a child window with AW_BLEND.
            /// # If the thread does not own the window. Note that, in this case, AnimateWindow fails but GetLastError returns ERROR_SUCCESS.
            /// To get extended error information, call the GetLastError function. 
            /// </returns>
            [DllImport("User32.dll", SetLastError = true)]
            public static extern bool AnimateWindow(IntPtr hwnd, uint dwTime, AnimateWindowFlags dwFlags);

            /// <summary>
            /// Flags used to specify the type of animation for AnimateWindow.
            /// </summary>
            [Flags]
            public enum AnimateWindowFlags : uint
            {
                /// <summary>
                /// Animates the window from left to right. This flag can be used with roll or slide animation. It is ignored when used with AW_CENTER or AW_BLEND.
                /// </summary>
                AW_HOR_POSITIVE = 0x00000001,
                /// <summary>
                /// Animates the window from right to left. This flag can be used with roll or slide animation. It is ignored when used with AW_CENTER or AW_BLEND.
                /// </summary>
                AW_HOR_NEGATIVE = 0x00000002,
                /// <summary>
                /// Animates the window from top to bottom. This flag can be used with roll or slide animation. It is ignored when used with AW_CENTER or AW_BLEND. 
                /// </summary>
                AW_VER_POSITIVE = 0x00000004,
                /// <summary>
                /// Animates the window from bottom to top. This flag can be used with roll or slide animation. It is ignored when used with AW_CENTER or AW_BLEND. 
                /// </summary>
                AW_VER_NEGATIVE = 0x00000008,

                /// <summary>
                /// Makes the window appear to collapse inward if AW_HIDE is used or expand outward if the AW_HIDE is not used. The various direction flags have no effect.
                /// </summary>
                AW_CENTER = 0x00000010,
                /// <summary>
                /// Hides the window. By default, the window is shown. 
                /// </summary>
                AW_HIDE = 0x00010000,
                /// <summary>
                /// Activates the window. Do not use this value with AW_HIDE.
                /// </summary>
                AW_ACTIVATE = 0x00020000,
                /// <summary>
                /// Uses slide animation. By default, roll animation is used. This flag is ignored when used with AW_CENTER.
                /// </summary>
                AW_SLIDE = 0x00040000,
                /// <summary>
                /// Uses a fade effect. This flag can be used only if hwnd is a top-level window. 
                /// </summary>
                AW_BLEND = 0x00080000,
            }


            /// <summary>
            /// Determines whether the specified window is minimized (iconic).
            /// </summary>
            /// <param name="handle">A handle to the window to be tested.</param>
            /// <returns>If the window is iconic, the return value is nonzero.
            /// If the window is not iconic, the return value is zero.</returns>
            [DllImport("User32.dll")]
            public static extern bool IsIconic(IntPtr handle);

            /// <summary>
            /// Restores a minimized (iconic) window to its previous size and position; it then activates the window. 
            /// 
            /// Remarks:
            /// OpenIcon sends a WM_QUERYOPEN message to the given window. 
            /// </summary>
            /// <param name="hWnd">A handle to the window to be restored and activated.</param>
            /// <returns>
            /// <para>If the function succeeds, the return value is nonzero.</para>
            /// <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool OpenIcon(IntPtr hWnd);

            #endregion Show/Hide Window


            #region Flash Window

            /// <summary>
            /// <para>Flashes the specified window. It does not change the active state of the window.</para>
            /// <para></para>
            /// <para>Remarks:</para>
            /// <para>Typically, you flash a window to inform the user that the window requires attention but does not currently have the keyboard focus. When a window flashes, it appears to change from inactive to active status. An inactive caption bar changes to an active caption bar; an active caption bar changes to an inactive caption bar.</para>
            /// </summary>
            /// <param name="pwfi">A pointer to a FLASHWINFO structure.</param>
            /// <returns>The return value specifies the window's state before the call to the FlashWindowEx function. If the window caption was drawn as active before the call, the return value is nonzero. Otherwise, the return value is zero.</returns>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FlashWindowEx(ref FLASHWINFO pwfi);

            /// <summary>
            /// Flashes the specified window one time. It does not change the active state of the window.
            /// To flash the window a specified number of times, use the FlashWindowEx function.
            /// 
            /// Remarks:
            /// Flashing a window means changing the appearance of its caption bar as if the window were changing from inactive to active status, or vice versa. (An inactive caption bar changes to an active caption bar; an active caption bar changes to an inactive caption bar.)
            /// Typically, a window is flashed to inform the user that the window requires attention but that it does not currently have the keyboard focus.
            /// The FlashWindow function flashes the window only once; for repeated flashing, the application should create a system timer.
            /// </summary>
            /// <param name="hWnd">A handle to the window to be flashed. The window can be either open or minimized.</param>
            /// <param name="bInvert">
            /// If this parameter is TRUE, the window is flashed from one state to the other. If it is FALSE, the window is returned to its original state (either active or inactive).
            /// When an application is minimized and this parameter is TRUE, the taskbar window button flashes active/inactive. If it is FALSE, the taskbar window button flashes inactive, meaning that it does not change colors. It flashes, as if it were being redrawn, but it does not provide the visual invert clue to the user.
            /// </param>
            /// <returns>The return value specifies the window's state before the call to the FlashWindow function. If the window caption was drawn as active before the call, the return value is nonzero. Otherwise, the return value is zero.</returns>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool FlashWindow(IntPtr hWnd, bool bInvert);

            #endregion Flash Window


            #region Close Window

            /// <summary>
            /// <para>Destroys the specified window. The function sends WM_DESTROY and WM_NCDESTROY messages to the window to deactivate it and remove the keyboard focus from it. The function also destroys the window's menu, flushes the thread message queue, destroys timers, removes clipboard ownership, and breaks the clipboard viewer chain (if the window is at the top of the viewer chain).</para>
            /// <para>If the specified window is a parent or owner window, DestroyWindow automatically destroys the associated child or owned windows when it destroys the parent or owner window. The function first destroys child or owned windows, and then it destroys the parent or owner window.</para>
            /// <para>DestroyWindow also destroys modeless dialog boxes created by the CreateDialog function.</para>
            /// <para>Remarks:</para>
            /// <para>A thread cannot use DestroyWindow to destroy a window created by a different thread.</para>
            /// <para>If the window being destroyed is a child window that does not have the WS_EX_NOPARENTNOTIFY style, a WM_PARENTNOTIFY message is sent to the parent.</para>
            /// </summary>
            /// <param name="hwnd">Handle to the window to be destroyed.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero. 
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool DestroyWindow(IntPtr hwnd);

            #endregion Close Window


            #region Window Info

            /// <summary>
            /// Retrieves information about the specified window.
            /// </summary>
            /// <param name="hwnd">A handle to the window whose information is to be retrieved. </param>
            /// <param name="pwi">A pointer to a WINDOWINFO structure to receive the information. Note that you must set the cbSize member to sizeof(WINDOWINFO) before calling this function.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. 
            /// To get extended error information, call GetLastError. 
            /// </returns>
            [return: MarshalAs(UnmanagedType.Bool)]
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowInfo(IntPtr hwnd, ref WINDOWINFO pwi);

            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool GetWindowRect(IntPtr hwnd, out RECT lpRect);

            public enum DwmWindowAttribute
            {
                DWMWA_NCRENDERING_ENABLED = 1,
                DWMWA_NCRENDERING_POLICY,
                DWMWA_TRANSITIONS_FORCEDISABLED,
                DWMWA_ALLOW_NCPAINT,
                DWMWA_CAPTION_BUTTON_BOUNDS,
                DWMWA_NONCLIENT_RTL_LAYOUT,
                DWMWA_FORCE_ICONIC_REPRESENTATION,
                DWMWA_FLIP3D_POLICY,
                DWMWA_EXTENDED_FRAME_BOUNDS,
                DWMWA_HAS_ICONIC_BITMAP,
                DWMWA_DISALLOW_PEEK,
                DWMWA_EXCLUDED_FROM_PEEK,
                DWMWA_CLOAK,
                DWMWA_CLOAKED,
                DWMWA_FREEZE_REPRESENTATION,
                DWMWA_LAST
            };

            [DllImport("dwmapi.dll")]
            public static extern int DwmGetWindowAttribute(IntPtr hWnd, DwmWindowAttribute dwAttribute, out RECT lpRect, int cbAttribute);

            #endregion Window Info


            #region Get Window Process

            /// <summary>
            /// Retrieves the identifier of the thread that created the specified window and, optionally, the identifier of the process that created the window. 
            /// </summary>
            /// <param name="hWnd">A handle to the window.</param>
            /// <param name="lpdwProcessId">A pointer to a variable that receives the process identifier. If this parameter is not NULL, GetWindowThreadProcessId copies the identifier of the process to the variable; otherwise, it does not.</param>
            /// <returns>The identifier of the thread that created the window.</returns>
            [DllImport("user32.dll")]
            public static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

            #endregion Get Window Process


            #region Title Text

            /// <summary>
            /// Retrieves the length, in characters, of the specified window's title bar text (if the window has a title bar). If the specified window is a control, the function retrieves the length of the text within the control. However, GetWindowTextLength cannot retrieve the length of the text of an edit control in another application.
            /// </summary>
            /// <param name="hWnd">A handle to the window or control.</param>
            /// <returns>If the function succeeds, the return value is the length, in characters, of the text. Under certain conditions, this value may actually be greater than the length of the text. For more information, see the following Remarks section.
            ///
            /// If the window has no text, the return value is zero.To get extended error information, call GetLastError. </returns>
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowTextLength(IntPtr hWnd);

            /// <summary>
            /// Copies the text of the specified window's title bar (if it has one) into a buffer. If the specified window is a control, the text of the control is copied. However, GetWindowText cannot retrieve the text of a control in another application.
            /// </summary>
            /// <param name="hWnd">A handle to the window or control containing the text.</param>
            /// <param name="lpString">The buffer that will receive the text. If the string is as long or longer than the buffer, the string is truncated and terminated with a null character.</param>
            /// <param name="nMaxCount">The maximum number of characters to copy to the buffer, including the null character. If the text exceeds this limit, it is truncated.</param>
            /// <returns>If the function succeeds, the return value is the length, in characters, of the copied string, not including the terminating null character. 
            /// If the window has no title bar or text, if the title bar is empty, or if the window or control handle is invalid, the return value is zero. To get extended error information, call GetLastError.
            ///
            /// This function cannot retrieve the text of an edit control in another application.</returns>
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetWindowText(IntPtr hWnd, StringBuilder lpString, int nMaxCount);

            #endregion Title Text


            #region Class Name

            /// <summary>
            /// Retrieves the name of the class to which the specified window belongs.
            /// </summary>
            /// <param name="hWnd">A handle to the window and, indirectly, the class to which the window belongs.</param>
            /// <param name="lpClassName">The class name string.</param>
            /// <param name="nMaxCount">The length of the lpClassName buffer, in characters. The buffer must be large enough to include the terminating null character; otherwise, the class name string is truncated to nMaxCount-1 characters.</param>
            /// <returns>
            /// <para>If the function succeeds, the return value is the number of characters copied to the buffer, not including the terminating null character.</para>
            /// <para>If the function fails, the return value is zero. To get extended error information, call GetLastError.</para>
            /// </returns>
            [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto)]
            public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

            #endregion Class Name


            #region Get Window

            /// <summary>
            /// <para>Retrieves a handle to the top-level window whose class name and window name match the specified strings. This function does not search child windows. This function does not perform a case-sensitive search.</para>
            /// <para>To search child windows, beginning with a specified child window, use the FindWindowEx function.</para>
            /// </summary>
            /// <param name="lpClassName">Should use the form's class and not name as forms sometimes do not have titles or even the controlbox. Use Spy++ to dig deeper. (Can be null to ignore)</param>
            /// <param name="lpWindowName">Title of the wanted form. (If this parameter is NULL, all window names match.)</param>
            /// <returns>Handle for the found form. NULL if function failed. To get extended error information, call GetLastError.</returns>
            [DllImport("USER32.DLL", CharSet = CharSet.Unicode, SetLastError = true)]
            public static extern IntPtr FindWindow(String lpClassName, String lpWindowName);

            /// <summary>
            /// <para>Retrieves a handle to a window whose class name and window name match the specified strings. The function searches child windows, beginning with the one following the specified child window. This function does not perform a case-sensitive search.</para>
            /// <para>Remarks:</para>
            /// <para>If the lpszWindow parameter is not NULL, FindWindowEx calls the GetWindowText function to retrieve the window name for comparison. For a description of a potential problem that can arise, see the Remarks section of GetWindowText.</para>
            /// <para>An application can call this function in the following way: FindWindowEx( NULL, NULL, MAKEINTATOM(0x8000), NULL );</para>
            /// <para>Note that 0x8000 is the atom for a menu class. When an application calls this function, the function checks whether a context menu is being displayed that the application created.</para>
            /// </summary>
            /// <param name="hwndParent">
            /// <para>A handle to the parent window whose child windows are to be searched.</para>
            /// <para>If hwndParent is NULL, the function uses the desktop window as the parent window. The function searches among windows that are child windows of the desktop. </para>
            /// <para>If hwndParent is HWND_MESSAGE, the function searches all message-only windows.</para>
            /// </param>
            /// <param name="hwndChildAfter">
            /// <para>A handle to a child window. The search begins with the next child window in the Z order. The child window must be a direct child window of hwndParent, not just a descendant window. </para>
            /// <para>If hwndChildAfter is NULL, the search begins with the first child window of hwndParent. </para>
            /// <para>Note that if both hwndParent and hwndChildAfter are NULL, the function searches all top-level and message-only windows. </para>
            /// </param>
            /// <param name="lpszClass">
            /// <para>Note that if both hwndParent and hwndChildAfter are NULL, the function searches all top-level and message-only windows. </para>
            /// <para>If lpszClass is a string, it specifies the window class name. The class name can be any name registered with RegisterClass or RegisterClassEx, or any of the predefined control-class names, or it can be MAKEINTATOM(0x8000). In this latter case, 0x8000 is the atom for a menu class. For more information, see the Remarks section of this topic.</para>
            /// </param>
            /// <param name="lpszWindow">The window name (the window's title). If this parameter is NULL, all window names match.</param>
            /// <returns>
            /// <para>If the function succeeds, the return value is a handle to the window that has the specified class and window names.</para>
            /// <para>If the function fails, the return value is NULL. To get extended error information, call GetLastError.</para>
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr FindWindowEx(IntPtr hwndParent, IntPtr hwndChildAfter, string lpszClass, string lpszWindow);


            /// <summary>
            /// <para>Retrieves a handle to the window that contains the specified point.</para>
            /// <para>Remarks:</para>
            /// <para>The WindowFromPoint function does not retrieve a handle to a hidden or disabled window, even if the point is within the window. An application should use the ChildWindowFromPoint function for a nonrestrictive search.</para>
            /// </summary>
            /// <param name="Point">The point to be checked.</param>
            /// <returns>The return value is a handle to the window that contains the point. If no window exists at the given point, the return value is NULL. If the point is over a static text control, the return value is a handle to the window under the static text control.</returns>
            [DllImport("user32.dll")]
            public static extern IntPtr WindowFromPoint(System.Drawing.Point Point);


            /// <summary>
            /// Enumerates all nonchild windows associated with a thread by passing the handle to each window, in turn, to an application-defined callback function. EnumThreadWindows continues until the last window is enumerated or the callback function returns FALSE. To enumerate child windows of a particular window, use the EnumChildWindows function.
            /// </summary>
            /// <param name="dwThreadId">The identifier of the thread whose windows are to be enumerated.</param>
            /// <param name="lpfn">A pointer to an application-defined callback function. For more information, see EnumThreadWndProc.</param>
            /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
            /// <returns>If the callback function returns TRUE for all windows in the thread specified by dwThreadId, the return value is TRUE. If the callback function returns FALSE on any enumerated window, or if there are no windows found in the thread specified by dwThreadId, the return value is FALSE.</returns>
            [DllImport("user32.dll")]
            public static extern bool EnumThreadWindows(uint dwThreadId, EnumThreadWndProc lpfn, IntPtr lParam);

            /// <summary>
            /// An application-defined callback function used with the EnumThreadWindows function. It receives the window handles associated with a thread. The WNDENUMPROC type defines a pointer to this callback function. EnumThreadWndProc is a placeholder for the application-defined function name. 
            /// Remarks:
            /// An application must register this callback function by passing its address to the EnumThreadWindows function. 
            /// </summary>
            /// <param name="hWnd">A handle to a window associated with the thread specified in the EnumThreadWindows function.</param>
            /// <param name="lParam">The application-defined value given in the EnumThreadWindows function.</param>
            /// <returns>To continue enumeration, the callback function must return TRUE; to stop enumeration, it must return FALSE.</returns>
            public delegate bool EnumThreadWndProc(IntPtr hWnd, IntPtr lParam);


            /// <summary>
            /// Retrieves a handle to the desktop window. The desktop window covers the entire screen. The desktop window is the area on top of which other windows are painted. 
            /// </summary>
            /// <returns>The return value is a handle to the desktop window.</returns>
            [DllImport("user32.dll")]
            public static extern IntPtr GetDesktopWindow();

            /// <summary>
            /// Retrieves a handle to the Shell's desktop window.
            /// </summary>
            /// <returns>The return value is the handle of the Shell's desktop window. If no Shell process is present, the return value is NULL.</returns>
            [DllImport("user32.dll")]
            public static extern IntPtr GetShellWindow();


            #region Get Relative Window

            /// <summary>
            /// Enumerates the child windows that belong to the specified parent window by passing the handle to each child window, in turn, to an application-defined callback function. EnumChildWindows continues until the last child window is enumerated or the callback function returns FALSE.
            /// 
            /// Remarks:
            /// If a child window has created child windows of its own, EnumChildWindows enumerates those windows as well.
            /// A child window that is moved or repositioned in the Z order during the enumeration process will be properly enumerated.The function does not enumerate a child window that is destroyed before being enumerated or that is created during the enumeration process.
            /// </summary>
            /// <param name="parentHandle">A handle to the parent window whose child windows are to be enumerated. If this parameter is NULL, this function is equivalent to EnumWindows.</param>
            /// <param name="callback">A pointer to an application-defined callback function. For more information, see EnumChildProc.</param>
            /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
            /// <returns>The return value is not used.</returns>
            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool EnumChildWindows(IntPtr parentHandle, EnumChildProc callback, IntPtr lParam);

            /// <summary>
            /// An application-defined callback function used with the EnumChildWindows function. It receives the child window handles. The WNDENUMPROC type defines a pointer to this callback function. EnumChildProc is a placeholder for the application-defined function name. 
            /// </summary>
            /// <param name="hwnd">A handle to a child window of the parent window specified in EnumChildWindows. </param>
            /// <param name="lParam">The application-defined value given in EnumChildWindows. </param>
            /// <returns>To continue enumeration, the callback function must return TRUE; to stop enumeration, it must return FALSE. </returns>
            public delegate bool EnumChildProc(IntPtr hwnd, IntPtr lParam);


            /// <summary>
            /// Enumerates all top-level windows associated with the specified desktop. It passes the handle to each window, in turn, to an application-defined callback function.
            /// Remarks:
            /// The EnumDesktopWindows function repeatedly invokes the lpfn callback function until the last top-level window is enumerated or the callback function returns FALSE.
            /// </summary>
            /// <param name="hDesktop">
            /// A handle to the desktop whose top-level windows are to be enumerated. This handle is returned by the CreateDesktop, GetThreadDesktop, OpenDesktop, or OpenInputDesktop function, and must have the DESKTOP_READOBJECTS access right. For more information, see Desktop Security and Access Rights.
            /// If this parameter is NULL, the current desktop is used.
            /// </param>
            /// <param name="lpfn">A pointer to an application-defined EnumWindowsProc callback function.</param>
            /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
            /// <returns>
            /// If the function fails or is unable to perform the enumeration, the return value is zero.
            /// To get extended error information, call GetLastError.
            /// You must ensure that the callback function sets SetLastError if it fails.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool EnumDesktopWindows(IntPtr hDesktop, EnumWindowsProc lpfn, IntPtr lParam);

            /// <summary>
            /// Enumerates all top-level windows on the screen by passing the handle to each window, in turn, to an application-defined callback function. EnumWindows continues until the last top-level window is enumerated or the callback function returns FALSE. 
            /// Remarks
            /// The EnumWindows function does not enumerate child windows, with the exception of a few top-level windows owned by the system that have the WS_CHILD style.
            /// This function is more reliable than calling the GetWindow function in a loop.An application that calls GetWindow to perform this task risks being caught in an infinite loop or referencing a handle to a window that has been destroyed. 
            /// Note  For Windows 8 and later, EnumWindows enumerates only top-level windows of desktop apps.
            /// </summary>
            /// <param name="lpEnumFunc">A pointer to an application-defined callback function. For more information, see EnumWindowsProc.</param>
            /// <param name="lParam">An application-defined value to be passed to the callback function.</param>
            /// <returns>If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// If EnumWindowsProc returns zero, the return value is also zero. In this case, the callback function should call SetLastError to obtain a meaningful error code to be returned to the caller of EnumWindows.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

            /// <summary>
            /// An application-defined callback function used with the EnumWindows or EnumDesktopWindows function. It receives top-level window handles. The WNDENUMPROC type defines a pointer to this callback function. EnumWindowsProc is a placeholder for the application-defined function name. 
            /// </summary>
            /// <param name="hWnd">A handle to a top-level window.</param>
            /// <param name="lParam">The application-defined value given in EnumWindows or EnumDesktopWindows.</param>
            /// <returns>To continue enumeration, the callback function must return TRUE; to stop enumeration, it must return FALSE.</returns>
            public delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);


            /// <summary>
            /// Retrieves the handle to the ancestor of the specified window. 
            /// </summary>
            /// <param name="hwnd">A handle to the window whose ancestor is to be retrieved. If this parameter is the desktop window, the function returns NULL.</param>
            /// <param name="gaFlags">The ancestor to be retrieved.</param>
            /// <returns>The return value is the handle to the ancestor window.</returns>
            [DllImport("USER32.DLL")]
            public static extern IntPtr GetAncestor(IntPtr hwnd, Ancestor gaFlags);

            public enum Ancestor : uint
            {
                /// <summary>
                /// Retrieves the parent window. This does not include the owner, as it does with the GetParent function.
                /// </summary>
                GA_PARENT = 1,
                /// <summary>
                /// Retrieves the root window by walking the chain of parent windows.
                /// </summary>
                GA_ROOT = 2,
                /// <summary>
                /// Retrieves the owned root window by walking the chain of parent and owner windows returned by GetParent.
                /// </summary>
                GA_ROOTOWNER = 3,
            }


            /// <summary>
            /// Retrieves a handle to a window that has the specified relationship (Z-Order or owner) to the specified window. 
            /// 
            /// Remark:
            /// The EnumChildWindows function is more reliable than calling GetWindow in a loop. An application that calls GetWindow to perform this task risks being caught in an infinite loop or referencing a handle to a window that has been destroyed. 
            /// </summary>
            /// <param name="hwnd">A handle to a window. The window handle retrieved is relative to this window, based on the value of the uCmd parameter.</param>
            /// <param name="uCmd">The relationship between the specified window and the window whose handle is to be retrieved.</param>
            /// <returns>If the function succeeds, the return value is a window handle. If no window exists with the specified relationship to the specified window, the return value is NULL. To get extended error information, call GetLastError.</returns>
            [DllImport("USER32.DLL", SetLastError = true)]
            public static extern IntPtr GetWindow(IntPtr hwnd, Relationship uCmd);

            public enum Relationship : uint
            {
                /// <summary>
                /// The retrieved handle identifies the child window at the top of the Z order, if the specified window is a parent window; otherwise, the retrieved handle is NULL. The function examines only child windows of the specified window. It does not examine descendant windows.
                /// </summary>
                GW_CHILD = 5,
                /// <summary>
                /// The retrieved handle identifies the enabled popup window owned by the specified window (the search uses the first such window found using GW_HWNDNEXT); otherwise, if there are no enabled popup windows, the retrieved handle is that of the specified window. 
                /// </summary>
                GW_ENABLEDPOPUP = 6,

                /// <summary>
                /// The retrieved handle identifies the window of the same type that is highest in the Z order.
                /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
                /// </summary>
                GW_HWNDFIRST = 0,
                /// <summary>
                /// The retrieved handle identifies the window of the same type that is lowest in the Z order.
                /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
                /// </summary>
                GW_HWNDLAST = 1,

                /// <summary>
                /// The retrieved handle identifies the window below the specified window in the Z order.
                /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
                /// </summary>
                GW_HWNDNEXT = 2,
                /// <summary>
                /// The retrieved handle identifies the window above the specified window in the Z order.
                /// If the specified window is a topmost window, the handle identifies a topmost window. If the specified window is a top-level window, the handle identifies a top-level window. If the specified window is a child window, the handle identifies a sibling window.
                /// </summary>
                GW_HWNDPREV = 3,

                /// <summary>
                /// The retrieved handle identifies the specified window's owner window, if any. For more information, see Owned Windows ( https://msdn.microsoft.com/en-us/library/windows/desktop/ms632599(v=vs.85).aspx#owned_windows ). 
                /// </summary>
                GW_OWNER = 4,
            }


            /// <summary>
            /// Retrieves a handle to the specified window's parent or owner.
            /// To retrieve a handle to a specified ancestor, use the GetAncestor function.
            /// 
            /// Remark:
            /// To obtain a window's owner window, instead of using GetParent, use GetWindow with the GW_OWNER flag. 
            /// To obtain the parent window and not the owner, instead of using GetParent, use GetAncestor with the GA_PARENT flag. 
            /// </summary>
            /// <param name="hWnd">A handle to the window whose parent window handle is to be retrieved.</param>
            /// <returns>If the window is a child window, the return value is a handle to the parent window. If the window is a top-level window with the WS_POPUP style, the return value is a handle to the owner window. 
            /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
            /// This function typically fails for one of the following reasons:
            /// # The window is a top-level window that is unowned or does not have the WS_POPUP style.
            /// # The owner window has WS_POPUP style.</returns>
            [DllImport("user32.dll", ExactSpelling = true, CharSet = CharSet.Auto, SetLastError = true)]
            public static extern IntPtr GetParent(IntPtr hWnd);


            /// <summary>
            /// Examines the Z order of the child windows associated with the specified parent window and retrieves a handle to the child window at the top of the Z order. 
            /// </summary>
            /// <param name="hWnd">A handle to the parent window whose child windows are to be examined. If this parameter is NULL, the function returns a handle to the window at the top of the Z order. </param>
            /// <returns>If the function succeeds, the return value is a handle to the child window at the top of the Z order. If the specified window has no child windows, the return value is NULL. To get extended error information, use the GetLastError function. </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr GetTopWindow(IntPtr hWnd);

            #endregion Get Relative Window

            #endregion Get Window


            #region Window Event

            /// <summary>
            /// An application-defined callback (or hook) function that the system calls in response to events generated by an accessible object. The hook function processes the event notifications as required. Clients install the hook function and request specific types of event notifications by calling SetWinEventHook.
            ///
            /// The WINEVENTPROC type defines a pointer to this callback function. WinEventProc is a placeholder for the application-defined function name.
            /// </summary>
            /// <param name="hWinEventHook">Handle to an event hook function. This value is returned by SetWinEventHook when the hook function is installed and is specific to each instance of the hook function.</param>
            /// <param name="eventType">Specifies the event that occurred. This value is one of the event constants[https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396].</param>
            /// <param name="hwnd">Handle to the window that generates the event, or NULL if no window is associated with the event. For example, the mouse pointer is not associated with a window.</param>
            /// <param name="idObject">Identifies the object associated with the event. This is one of the object identifiers [https://msdn.microsoft.com/en-us/library/windows/desktop/dd373606(v=vs.85).aspx] or a custom object ID.</param>
            /// <param name="idChild">Identifies whether the event was triggered by an object or a child element of the object. If this value is CHILDID_SELF, the event was triggered by the object; otherwise, this value is the child ID of the element that triggered the event.</param>
            /// <param name="dwEventThread">Identifies the thread that generated the event, or the thread that owns the current window.</param>
            /// <param name="dwmsEventTime">Specifies the time, in milliseconds, that the event was generated.</param>
            public delegate void WinEventProc(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime);

            /// <summary>
            /// Sets an event hook function for a range of events.
            /// </summary>
            /// <param name="eventMin">Specifies the event constant [https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066(v=vs.85).aspx] for the lowest event value in the range of events that are handled by the hook function. This parameter can be set to EVENT_MIN to indicate the lowest possible event value.</param>
            /// <param name="eventMax">Specifies the event constant for the highest event value in the range of events that are handled by the hook function. This parameter can be set to EVENT_MAX to indicate the highest possible event value.</param>
            /// <param name="hmodWinEventProc">Handle to the DLL that contains the hook function at lpfnWinEventProc, if the WINEVENT_INCONTEXT flag is specified in the dwFlags parameter. If the hook function is not located in a DLL, or if the WINEVENT_OUTOFCONTEXT flag is specified, this parameter is NULL.</param>
            /// <param name="lpfnWinEventProc">Pointer to the event hook function. For more information about this function, see WinEventProc [https://msdn.microsoft.com/en-us/library/windows/desktop/dd373885(v=vs.85).aspx].</param>
            /// <param name="idProcess">Specifies the ID of the process from which the hook function receives events. Specify zero (0) to receive events from all processes on the current desktop.</param>
            /// <param name="idThread">Specifies the ID of the thread from which the hook function receives events. If this parameter is zero, the hook function is associated with all existing threads on the current desktop.</param>
            /// <param name="dwFlags">Flag values that specify the location of the hook function and of the events to be skipped. The following flags are valid: [https://msdn.microsoft.com/en-us/library/windows/desktop/dd373640(v=vs.85).aspx]</param>
            /// <returns>If successful, returns an HWINEVENTHOOK value that identifies this event hook instance. Applications save this return value to use it with the UnhookWinEvent function.
            ///
            /// If unsuccessful, returns zero.</returns>
            [DllImport("user32.dll")]
            public static extern IntPtr SetWinEventHook(uint eventMin, uint eventMax, IntPtr hmodWinEventProc, WinEventProc lpfnWinEventProc, uint idProcess, uint idThread, uint dwFlags);

            /// <summary>
            /// Events that are generated by the operating system and by server applications.
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            /// </summary>
            public enum EventConstant : uint
            {
                /// <summary>
                /// The foreground window has changed. The system sends this event even if the foreground window has changed to another window in the same thread. Server applications never send this event.
                ///
                /// For this event, the WinEventProc callback function's hwnd parameter is the handle to the window that is in the foreground, the idObject parameter is OBJID_WINDOW, and the idChild parameter is CHILDID_SELF.
                /// </summary>
                EVENT_SYSTEM_FOREGROUND = 3
            }

            public enum HookFunctionFlags : uint
            {
                /// <summary>
                /// The callback function is not mapped into the address space of the process that generates the event. Because the hook function is called across process boundaries, the system must queue events. Although this method is asynchronous, events are guaranteed to be in sequential order. For more information, see Out-of-Context Hook Functions [https://msdn.microsoft.com/en-us/library/windows/desktop/dd373611(v=vs.85).aspx].
                /// </summary>
                WINEVENT_OUTOFCONTEXT = 0,
            }

            /// <summary>
            /// Removes an event hook function created by a previous call to SetWinEventHook.
            /// </summary>
            /// <param name="hWinEventHook">Handle to the event hook returned in the previous call to SetWinEventHook.</param>
            /// <returns>If successful, returns TRUE; otherwise, returns FALSE.
            /// 
            /// Three common errors cause this function to fail:
            /// 
            /// # The hWinEventHook parameter is NULL or not valid.
            /// # The event hook specified by hWinEventHook was already removed.
            /// # UnhookWinEvent is called from a thread that is different from the original call to SetWinEventHook.
            /// </returns>
            [DllImport("user32.dll")]
            public static extern bool UnhookWinEvent(IntPtr hWinEventHook);

            #endregion Window Event


            #region Shell Event

            /// <summary>
            /// wParam parameter values passed to the window procedure for the Shell hook messages.
            /// </summary>
            public enum ShellEvent : int
            {
                /// <summary>
                /// lParam is: A handle to the window being created.
                /// </summary>
                HSHELL_WINDOWCREATED = 1,
                /// <summary>
                /// lParam is: A handle to the top-level window being destroyed.
                /// </summary>
                HSHELL_WINDOWDESTROYED = 2,
                /// <summary>
                /// lParam is: Not used.
                /// </summary>
                HSHELL_ACTIVATESHELLWINDOW = 3,
                /// <summary>
                /// lParam is: A handle to the activated window.
                /// </summary>
                HSHELL_WINDOWACTIVATED = 4,
                /// <summary>
                /// lParam is: A pointer to a SHELLHOOKINFO structure.
                /// </summary>
                HSHELL_GETMINRECT = 5,
                /// <summary>
                /// lParam is: A handle to the window that needs to be redrawn.
                /// </summary>
                HSHELL_REDRAW = 6,
                /// <summary>
                /// lParam is: Can be ignored.
                /// </summary>
                HSHELL_TASKMAN = 7,
                /// <summary>
                /// lParam is: Keyboard language was changed or a new keyboard layout was loaded.
                /// </summary>
                HSHELL_LANGUAGE = 8,
                /// <summary>
                /// lParam is: The accessibility state has changed. 
                /// </summary>
                HSHELL_ACCESSIBILITYSTATE = 11,
                /// <summary>
                /// lParam is:  The APPCOMMAND which has been unhandled by the application or other hooks. See WM_APPCOMMAND and use the GET_APPCOMMAND_LPARAM macro to retrieve this parameter.
                /// </summary>
                HSHELL_APPCOMMAND = 12,
                /// <summary>
                /// lParam is:  A handle to the window being replaced.
                /// </summary>
                HSHELL_WINDOWREPLACED = 13,
            }
            /// <summary>
            /// Defines a new window message that is guaranteed to be unique throughout the system. The message value can be used when sending or posting messages.
            /// 
            /// Remarks:
            /// The RegisterWindowMessage function is typically used to register messages for communicating between two cooperating applications.
            /// If two different applications register the same message string, the applications return the same message value. The message remains registered until the session ends.
            /// Only use RegisterWindowMessage when more than one application must process the same message.
            /// </summary>
            /// <param name="lpString">The message to be registered.</param>
            /// <returns>If the message is successfully registered, the return value is a message identifier in the range 0xC000 through 0xFFFF.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern uint RegisterWindowMessage(string lpString);

            /// <summary>
            /// [This function is not intended for general use. It may be altered or unavailable in subsequent versions of Windows.]
            /// Unregisters a specified Shell window that is registered to receive Shell hook messages.
            /// </summary>
            /// <param name="hWnd">A handle to the window to be unregistered. The window was registered with a call to the RegisterShellHookWindow function.</param>
            /// <returns>TRUE if the function succeeds; FALSE if the function fails.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DeregisterShellHookWindow(IntPtr hWnd);

            /// <summary>
            /// [This function is not intended for general use. It may be altered or unavailable in subsequent versions of Windows.]
            /// Registers a specified Shell window to receive certain messages for events or notifications that are useful to Shell applications.
            /// The event messages received are only those sent to the Shell window associated with the specified window's desktop. Many of the messages are the same as those that can be received after calling the SetWindowsHookEx function and specifying WH_SHELL for the hook type. The difference with RegisterShellHookWindow is that the messages are received through the specified window's WindowProc and not through a call back procedure.
            /// </summary>
            /// <param name="hWnd">A handle to the window to register for Shell hook messages.</param>
            /// <returns>TRUE if the function succeeds; otherwise, FALSE.</returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool RegisterShellHookWindow(IntPtr hWnd);

            #endregion Shell Event


            #region Advanced Shell Event

            /// <summary>
            /// SetWindowsHook() codes
            /// More info:
            /// https://msdn.microsoft.com/en-us/library/ms644990(v=vs.85).aspx
            /// </summary>
            public enum Hook : int
            {
                WH_MIN = (-1)
,
                WH_MSGFILTER = (-1)
,
                WH_JOURNALRECORD = 0
,
                WH_JOURNALPLAYBACK = 1
,
                WH_KEYBOARD = 2
,
                WH_GETMESSAGE = 3
,
                WH_CALLWNDPROC = 4
,
                WH_CBT = 5
,
                WH_SYSMSGFILTER = 6
,
                WH_MOUSE = 7
// defined(_WIN32_WINDOWS)
,
                WH_HARDWARE = 8
// END
,
                WH_DEBUG = 9
,
                /// <summary>
                /// Installs a hook procedure that receives notifications useful to shell applications. For more information, see the ShellProc <see cref="https://msdn.microsoft.com/en-us/library/ms644991(v=vs.85).aspx"/> hook procedure.
                /// </summary>
                WH_SHELL = 10
,
                WH_FOREGROUNDIDLE = 11
// (WINVER >= 0x0400)
,
                WH_CALLWNDPROCRET = 12
// END /* WINVER >= 0x0400 */

// (_WIN32_WINNT >= 0x0400)
,
                WH_KEYBOARD_LL = 13
,
                WH_MOUSE_LL = 14
// END (_WIN32_WINNT >= 0x0400)

// (WINVER >= 0x0400)
// (_WIN32_WINNT >= 0x0400)
,
                WH_MAX = 14
// ELSE
/*
,
 WH_MAX = 12
// END (_WIN32_WINNT >= 0x0400)
// ELSE
,
 WH_MAX = 11
// END
*/

,
                WH_MINHOOK = WH_MIN
,
                WH_MAXHOOK = WH_MAX

            }

            public delegate IntPtr CallShellProc(int nCode, IntPtr wParam, IntPtr lParam);

            /// <summary>
            /// <para>Installs an application-defined hook procedure into a hook chain. You would install a hook procedure to monitor the system for certain types of events. These events are associated either with a specific thread or with all threads in the same desktop as the calling thread.</para> 
            /// </summary>
            /// <param name="idHook">The type of hook procedure to be installed. This parameter can be one of the following values. (see Enum)</param>
            /// <param name="lpfn">A pointer to the hook procedure. If the dwThreadId parameter is zero or specifies the identifier of a thread created by a different process, the lpfn parameter must point to a hook procedure in a DLL. Otherwise, lpfn can point to a hook procedure in the code associated with the current process. </param>
            /// <param name="hMod">A handle to the DLL containing the hook procedure pointed to by the lpfn parameter. The hMod parameter must be set to NULL if the dwThreadId parameter specifies a thread created by the current process and if the hook procedure is within the code associated with the current process. </param>
            /// <param name="dwThreadId">The identifier of the thread with which the hook procedure is to be associated. For desktop apps, if this parameter is zero, the hook procedure is associated with all existing threads running in the same desktop as the calling thread. For Windows Store apps, see the Remarks section.</param>
            /// <returns>
            /// If the function succeeds, the return value is the handle to the hook procedure.
            /// If the function fails, the return value is NULL. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern IntPtr SetWindowsHookEx(Hook idHook, CallShellProc lpfn, IntPtr hMod, int dwThreadId);

            /// <summary>
            /// Removes a hook procedure installed in a hook chain by the SetWindowsHookEx function. 
            /// </summary>
            /// <param name="hhk">A handle to the hook to be removed. This parameter is a hook handle obtained by a previous call to SetWindowsHookEx. </param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            [return: MarshalAs(UnmanagedType.Bool)]
            public static extern bool UnhookWindowsHookEx(IntPtr hhk);

            /// <summary>
            /// Passes the hook information to the next hook procedure in the current hook chain. A hook procedure can call this function either before or after processing the hook information. 
            /// 
            /// Remarks:
            /// Hook procedures are installed in chains for particular hook types. CallNextHookEx calls the next hook in the chain.
            /// Calling CallNextHookEx is optional, but it is highly recommended; otherwise, other applications that have installed hooks will not receive hook notifications and may behave incorrectly as a result. You should call CallNextHookEx unless you absolutely need to prevent the notification from being seen by other applications.
            /// </summary>
            /// <param name="idHook">This parameter is ignored.</param>
            /// <param name="nCode">The hook code passed to the current hook procedure. The next hook procedure uses this code to determine how to process the hook information.</param>
            /// <param name="wParam">The wParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
            /// <param name="lParam">The lParam value passed to the current hook procedure. The meaning of this parameter depends on the type of hook associated with the current hook chain.</param>
            /// <returns>This value is returned by the next hook procedure in the chain. The current hook procedure must also return this value. The meaning of the return value depends on the hook type. For more information, see the descriptions of the individual hook procedures.</returns>
            [DllImport("user32.dll")]
            public static extern IntPtr CallNextHookEx(IntPtr idHook, int nCode, IntPtr wParam, IntPtr lParam);


            /// <summary>
            /// Retrieves the thread identifier of the calling thread.
            /// 
            /// Remarks:
            /// Until the thread terminates, the thread identifier uniquely identifies the thread throughout the system.
            /// </summary>
            /// <returns>The return value is the thread identifier of the calling thread.</returns>
            [DllImport("kernel32.dll")]
            public static extern int GetCurrentThreadId();

            #endregion Advanced Shell Event


            #region Messages

            /// <summary>
            /// <para>Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message.</para>
            /// <para>To send a message and return immediately, use the SendMessageCallback or SendNotifyMessage function. To post a message to a thread's message queue and return immediately, use the PostMessage or PostThreadMessage function.</para>
            /// <para>Remarks:</para>
            /// <para>When a message is blocked by UIPI the last error, retrieved with GetLastError, is set to 5 (access denied).</para>
            /// <para>Applications that need to communicate using HWND_BROADCAST should use the RegisterWindowMessage function to obtain a unique message for inter-application communication.</para>
            /// <para>The system only does marshalling for system messages (those in the range 0 to (WM_USER-1)). To send other messages (those >= WM_USER) to another process, you must do custom marshalling.</para>
            /// <para>If the specified window was created by the calling thread, the window procedure is called immediately as a subroutine. If the specified window was created by a different thread, the system switches to that thread and calls the appropriate window procedure. Messages sent between threads are processed only when the receiving thread executes message retrieval code. The sending thread is blocked until the receiving thread processes the message. However, the sending thread will process incoming nonqueued messages while waiting for its message to be processed. To prevent this, use SendMessageTimeout with SMTO_BLOCK set. For more information on nonqueued messages, see Nonqueued Messages.</para>
            /// <para>An accessibility application can use SendMessage to send WM_APPCOMMAND messages to the shell to launch applications. This functionality is not guaranteed to work for other types of applications.</para>
            /// </summary>
            /// <param name="hWnd">
            /// <para>A handle to the window whose window procedure will receive the message. If this parameter is HWND_BROADCAST ((HWND)0xffff), the message is sent to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows; but the message is not sent to child windows.</para>
            /// <para>Message sending is subject to UIPI. The thread of a process can send messages only to message queues of threads in processes of lesser or equal integrity level.</para>
            /// </param>
            /// <param name="Msg">
            /// <para>The message to be sent.</para>
            /// <para>For lists of the system-provided messages, see System-Defined Messages<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined"/>.</para>
            /// </param>
            /// <param name="wParam">Additional message-specific information.</param>
            /// <param name="lParam">Additional message-specific information.</param>
            /// <returns>The return value specifies the result of the message processing; it depends on the message sent.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int SendMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);

            /// <summary>
            /// <para>Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.</para>
            /// <para>To post a message in the message queue associated with a thread, use the PostThreadMessage function.</para>
            /// <para>Remarks:</para>
            /// <para>When a message is blocked by UIPI the last error, retrieved with GetLastError, is set to 5 (access denied).</para>
            /// <para>Messages in a message queue are retrieved by calls to the GetMessage or PeekMessage function.</para>
            /// <para>Applications that need to communicate using HWND_BROADCAST should use the RegisterWindowMessage function to obtain a unique message for inter-application communication.</para>
            /// <para>The system only does marshalling for system messages (those in the range 0 to (WM_USER-1)). To send other messages (those >= WM_USER) to another process, you must do custom marshalling.</para>
            /// <para>If you send a message in the range below WM_USER to the asynchronous message functions (PostMessage, SendNotifyMessage, and SendMessageCallback), its message parameters cannot include pointers. Otherwise, the operation will fail. The functions will return before the receiving thread has had a chance to process the message and the sender will free the memory before it is used.</para>
            /// <para>Do not post the WM_QUIT message using PostMessage; use the PostQuitMessage function.</para>
            /// <para>An accessibility application can use PostMessage to post WM_APPCOMMAND messages to the shell to launch applications. This functionality is not guaranteed to work for other types of applications.</para>
            /// <para>There is a limit of 10,000 posted messages per message queue. This limit should be sufficiently large. If your application exceeds the limit, it should be redesigned to avoid consuming so many system resources. To adjust this limit, modify the following registry key.</para>
            /// <para>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows\USERPostMessageLimit</para>
            /// <para>The minimum acceptable value is 4000.</para>
            /// </summary>
            /// <param name="hWnd">
            /// <para>A handle to the window whose window procedure is to receive the message. The following values have special meanings.</para>
            /// <para>HWND_BROADCAST((HWND)0xffff) = The message is posted to all top-level windows in the system, including disabled or invisible unowned windows, overlapped windows, and pop-up windows. The message is not posted to child windows.</para>
            /// <para>NULL = The function behaves like a call to PostThreadMessage with the dwThreadId parameter set to the identifier of the current thread.</para>
            /// <para>Starting with Windows Vista, message posting is subject to UIPI. The thread of a process can post messages only to message queues of threads in processes of lesser or equal integrity level.</para>
            /// </param>
            /// <param name="Msg">
            /// <para>The message to be posted.</para>
            /// <para>For lists of the system-provided messages, see System-Defined Messages<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined"/>.</para>
            /// </param>
            /// <param name="wParam">Additional message-specific information.</param>
            /// <param name="lParam">Additional message-specific information.</param>
            /// <returns>
            /// <para>If the function succeeds, the return value is nonzero.</para>
            /// <para>If the function fails, the return value is zero. To get extended error information, call GetLastError. GetLastError returns ERROR_NOT_ENOUGH_QUOTA when the limit is hit.</para>
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool PostMessage(IntPtr hWnd, uint Msg, IntPtr wParam, IntPtr lParam);


            /// <summary>
            /// Used as window handle for SendMessage or PostMessage to send to all windows.
            /// </summary>
            public readonly static IntPtr HWND_BROADCAST = new IntPtr(0xffff);

            /// <summary>
            /// Can be used when searching windows (with FindWindowEx) to only search message windows.
            /// </summary>
            public readonly static IntPtr HWND_MESSAGE = new IntPtr(-3);

            /// <summary>
            /// <para>Sent as a signal that a window or an application should terminate.</para>
            /// <para>An application can prompt the user for confirmation, prior to destroying a window, by processing the WM_CLOSE message and calling the DestroyWindow function only if the user confirms the choice.</para>
            /// <para>wParam = This parameter is not used.</para>
            /// <para>lParam = This parameter is not used.</para>
            /// <para>Return = If an application processes this message, it should return zero.</para>
            /// </summary>
            public const uint WM_CLOSE = 0x0010;
            /// <summary>
            /// <para>Indicates a request to terminate an application, and is generated when the application calls the PostQuitMessage function. This message causes the GetMessage function to return zero.</para>
            /// <para>The WM_QUIT message is not associated with a window and therefore will never be received through a window's window procedure. It is retrieved only by the GetMessage or PeekMessage functions.</para>
            /// <para>Do not post the WM_QUIT message using the PostMessage function; use PostQuitMessage.</para>
            /// <para>wParam = The exit code given in the PostQuitMessage function.</para>
            /// <para>lParam = This parameter is not used.</para>
            /// <para>Return = This message does not have a return value because it causes the message loop to terminate before the message is sent to the application's window procedure.</para>
            /// </summary>
            public const uint WM_QUIT = 0x0012;


            /// <summary>
            /// <para>Posts a message to the message queue of the specified thread. It returns without waiting for the thread to process the message.</para>
            /// <para>Remarks:</para>
            /// <para>When a message is blocked by UIPI the last error, retrieved with GetLastError, is set to 5 (access denied).</para>
            /// <para>The thread to which the message is posted must have created a message queue, or else the call to PostThreadMessage fails. Use the following method to handle this situation.</para>
            /// <para># Create an event object, then create the thread.</para>
            /// <para># Use the WaitForSingleObject function to wait for the event to be set to the signaled state before calling PostThreadMessage.</para>
            /// <para># In the thread to which the message will be posted, call PeekMessage as shown here to force the system to create the message queue.</para>
            /// <para>PeekMessage(msg, NULL, WM_USER, WM_USER, PM_NOREMOVE)</para>
            /// <para># Set the event, to indicate that the thread is ready to receive posted messages.</para>
            /// <para>|</para>
            /// <para>The thread to which the message is posted retrieves the message by calling the GetMessage or PeekMessage function. The hwnd member of the returned MSG structure is NULL.</para>
            /// <para>Messages sent by PostThreadMessage are not associated with a window. As a general rule, messages that are not associated with a window cannot be dispatched by the DispatchMessage function. Therefore, if the recipient thread is in a modal loop (as used by MessageBox or DialogBox), the messages will be lost. To intercept thread messages while in a modal loop, use a thread-specific hook.</para>
            /// <para>The system only does marshalling for system messages (those in the range 0 to (WM_USER-1)). To send other messages (those >= WM_USER) to another process, you must do custom marshalling.</para>
            /// <para>There is a limit of 10,000 posted messages per message queue. This limit should be sufficiently large. If your application exceeds the limit, it should be redesigned to avoid consuming so many system resources. To adjust this limit, modify the following registry key.</para>
            /// <para>HKEY_LOCAL_MACHINE\SOFTWARE\Microsoft\Windows NT\CurrentVersion\Windows\USERPostMessageLimit</para>
            /// <para>The minimum acceptable value is 4000.</para>
            /// </summary>
            /// <param name="idThread">
            /// <para>The identifier of the thread to which the message is to be posted.</para>
            /// <para>The function fails if the specified thread does not have a message queue. The system creates a thread's message queue when the thread makes its first call to one of the User or GDI functions. For more information, see the Remarks section.</para>
            /// <para>Message posting is subject to UIPI. The thread of a process can post messages only to posted-message queues of threads in processes of lesser or equal integrity level.</para>
            /// <para>This thread must have the SE_TCB_NAME privilege to post a message to a thread that belongs to a process with the same locally unique identifier (LUID) but is in a different desktop. Otherwise, the function fails and returns ERROR_INVALID_THREAD_ID.</para>
            /// <para>This thread must either belong to the same desktop as the calling thread or to a process with the same LUID. Otherwise, the function fails and returns ERROR_INVALID_THREAD_ID.</para>
            /// </param>
            /// <param name="Msg">The type of message to be posted.</param>
            /// <param name="wParam">Additional message-specific information.</param>
            /// <param name="lParam">Additional message-specific information.</param>
            /// <returns>
            /// <para>If the function succeeds, the return value is nonzero.</para>
            /// <para>If the function fails, the return value is zero. To get extended error information, call GetLastError. GetLastError returns ERROR_INVALID_THREAD_ID if idThread is not a valid thread identifier, or if the thread specified by idThread does not have a message queue. GetLastError returns ERROR_NOT_ENOUGH_QUOTA when the message limit is hit. </para>
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool PostThreadMessage(uint idThread, uint Msg, IntPtr wParam, IntPtr lParam);

            #endregion Messages


            #region Keyboard Event

            /// <summary>
            /// <para>Synthesizes a keystroke. The system can use such a synthesized keystroke to generate a WM_KEYUP or WM_KEYDOWN message. The keyboard driver's interrupt handler calls the keybd_event function.</para>
            /// <para>Remarks:</para>
            /// <para>An application can simulate a press of the PRINTSCRN key in order to obtain a screen snapshot and save it to the clipboard. To do this, call keybd_event with the bVk parameter set to VK_SNAPSHOT.</para>
            /// </summary>
            /// <param name="bVk">A virtual-key code. The code must be a value in the range 1 to 254. For a complete list, see Virtual Key Codes. <see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx"/></param>
            /// <param name="bScan">A hardware scan code for the key.</param>
            /// <param name="dwFlags">Controls various aspects of function operation. This parameter can be one or MORE of the flag values.</param>
            /// <param name="dwExtraInfo">An additional value associated with the key stroke. </param>
            [DllImport("user32.dll")]
            public static extern void keybd_event(byte bVk, byte bScan, KeyEventFlag dwFlags, int dwExtraInfo);

            public enum KeyEventFlag : uint
            {
                /// <summary>
                /// If specified, the scan code was preceded by a prefix byte having the value 0xE0 (224).
                /// </summary>
                KEYEVENTF_EXTENDEDKEY = 0x0001,
                /// <summary>
                /// If specified, the key is being released. If not specified, the key is being depressed.
                /// </summary>
                KEYEVENTF_KEYUP = 0x0002,
            }

            #endregion Keyboard Event
        }


        #region Event Listeners

        /// <summary>
        /// Listens to Window Events. Uses SetWinEventHook to hook into window event notifications.
        /// Can be used to keep track of foreground window. 
        /// Can also be used to for example get notified on drag and drop, text selection changes and Window minimize and maximize.
        /// </summary>
        public class WindowEventListener : IDisposable
        {
            #region Classes

            public class WindowEventListenerEventArgs : EventArgs
            {
                #region Classes

                #endregion Classes


                #region Member Variables

                /// <summary>
                /// Specifies the event that occurred. This value is one of the event constants:
                /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066(v=vs.85).aspx
                /// </summary>
                public EventConstants EventConstant { get; }

                /// <summary>
                /// Handle to the window that generates the event, or NULL if no window is associated with the event. For example, the mouse pointer is not associated with a window.
                /// </summary>
                public IntPtr WindowHandle { get; }

                /// <summary>
                /// Identifies the object associated with the event. This is one of the object identifiers [https://msdn.microsoft.com/en-us/library/windows/desktop/dd373606(v=vs.85).aspx] or a custom object ID.
                /// Object identifiers are used to identify parts of a window.
                /// </summary>
                public ObjectIdentifiers ObjectIdentifier { get; }

                /// <summary>
                /// Identifies whether the event was triggered by an object or a child element of the object. If this value is CHILDID_SELF (0), the event was triggered by the object; otherwise, this value is the child ID of the element that triggered the event.
                /// </summary>
                public int ChildId { get; }

                /// <summary>
                /// Identifies the thread that generated the event, or the thread that owns the current window.
                /// </summary>
                public uint EventThread { get; }

                /// <summary>
                /// Specifies the time, in milliseconds, that the event was generated.
                /// </summary>
                public uint EventTimeInMilliseconds { get; }

                #endregion Member Variables


                #region Constructors

                public WindowEventListenerEventArgs(uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
                {
                    EventConstant = (EventConstants)eventType;
                    WindowHandle = hwnd;
                    ObjectIdentifier = (ObjectIdentifiers)idObject;
                    ChildId = idChild;
                    EventThread = dwEventThread;
                    EventTimeInMilliseconds = dwmsEventTime;
                }

                #endregion Constructors


                #region Methods

                #endregion Methods


                #region Properties

                #endregion Properties
            }

            /// <summary>
            /// Events that are generated by the operating system and by server applications.
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd318066%28v=vs.85%29.aspx?f=255&MSPPError=-2147217396
            /// </summary>
            public enum EventConstants : uint
            {
                EventMin = 0x00000001,                              // EVENT_MIN
                SystemSound = 0x0001,                               // EVENT_SYSTEM_SOUND
                SystemAlert = 0x0002,                               // EVENT_SYSTEM_ALERT
                /// <summary>
                /// The foreground window has changed. The system sends this event even if the foreground window has changed to another window in the same thread. Server applications never send this event.
                ///
                /// For this event, the WinEventProc callback function's hwnd parameter is the handle to the window that is in the foreground, the idObject parameter is OBJID_WINDOW, and the idChild parameter is CHILDID_SELF.
                /// </summary>
                SystemForeground = 0x0003,                          // EVENT_SYSTEM_FOREGROUND
                SystemMenuStart = 0x0004,                           // EVENT_SYSTEM_MENUSTART
                SystemMenuEnd = 0x0005,                             // EVENT_SYSTEM_MENUEND
                SystemMenuPopupStart = 0x0006,                      // EVENT_SYSTEM_MENUPOPUPSTART
                SystemMenuPopupEnd = 0x0007,                        // EVENT_SYSTEM_MENUPOPUPEND
                SystemCaptureStart = 0x0008,                        // EVENT_SYSTEM_CAPTURESTART
                SystemCaptureEnd = 0x0009,                          // EVENT_SYSTEM_CAPTUREEND
                SystemMoveSizeStart = 0x000A,                       // EVENT_SYSTEM_MOVESIZESTART
                SystemMoveSizeEnd = 0x000B,                         // EVENT_SYSTEM_MOVESIZEEND
                SystemContextHelpStart = 0x000C,                    // EVENT_SYSTEM_CONTEXTHELPSTART
                SystemContextHelpEnd = 0x000D,                      // EVENT_SYSTEM_CONTEXTHELPEND
                SystemDragStart = 0x000E,                           // EVENT_SYSTEM_DRAGDROPSTART
                SystemDragEnd = 0x000F,                             // EVENT_SYSTEM_DRAGDROPEND
                SystemDialogStart = 0x0010,                         // EVENT_SYSTEM_DIALOGSTART
                SystemDialogEnd = 0x0011,                           // EVENT_SYSTEM_DIALOGEND
                SystemScrollingStart = 0x0012,                      // EVENT_SYSTEM_SCROLLINGSTART
                SystemScrollingEnd = 0x0013,                        // EVENT_SYSTEM_SCROLLINGEND
                SystemSwitchStart = 0x0014,                         // EVENT_SYSTEM_SWITCHSTART
                SystemSwitchEnd = 0x0015,                           // EVENT_SYSTEM_SWITCHEND
                SystemMinimizeStart = 0x0016,                       // EVENT_SYSTEM_MINIMIZESTART
                SystemMinimizeEnd = 0x0017,                         // EVENT_SYSTEM_MINIMIZEEND
                ObjectCreate = 0x8000,                              // EVENT_OBJECT_CREATE
                ObjectDestroy = 0x8001,                             // EVENT_OBJECT_DESTROY
                ObjectShow = 0x8002,                                // EVENT_OBJECT_SHOW
                ObjectHide = 0x8003,                                // EVENT_OBJECT_HIDE
                ObjectReorder = 0x8004,                             // EVENT_OBJECT_REORDER
                ObjectFocus = 0x8005,                               // EVENT_OBJECT_FOCUS
                ObjectSelection = 0x8006,                           // EVENT_OBJECT_SELECTION
                ObjectSelectionAdd = 0x8007,                        // EVENT_OBJECT_SELECTIONADD
                ObjectSelectionRemove = 0x8008,                     // EVENT_OBJECT_SELECTIONREMOVE
                ObjectSelectionWithin = 0x8009,                     // EVENT_OBJECT_SELECTIONWITHIN
                ObjectStateChange = 0x800A,                         // EVENT_OBJECT_STATECHANGE
                ObjectLocationChange = 0x800B,                      // EVENT_OBJECT_LOCATIONCHANGE
                ObjectNameChange = 0x800C,                          // EVENT_OBJECT_NAMECHANGE
                ObjectDescriptionChange = 0x800D,                   // EVENT_OBJECT_DESCRIPTIONCHANGE
                ObjectValueChange = 0x800E,                         // EVENT_OBJECT_VALUECHANGE
                ObjectParentChange = 0x800F,                        // EVENT_OBJECT_PARENTCHANGE
                ObjectHelpChange = 0x8010,                          // EVENT_OBJECT_HELPCHANGE
                ObjectDefactionChange = 0x8011,                     // EVENT_OBJECT_DEFACTIONCHANGE
                ObjectAcceleratorChange = 0x8012,                   // EVENT_OBJECT_ACCELERATORCHANGE
                EventMax = 0x7FFFFFFF,                              // EVENT_MAX

                // Vista or later.
                ObjectContentScrolled = 0x8015,                     // EVENT_OBJECT_CONTENTSCROLLED
                ObjectTextSelectionChanged = 0x8014,                // EVENT_OBJECT_TEXTSELECTIONCHANGED
                ObjectInvoked = 0x8013,                             // EVENT_OBJECT_INVOKED
                SystemDesktopSwitch = 0x0020,                       // EVENT_SYSTEM_DESKTOPSWITCH

                // More:
                EVENT_SYSTEM_ARRANGMENTPREVIEW = 0x8016,
                EVENT_OBJECT_CLOAKED = 0x8017,
                EVENT_OBJECT_UNCLOAKED = 0x8018,
                EVENT_OBJECT_LIVEREGIONCHANGED = 0x8019,
                EVENT_OBJECT_HOSTEDOBJECTSINVALIDATED = 0x8020,
                EVENT_OBJECT_DRAGSTART = 0x8021,
                EVENT_OBJECT_DRAGCANCEL = 0x8022,
                EVENT_OBJECT_DRAGCOMPLETE = 0x8023,
                EVENT_OBJECT_DRAGENTER = 0x8024,
                EVENT_OBJECT_DRAGLEAVE = 0x8025,
                EVENT_OBJECT_DRAGDROPPED = 0x8026,
                EVENT_OBJECT_IME_SHOW = 0x8027,
                EVENT_OBJECT_IME_HIDE = 0x8028,
                EVENT_OBJECT_IME_CHANGE = 0x8029,
                EVENT_OBJECT_TEXTEDIT_CONVERSIONTARGETCHANGED = 0x8030,
            }

            /// <summary>
            /// Identify categories of accessible objects within a window. Identify parts of a window.
            /// Servers can define custom object IDs. Custom object IDs must be assigned positive values.
            /// Zero and all negative values reserved for standard object identifiers.
            /// 
            /// Constants:
            /// https://msdn.microsoft.com/en-us/library/windows/desktop/dd373606(v=vs.85).aspx
            /// </summary>
            public enum ObjectIdentifiers : uint
            {
                /// <summary>
                /// The window itself rather than a child object.
                /// </summary>
                OBJID_WINDOW = 0x00000000,
                /// <summary>
                /// The window's system menu.
                /// </summary>
                OBJID_SYSMENU = 0xFFFFFFFF,
                /// <summary>
                /// The window's title bar.
                /// </summary>
                OBJID_TITLEBAR = 0xFFFFFFFE,
                /// <summary>
                /// The window's menu bar.
                /// </summary>
                OBJID_MENU = 0xFFFFFFFD,
                /// <summary>
                /// The window's client area. In most cases, the operating system controls the frame elements and the client object contains all elements that are controlled by the application. Servers only process the WM_GETOBJECT messages in which the lParam is OBJID_CLIENT, OBJID_WINDOW, or a custom object identifier.
                /// </summary>
                OBJID_CLIENT = 0xFFFFFFFC,
                /// <summary>
                /// The window's vertical scroll bar.
                /// </summary>
                OBJID_VSCROLL = 0xFFFFFFFB,
                /// <summary>
                /// The window's horizontal scroll bar.
                /// </summary>
                OBJID_HSCROLL = 0xFFFFFFFA,
                /// <summary>
                /// The window's size grip: an optional frame component located at the lower-right corner of the window frame.
                /// </summary>
                OBJID_SIZEGRIP = 0xFFFFFFF9,
                /// <summary>
                /// The text insertion bar (caret) in the window.
                /// </summary>
                OBJID_CARET = 0xFFFFFFF8,
                /// <summary>
                /// The mouse pointer. There is only one mouse pointer in the system, and it is not a child of any window.
                /// </summary>
                OBJID_CURSOR = 0xFFFFFFF7,
                /// <summary>
                /// An alert that is associated with a window or an application. System provided message boxes are the only UI elements that send events with this object identifier. Server applications cannot use the AccessibleObjectFromX functions with this object identifier. This is a known issue with Microsoft Active Accessibility.
                /// </summary>
                OBJID_ALERT = 0xFFFFFFF6,
                /// <summary>
                /// A sound object. Sound objects do not have screen locations or children, but they do have name and state attributes. They are children of the application that is playing the sound.
                /// </summary>
                OBJID_SOUND = 0xFFFFFFF5,
                /// <summary>
                /// An object identifier that Oleacc.dll uses internally. For more information, see Appendix F: Object Identifier Values for OBJID_QUERYCLASSNAMEIDX.
                /// </summary>
                OBJID_QUERYCLASSNAMEIDX = 0xFFFFFFF4,
                /// <summary>
                /// In response to this object identifier, third-party applications can expose their own object model. Third-party applications can return any COM interface in response to this object identifier.
                /// </summary>
                OBJID_NATIVEOM = 0xFFFFFFF0,
            }

            #endregion Classes


            #region Member Variables

            protected readonly object locker = new object();

            private readonly Native.WinEventProc callback = null;
            private GCHandle? callbackGCHandle = null;
            protected readonly IntPtr eventHookHandle;
            private bool isDisposed = false;

            public event EventHandler<WindowEventListenerEventArgs> SafeEvent;
            public event EventHandler<WindowEventListenerEventArgs> UnsafeEvent;

            public SynchronizationContext SyncObject { get; } = SynchronizationContext.Current ?? new SynchronizationContext();

            #endregion Member Variables


            #region Constructors

            /// <summary>
            /// The client thread that calls this must have a message loop in order to receive events.
            /// Listens to windows events for a specified EventConstants. Can be limited to windows on a specific process or thread.
            /// </summary>
            /// <param name="eventConstant">Specifies the event constant for the events that are handled by the hook function.</param>
            /// <param name="processId">Specifies the ID of the process from which the hook function receives events. Specify zero (0) to receive events from all processes on the current desktop.</param>
            /// <param name="threadId">Specifies the ID of the thread from which the hook function receives events. If this parameter is zero, the hook function is associated with all existing threads on the current desktop.
            /// 
            /// Id can for example be retrieved in the following ways:
            /// # The "System.Diagnostics.ProcessThread"'s "Id" property.
            ///     # The "System.Diagnostics.Process"'s "Threads" property can be used to get "System.Diagnostics.ProcessThread" objects.
            /// # PInvoke to the Win32 API method "GetThreadId".
            ///     #https://msdn.microsoft.com/en-us/library/ms683233(VS.85).aspx
            /// # PInvoke to the Win32 API method "GetCurrentThreadID".
            ///     # https://msdn.microsoft.com/en-us/library/ms683183(VS.85).aspx
            ///     # Wrapped in deprecated "AppDomain.GetCurrentThreadId".
            /// Note: Do not use "System.Threading.Thread"'s "ManagedThreadId" property. ".Net" threads do not correspond to OS threads.
            /// </param>
            public WindowEventListener(EventConstants eventConstant, uint processId = 0, uint threadId = 0) : this(eventConstant, eventConstant, processId, threadId)
            { }
            /// <summary>
            /// The client thread that calls this must have a message loop in order to receive events.
            /// Listens to windows events for a specified interval of EventConstants. Can be limited to windows on a specific process or thread.
            /// </summary>
            /// <param name="eventMin">Specifies the event constant for the lowest event value in the range of events that are handled by the hook function. This parameter can be set to EVENT_MIN to indicate the lowest possible event value.</param>
            /// <param name="eventMax">Specifies the event constant for the highest event value in the range of events that are handled by the hook function. This parameter can be set to EVENT_MAX to indicate the highest possible event value.</param>
            /// <param name="processId">Specifies the ID of the process from which the hook function receives events. Specify zero (0) to receive events from all processes on the current desktop.</param>
            /// <param name="threadId">Specifies the ID of the thread from which the hook function receives events. If this parameter is zero, the hook function is associated with all existing threads on the current desktop.
            /// 
            /// Id can for example be retrieved in the following ways:
            /// # The "System.Diagnostics.ProcessThread"'s "Id" property.
            ///     # The "System.Diagnostics.Process"'s "Threads" property can be used to get "System.Diagnostics.ProcessThread" objects.
            /// # PInvoke to the Win32 API method "GetThreadId".
            ///     #https://msdn.microsoft.com/en-us/library/ms683233(VS.85).aspx
            /// # PInvoke to the Win32 API method "GetCurrentThreadID".
            ///     # https://msdn.microsoft.com/en-us/library/ms683183(VS.85).aspx
            ///     # Wrapped in deprecated "AppDomain.GetCurrentThreadId".
            /// Note: Do not use "System.Threading.Thread"'s "ManagedThreadId" property. ".Net" threads do not correspond to OS threads.
            /// </param>
            public WindowEventListener(EventConstants eventMin, EventConstants eventMax, uint processId = 0, uint threadId = 0)
            {
                callback = new Native.WinEventProc(WinEventProcCallback);
                eventHookHandle = Native.SetWinEventHook((uint)eventMin, (uint)eventMax, IntPtr.Zero, callback, processId, threadId, (uint)Native.HookFunctionFlags.WINEVENT_OUTOFCONTEXT);

                if (eventHookHandle == null || eventHookHandle == IntPtr.Zero)
                    throw new Exception("Failed to hook event");

                callbackGCHandle = GCHandle.Alloc(callback);
            }

            #endregion Constructors


            #region Methods

            /// <summary>
            /// If the client's thread ends, the system automatically disposes of the underlying callback.
            /// </summary>
            public virtual void Dispose()
            {
                lock (locker)
                {
                    if (isDisposed && callbackGCHandle == null)
                        return;

                    // If the client's thread ends, the system automatically calls this function.
                    // Call this function from the same thread that installed the event hook
                    if (!Native.UnhookWinEvent(eventHookHandle))
                    {
                        // Don't need to throw exception since we can just unhook on the next callback.
                        /*
                        throw new Exception("Failed to unhook event" + Environment.NewLine + Environment.NewLine +
                        "Common Reasons to fail:" + Environment.NewLine +
                        "# The event hook was already removed." + Environment.NewLine +
                        "# UnhookWinEvent is called from a thread that is different from the original call to SetWinEventHook.");
                        */
                    }
                    else
                    {
                        // Unhooked:
                        if (callbackGCHandle != null)
                        {
                            callbackGCHandle.Value.Free();
                            callbackGCHandle = null;
                        }
                    }

                    isDisposed = true;
                }
            }

            private void WinEventProcCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                lock (locker)
                {
                    if (isDisposed)
                    {
                        if (Native.UnhookWinEvent(hWinEventHook))
                        {
                            // Unhooked:
                            if (callbackGCHandle != null)
                            {
                                callbackGCHandle.Value.Free();
                                callbackGCHandle = null;
                            }
                        }
                        return;
                    }
                }
                OnCallback(hWinEventHook, eventType, hwnd, idObject, idChild, dwEventThread, dwmsEventTime);
            }

            protected virtual void OnCallback(IntPtr hWinEventHook, uint eventType, IntPtr hwnd, int idObject, int idChild, uint dwEventThread, uint dwmsEventTime)
            {
                var currentSafeEvent = SafeEvent;
                var currentUnsafeEvent = UnsafeEvent;
                if (currentSafeEvent == null && currentUnsafeEvent == null)
                    return;

                WindowEventListenerEventArgs args = new WindowEventListenerEventArgs(eventType, hwnd, idObject, idChild, dwEventThread, dwmsEventTime);

                if (currentSafeEvent != null)
                {
                    SyncObject.Post((state) =>
                    {
                        if (IsDisposed)
                            return;

                        currentSafeEvent(this, args);
                    }, null);
                }
                currentUnsafeEvent?.Invoke(this, args);
            }

            #endregion Methods


            #region Properties

            public bool IsDisposed
            {
                get
                {
                    lock (locker)
                    {
                        return isDisposed;
                    }
                }
            }

            #endregion Properties
        }


        /// <summary>
        /// A collection of events exposed by a shell event listener.
        /// </summary>
        /// <typeparam name="TShellEventListener">The type of the shell event listener.</typeparam>
        /// <typeparam name="TShellEventArgs">The type of event information provided by the listener.</typeparam>
        public interface IShellEvents<TShellEventListener, TShellEventArgs> where TShellEventListener : IDisposable
        {
            /// <summary>
            /// This event will be dispatched for any shell event that is received by the listener.
            /// </summary>
            event EventHandler<TShellEventArgs> ShellEvent;

            /// <summary>
            /// This event indicates that a window was opened.
            /// </summary>
            event EventHandler<TShellEventArgs> WindowOpened;
            /// <summary>
            /// This event indicates that a window was closed.
            /// </summary>
            event EventHandler<TShellEventArgs> WindowClosed;

            /// <summary>
            /// This event indicates that the currently selected window was changed.
            /// </summary>
            event EventHandler<TShellEventArgs> ForegroundWindowChanged;
            /// <summary>
            /// This event indicates that a window title was changed.
            /// </summary>
            event EventHandler<TShellEventArgs> WindowTitleChanged;
            /// <summary>
            /// This event indicates that a window was minimized.
            /// </summary>
            event EventHandler<TShellEventArgs> WindowMinimized;

            /// <summary>
            /// The listener that is the source of this event collection.
            /// </summary>
            TShellEventListener ShellEventListenerSource { get; }

            /// <summary>
            /// Used to run some code at a later time. This is a good idea to do when running complex code in response to an event where the "IsSafe" property is false.
            /// </summary>
            SynchronizationContext SyncObject { get; }

            /// <summary>
            /// If this is false then you must be careful with what code is run in response to an event.
            /// </summary>
            bool IsSafe { get; }
        }
        /// <summary>
        /// This class implements the basic functionality exposed by the IShellEvents interface.
        /// </summary>
        /// <typeparam name="TShellEventListener">This is the class that generates the events.</typeparam>
        /// <typeparam name="TShellEventArgs">The type of the event args.</typeparam>
        private class ShellEvents<TShellEventListener, TShellEventArgs> : IShellEvents<TShellEventListener, TShellEventArgs> where TShellEventListener : IDisposable
        {
            public delegate SynchronizationContext GetSyncObject();

            /// <summary>
            /// Any shell event that was received by this listener.
            /// </summary>
            public event EventHandler<TShellEventArgs> ShellEvent;

            public event EventHandler<TShellEventArgs> WindowOpened;
            public event EventHandler<TShellEventArgs> WindowClosed;

            public event EventHandler<TShellEventArgs> ForegroundWindowChanged;
            public event EventHandler<TShellEventArgs> WindowTitleChanged;
            public event EventHandler<TShellEventArgs> WindowMinimized;

            public TShellEventListener ShellEventListenerSource { get; }

            public SynchronizationContext SyncObject => getSync?.Invoke();
            private readonly GetSyncObject getSync = null;
            /// <summary>
            /// If this is false then you must be careful with what code is run in response to an event.
            /// </summary>
            public bool IsSafe { get; } = true;

            public ShellEvents(TShellEventListener shellEventListener, GetSyncObject getSync, bool isSafe)
            {
                ShellEventListenerSource = shellEventListener;
                this.getSync = getSync;
                IsSafe = isSafe;
            }

            public void RaiseEvents(ShellEventType nCode, TShellEventArgs args)
            {
                OnEvent(ShellEvent, args);
                OnEvent(GetAffectedEvent((ShellEventType)nCode), args);
            }
            protected EventHandler<TShellEventArgs> GetAffectedEvent(ShellEventType shellEventType)
            {
                switch (shellEventType)
                {
                    case ShellEventType.HSHELL_WINDOWCREATED:
                        return WindowOpened;
                    case ShellEventType.HSHELL_WINDOWDESTROYED:
                    case ShellEventType.HSHELL_WINDOWREPLACED:
                        return WindowClosed;
                    case ShellEventType.HSHELL_RUDEAPPACTIVATED:
                        return ForegroundWindowChanged;
                    case ShellEventType.HSHELL_REDRAW:
                        return WindowTitleChanged;
                    case ShellEventType.HSHELL_GETMINRECT:
                        return WindowMinimized;
                    default:
                        return null;
                }
            }
            protected void OnEvent(EventHandler<TShellEventArgs> eventToRaise, TShellEventArgs args)
            {
                if (eventToRaise == null)
                    return;

                if (!IsSafe)
                {
                    eventToRaise(this, args);
                }
                else
                {
                    SyncObject.Post((state) =>
                    {
                        eventToRaise(this, args);
                    }, null);
                }
            }
        }

        /// <summary>
        /// Listens to Shell Events. Uses RegisterShellHookWindow to get shell events sent as messages sent to an unopened form.
        /// Can only access one of the data variables passed along with the shell message.
        /// 
        /// Uses:
        /// Can be used to keep track of foreground window (HSHELL_RUDEAPPACTIVATED). (Doesn't detect tray icon context menu windows - window handle is Zero)
        /// Can be used to get notified on window open or close. (HSHELL_WINDOWCREATED) (HSHELL_WINDOWDESTROYED) (HSHELL_WINDOWREPLACED ?)
        /// Can be used to keep track of window titles. (HSHELL_REDRAW)
        /// Can be used to get notified on window minimize. (HSHELL_GETMINRECT)
        /// </summary>
        public class ShellEventListener : IDisposable
        {
            #region Classes

            private class SystemProcessHookForm : Form
            {
                #region Classes

                #endregion Classes


                #region Member Variables

                public delegate void MessageCallback(IntPtr wParam, IntPtr lParam);

                private readonly uint msgNotify;
                private readonly MessageCallback messageCallback;

                #endregion Member Variables


                #region Constructors

                public SystemProcessHookForm(MessageCallback messageCallback)
                {
                    // Hook on to the shell
                    msgNotify = Native.RegisterWindowMessage("SHELLHOOK");
                    if (msgNotify == 0)
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    if (!Native.RegisterShellHookWindow(this.Handle))
                    {
                        Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                    }
                    this.messageCallback = messageCallback;
                }

                #endregion Constructors


                #region Methods

                protected override void WndProc(ref Message m)
                {
                    if (m.Msg == msgNotify)
                    {
                        // Receive shell messages:
                        messageCallback(m.WParam, m.LParam);
                    }
                    base.WndProc(ref m);
                }

                protected override void Dispose(bool disposing)
                {
                    try
                    {
                        if (!Native.DeregisterShellHookWindow(this.Handle))
                        {
                            Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                        }
                    }
                    catch { }
                    base.Dispose(disposing);
                }

                #endregion Methods


                #region Properties

                #endregion Properties
            }

            public class ShellEventArgs : EventArgs
            {
                /// <summary>
                /// Indicates what type of shell event occurred.
                /// </summary>
                public ShellEventType EventType { get; }
                /// <summary>
                /// For most actions this will be a handle for the affected window.
                /// For some actions this handle is used for something else.
                /// </summary>
                public IntPtr EventData { get; }

                public IShellEvents<ShellEventListener, ShellEventArgs> EventSource { get; }

                public ShellEventArgs(IShellEvents<ShellEventListener, ShellEventArgs> shellEventsSource, IntPtr wParam, IntPtr lParam)
                {
                    EventSource = shellEventsSource;
                    EventType = (ShellEventType)wParam;
                    EventData = lParam;
                }
            }

            #endregion Classes


            #region Member Variables

            protected readonly object locker = new object();

            private SystemProcessHookForm hookForm = null;

            private readonly ShellEvents<ShellEventListener, ShellEventArgs> safeEvents = null;
            private readonly ShellEvents<ShellEventListener, ShellEventArgs> unsafeEvents = null;

            public SynchronizationContext SyncObject { get; } = SynchronizationContext.Current ?? new SynchronizationContext();

            #endregion Member Variables


            #region Constructors

            /// <summary>
            /// Calling thread must be a UI thread.
            /// </summary>
            public ShellEventListener() : this(true)
            { }
            protected ShellEventListener(bool createShellEvents)
            {
                if (createShellEvents)
                {
                    safeEvents = new ShellEvents<ShellEventListener, ShellEventArgs>(this, () => SyncObject, true);
                    unsafeEvents = new ShellEvents<ShellEventListener, ShellEventArgs>(this, () => SyncObject, false);
                }

                hookForm = new SystemProcessHookForm(OnShellMessage);
            }

            #endregion Constructors


            #region Methods

            public virtual void Dispose()
            {
                lock (locker)
                {
                    if (hookForm != null)
                    {
                        hookForm.Dispose();
                        hookForm = null;
                    }
                }
            }

            protected virtual void OnShellMessage(IntPtr wParam, IntPtr lParam)
            {
                ShellEventType type = (ShellEventType)wParam;
                safeEvents.RaiseEvents(type, new ShellEventArgs(SafeEvents, wParam, lParam));
                unsafeEvents.RaiseEvents(type, new ShellEventArgs(UnsafeEvents, wParam, lParam));
            }

            #endregion Methods


            #region Properties

            public bool IsDisposed
            {
                get
                {
                    lock (locker)
                    {
                        return hookForm == null;
                    }
                }
            }

            protected Form HookForm
            {
                get
                {
                    lock (locker)
                    {
                        return hookForm;
                    }
                }
            }

            public virtual IShellEvents<ShellEventListener, ShellEventArgs> SafeEvents { get { return safeEvents; } }
            public virtual IShellEvents<ShellEventListener, ShellEventArgs> UnsafeEvents { get { return unsafeEvents; } }

            #endregion Properties
        }

        /// <summary>
        /// Listens to Shell Events. Uses SetWindowsHookEx to intercept shell messages.
        /// Can access both data variables passed along with the shell message.
        /// 
        /// Note that currently this is only notified for events that happen for windows associated with the current thread.
        /// 
        /// /// Based on code from:
        /// https://stackoverflow.com/questions/25681443/how-to-detect-if-window-is-flashing
        /// https://msdn.microsoft.com/en-us/library/ms644991(v=VS.85).aspx
        /// </summary>
        public class AdvancedShellEventListener : IDisposable
        {
            #region Classes

            public class AdvancedShellEventArgs : EventArgs
            {
                /// <summary>
                /// Indicates what type of shell event occurred.
                /// </summary>
                public ShellEventType EventType { get; }
                /// <summary>
                /// For most actions this will be a handle for the affected window.
                /// For some actions this handle is used for something else.
                /// </summary>
                public IntPtr WParam { get; }
                /// <summary>
                /// Contains secondary data for the event. What it is used for depends on the event type.
                /// </summary>
                public IntPtr LParam { get; }

                public IShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> EventSource { get; }

                public AdvancedShellEventArgs(IShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> shellEventsSource, ShellEventType shellEvent, IntPtr wParam, IntPtr lParam)
                {
                    EventSource = shellEventsSource;
                    EventType = shellEvent;
                    WParam = wParam;
                    LParam = lParam;
                }
            }

            #endregion Classes


            #region Member Variables

            private Native.CallShellProc procShell = null;
            private IntPtr hookProcedureHandle = IntPtr.Zero;

            private readonly ShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> safeEvents = null;
            private readonly ShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> unsafeEvents = null;

            public SynchronizationContext SyncObject { get; } = SynchronizationContext.Current ?? new SynchronizationContext();

            #endregion Member Variables


            #region Constructors

            public AdvancedShellEventListener()
            {
                safeEvents = new ShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs>(this, () => SyncObject, true);
                unsafeEvents = new ShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs>(this, () => SyncObject, false);

                // Use a threadId to only get messages related to that thread.
                // To not use this we would need to specify a module handle.
                int threadID = Native.GetCurrentThreadId();

                // we are interested in listening to WH_SHELL events.
                procShell = ShellProc;
                hookProcedureHandle = Native.SetWindowsHookEx(Native.Hook.WH_SHELL, procShell, IntPtr.Zero, threadID);

                if (hookProcedureHandle == IntPtr.Zero)
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                System.Windows.Forms.Application.ApplicationExit += Application_ApplicationExit;
            }

            #endregion Constructors


            #region Methods

            private IntPtr ShellProc(int nCode, IntPtr wParam, IntPtr lParam)
            {
                OnMessage(nCode, wParam, lParam);
                return Native.CallNextHookEx(hookProcedureHandle, nCode, wParam, lParam);
            }

            protected virtual void OnMessage(int nCode, IntPtr wParam, IntPtr lParam)
            {
                ShellEventType type = (ShellEventType)nCode;
                safeEvents.RaiseEvents(type, new AdvancedShellEventArgs(SafeEvents, type, wParam, lParam));
                unsafeEvents.RaiseEvents(type, new AdvancedShellEventArgs(UnsafeEvents, type, wParam, lParam));
            }


            private void Application_ApplicationExit(object sender, EventArgs e)
            {
                Dispose();
            }

            public void Dispose()
            {
                if (IsDisposed)
                    return;

                if (!Native.UnhookWindowsHookEx(hookProcedureHandle))
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

                System.Windows.Forms.Application.ApplicationExit -= Application_ApplicationExit;
                hookProcedureHandle = IntPtr.Zero;
            }

            #endregion Methods


            #region Properties

            public bool IsDisposed
            {
                get { return hookProcedureHandle == IntPtr.Zero; }
            }

            public virtual IShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> SafeEvents { get { return safeEvents; } }
            public virtual IShellEvents<AdvancedShellEventListener, AdvancedShellEventArgs> UnsafeEvents { get { return unsafeEvents; } }

            #endregion Properties
        }


        /// <summary>
        /// Uses NativeWindow class to listen to operating system messages for a window.
        /// 
        /// Based on code from:
        /// https://msdn.microsoft.com/en-us/library/system.windows.forms.nativewindow%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
        /// </summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        public class NativeWindowListener<TMonitoredForm> : NativeWindowListener where TMonitoredForm : Form
        {
            #region Classes

            #endregion Classes


            #region Member Variables

            public TMonitoredForm MonitoredForm { get; }

            #endregion Member Variables


            #region Constructors

            public NativeWindowListener(TMonitoredForm formToMonitor)
            {
                if (formToMonitor.IsDisposed)
                    throw new ArgumentException("Cannot use disposed form.");

                this.MonitoredForm = formToMonitor;


                if (formToMonitor.Handle == IntPtr.Zero)
                    formToMonitor.HandleCreated += OnHandleCreated;
                else
                    AssignHandle(formToMonitor.Handle);


                formToMonitor.HandleDestroyed += OnHandleDestroyed;
            }

            #endregion Constructors


            #region Methods

            // Listen for the control's window creation and then hook into it.
            internal void OnHandleCreated(object sender, EventArgs e)
            {
                // Window is now created, assign handle to NativeWindow.
                if (!IsDisposed)
                    AssignHandle((sender as TMonitoredForm).Handle);
            }
            internal void OnHandleDestroyed(object sender, EventArgs e)
            {
                // Window was destroyed, release hook.
                Dispose();
            }
            public override void Dispose()
            {
                base.Dispose();
                this.MonitoredForm.HandleCreated -= OnHandleCreated;
                this.MonitoredForm.HandleDestroyed -= OnHandleDestroyed;
            }

            #endregion Methods


            #region Properties

            #endregion Properties
        }

        /// <summary>
        /// Uses NativeWindow class to listen to operating system messages for a window.
        /// 
        /// Uses:
        /// Can listen to WM_ACTIVATEAPP (0x001C) to detect when a window is activated.
        /// 
        /// Based on code from:
        /// https://msdn.microsoft.com/en-us/library/system.windows.forms.nativewindow%28v=vs.110%29.aspx?f=255&MSPPError=-2147217396
        /// </summary>
        [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
        public class NativeWindowListener : NativeWindow, IDisposable
        {
            #region Classes

            public class WindowMessageEventArgs : EventArgs
            {
                public IntPtr WindowHandle { get; }
                public int Msg { get; }
                public IntPtr WParam { get; }
                public IntPtr LParam { get; }

                public WindowMessageEventArgs(Message message) : this(message.HWnd, message.Msg, message.WParam, message.LParam)
                { }
                public WindowMessageEventArgs(IntPtr windowHandle, int Msg, IntPtr wParam, IntPtr lParam)
                {
                    this.WindowHandle = windowHandle;
                    this.Msg = Msg;
                    this.WParam = wParam;
                    this.LParam = lParam;
                }
            }

            #endregion Classes


            #region Member Variables

            public SynchronizationContext SyncObject { get; } = SynchronizationContext.Current ?? new SynchronizationContext();
            private volatile bool isDisposed = false;

            public event EventHandler<WindowMessageEventArgs> SafeWindowMessage;
            public event EventHandler<WindowMessageEventArgs> UnsafeWindowMessage;

            public event EventHandler<EventArgs> HandleChanged;

            #endregion Member Variables


            #region Constructors

            public NativeWindowListener(IntPtr windowHandle)
            {
                AssignHandle(windowHandle);
            }

            protected NativeWindowListener()
            { }

            #endregion Constructors


            #region Methods

            protected override void OnHandleChange()
            {
                base.OnHandleChange();
                HandleChanged?.Invoke(this, EventArgs.Empty);
            }

            [System.Security.Permissions.PermissionSet(System.Security.Permissions.SecurityAction.Demand, Name = "FullTrust")]
            protected override void WndProc(ref Message m)
            {
                // Listen for operating system messages
                OnWindowMessage(m);
                base.WndProc(ref m);
            }

            protected virtual void OnWindowMessage(Message message)
            {
                var currentSafeEvent = SafeWindowMessage;
                var currentUnsafeEvent = UnsafeWindowMessage;
                if (currentSafeEvent == null && currentUnsafeEvent == null)
                    return;

                WindowMessageEventArgs args = new WindowMessageEventArgs(message);

                if (currentSafeEvent != null)
                {
                    SyncObject.Post((state) =>
                    {
                        currentSafeEvent(this, args);
                    }, null);
                }
                currentUnsafeEvent?.Invoke(this, args);
            }


            public virtual void Dispose()
            {
                if (IsDisposed)
                    return;

                ReleaseHandle();    // Stop intercepting messages.
                isDisposed = true;
            }

            #endregion Methods


            #region Properties

            public bool IsDisposed
            {
                get { return isDisposed; }
            }

            #endregion Properties
        }

        #endregion Event Listeners


        /// <summary>
        /// WORK IN PROGRESS
        /// </summary>
        public class WindowsMonitor
        {
            #region Classes

            private class OpenCloseAutomationEventListener : IDisposable
            {
                #region Classes

                public class WindowOpenedEventArgs : EventArgs
                {
                    public int ProcessId { get; }
                    public IntPtr WindowHandle { get; }

                    public WindowOpenedEventArgs(AutomationElement automationElement)
                    {
                        var current = automationElement.Current;
                        ProcessId = current.ProcessId;
                        WindowHandle = new IntPtr(current.NativeWindowHandle);
                    }
                }

                #endregion Classes


                #region Member Variables

                public event EventHandler<WindowOpenedEventArgs> WindowOpened;
                public event EventHandler<EventArgs> WindowClosed;

                private readonly object locker = new object();
                private bool isDisposed = false;

                #endregion Member Variables


                #region Constructors

                public OpenCloseAutomationEventListener()
                {
                    Automation.AddAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, TreeScope.Children, OnWindowOpened);
                    Automation.AddAutomationEventHandler(WindowPattern.WindowClosedEvent, AutomationElement.RootElement, TreeScope.Children, OnWindowClosed);
                }

                #endregion Constructors


                #region Methods

                public void Dispose()
                {
                    lock (locker)
                    {
                        if (!isDisposed)
                        {
                            Automation.RemoveAutomationEventHandler(WindowPattern.WindowOpenedEvent, AutomationElement.RootElement, OnWindowOpened);
                            Automation.RemoveAutomationEventHandler(WindowPattern.WindowClosedEvent, AutomationElement.RootElement, OnWindowClosed);
                            isDisposed = true;
                        }
                    }
                }

                private void OnWindowOpened(object sender, AutomationEventArgs e)
                {
                    WindowOpened?.Invoke(this, new WindowOpenedEventArgs(sender as AutomationElement));
                }
                private void OnWindowClosed(object sender, AutomationEventArgs e)
                {
                    AutomationElement element = sender as AutomationElement;
                    // Can't get info since window is closed

                    // Try:
                    var current = element.Current;
                    var id = current.ProcessId;
                    var handle = current.NativeWindowHandle;

                    var cached = element.Cached;
                    id = cached.ProcessId;
                    handle = cached.NativeWindowHandle;

                    WindowClosed?.Invoke(this, EventArgs.Empty);
                }

                #endregion Methods


                #region Properties

                public bool IsDisposed
                {
                    get
                    {
                        lock (locker)
                        {
                            return isDisposed;
                        }
                    }
                }

                #endregion Properties
            }

            public interface IWindowInfo
            {
                IntPtr WindowHandle { get; }
                string WindowTitle { get; }
                bool IsForegroundWindow { get; }
                bool IsClosed { get; }

                event EventHandler<EventArgs> WindowTitleChanged;
                event EventHandler<EventArgs> ForegroundStatusChanged;
                event EventHandler<EventArgs> Closed;
            }

            private class WindowInfo : IWindowInfo
            {
                #region Classes

                #endregion Classes


                #region Member Variables

                private readonly object locker = new object();

                public IntPtr WindowHandle { get; } = IntPtr.Zero;

                private bool isClosed = false;
                private WindowsMonitor monitor = null;

                #endregion Member Variables


                #region Constructors

                public WindowInfo(IntPtr windowHandle)
                {
                    WindowHandle = windowHandle;
                }

                #endregion Constructors


                #region Methods

                #endregion Methods


                #region Properties

                public string WindowTitle
                {
                    get
                    {
                        throw new NotImplementedException();
                    }
                }


                public bool IsForegroundWindow
                {
                    get
                    {
                        return monitor.ForegroundWindow == this;
                    }
                }

                public bool IsClosed
                {
                    get
                    {
                        lock (locker)
                        {
                            return isClosed;
                        }
                    }
                }

                public event EventHandler<EventArgs> WindowTitleChanged
                {
                    add
                    {
                        throw new NotImplementedException();
                    }
                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
                public event EventHandler<EventArgs> ForegroundStatusChanged
                {
                    add
                    {
                        throw new NotImplementedException();
                    }
                    remove
                    {
                        throw new NotImplementedException();
                    }
                }
                public event EventHandler<EventArgs> Closed
                {
                    add
                    {
                        throw new NotImplementedException();
                    }
                    remove
                    {
                        throw new NotImplementedException();
                    }
                }

                #endregion Properties
            }

            #endregion Classes


            #region Member Variables

            public SynchronizationContext SyncObject { get; } = SynchronizationContext.Current ?? new SynchronizationContext();

            private readonly object locker = new object();
            private readonly object pollLocker = new object();

            private OpenCloseAutomationEventListener openCloseAutomationListener = null;
            private ShellEventListener openCloseShellListener = null;

            private List<WindowInfo> trackedWindows = new List<WindowInfo>();


            private event EventHandler<IWindowInfo> WindowOpenedEvent;
            private event EventHandler<IWindowInfo> WindowClosedEvent;
            private event EventHandler<IWindowInfo> ForegroundWindowChangedEvent;

            #endregion Member Variables


            #region Constructors

            public WindowsMonitor()
            {
                openCloseShellListener = new ShellEventListener();
                openCloseShellListener.UnsafeEvents.WindowOpened += ShellListener_UnsafeEvents_WindowOpened;
                openCloseShellListener.UnsafeEvents.WindowClosed += ShellListener_UnsafeEvents_WindowClosed;
                PollOpenWindows();
            }

            #endregion Constructors


            #region Methods

            private void ShellListener_UnsafeEvents_WindowOpened(object sender, ShellEventListener.ShellEventArgs e)
            {
                throw new NotImplementedException();
            }

            private void ShellListener_UnsafeEvents_WindowClosed(object sender, ShellEventListener.ShellEventArgs e)
            {
                throw new NotImplementedException();
            }

            private void PollOpenWindows()
            {
                lock (pollLocker)
                {
                    List<IntPtr> windowHandles = GetAllWindows();
                    windowHandles.Sort();
                    lock (locker)
                    {
                        foreach (var trackedWindow in trackedWindows)
                        {

                        }
                    }
                }
            }

            #endregion Methods


            #region Properties

            public event EventHandler<IWindowInfo> WindowOpened
            {
                add
                {
                    WindowOpenedEvent += value;
                }
                remove
                {
                    WindowOpenedEvent -= value;
                }
            }
            public event EventHandler<IWindowInfo> WindowClosed
            {
                add
                {
                    WindowClosedEvent += value;
                }
                remove
                {
                    WindowClosedEvent -= value;
                }
            }
            public event EventHandler<IWindowInfo> ForegroundWindowChanged
            {
                add
                {
                    ForegroundWindowChangedEvent += value;
                }
                remove
                {
                    ForegroundWindowChangedEvent -= value;
                }
            }

            public IWindowInfo ForegroundWindow
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            public IWindowInfo[] AllWindows
            {
                get
                {
                    throw new NotImplementedException();
                }
            }

            #endregion Properties
        }

        #endregion Classes


        #region Methods

        #region Get Window Info

        /// <summary>
        /// Get the process that created a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the created window.</param>
        /// <returns>The id for the process that created the window.</returns>
        public static Process GetWindowProcess(IntPtr windowHandle)
        {
            return Process.GetProcessById(GetWindowProcessID(windowHandle));
        }

        /// <summary>
        /// Get the id for the process that created a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the created window.</param>
        /// <returns>The id for the process that created the window.</returns>
        public static int GetWindowProcessID(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                return 0;

            Native.GetWindowThreadProcessId(windowHandle, out uint windowProcessId);
            return (int)windowProcessId;
        }

        /// <summary>
        /// Get the title text of a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window.</param>
        /// <returns>Title text of the window.</returns>
        public static string GetWindowTitle(IntPtr windowHandle)
        {
            if (windowHandle == null)
                return null;

            int capacity = Native.GetWindowTextLength(windowHandle) * 2;
            if (capacity == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }

            StringBuilder stringBuilder = new StringBuilder(capacity);
            int returned = Native.GetWindowText(windowHandle, stringBuilder, stringBuilder.Capacity);
            if (returned == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return stringBuilder.ToString();
        }

        /// <summary>
        /// Retrieves the name of the class to which the specified window belongs.
        /// </summary>
        /// <param name="windowHandle">Handle for the window.</param>
        /// <returns>Name of the class the window belongs to.</returns>
        public static string GetWindowClassName(IntPtr windowHandle)
        {
            // Pre-allocate 256 characters, since this is the maximum class name length.
            StringBuilder className = new StringBuilder(256);
            //Get the window class name
            int classNameLength = Native.GetClassName(windowHandle, className, className.Capacity);
            if (classNameLength == 0)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                return null;
            }
            return className.ToString();
        }

        /// <summary>
        /// Retrieves information about the specified window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window whose information is to be retrieved. </param>
        /// <returns>A structure that contains the window's information.</returns>
        public static WindowInfo GetWindowInfo(IntPtr windowHandle)
        {
            Native.WINDOWINFO info = new Native.WINDOWINFO(true);

            if (!Native.GetWindowInfo(windowHandle, ref info))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            return info;
        }

        /// <summary>
        /// Retrieves the dimensions of the bounding rectangle of the specified window. The dimensions are given in screen coordinates that are relative to the upper-left corner of the screen.
        /// </summary>
        /// <param name="windowHandle">A handle to the window whose location should be determined.</param>
        /// <param name="extendedFrameBounds">
        ///     Get the window's size with its extended frame bounds, this will exclude the window's drop shadow from the bounds.
        ///     
        ///     If this is false then you might get faulty values. This is largely due to Aero incorporating additional invisible borders which are used to "resize" the window using the cursor.
        ///     See <see href="https://docs.microsoft.com/en-us/windows/win32/api/winuser/nf-winuser-getwindowrect#remarks"/> for more details.
        /// </param>
        /// <returns>The bounds of the specified window.</returns>
        public static Rectangle GetWindowLocation(IntPtr windowHandle, bool extendedFrameBounds = true)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(windowHandle));

            Native.RECT bounds;
            if (extendedFrameBounds)
            {
                Marshal.ThrowExceptionForHR(Native.DwmGetWindowAttribute(windowHandle, Native.DwmWindowAttribute.DWMWA_EXTENDED_FRAME_BOUNDS, out bounds, Marshal.SizeOf(typeof(Native.RECT))));
            }
            else
            {
                if (!Native.GetWindowRect(windowHandle, out bounds))
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return bounds;
        }

        #endregion Get Window Info


        #region Get Window Collection

        /// <summary>
        /// Get all windows created by a process.
        /// </summary>
        /// <param name="process">The process that created the windows.</param>
        /// <returns>A list of handles for the windows created by the spcified process.</returns>
        public static List<IntPtr> GetAllWindowsForProcess(Process process)
        {
            return GetAllWindowsForProcess(process.Id);
        }

        /// <summary>
        /// Get all windows created by a process.
        /// Implementation: Get all windows and filter them based on processId.
        /// </summary>
        /// <param name="processID">Id for the process that created the windows.</param>
        /// <returns>A list of handles for the windows created by the spcified process.</returns>
        public static List<IntPtr> GetAllWindowsForProcess(int processID)
        {
            List<IntPtr> processWindows = new List<IntPtr>();
            foreach (IntPtr windowHandle in GetAllWindows())
            {
                if (GetWindowProcessID(windowHandle) == processID)
                    processWindows.Add(windowHandle);
            }
            return processWindows;
        }


        /// <summary>
        /// Get main windows for all processes.
        /// Only windows visible in the taskbar can be main windows.
        /// </summary>
        /// <returns>A list with the main windows for all processes.</returns>
        public static List<IntPtr> GetAllMainWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            foreach (Process p in Process.GetProcesses())
            {
                IntPtr mainWindow = p.MainWindowHandle;
                if (mainWindow != IntPtr.Zero && !windows.Contains(mainWindow))
                    windows.Add(mainWindow);
            }
            return windows;
        }

        /// <summary>
        /// Get all windows.
        /// </summary>
        /// <returns>A list with all windows.</returns>
        public static List<IntPtr> GetAllWindows()
        {
            return GetAllDescendantWindows(IntPtr.Zero);
        }

        /// <summary>
        /// Get all child / descendant windows of a parent window. (Is recursive so child windows of the child windows will be included)
        /// </summary>
        /// <param name="parentWindowHandle">Handle for window to find child windows for. (Use zero/null to get all windows.)</param>
        /// <returns>A list with handles for all child / descendant windows.</returns>
        public static List<IntPtr> GetAllDescendantWindows(IntPtr parentWindowHandle)
        {
            return EnumWindowsHelper((listPointer) =>
            {
                Native.EnumChildWindows(parentWindowHandle, new Native.EnumChildProc(EnumWindow), listPointer);
            });
        }

        /// <summary>
        /// Get all top level windows on the screen. 
        /// Windows owned by the system that have the WS_CHILD style will be retrieved as well.
        /// </summary>
        /// <returns>A list with handles for all top level windows.</returns>
        public static List<IntPtr> GetAllTopLevelWindows()
        {
            return EnumWindowsHelper((listPointer) =>
            {
                if (!Native.EnumWindows(new Native.EnumWindowsProc(EnumWindow), listPointer))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            });
        }

        /// <summary>
        /// Get all top-level windows associated with a specified desktop
        /// </summary>
        /// <param name="desktopHandle">A handle to the desktop whose top-level windows to get. If this parameter is NULL, the current desktop is used.</param>
        /// <returns>A list with handles for all top level windows on the specified desktop.</returns>
        public static List<IntPtr> GetAllDesktopTopLevelWindows(IntPtr? desktopHandle = null)
        {
            return EnumWindowsHelper((listPointer) =>
            {
                if (!Native.EnumDesktopWindows(desktopHandle ?? IntPtr.Zero, new Native.EnumWindowsProc(EnumWindow), listPointer))
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
            });
        }

        /// <summary>
        /// Get all nonchild windows associated with a thread.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread whose windows to get.
        /// Id can for example be retrieved in the following ways:
        /// # The "System.Diagnostics.ProcessThread"'s "Id" property.
        ///     # The "System.Diagnostics.Process"'s "Threads" property can be used to get "System.Diagnostics.ProcessThread" objects.
        /// # PInvoke to the Win32 API method "GetThreadId".
        ///     #https://msdn.microsoft.com/en-us/library/ms683233(VS.85).aspx
        /// # PInvoke to the Win32 API method "GetCurrentThreadID".
        ///     # https://msdn.microsoft.com/en-us/library/ms683183(VS.85).aspx
        ///     # Wrapped in deprecated "AppDomain.GetCurrentThreadId".
        /// Note: Do not use "System.Threading.Thread"'s "ManagedThreadId" property. ".Net" threads do not correspond to OS threads.
        /// </param>
        /// <returns>A list with handles for all nonchild windows associated with a thread.</returns>
        public static List<IntPtr> GetAllThreadNonChildWindows(int threadId)
        {
            return EnumWindowsHelper((listPointer) =>
            {
                Native.EnumThreadWindows((uint)threadId, new Native.EnumThreadWndProc(EnumWindow), listPointer);
            });
        }

        private delegate void EnumWindowsHelperCallback(IntPtr listPointer);
        private static List<IntPtr> EnumWindowsHelper(EnumWindowsHelperCallback callback)
        {
            List<IntPtr> enumeratedWindows = new List<IntPtr>();
            GCHandle? listHandle = null;
            try
            {
                listHandle = GCHandle.Alloc(enumeratedWindows);

                callback(GCHandle.ToIntPtr(listHandle.Value));
            }
            finally
            {
                if (listHandle.HasValue && listHandle.Value.IsAllocated)
                    listHandle.Value.Free();
            }
            return enumeratedWindows;
        }
        private static bool EnumWindow(IntPtr handle, IntPtr pointer)
        {
            GCHandle gch = GCHandle.FromIntPtr(pointer);
            var list = gch.Target as List<IntPtr>;
            if (list == null)
            {
                throw new InvalidCastException("GCHandle Target could not be cast as List<IntPtr>");
            }
            list.Add(handle);
            return true;
        }


        /// <summary>
        /// Gets all shell windows. Essentially only explorer.exe windows.
        /// </summary>
        /// <returns>A list with a window handle for each shell window.</returns>
        public static List<IntPtr> GetAllShellWindows()
        {
            List<IntPtr> windows = new List<IntPtr>();
            // Note: SHDocVw requries a reference to the COM component called "Microsoft Internet Controls".
            SHDocVw.ShellWindows shellWindows = new SHDocVw.ShellWindows();

            foreach (SHDocVw.InternetExplorer ie in shellWindows)
            {
                if (ie != null && !windows.Any(e => e.ToInt32() == ie.HWND))
                    windows.Add(new IntPtr(ie.HWND));
            }
            return windows;
        }

        #endregion Get Window Collection


        #region Get Specific Window

        /// <summary>
        /// Retrieves a handle to the specified window's parent or owner.
        /// </summary>
        /// <param name="childWindowHandle">A handle to the window whose parent window handle is to be retrieved.</param>
        /// <returns>If the window is a child window, the return value is a handle to the parent window. 
        /// If the window is a top-level window with the WS_POPUP style, the return value is a handle to the owner window.</returns>
        public static IntPtr GetParentWindow(IntPtr childWindowHandle)
        {
            IntPtr result = Native.GetParent(childWindowHandle);
            if (result == IntPtr.Zero)
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
            return result;
        }

        /// <summary>
        /// Retrieves a handle to the specified window's root parent or owner.
        /// (Walks the parents provided by GetParentWindow.)
        /// </summary>
        /// <param name="childWindowHandle">A handle to the window whose parent window handle is to be retrieved.</param>
        /// <returns>Handle for the provided window's root parent or owner. If the provided window is the desktop window, the function returns NULL</returns>
        public static IntPtr GetRootParentWindow(IntPtr childWindowHandle)
        {
            return Native.GetAncestor(childWindowHandle, Native.Ancestor.GA_ROOTOWNER);
        }


        /// <summary>
        /// Gets the handle for a process' main window.
        /// </summary>
        /// <param name="processName">Name of the process.</param>
        /// <returns>Handle for the process' main window. Handle will be zero if process couldn't be found or if the window is hidden.</returns>
        public static IntPtr GetMainWindowForProcess(string processName)
        {
            Process[] processes = Process.GetProcessesByName(processName);

            foreach (Process p in processes)
            {
                return p.MainWindowHandle;
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Find a window with a spcified title.
        /// Retrieves a handle to the top-level window whose window name match the specified string. This function does not search child windows. This function does not perform a case-sensitive search.
        /// </summary>
        /// <param name="windowTitle">The window's title. If this parameter is NULL, all window names match.</param>
        /// <returns>Handle to a window with the specified title.</returns>
        public static IntPtr GetWindowFromWindowTitle(string windowTitle)
        {
            return GetWindowFromClassAndWindowTitle(null, windowTitle);
        }

        /// <summary>
        /// Find a window with a spcified title and class name.
        /// Retrieves a handle to the top-level window whose class name and window name match the specified strings. This function does not search child windows. This function does not perform a case-sensitive search.
        /// </summary>
        /// <param name="className">Specifies the window class name. If this parameter is NULL, it finds any window whose title matches the windowTitle parameter.</param>
        /// <param name="windowTitle">The window's title. If this parameter is NULL, all window names match.</param>
        /// <returns>Handle to a window with the specified info.</returns>
        public static IntPtr GetWindowFromClassAndWindowTitle(string className, string windowTitle)
        {
            IntPtr handle = Native.FindWindow(className, windowTitle);
            if (handle == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            return handle;
        }

        /// <summary>
        /// Find a window with a spcified title and class name. Starts search from a child window.
        /// Retrieves a handle to a window whose class name and window name match the specified strings. The function searches child windows, beginning with the one following the specified child window. This function does not perform a case-sensitive search. 
        /// Note that if both parent and childAfter are NULL, the function searches all top-level and message-only windows. 
        /// </summary>
        /// <param name="parent">A handle to the parent window whose child windows are to be searched. If NULL then the function uses the desktop window as the parent window.</param>
        /// <param name="childAfter">
        /// <para>A handle to a child window. The search begins with the next child window in the Z order. The child window must be a direct child window of the parent window used for the search, not just a descendant window.</para>
        /// <para>If NULL then the search begins with the first child window of hwndParent.</para>
        /// </param>
        /// <param name="className">Specifies the window class name. If this parameter is NULL, it finds any window whose title matches the windowTitle parameter.</param>
        /// <param name="windowTitle">The window's title. If this parameter is NULL, all window names match.</param>
        /// <returns>Handle to a window with the specified info.</returns>
        public static IntPtr GetWindowFromClassAndWindowTitle(IntPtr parent, IntPtr childAfter, string className, string windowTitle)
        {
            IntPtr handle = Native.FindWindowEx(parent, childAfter, className, windowTitle);
            if (handle == IntPtr.Zero)
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());

            return handle;
        }

        /// <summary>
        /// Retrieves a handle to the foreground window (the window with which the user is currently working). The system assigns a slightly higher priority to the thread that creates the foreground window than it does to other threads.
        /// </summary>
        /// <returns>The return value is a handle to the foreground window. The foreground window can be NULL in certain circumstances, such as when a window is losing activation.</returns>
        public static IntPtr GetForegroundWindow()
        {
            return Native.GetForegroundWindow();
        }

        /// <summary>
        /// Get a Handle for the Desktop. Can for example be used to set desktop as foreground window to deactivate the current window.
        /// </summary>
        /// <returns></returns>
        public static IntPtr GetDesktopWindow()
        {
            return Native.GetDesktopWindow();
        }

        #endregion Get Specific Window


        #region Manipulate Window

        [Flags]
        public enum FlashWindowFlags : uint
        {
            /// <summary>
            /// Stop flashing. The system restores the window to its original state. 
            /// </summary>    
            FLASHW_STOP = 0,

            /// <summary>
            /// Flash the window caption 
            /// </summary>
            FLASHW_CAPTION = 1,

            /// <summary>
            /// Flash the taskbar button. 
            /// </summary>
            FLASHW_TRAY = 2,

            /// <summary>
            /// Flash both the window caption and taskbar button.
            /// This is equivalent to setting the FLASHW_CAPTION | FLASHW_TRAY flags. 
            /// </summary>
            FLASHW_ALL = 3,

            /// <summary>
            /// Flash continuously, until the FLASHW_STOP flag is set.
            /// </summary>
            FLASHW_TIMER = 4,

            /// <summary>
            /// Flash continuously until the window comes to the foreground. 
            /// </summary>
            FLASHW_TIMERNOFG = 12
        }

        /// <summary>
        /// Flashes the specified window. It does not change the active state of the window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window to be flashed. The window can be either opened or minimized.</param>
        /// <param name="flags">The flash status. This parameter can be one or more of the flag values.</param>
        /// <param name="count">The number of times to flash the window.</param>
        /// <param name="intervallInMilliseconds">The rate at which the window is to be flashed, in milliseconds. If zero, the function uses the default cursor blink rate.</param>
        /// <returns>Specifies the window's state before the new flash information is applied. If the window caption/title was drawn as active before the call, the return value is true. Otherwise, the return value is false.</returns>
        public static bool FlashWindow(IntPtr windowHandle, FlashWindowFlags flags, uint count = 1, uint intervallInMilliseconds = 0)
        {
            // A boolean value indicating whether the application is running on Windows 2000 or later.            
            bool isWin2000OrLater = System.Environment.OSVersion.Version.Major >= 5;
            if (!isWin2000OrLater)
                return false;   // Not Supported

            Native.FLASHWINFO info = new Native.FLASHWINFO(true)
            {
                hwnd = windowHandle,
                dwFlags = (uint)flags,
                uCount = count,
                dwTimeout = intervallInMilliseconds,
            };
            try
            {
                return Native.FlashWindowEx(ref info);
            }
            finally
            {
                GC.KeepAlive(info);
            }
        }

        /// <summary>
        /// Flashes the specified window one time. It does not change the active state of the window.
        /// </summary>
        /// <param name="windowHandle">A handle to the window to be flashed. The window can be either open or minimized.</param>
        /// <param name="invertActiveState">
        /// If this parameter is TRUE, the window is flashed from one state to the other. If it is FALSE, the window is returned to its original state (either active or inactive).
        /// When an application is minimized and this parameter is TRUE, the taskbar window button flashes active/inactive. If it is FALSE, the taskbar window button flashes inactive, meaning that it does not change colors. It flashes, as if it were being redrawn, but it does not provide the visual invert clue to the user.
        /// </param>
        /// <returns>
        /// The return value specifies the window's state before the call to the FlashWindow function. If the window caption was drawn as active before the call, the return value is nonzero. Otherwise, the return value is zero.
        /// </returns>
        public static bool FlashWindow(IntPtr windowHandle, bool invertActiveState)
        {
            return Native.FlashWindow(windowHandle, invertActiveState);
        }


        public static void SetFocus(IntPtr windowHandle)
        {
            const uint WM_SETFOCUS = 0x0007;
            PostMessageToWindow(windowHandle, WM_SETFOCUS, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Kill a windows focus.
        /// </summary>
        /// <param name="windowHandle">Handle for the window whose focus should be killed.</param>
        public static void KillFocus(IntPtr windowHandle)
        {
            const uint WM_KILLFOCUS = 0x0008;
            PostMessageToWindow(windowHandle, WM_KILLFOCUS, IntPtr.Zero, IntPtr.Zero);
        }


        public enum ActivatedState
        {
            /// <summary>
            /// Activated by some method other than a mouse click (for example, by a call to the SetActiveWindow function or by use of the keyboard interface to select the window).
            /// </summary>
            WA_ACTIVE = 1,
            /// <summary>
            /// Activated by a mouse click.
            /// </summary>
            WA_CLICKACTIVE = 2,
            /// <summary>
            /// Deactivated.
            /// </summary>
            WA_INACTIVE = 0,
        }

        /// <summary>
        /// Activate or deactivate a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window to activate or deativate.</param>
        /// <param name="activated">Specifies whether the window is being activated or deactivated.</param>
        public static void ChangeActivated(IntPtr windowHandle, ActivatedState activated)
        {
            const uint WM_ACTIVATE = 0x0006;
            PostMessageToWindow(windowHandle, WM_ACTIVATE, new IntPtr((((int)activated) << 16) + (Native.IsIconic(windowHandle) ? 1 : 0)), IntPtr.Zero);
        }

        /// <summary>
        /// Sent to a window when its nonclient area needs to be changed to indicate an active or inactive state.
        /// </summary>
        /// <param name="windowHandle"></param>
        /// <param name="activated">Indicates when a title bar or icon needs to be changed to indicate an active or inactive state. TRUE if an active title bar or icon is to be drawn. Flase if an inactive title bar or icon is to be drawn.</param>
        public static void ChangeActivatedForNonclientArea(IntPtr windowHandle, bool activated)
        {
            const uint WM_NCACTIVATE = 0x0086;
            PostMessageToWindow(windowHandle, WM_NCACTIVATE, new IntPtr(activated ? 1 : 0), IntPtr.Zero);
        }

        /// <summary>
        /// Bring a window to the front.
        /// </summary>
        /// <param name="windowHandle">Handle for the window.</param>
        public static void BringWindowToFront(IntPtr windowHandle)
        {
            // Verify that the handle is a running process.
            if (windowHandle == IntPtr.Zero)
                return;

            // Restores from minimized state (if neccessary)
            RestoreWindow(windowHandle);

            // Make window the foreground application
            Native.SetForegroundWindow(windowHandle);
        }

        /// <summary>
        /// Set the foreground window (the focused window).
        /// </summary>
        /// <param name="windowHandle">The window to focus.</param>
        public static void SetForegroundWindow(IntPtr windowHandle)
        {
            Native.SetForegroundWindow(windowHandle);
        }

        /// <summary>
        /// Show or Hide a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window.</param>
        /// <param name="visible">Determines if the window should be hidden or shown.</param>
        /// <param name="asyncOperation">Determines if the function should wait for the operation to complete. TRUE to not wait for the operation to complete.</param>
        /// <returns>
        /// <para>If the operation was asynchronous this value is TRUE if operation was successfully started.</para>
        /// <para>If the opeartion was synchronous this value is TRUE if the window was previously visible.</para>
        /// </returns>
        public static bool ChangeWindowVisibility(IntPtr windowHandle, bool visible, bool showWithoutActivation = true, bool asyncOperation = false)
        {
            if (IntPtr.Zero == windowHandle) return false;

            ShowCommand command = visible ? (showWithoutActivation ? ShowCommand.SW_SHOWNA : ShowCommand.SW_SHOW) : ShowCommand.SW_HIDE;

            if (asyncOperation)
                return Native.ShowWindowAsync(windowHandle, command);
            else
                return Native.ShowWindow(windowHandle, command);
        }

        /// <summary>
        /// Sets the specified window's show state.
        /// </summary>
        /// <param name="windowHandle">Handle for the window.</param>
        /// <param name="showState">The wanted show state.</param>
        /// <param name="asyncOperation">Determines if the function should wait for the operation to complete. TRUE to not wait for the operation to complete.</param>
        /// <returns>
        /// <para>If the operation was asynchronous this value is TRUE if operation was successfully started.</para>
        /// <para>If the opeartion was synchronous this value is TRUE if the window was previously visible.</para>
        /// </returns>
        public static bool SetWindowShowState(IntPtr windowHandle, ShowCommand showState, bool asyncOperation = false)
        {
            if (IntPtr.Zero == windowHandle) return false;

            if (asyncOperation)
                return Native.ShowWindowAsync(windowHandle, showState);
            else
                return Native.ShowWindow(windowHandle, showState);
        }

        /// <summary>
        /// Determines whether the specified window is minimized.
        /// </summary>
        /// <param name="windowHandle">A handle to the window that should be tested.</param>
        /// <returns>True if the window is minimized.</returns>
        public static bool IsMinimized(IntPtr windowHandle)
        {
            return Native.IsIconic(windowHandle);
        }

        /// <summary>
        /// Restore a minimized window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window to restore.</param>
        public static void RestoreWindow(IntPtr windowHandle)
        {
            if (IntPtr.Zero == windowHandle) return;
            if (Native.IsIconic(windowHandle))
            {
                Native.ShowWindow(windowHandle, ShowCommand.SW_RESTORE);
            }
        }

        /// <summary>
        /// Minimze a window.
        /// </summary>
        /// <param name="windowHandle">Handle for the window that should be minimzied.</param>
        /// <param name="force">Force the window to be minimized even if it is unresponsive.</param>
        public static void MinimzieWindow(IntPtr windowHandle, bool force = false, bool asyncOperation = false)
        {
            if (IntPtr.Zero == windowHandle) return;
            if (!Native.IsIconic(windowHandle))
            {
                ShowCommand command = force ? ShowCommand.SW_FORCEMINIMIZE : ShowCommand.SW_MINIMIZE;
                if (asyncOperation)
                    Native.ShowWindowAsync(windowHandle, command);
                else
                    Native.ShowWindow(windowHandle, command);
            }
        }

        /// <summary>
        /// Send a message to a window indicating that it should close.
        /// </summary>
        /// <param name="windowHandle">A handle to the window that should be closed. Can be Zero to broadcast to all windows.</param>
        public static void CloseWindow(IntPtr windowHandle)
        {
            PostMessageToWindow(windowHandle, Native.WM_CLOSE, IntPtr.Zero, IntPtr.Zero);
        }

        /// <summary>
        /// Send a message to a message queue on a thread indicating that it should exit.
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to which the message is to be posted.
        /// 
        /// Id can for example be retrieved in the following ways:
        /// # The "System.Diagnostics.ProcessThread"'s "Id" property.
        ///     # The "System.Diagnostics.Process"'s "Threads" property can be used to get "System.Diagnostics.ProcessThread" objects.
        /// # PInvoke to the Win32 API method "GetThreadId".
        ///     #https://msdn.microsoft.com/en-us/library/ms683233(VS.85).aspx
        /// # PInvoke to the Win32 API method "GetCurrentThreadID".
        ///     # https://msdn.microsoft.com/en-us/library/ms683183(VS.85).aspx
        ///     # Wrapped in deprecated "AppDomain.GetCurrentThreadId".
        /// Note: Do not use "System.Threading.Thread"'s "ManagedThreadId" property. ".Net" threads do not correspond to OS threads.
        /// </param>
        /// <param name="exitCode">The exit code the message queue should use.</param>
        public static void ExitMessageQueueOnThread(int threadId, int exitCode = 0)
        {
            PostMessageToThread(threadId, Native.WM_QUIT, exitCode == 0 ? IntPtr.Zero : new IntPtr(exitCode), IntPtr.Zero);
        }

        #endregion Manipulate Window


        #region Messages

        /// <summary>
        /// <para>Sends the specified message to a window or windows. The SendMessage function calls the window procedure for the specified window and does not return until the window procedure has processed the message.</para>
        /// <para>To post a message to a thread's message queue and return immediately, use the PostMessage or PostThreadMessage function.</para>
        /// </summary>
        /// <param name="windowHandle">A handle to the window whose window procedure is to receive the message. Can be Zero to broadcast to all windows.</param>
        /// <param name="message">The type of message to be sent. For lists of the system-provided messages, see System-Defined Messages<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined"/>.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        public static void SendMessageToWindow(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (windowHandle == IntPtr.Zero)
                windowHandle = Native.HWND_BROADCAST;

            Native.SendMessage(windowHandle, message, wParam, lParam);

            CheckHRCodeAndThrowException(Marshal.GetHRForLastWin32Error());
        }

        /// <summary>
        /// <para>Places (posts) a message in the message queue associated with the thread that created the specified window and returns without waiting for the thread to process the message.</para>
        /// <para>To post a message in the message queue associated with a thread, use the PostThreadMessage function.</para>
        /// </summary>
        /// <param name="windowHandle">A handle to the window whose window procedure is to receive the message. Can be Zero to broadcast to all windows.</param>
        /// <param name="message">The type of message to be posted. For lists of the system-provided messages, see System-Defined Messages<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined"/>.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        public static void PostMessageToWindow(IntPtr windowHandle, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (windowHandle == IntPtr.Zero)
                windowHandle = Native.HWND_BROADCAST;

            if (!Native.PostMessage(windowHandle, message, wParam, lParam))
                CheckHRCodeAndThrowException(Marshal.GetHRForLastWin32Error());
        }


        /// <summary>
        /// <para>Posts a message to the message queue of the specified thread. It returns without waiting for the thread to process the message.</para>
        /// <para>The function fails if the specified thread does not have a message queue. The system creates a thread's message queue when the thread makes its first call to one of the User or GDI functions.</para>
        /// </summary>
        /// <param name="threadId">
        /// The identifier of the thread to which the message is to be posted.
        /// 
        /// Id can for example be retrieved in the following ways:
        /// # The "System.Diagnostics.ProcessThread"'s "Id" property.
        ///     # The "System.Diagnostics.Process"'s "Threads" property can be used to get "System.Diagnostics.ProcessThread" objects.
        /// # PInvoke to the Win32 API method "GetThreadId".
        ///     #https://msdn.microsoft.com/en-us/library/ms683233(VS.85).aspx
        /// # PInvoke to the Win32 API method "GetCurrentThreadID".
        ///     # https://msdn.microsoft.com/en-us/library/ms683183(VS.85).aspx
        ///     # Wrapped in deprecated "AppDomain.GetCurrentThreadId".
        /// Note: Do not use "System.Threading.Thread"'s "ManagedThreadId" property. ".Net" threads do not correspond to OS threads.
        /// </param>
        /// <param name="message">The type of message to be posted. For lists of the system-provided messages, see System-Defined Messages<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/ms644927(v=vs.85).aspx#system_defined"/>.</param>
        /// <param name="wParam">Additional message-specific information.</param>
        /// <param name="lParam">Additional message-specific information.</param>
        public static void PostMessageToThread(int threadId, uint message, IntPtr wParam, IntPtr lParam)
        {
            if (!Native.PostThreadMessage((uint)threadId, message, wParam, lParam))
                CheckHRCodeAndThrowException(Marshal.GetHRForLastWin32Error());
        }

        private static void CheckHRCodeAndThrowException(int hrCode)
        {
            switch (hrCode)
            {
                case -2147024809: // Value does not fall in expected range HR Code.
                case -2147024896: // "The operation completed successfully. (Exception from HRESULT: 0x80070000)"
                    break;
                default:
                    Marshal.ThrowExceptionForHR(hrCode);
                    break;
            }
        }

        #endregion Messages


        #region Move Window

        /// <summary>
        ///     The MoveWindow function changes the position and dimensions of the specified window. For a top-level window, the
        ///     position and dimensions are relative to the upper-left corner of the screen. For a child window, they are relative
        ///     to the upper-left corner of the parent window's client area.
        ///     <para>
        ///     Go to https://msdn.microsoft.com/en-us/library/windows/desktop/ms633534%28v=vs.85%29.aspx for more
        ///     information
        ///     </para>
        /// </summary>
        /// <param name="windowHandle">C++ ( hWnd [in]. Type: HWND )<br /> Handle to the window.</param>
        /// <param name="newLocation">Specifies the new position and size of the window.</param>
        /// <param name="bRepaint">
        ///     C++ ( bRepaint [in]. Type: bool )<br />Specifies whether the window is to be repainted. If this
        ///     parameter is TRUE, the window receives a message. If the parameter is FALSE, no repainting of any kind occurs. This
        ///     applies to the client area, the nonclient area (including the title bar and scroll bars), and any part of the
        ///     parent window uncovered as a result of moving a child window.
        /// </param>
        public static void MoveWindow(IntPtr windowHandle, Rectangle newLocation, bool bRepaint)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(windowHandle));

            if (!Native.MoveWindow(windowHandle, newLocation.X, newLocation.Y, newLocation.Width, newLocation.Height, bRepaint))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        #endregion Move Window


        #region Arrange Windows

        /// <summary>
        /// Tiles the specified child windows of the specified parent window.
        /// All maximized windows will be restored to their previous size.
        /// </summary>
        /// <param name="parentWindow">A handle to the parent window. If this parameter is NULL, the desktop window is assumed.</param>
        /// <param name="tileHorizontal">Tiles windows horizontally. Otherwise tiles windows vertically.</param>
        /// <param name="rectangle">The rectangular area, in client coordinates, within which the windows are arranged. If this parameter is NULL, the client area of the parent window is used.</param>
        /// <param name="childWindowsToArrange">An array of handles to the child windows to arrange. If a specified child window is a top-level window with the style WS_EX_TOPMOST or WS_EX_TOOLWINDOW, the child window is not arranged. If this parameter is NULL, all child windows of the specified parent window (or of the desktop window) are arranged.</param>
        /// <returns>The number of windows arranged.</returns>
        public static int TileWindows(IntPtr? parentWindow = null, bool tileHorizontal = false, System.Drawing.Rectangle? rectangle = null, IntPtr[] childWindowsToArrange = null)
        {
            return ArrangeWindowsHelper(rectangle, childWindowsToArrange, (rectangleHandle, arrayLength, arrayHandle) =>
            {
                return Native.TileWindows((parentWindow.HasValue ? parentWindow.Value : IntPtr.Zero),
                       ((tileHorizontal ? Native.ArrangeFlags.MDITILE_HORIZONTAL : Native.ArrangeFlags.MDITILE_VERTICAL) | Native.ArrangeFlags.MDITILE_SKIPDISABLED),
                       rectangleHandle,
                       arrayLength, arrayHandle);
            });
        }

        /// <summary>
        /// Cascades the specified child windows of the specified parent window.
        /// All maximized windows will be restored to their previous size.
        /// </summary>
        /// <param name="parentWindow">A handle to the parent window. If this parameter is NULL, the desktop window is assumed.</param>
        /// <param name="arrangeInZOrder">If TRUE arranges the windows in Z-Order. If FALSE arranges the windows in the order provided by the childWindowsToArrange array, but preserves the Z-Order.</param>
        /// <param name="rectangle">The rectangular area, in client coordinates, within which the windows are arranged. This parameter can be NULL, in which case the client area of the parent window is used.</param>
        /// <param name="childWindowsToArrange">An array of handles to the child windows to arrange. If a specified child window is a top-level window with the style WS_EX_TOPMOST or WS_EX_TOOLWINDOW, the child window is not arranged. If this parameter is NULL, all child windows of the specified parent window (or of the desktop window) are arranged.</param>
        /// <returns>The number of windows arranged.</returns>
        public static int CascadeWindows(IntPtr? parentWindow = null, bool arrangeInZOrder = false, System.Drawing.Rectangle? rectangle = null, IntPtr[] childWindowsToArrange = null)
        {
            return ArrangeWindowsHelper(rectangle, childWindowsToArrange, (rectangleHandle, arrayLength, arrayHandle) =>
            {
                return Native.CascadeWindows((parentWindow.HasValue ? parentWindow.Value : IntPtr.Zero),
                       ((arrangeInZOrder ? Native.ArrangeFlags.MDITILE_ZORDER : 0) | Native.ArrangeFlags.MDITILE_SKIPDISABLED),
                       rectangleHandle,
                       arrayLength, arrayHandle);
            });
        }

        private delegate int ArrangeWindowsCallback(IntPtr rectangleHandle, uint arrayLength, IntPtr arrayHandle);
        private static int ArrangeWindowsHelper(System.Drawing.Rectangle? rectangle, IntPtr[] handles, ArrangeWindowsCallback arrangeWindows)
        {
            Native.RECT? rect = rectangle;

            GCHandle? arrayGCHandle = null;
            GCHandle? rectGCHandle = null;
            try
            {
                if (handles != null)
                    arrayGCHandle = GCHandle.Alloc(handles, GCHandleType.Pinned);
                if (rect.HasValue)
                    rectGCHandle = GCHandle.Alloc(rect, GCHandleType.Pinned);

                IntPtr arrayHandle = IntPtr.Zero;
                if (arrayGCHandle.HasValue)
                    arrayHandle = arrayGCHandle.Value.AddrOfPinnedObject();

                IntPtr rectHandle = IntPtr.Zero;
                if (rectGCHandle.HasValue)
                    rectHandle = rectGCHandle.Value.AddrOfPinnedObject();

                int returned = arrangeWindows(rectHandle, (uint)(handles == null ? 0 : handles.Length), arrayHandle);

                if (returned == 0)
                {
                    Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
                }
                return returned;
            }
            finally
            {
                if (arrayGCHandle.HasValue && arrayGCHandle.Value.IsAllocated)
                {
                    arrayGCHandle.Value.Free();
                }
                if (rectGCHandle.HasValue && rectGCHandle.Value.IsAllocated)
                {
                    rectGCHandle.Value.Free();
                }
            }
        }

        /// <summary>
        /// Brings the specified window to the top of the Z order. If the window is a top-level window, it is activated. If the 
        /// window is a child window, the top-level parent window associated with the child window is activated.
        /// </summary>
        /// <param name="windowHandle">A handle to the window to bring to the top of the Z order.</param>
        /// <exception cref="ArgumentNullException">If the provided window handle is null.</exception>
        public static void BringWindowToTop(IntPtr windowHandle)
        {
            if (windowHandle == IntPtr.Zero)
                throw new ArgumentNullException(nameof(windowHandle));

            if (!Native.BringWindowToTop(windowHandle))
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
        }

        #endregion Arrange Windows


        #region Keyboard Event

        /// <summary>
        /// Key codes to use when synthesizing a keypress.
        /// </summary>
        public enum VirtualKeyCode : byte
        {
            /*
             * Virtual Keys, Standard Set
             */
            VK_LBUTTON = 0x01
            , VK_RBUTTON = 0x02
            , VK_CANCEL = 0x03
            , VK_MBUTTON = 0x04    /* NOT contiguous with L & RBUTTON */

            , VK_XBUTTON1 = 0x05    /* NOT contiguous with L & RBUTTON */
            , VK_XBUTTON2 = 0x06    /* NOT contiguous with L & RBUTTON */
            /* _WIN32_WINNT >= = 0x0500 */

            /*
             * = 0x07 : reserved
             */


            , VK_BACK = 0x08
            , VK_TAB = 0x09

            /*
             * = 0x0A - = 0x0B : reserved
             */

            , VK_CLEAR = 0x0C
            , VK_RETURN = 0x0D

            /*
             * = 0x0E - = 0x0F : unassigned
             */

            , VK_SHIFT = 0x10
            , VK_CONTROL = 0x11
            , VK_MENU = 0x12
            , VK_PAUSE = 0x13
            , VK_CAPITAL = 0x14

            , VK_KANA = 0x15
            , VK_HANGEUL = 0x15  /* old name - should be here for compatibility */
            , VK_HANGUL = 0x15

            /*
             * = 0x16 : unassigned
             */

            , VK_JUNJA = 0x17
            , VK_FINAL = 0x18
            , VK_HANJA = 0x19
            , VK_KANJI = 0x19

            /*
             * = 0x1A : unassigned
             */

            , VK_ESCAPE = 0x1B

            , VK_CONVERT = 0x1C
            , VK_NONCONVERT = 0x1D
            , VK_ACCEPT = 0x1E
            , VK_MODECHANGE = 0x1F

            , VK_SPACE = 0x20
            , VK_PRIOR = 0x21
            , VK_NEXT = 0x22
            , VK_END = 0x23
            , VK_HOME = 0x24
            , VK_LEFT = 0x25
            , VK_UP = 0x26
            , VK_RIGHT = 0x27
            , VK_DOWN = 0x28
            , VK_SELECT = 0x29
            , VK_PRINT = 0x2A
            , VK_EXECUTE = 0x2B
            , VK_SNAPSHOT = 0x2C
            , VK_INSERT = 0x2D
            , VK_DELETE = 0x2E
            , VK_HELP = 0x2F

            /*
             * VK_0 - VK_9 are the same as ASCII '0' - '9' (= 0x30 - = 0x39)
             * = 0x3A - = 0x40 : unassigned
             * VK_A - VK_Z are the same as ASCII 'A' - 'Z' (= 0x41 - = 0x5A)
             */

            , VK_LWIN = 0x5B
            , VK_RWIN = 0x5C
            , VK_APPS = 0x5D

            /*
             * = 0x5E : reserved
             */

            , VK_SLEEP = 0x5F

            , VK_NUMPAD0 = 0x60
            , VK_NUMPAD1 = 0x61
            , VK_NUMPAD2 = 0x62
            , VK_NUMPAD3 = 0x63
            , VK_NUMPAD4 = 0x64
            , VK_NUMPAD5 = 0x65
            , VK_NUMPAD6 = 0x66
            , VK_NUMPAD7 = 0x67
            , VK_NUMPAD8 = 0x68
            , VK_NUMPAD9 = 0x69
            , VK_MULTIPLY = 0x6A
            , VK_ADD = 0x6B
            , VK_SEPARATOR = 0x6C
            , VK_SUBTRACT = 0x6D
            , VK_DECIMAL = 0x6E
            , VK_DIVIDE = 0x6F
            , VK_F1 = 0x70
            , VK_F2 = 0x71
            , VK_F3 = 0x72
            , VK_F4 = 0x73
            , VK_F5 = 0x74
            , VK_F6 = 0x75
            , VK_F7 = 0x76
            , VK_F8 = 0x77
            , VK_F9 = 0x78
            , VK_F10 = 0x79
            , VK_F11 = 0x7A
            , VK_F12 = 0x7B
            , VK_F13 = 0x7C
            , VK_F14 = 0x7D
            , VK_F15 = 0x7E
            , VK_F16 = 0x7F
            , VK_F17 = 0x80
            , VK_F18 = 0x81
            , VK_F19 = 0x82
            , VK_F20 = 0x83
            , VK_F21 = 0x84
            , VK_F22 = 0x85
            , VK_F23 = 0x86
            , VK_F24 = 0x87


            /*
             * = 0x88 - = 0x8F : UI navigation
             */

            , VK_NAVIGATION_VIEW = 0x88 // reserved
            , VK_NAVIGATION_MENU = 0x89 // reserved
            , VK_NAVIGATION_UP = 0x8A // reserved
            , VK_NAVIGATION_DOWN = 0x8B // reserved
            , VK_NAVIGATION_LEFT = 0x8C // reserved
            , VK_NAVIGATION_RIGHT = 0x8D // reserved
            , VK_NAVIGATION_ACCEPT = 0x8E // reserved
            , VK_NAVIGATION_CANCEL = 0x8F // reserved

            /* _WIN32_WINNT >= = 0x0604 */

            , VK_NUMLOCK = 0x90
            , VK_SCROLL = 0x91

            /*
             * NEC PC-9800 kbd definitions
             */
            , VK_OEM_NEC_EQUAL = 0x92   // '=' key on numpad

            /*
             * Fujitsu/OASYS kbd definitions
             */
            , VK_OEM_FJ_JISHO = 0x92   // 'Dictionary' key
            , VK_OEM_FJ_MASSHOU = 0x93   // 'Unregister word' key
            , VK_OEM_FJ_TOUROKU = 0x94   // 'Register word' key
            , VK_OEM_FJ_LOYA = 0x95   // 'Left OYAYUBI' key
            , VK_OEM_FJ_ROYA = 0x96   // 'Right OYAYUBI' key

            /*
             * = 0x97 - = 0x9F : unassigned
             */

            /*
             * VK_L* & VK_R* - left and right Alt, Ctrl and Shift virtual keys.
             * Used only as parameters to GetAsyncKeyState() and GetKeyState().
             * No other API or message will distinguish left and right keys in this way.
             */
            , VK_LSHIFT = 0xA0
            , VK_RSHIFT = 0xA1
            , VK_LCONTROL = 0xA2
            , VK_RCONTROL = 0xA3
            , VK_LMENU = 0xA4
            , VK_RMENU = 0xA5

            , VK_BROWSER_BACK = 0xA6
            , VK_BROWSER_FORWARD = 0xA7
            , VK_BROWSER_REFRESH = 0xA8
            , VK_BROWSER_STOP = 0xA9
            , VK_BROWSER_SEARCH = 0xAA
            , VK_BROWSER_FAVORITES = 0xAB
            , VK_BROWSER_HOME = 0xAC

            , VK_VOLUME_MUTE = 0xAD
            , VK_VOLUME_DOWN = 0xAE
            , VK_VOLUME_UP = 0xAF
            , VK_MEDIA_NEXT_TRACK = 0xB0
            , VK_MEDIA_PREV_TRACK = 0xB1
            , VK_MEDIA_STOP = 0xB2
            , VK_MEDIA_PLAY_PAUSE = 0xB3
            , VK_LAUNCH_MAIL = 0xB4
            , VK_LAUNCH_MEDIA_SELECT = 0xB5
            , VK_LAUNCH_APP1 = 0xB6
            , VK_LAUNCH_APP2 = 0xB7

            /* _WIN32_WINNT >= = 0x0500 */

            /*
             * = 0xB8 - = 0xB9 : reserved
             */

            , VK_OEM_1 = 0xBA   // ';:' for US
            , VK_OEM_PLUS = 0xBB   // '+' any country
            , VK_OEM_COMMA = 0xBC   // ',' any country
            , VK_OEM_MINUS = 0xBD   // '-' any country
            , VK_OEM_PERIOD = 0xBE   // '.' any country
            , VK_OEM_2 = 0xBF   // '/?' for US
            , VK_OEM_3 = 0xC0   // '`~' for US

            /*
             * = 0xC1 - = 0xC2 : reserved
             */


            /*
             * = 0xC3 - = 0xDA : Gamepad input
             */

            , VK_GAMEPAD_A = 0xC3 // reserved
            , VK_GAMEPAD_B = 0xC4 // reserved
            , VK_GAMEPAD_X = 0xC5 // reserved
            , VK_GAMEPAD_Y = 0xC6 // reserved
            , VK_GAMEPAD_RIGHT_SHOULDER = 0xC7 // reserved
            , VK_GAMEPAD_LEFT_SHOULDER = 0xC8 // reserved
            , VK_GAMEPAD_LEFT_TRIGGER = 0xC9 // reserved
            , VK_GAMEPAD_RIGHT_TRIGGER = 0xCA // reserved
            , VK_GAMEPAD_DPAD_UP = 0xCB // reserved
            , VK_GAMEPAD_DPAD_DOWN = 0xCC // reserved
            , VK_GAMEPAD_DPAD_LEFT = 0xCD // reserved
            , VK_GAMEPAD_DPAD_RIGHT = 0xCE // reserved
            , VK_GAMEPAD_MENU = 0xCF // reserved
            , VK_GAMEPAD_VIEW = 0xD0 // reserved
            , VK_GAMEPAD_LEFT_THUMBSTICK_BUTTON = 0xD1 // reserved
            , VK_GAMEPAD_RIGHT_THUMBSTICK_BUTTON = 0xD2 // reserved
            , VK_GAMEPAD_LEFT_THUMBSTICK_UP = 0xD3 // reserved
            , VK_GAMEPAD_LEFT_THUMBSTICK_DOWN = 0xD4 // reserved
            , VK_GAMEPAD_LEFT_THUMBSTICK_RIGHT = 0xD5 // reserved
            , VK_GAMEPAD_LEFT_THUMBSTICK_LEFT = 0xD6 // reserved
            , VK_GAMEPAD_RIGHT_THUMBSTICK_UP = 0xD7 // reserved
            , VK_GAMEPAD_RIGHT_THUMBSTICK_DOWN = 0xD8 // reserved
            , VK_GAMEPAD_RIGHT_THUMBSTICK_RIGHT = 0xD9 // reserved
            , VK_GAMEPAD_RIGHT_THUMBSTICK_LEFT = 0xDA // reserved

            /* _WIN32_WINNT >= = 0x0604 */


            , VK_OEM_4 = 0xDB  //  '[{' for US
            , VK_OEM_5 = 0xDC  //  '\|' for US
            , VK_OEM_6 = 0xDD  //  ']}' for US
            , VK_OEM_7 = 0xDE  //  ''"' for US
            , VK_OEM_8 = 0xDF

            /*
             * = 0xE0 : reserved
             */

            /*
             * Various extended or enhanced keyboards
             */
            , VK_OEM_AX = 0xE1  //  'AX' key on Japanese AX kbd
            , VK_OEM_102 = 0xE2  //  "<>" or "\|" on RT 102-key kbd.
            , VK_ICO_HELP = 0xE3  //  Help key on ICO
            , VK_ICO_00 = 0xE4  //  00 key on ICO

            , VK_PROCESSKEY = 0xE5
            /* WINVER >= = 0x0400 */

            , VK_ICO_CLEAR = 0xE6


            , VK_PACKET = 0xE7
            /* _WIN32_WINNT >= = 0x0500 */

            /*
             * = 0xE8 : unassigned
             */

            /*
             * Nokia/Ericsson definitions
             */
            , VK_OEM_RESET = 0xE9
            , VK_OEM_JUMP = 0xEA
            , VK_OEM_PA1 = 0xEB
            , VK_OEM_PA2 = 0xEC
            , VK_OEM_PA3 = 0xED
            , VK_OEM_WSCTRL = 0xEE
            , VK_OEM_CUSEL = 0xEF
            , VK_OEM_ATTN = 0xF0
            , VK_OEM_FINISH = 0xF1
            , VK_OEM_COPY = 0xF2
            , VK_OEM_AUTO = 0xF3
            , VK_OEM_ENLW = 0xF4
            , VK_OEM_BACKTAB = 0xF5

            , VK_ATTN = 0xF6
            , VK_CRSEL = 0xF7
            , VK_EXSEL = 0xF8
            , VK_EREOF = 0xF9
            , VK_PLAY = 0xFA
            , VK_ZOOM = 0xFB
            , VK_NONAME = 0xFC
            , VK_PA1 = 0xFD
            , VK_OEM_CLEAR = 0xFE

            /*
             * 0xFF : reserved
             */
        }

        /// <summary>
        /// Synthesizes a keypress. The system will generate a WM_KEYUP and WM_KEYDOWN message.
        /// </summary>
        /// <param name="virtualKeyCode">
        /// <para>A virtual-key code. The code must be a value in the range 1 to 254. For a complete list, see Virtual Key Codes.<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx"/></para>
        /// <para>Possible Keys can also be found in the "WinUser.h" header file, it is the variables prefixed with "VK_".</para>
        /// </param>
        /// <param name="timeInMilliseconds">Determines the time until the key is released.</param>
        public static void PressKeyboardButton(VirtualKeyCode virtualKeyCode, int timeInMilliseconds = 0)
        {
            PressKeyboardButton(virtualKeyCode, TimeSpan.FromMilliseconds(timeInMilliseconds));
        }
        /// <summary>
        /// Synthesizes a keypress. The system will generate a WM_KEYUP and WM_KEYDOWN message.
        /// </summary>
        /// <param name="virtualKeyCode">
        /// <para>A virtual-key code. The code must be a value in the range 1 to 254. For a complete list, see Virtual Key Codes.<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx"/></para>
        /// <para>Possible Keys can also be found in the "WinUser.h" header file, it is the variables prefixed with "VK_".</para>
        /// </param>
        /// <param name="timeSpan">Determines the time until the key is released.</param>
        public static void PressKeyboardButton(VirtualKeyCode virtualKeyCode, TimeSpan timeSpan)
        {
            SendKeyboardEvent(virtualKeyCode, false);
            Thread.Sleep(timeSpan);
            SendKeyboardEvent(virtualKeyCode, true);
        }

        /// <summary>
        /// Synthesizes a keystroke. The system can use such a synthesized keystroke to generate a WM_KEYUP or WM_KEYDOWN message.
        /// </summary>
        /// <param name="virtualKeyCode">
        /// <para>A virtual-key code. The code must be a value in the range 1 to 254. For a complete list, see Virtual Key Codes.<see cref="https://msdn.microsoft.com/en-us/library/windows/desktop/dd375731(v=vs.85).aspx"/></para>
        /// <para>Possible Keys can also be found in the "WinUser.h" header file, it is the variables prefixed with "VK_".</para>
        /// </param>
        /// <param name="releasedKey">Indicates if the key was pressed or released.</param>
        public static void SendKeyboardEvent(VirtualKeyCode virtualKeyCode, bool releasedKey)
        {
            Native.keybd_event((byte)virtualKeyCode, 0x45, (releasedKey ? Native.KeyEventFlag.KEYEVENTF_KEYUP : 0), 0);
        }

        #endregion Keyboard Event

        #endregion Methods


        #region Properties

        #endregion Properties
    }
}
