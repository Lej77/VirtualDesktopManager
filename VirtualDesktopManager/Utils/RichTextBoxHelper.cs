using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Threading;

namespace VirtualDesktopManager.Utils
{
    /// <summary>
    /// Useful code for handling rich text boxes.
    /// </summary>
    public class RichTextBoxHelper
    {
        #region Classes

        /// <summary>
        /// Provideds information about a text selection.
        /// </summary>
        [Serializable]
        public class SelectionInfo
        {
            public int StartIndex { get; }
            public int Length { get; }

            public SelectionInfo(int startIndex, int length)
            {
                StartIndex = startIndex;
                Length = length;
            }


            public int EndIndex
            {
                get
                {
                    return StartIndex + Length;
                }
            }

            /// <summary>
            /// Determines how to keep selection unchanged after a text insert.
            /// </summary>
            /// <param name="indexToInsertAt">Index to insert text at.</param>
            /// <param name="textToInsert">Text to insert.</param>
            /// <returns>Selection that should be used to keep selection unchanged after a text insert.</returns>
            public SelectionInfo GetSelectionInfoAfterTextInsert(int indexToInsertAt, string textToInsert)
            {
                return getSelectionInfoAfterTextChange(true, indexToInsertAt, TextManager.CountNumberOfCharactersInText(textToInsert), this);
            }
            /// <summary>
            /// Determines how to keep selection unchanged after a text insert.
            /// </summary>
            /// <param name="indexToInsertAt">Index to insert text at.</param>
            /// <param name="numberOfCharactersToInsert">Length of the text to insert.</param>
            /// <returns>Selection that should be used to keep selection unchanged after a text insert.</returns>
            public SelectionInfo GetSelectionInfoAfterTextInsert(int indexToInsertAt, int numberOfCharactersToInsert)
            {
                return getSelectionInfoAfterTextChange(true, indexToInsertAt, numberOfCharactersToInsert, this);
            }

            /// <summary>
            /// Determines how to keep selection unchanged after a text is removed.
            /// </summary>
            /// <param name="indexToStartRemovingAt">Index to remove text at.</param>
            /// <param name="textToRemove">Text to remove.</param>
            /// <returns>Selection that should be used to keep selection unchanged after text is removed.</returns>
            public SelectionInfo GetSelectionInfoAfterTextRemove(int indexToStartRemovingAt, string textToRemove)
            {
                return getSelectionInfoAfterTextChange(false, indexToStartRemovingAt, TextManager.CountNumberOfCharactersInText(textToRemove), this);
            }
            /// <summary>
            /// Determines how to keep selection unchanged after a text is removed.
            /// </summary>
            /// <param name="indexToStartRemovingAt">Index to remove text at.</param>
            /// <param name="numberOfCharactersToRemove">Length of text to remove.</param>
            /// <returns>Selection that should be used to keep selection unchanged after text is removed.</returns>
            public SelectionInfo GetSelectionInfoAfterTextRemove(int indexToStartRemovingAt, int numberOfCharactersToRemove)
            {
                return getSelectionInfoAfterTextChange(false, indexToStartRemovingAt, numberOfCharactersToRemove, this);
            }

            private static SelectionInfo getSelectionInfoAfterTextChange(bool positiveChange, int startIndex, int count, SelectionInfo orignalSelection)
            {
                if (startIndex < 0) startIndex = 0;
                if (count < 0) count = 0;

                int selectionIndex = orignalSelection.StartIndex;
                if (selectionIndex < 0) selectionIndex = 0;
                int selectionLength = orignalSelection.Length;
                if (selectionLength < 0) selectionLength = 0;


                if (startIndex > selectionIndex + selectionLength || count == 0)
                {
                    // Changed text won't change current text selection.
                    return orignalSelection;
                }

                if (positiveChange)
                {
                    bool selectionIndexAffected = selectionIndex >= startIndex;
                    bool selectionLengthAffected = !selectionIndexAffected && selectionIndex + selectionLength >= startIndex;
                    if (selectionIndexAffected) selectionIndex += count;
                    if (selectionLengthAffected) selectionLength += count;
                }
                else
                {
                    bool selectionLengthAffected = startIndex < selectionIndex + selectionLength && startIndex + count >= selectionIndex;
                    bool selectionIndexAffected = selectionIndex > startIndex;
                    if (selectionLengthAffected)
                    {
                        selectionLength -= (startIndex + count) - selectionIndex; // removeEndIndex - selectionStartIndex = LengthOfRemoveAfterSelectionStart
                        if (selectionLength < 0) selectionLength = 0;
                        int selectionBeforeStartIndexLength = startIndex - selectionIndex;
                        if (selectionBeforeStartIndexLength > 0) selectionLength += selectionBeforeStartIndexLength;
                    }
                    if (selectionIndexAffected) selectionIndex = selectionLengthAffected ? startIndex : selectionIndex - (selectionIndex - startIndex);
                }

                return new SelectionInfo(selectionIndex, selectionLength);
            }
        }


        /// <summary>
        /// Provideds information about a scroll position.
        /// </summary>
        [Serializable]
        public abstract class ScrollInfo
        {
            public abstract bool Absolute { get; }
            public abstract double XScroll { get; }
            public abstract double YScroll { get; }

            #region Public Methods

            public RelativeScrollInfo ToRelative(RichTextBox richTextBox)
            {
                return ChangeAbsolute(false, richTextBox) as RelativeScrollInfo;
            }
            public RelativeScrollInfo ToRelative(Size visibleArea, Size totalArea)
            {
                return ChangeAbsolute(false, visibleArea, totalArea) as RelativeScrollInfo;
            }

            public AbsoluteScrollInfo ToAbsolute(RichTextBox richTextBox)
            {
                return ChangeAbsolute(true, richTextBox) as AbsoluteScrollInfo;
            }
            public AbsoluteScrollInfo ToAbsolute(Size visibleArea, Size totalArea)
            {
                return ChangeAbsolute(true, visibleArea, totalArea) as AbsoluteScrollInfo;
            }

            /// <summary>
            /// Get a ScrollInfo object from the data in this object with relative or absolute values.
            /// </summary>
            /// <param name="absolute">Specify if the new object should have absoute or relative scroll.</param>
            /// <param name="richTextBox">The RichTextBox to get size info from.</param>
            /// <returns>A ScrollInfo object with the wanted data. Can be the current object if no changes were needed.</returns>
            public ScrollInfo ChangeAbsolute(bool absolute, RichTextBox richTextBox)
            {
                var helper = Manage(richTextBox);
                return ChangeAbsolute(absolute, helper.VisibleTextArea, helper.TotalTextArea);
            }
            /// <summary>
            /// Get a ScrollInfo object from the data in this object with relative or absolute values.
            /// </summary>
            /// <param name="absolute">Specify if the new object should have absoute or relative scroll.</param>
            /// <param name="visibleArea">The area that is visible inside the scrollable controller.</param>
            /// <param name="totalArea">The total area that the controller can scroll around in.</param>
            /// <returns>A ScrollInfo object with the wanted data. Can be the current object if no changes were needed.</returns>
            public ScrollInfo ChangeAbsolute(bool absolute, Size visibleArea, Size totalArea)
            {
                if (absolute == Absolute)
                    return this;

                int maxX = totalArea.Width - visibleArea.Width;
                int maxY = totalArea.Height - visibleArea.Height;

                Rectangle scrollRegion = GetVisibleRegion(visibleArea, totalArea);


                if (absolute)
                    return new AbsoluteScrollInfo(scrollRegion.X, scrollRegion.Y);
                else
                {
                    double horizontal, vertical;

                    if (maxX <= 0)
                        horizontal = 0;
                    else
                        horizontal = scrollRegion.X / (double)maxX;

                    if (maxY <= 0)
                        vertical = 0;
                    else
                        vertical = scrollRegion.Y / (double)maxY;

                    return new RelativeScrollInfo(horizontal, vertical);
                }
            }

            /// <summary>
            /// Determine the region of a rich text box that would be visible with the scroll determined by this object.
            /// </summary>
            /// <param name="richTextBox">The rich text box to determine the visible area from.</param>
            /// <returns>The region of the rich text box's total text area that would be visible if the scroll from this object was used.</returns>
            public Rectangle GetVisibleRegion(RichTextBox richTextBox)
            {
                var helper = Manage(richTextBox);
                return GetVisibleRegion(helper.VisibleTextArea, helper.TotalTextArea);
            }
            /// <summary>
            /// Determine the region of an area that would be visible with the scroll determined by this object.
            /// </summary>
            /// <param name="visibleArea">The area that is visible inside the scrollable controller.</param>
            /// <param name="totalArea">The total area that the controller can scroll around in.</param>
            /// <returns>The region of the total area that would be visible if the scroll from this object was used.</returns>
            public Rectangle GetVisibleRegion(Size visibleArea, Size totalArea)
            {
                int maxX = totalArea.Width - visibleArea.Width;
                int maxY = totalArea.Height - visibleArea.Height;

                if (maxX < 0)
                    maxX = 0;
                if (maxY < 0)
                    maxY = 0;

                int x, y;
                if (Absolute)
                {
                    x = (int)XScroll;
                    y = (int)YScroll;
                }
                else
                {
                    x = (int)Math.Ceiling(XScroll * maxX);
                    y = (int)Math.Ceiling(YScroll * maxY);
                }

                if (x < 0)
                    x = 0;
                if (y < 0)
                    y = 0;

                if (x > maxX)
                    x = maxX;
                if (y > maxY)
                    y = maxY;

                return new Rectangle(x, y, visibleArea.Width, visibleArea.Height);
            }

            #endregion Public Methods
        }

        public class RelativeScrollInfo : ScrollInfo
        {

            #region Classes

            #endregion Classes


            #region Member Variables

            /// <summary>
            /// A value from 0 to 1 indicating the current horizontal scroll.
            /// </summary>
            public double HorizontalScroll { get; }
            /// <summary>
            /// A value from 0 to 1 indicating the current vertical scroll.
            /// </summary>
            public double VerticalScroll { get; }

            #endregion Member Variables


            #region Constructors

            public RelativeScrollInfo(double horizontalScroll, double verticalScroll)
            {
                HorizontalScroll = GetScroll(horizontalScroll);
                VerticalScroll = GetScroll(verticalScroll);
            }
            public RelativeScrollInfo(Rectangle visibleRegion, Size totalArea)
            {
                int extraXSpace = totalArea.Width - visibleRegion.Width;
                double scrollX = 0;
                if (extraXSpace > 0)
                    scrollX = visibleRegion.X / (double)(extraXSpace);

                int extraYSpace = totalArea.Height - visibleRegion.Height;
                double scrollY = 0;
                if (extraYSpace > 0)
                    scrollY = visibleRegion.Y / (double)(extraYSpace);

                HorizontalScroll = GetScroll(scrollX);
                VerticalScroll = GetScroll(scrollY);
            }

            #endregion Constructors


            #region Methods

            private double GetScroll(double value)
            {
                if (value < 0)
                    return 0;
                else if (value > 1)
                    return 1;
                else
                    return value;
            }

            #endregion Methods


            #region Properties

            public override bool Absolute => false;
            public override double XScroll => HorizontalScroll;
            public override double YScroll => VerticalScroll;

            #endregion Properties
        }

        public class AbsoluteScrollInfo : ScrollInfo
        {
            #region Classes

            #endregion Classes


            #region Member Variables

            /// <summary>
            /// A value greater than 0 indicating how many pixels are to the left of the scrolled view.
            /// </summary>
            public int HorizontalScroll { get; }
            /// <summary>
            /// A value greater than 0 indicating how many pixels are above the scrolled view.
            /// </summary>
            public int VerticalScroll { get; }

            #endregion Member Variables


            #region Constructors

            public AbsoluteScrollInfo(int horizontalScroll, int verticalScroll)
            {
                HorizontalScroll = GetScroll(horizontalScroll);
                VerticalScroll = GetScroll(verticalScroll);
            }
            public AbsoluteScrollInfo(Point topLeftCornerOfView) : this(topLeftCornerOfView.X, topLeftCornerOfView.Y)
            { }

            #endregion Constructors


            #region Methods

            private int GetScroll(int value)
            {
                if (value < 0)
                    return 0;
                else
                    return value;
            }

            #endregion Methods


            #region Properties

            public override bool Absolute => true;
            public override double XScroll => HorizontalScroll;
            public override double YScroll => VerticalScroll;

            #endregion Properties
        }



        private static class Native
        {
            #region Scrollbar Position

            /// <summary>
            /// The GetScrollPos function retrieves the current position of the scroll box (thumb) in the specified scroll bar. The current position is a relative value that depends on the current scrolling range. For example, if the scrolling range is 0 through 100 and the scroll box is in the middle of the bar, the current position is 50.
            /// 
            /// Note: The GetScrollPos function is provided for backward compatibility. New applications should use the GetScrollInfo function.
            /// 
            /// Remarks:
            /// The GetScrollPos function enables applications to use 32-bit scroll positions. Although the messages that indicate scroll bar position, WM_HSCROLL and WM_VSCROLL, are limited to 16 bits of position data, the functions SetScrollPos, SetScrollRange, GetScrollPos, and GetScrollRange support 32-bit scroll bar position data. Thus, an application can call GetScrollPos while processing either the WM_HSCROLL or WM_VSCROLL messages to obtain 32-bit scroll bar position data.
            /// To get the 32-bit position of the scroll box (thumb) during a SB_THUMBTRACK request code in a WM_HSCROLL or WM_VSCROLL message, use the GetScrollInfo function.
            /// If the nBar parameter is SB_CTL and the window specified by the hWnd parameter is not a system scroll bar control, the system sends the SBM_GETPOS message to the window to obtain scroll bar information. This allows GetScrollPos to operate on a custom control that mimics a scroll bar. If the window does not handle the SBM_GETPOS message, the GetScrollPos function fails.
            /// </summary>
            /// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll bar, depending on the value of the nBar parameter.</param>
            /// <param name="nBar">Specifies the scroll bar to be examined.</param>
            /// <returns>If the function succeeds, the return value is the current position of the scroll box.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError. </returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern int GetScrollPos(IntPtr hWnd, ScrollBar nBar);

            /// <summary>
            /// The SetScrollPos function sets the position of the scroll box (thumb) in the specified scroll bar and, if requested, redraws the scroll bar to reflect the new position of the scroll box. 
            /// 
            /// Note: The SetScrollPos function is provided for backward compatibility. New applications should use the SetScrollInfo function.
            /// 
            /// Remarks:
            /// If the scroll bar is redrawn by a subsequent call to another function, setting the bRedraw parameter to FALSE is useful.
            /// Because the messages that indicate scroll bar position, WM_HSCROLL and WM_VSCROLL, are limited to 16 bits of position data, applications that rely solely on those messages for position data have a practical maximum value of 65,535 for the SetScrollPos function's nPos parameter.
            /// However, because the SetScrollInfo, SetScrollPos, SetScrollRange, GetScrollInfo, GetScrollPos, and GetScrollRange functions support 32-bit scroll bar position data, there is a way to circumvent the 16-bit barrier of the WM_HSCROLL and WM_VSCROLL messages. See GetScrollInfo for a description of the technique.
            /// If the nBar parameter is SB_CTL and the window specified by the hWnd parameter is not a system scroll bar control, the system sends the SBM_SETPOS message to the window to set scroll bar information. This allows SetScrollPos to operate on a custom control that mimics a scroll bar. If the window does not handle the SBM_SETPOS message, the SetScrollPos function fails. 
            /// </summary>
            /// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll bar, depending on the value of the nBar parameter.</param>
            /// <param name="nBar">Specifies the scroll bar to be set.</param>
            /// <param name="nPos">Specifies the new position of the scroll box. The position must be within the scrolling range. For more information about the scrolling range, see the SetScrollRange function.</param>
            /// <param name="bRedraw">Specifies whether the scroll bar is redrawn to reflect the new scroll box position. If this parameter is TRUE, the scroll bar is redrawn. If it is FALSE, the scroll bar is not redrawn. </param>
            /// <returns>If the function succeeds, the return value is the previous position of the scroll box.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError.</returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern int SetScrollPos(IntPtr hWnd, ScrollBar nBar, int nPos, bool bRedraw);

            /// <summary>
            /// Specifies a scroll bar.
            /// </summary>
            public enum ScrollBar
            {
                /// <summary>
                /// Sets the position of the scroll box in a window's standard horizontal scroll bar.
                /// </summary>
                SB_HORZ = 0x0,
                /// <summary>
                /// Sets the position of the scroll box in a window's standard vertical scroll bar.
                /// </summary>
                SB_VERT = 0x1,
                /// <summary>
                /// Sets the position of the scroll box in a scroll bar control. The hwnd parameter must be the handle to the scroll bar control.
                /// </summary>
                SB_CTL = 0x2,
                SB_BOTH = 0x3,
            }


            /// <summary>
            /// The GetScrollRange function retrieves the current minimum and maximum scroll box (thumb) positions for the specified scroll bar.
            /// 
            /// Note: The GetScrollRange function is provided for compatibility only. New applications should use the GetScrollInfo function.
            /// 
            /// Remarks:
            /// If the specified window does not have standard scroll bars or is not a scroll bar control, the GetScrollRange function copies zero to the lpMinPos and lpMaxPos parameters.
            /// The default range for a standard scroll bar is 0 through 100. The default range for a scroll bar control is empty (both values are zero).
            /// The messages that indicate scroll bar position, WM_HSCROLL and WM_VSCROLL, are limited to 16 bits of position data. However, because SetScrollInfo, SetScrollPos, SetScrollRange, GetScrollInfo, GetScrollPos, and GetScrollRange support 32-bit scroll bar position data, there is a way to circumvent the 16-bit barrier of the WM_HSCROLL and WM_VSCROLL messages. See the GetScrollInfo function for a description of the technique.
            /// If the nBar parameter is SB_CTL and the window specified by the hWnd parameter is not a system scroll bar control, the system sends the SBM_GETRANGE message to the window to obtain scroll bar information. This allows GetScrollRange to operate on a custom control that mimics a scroll bar. If the window does not handle the SBM_GETRANGE message, the GetScrollRange function fails.
            /// </summary>
            /// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll bar, depending on the value of the nBar parameter.</param>
            /// <param name="nBar">Specifies the scroll bar from which the positions are retrieved.</param>
            /// <param name="lpMinPos">Pointer to the integer variable that receives the minimum position.</param>
            /// <param name="lpMaxPos">Pointer to the integer variable that receives the maximum position.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError. 
            /// </returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool GetScrollRange(IntPtr hWnd, ScrollBar nBar, out int lpMinPos, out int lpMaxPos);

            /// <summary>
            /// The SetScrollRange function sets the minimum and maximum scroll box positions for the specified scroll bar.
            /// 
            /// Note: The SetScrollRange function is provided for backward compatibility. New applications should use the SetScrollInfo function.
            /// 
            /// Remarks:
            /// You can use SetScrollRange to hide the scroll bar by setting nMinPos and nMaxPos to the same value. An application should not call the SetScrollRange function to hide a scroll bar while processing a scroll bar message. New applications should use the ShowScrollBar function to hide the scroll bar.
            /// If the call to SetScrollRange immediately follows a call to the SetScrollPos function, the bRedraw parameter in SetScrollPos must be zero to prevent the scroll bar from being drawn twice.
            /// The default range for a standard scroll bar is 0 through 100. The default range for a scroll bar control is empty (both the nMinPos and nMaxPos parameter values are zero). The difference between the values specified by the nMinPos and nMaxPos parameters must not be greater than the value of MAXLONG.
            /// Because the messages that indicate scroll bar position, WM_HSCROLL and WM_VSCROLL, are limited to 16 bits of position data, applications that rely solely on those messages for position data have a practical maximum value of 65,535 for the SetScrollRange function's nMaxPos parameter.
            /// However, because the SetScrollInfo, SetScrollPos, SetScrollRange, GetScrollInfo, GetScrollPos, and GetScrollRange functions support 32-bit scroll bar position data, there is a way to circumvent the 16-bit barrier of the WM_HSCROLL and WM_VSCROLL messages. See GetScrollInfo for a description of the technique.
            /// If the nBar parameter is SB_CTL and the window specified by the hWnd parameter is not a system scroll bar control, the system sends the SBM_SETRANGE message to the window to set scroll bar information. This allows SetScrollRange to operate on a custom control that mimics a scroll bar. If the window does not handle the SBM_SETRANGE message, the SetScrollRange function fails.
            /// </summary>
            /// <param name="hWnd">Handle to a scroll bar control or a window with a standard scroll bar, depending on the value of the nBar parameter.</param>
            /// <param name="nBar">Specifies the scroll bar to be set.</param>
            /// <param name="nMinPos">Specifies the minimum scrolling position.</param>
            /// <param name="nMaxPos">Specifies the maximum scrolling position.</param>
            /// <param name="bRedraw">Specifies whether the scroll bar should be redrawn to reflect the change. If this parameter is TRUE, the scroll bar is redrawn. If it is FALSE, the scroll bar is not redrawn.</param>
            /// <returns>
            /// If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero. To get extended error information, call GetLastError. 
            /// </returns>
            [DllImport("user32.dll", SetLastError = true)]
            public static extern bool SetScrollRange(IntPtr hWnd, ScrollBar nBar, int nMinPos, int nMaxPos, bool bRedraw);


            [DllImport("user32.dll")]
            public static extern bool PostMessageA(IntPtr hWnd, int nBar, int wParam, int lParam);

            public const int WM_HSCROLL = 0x114;
            public const int WM_VSCROLL = 0x115;
            public const int SB_THUMBPOSITION = 4;

            #endregion Scrollbar Position


            #region Flicker Free Updates

            public const int WM_SETREDRAW = 0x000B;
            public const int WM_USER = 0x400;
            public const int EM_GETEVENTMASK = (WM_USER + 59);
            public const int EM_SETEVENTMASK = (WM_USER + 69);

            [DllImport("user32", CharSet = CharSet.Auto)]
            public extern static IntPtr SendMessage(IntPtr hWnd, int msg, int wParam, IntPtr lParam);

            #endregion Flicker Free Updates
        }

        #endregion Classes


        #region Member Variables

        private RichTextBox managedRichTextBox = null;
        public bool ScrollValuesArePixels { get; set; } = true;

        #endregion Member Variables


        #region Constructors

        public RichTextBoxHelper(RichTextBox richTextBoxToManage)
        {
            managedRichTextBox = richTextBoxToManage;
        }

        #endregion Constructors


        #region Methods

        /// <summary>
        /// A static function that simply initilizes a new helper object.
        /// </summary>
        /// <param name="richTextBox">Rich text box to create helper for.</param>
        /// <returns>A new helper object.</returns>
        public static RichTextBoxHelper Manage(RichTextBox richTextBox)
        {
            return new RichTextBoxHelper(richTextBox);
        }


        /// <summary>
        /// Warning: seems to cause artefacts.
        /// </summary>
        /// <param name="update"></param>
        public void FlickerFreeUpdate(Action update)
        {
            IntPtr eventMask = IntPtr.Zero;

            try
            {
                // Stop redrawing:
                Native.SendMessage(managedRichTextBox.Handle, Native.WM_SETREDRAW, 0, IntPtr.Zero);

                // Stop sending of events:
                eventMask = Native.SendMessage(managedRichTextBox.Handle, Native.EM_GETEVENTMASK, 0, IntPtr.Zero);

                // change colors and stuff in the RichTextBox:
                update();
            }
            finally
            {
                // turn on events
                Native.SendMessage(managedRichTextBox.Handle, Native.EM_SETEVENTMASK, 0, eventMask);

                // turn on redrawing
                Native.SendMessage(managedRichTextBox.Handle, Native.WM_SETREDRAW, 1, IntPtr.Zero);
            }
        }


        /// <summary>
        /// Append text while keeping scroll and selection unchanged.
        /// </summary>
        /// <param name="textToAppend">Text to append.</param>
        public void AppendText(string textToAppend)
        {
            InsertText(managedRichTextBox.Text.Length, textToAppend);
        }

        /// <summary>
        /// Inserts text while keeping scroll and selection unchanged.
        /// </summary>
        public void InsertText(int indexToInsertAt, string textToInsert)
        {
            if (indexToInsertAt < 0)
                indexToInsertAt = 0;

            if (textToInsert == null)
                textToInsert = "";

            SelectionInfo originalSelection = Selection;
            var scroll = AbsoluteScroll;

            if (indexToInsertAt >= managedRichTextBox.Text.Length)
            {
                managedRichTextBox.AppendText(textToInsert);
            }
            else
            {
                managedRichTextBox.Text = managedRichTextBox.Text.Insert(indexToInsertAt, textToInsert);
            }

            Selection = originalSelection.GetSelectionInfoAfterTextInsert(indexToInsertAt, textToInsert);
            AbsoluteScroll = scroll;
        }


        /// <summary>
        /// Begins an invoke to scrolls to selection.
        /// </summary>
        public void ScrollToSelection()
        {
            managedRichTextBox.BeginInvoke((Action)delegate ()
            {
                Selection = Selection;
            });
        }


        /// <summary>
        /// Get the bounds for some text in the Rich Text Box. The bounds are relative to the first character in the text box.
        /// WARNING: MIGHT BE BUGS!
        /// </summary>
        /// <param name="selection">The selection for the text to get bounds for.</param>
        /// <returns>A rectangle representing the bounds for the text.</returns>
        public Rectangle GetTextBounds(SelectionInfo selection)
        {
            Rectangle rectangle = new Rectangle();


            // Y:
            rectangle.Y = managedRichTextBox.GetPositionFromCharIndex(selection.StartIndex).Y;


            // Height (from line count and line height):
            int lines;
            if (managedRichTextBox.WordWrap)
                lines = ((int)Math.Round((managedRichTextBox.GetPositionFromCharIndex(selection.EndIndex).Y - rectangle.Y) / (double)LineHeight)) + 1;
            else
                lines = TextManager.SplitMutiLineString(managedRichTextBox.Text.Substring(selection.StartIndex, selection.Length)).Length;

            rectangle.Height = (int)(LineHeight * lines);


            // X:
            if (lines > 1)
                rectangle.X = 0;    // Multiline text will cover the start of the textbox.
            else
                rectangle.X = managedRichTextBox.GetPositionFromCharIndex(selection.StartIndex).X;


            // Width (from get pos at end of each affected line):
            int yFirstLine = managedRichTextBox.GetPositionFromCharIndex(0).Y;
            int maxWidth = managedRichTextBox.PreferredSize.Width;
            int indexForFirstSelectedLine = managedRichTextBox.GetLineFromCharIndex(selection.StartIndex);

            for (int iii = 0; iii < lines; iii++)
            {
                int lineYPos = (int)(yFirstLine + LineHeight / 2 + LineHeight * (indexForFirstSelectedLine + iii));
                int xPosForLastCharOnLine = managedRichTextBox.GetPositionFromCharIndex(managedRichTextBox.GetCharIndexFromPosition(new Point(maxWidth, lineYPos))).X;

                int width = xPosForLastCharOnLine - rectangle.X;
                if (rectangle.Width < width)
                    rectangle.Width = width;
            }


            // Current coordinates are relative to the text box view. 
            // To make it relative to the text box area we offset it with the coordinate for the first character (this value can be negative).
            rectangle.Offset(managedRichTextBox.GetPositionFromCharIndex(0));

            return rectangle;
        }


        #region Get and Set Scroll Position

        public double GetScrollPosition(bool horizontalScrollBar)
        {
            // Check if we can scroll:
            if (horizontalScrollBar ? !HorizontalScrollVisible : !VerticalScrollVisible)
                return 0;


            // Get Scroll bar range:
            GetScrollBarRange(horizontalScrollBar, out int scrollBarMin, out int scrollBarMax);

            int range = scrollBarMax - scrollBarMin;
            if (range <= 0)
                return 0;


            // Get Scroll bar position:
            int scrollValue = GetScrollAbsolutePosition(horizontalScrollBar);


            // Calcualte scroll and check value:
            double scroll = (scrollValue - scrollBarMin) / (double)range;
            if (scroll > 1)
                return 1;
            else if (scroll < 0)
                return 0;
            else
                return scroll;
        }
        public int GetScrollAbsolutePosition(bool horizontalScrollBar)
        {
            return Native.GetScrollPos(managedRichTextBox.Handle, horizontalScrollBar ? Native.ScrollBar.SB_HORZ : Native.ScrollBar.SB_VERT);
        }

        /// <summary>
        /// Set the top-left corner of the scroll view relative to the total scrollable area.
        /// </summary>
        /// <param name="horizontalScrollBar">Determines if the horizontal or vertical scroll bar's position is set.</param>
        /// <param name="scrollPos">The top-left corner of the scroll view divided by the total scrollable area.</param>
        public void SetScrollPosition(bool horizontalScrollBar, double scrollPos)
        {
            // Check argument;
            if (scrollPos < 0)
                scrollPos = 0;
            if (scrollPos > 1)
                scrollPos = 1;

            // Check if we can scroll:
            if (horizontalScrollBar ? !HorizontalScrollVisible : !VerticalScrollVisible)
                return;

            // Get scroll range on scroll bar:
            GetScrollBarRange(horizontalScrollBar, out int scrollBarMin, out int scrollBarMax);


            int scrollValue = (int)(scrollPos * (scrollBarMax - scrollBarMin)) + scrollBarMin;

            SetScrollAbsolutePosition(horizontalScrollBar, scrollValue);
        }
        public void SetScrollAbsolutePosition(bool horizontalScrollBar, int scrollValue)
        {
            // TextBox scroll and scrollbar pos are disconected and both need to be set manualy.

            // Set ScrollBar pos:
            Native.SetScrollPos(
                managedRichTextBox.Handle,
                horizontalScrollBar ? Native.ScrollBar.SB_HORZ : Native.ScrollBar.SB_VERT,
                scrollValue,
                true
                );
            // Set TextBox scroll:
            Native.PostMessageA(
                managedRichTextBox.Handle,
                horizontalScrollBar ? Native.WM_HSCROLL : Native.WM_VSCROLL,
                Native.SB_THUMBPOSITION + 0x10000 * scrollValue,
                0);
        }


        private void GetScrollBarRange(bool horizontalScrollBar, out int scrollBarMin, out int scrollBarMax)
        {
            if (!Native.GetScrollRange(
                managedRichTextBox.Handle,
                horizontalScrollBar ? Native.ScrollBar.SB_HORZ : Native.ScrollBar.SB_VERT,
                out scrollBarMin,
                out scrollBarMax
                ))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        #endregion Get and Set Scroll Position

        #endregion Methods


        #region Properties

        #region ScrollBar

        /// <summary>
        /// Determine horizontal scrollbar visibility by comparing richTextBox.ClientRectangle and richTextBox.Size.
        /// </summary>
        private bool HorizontalScrollVisible
        {
            get
            {
                return (managedRichTextBox.Size.Height - managedRichTextBox.ClientRectangle.Height) >= SystemInformation.HorizontalScrollBarHeight;
            }
        }

        /// <summary>
        /// Determine vertical scrollbar visibility by comparing richTextBox.ClientRectangle and richTextBox.Size.
        /// </summary>
        private bool VerticalScrollVisible
        {
            get
            {
                return (managedRichTextBox.Size.Width - managedRichTextBox.ClientRectangle.Width) >= SystemInformation.VerticalScrollBarWidth;
            }
        }


        /// <summary>
        /// A percentage value between 0 and 1 indicating the horizontal scroll bar's position.
        /// </summary>
        public double HorizontalScrollPosition
        {
            get { return GetScrollPosition(true); }
            set { SetScrollPosition(true, value); }
        }

        /// <summary>
        /// A percentage value between 0 and 1 indicating the vertical scroll bar's position.
        /// </summary>
        public double VerticalScrollPosition
        {
            get { return GetScrollPosition(false); }
            set { SetScrollPosition(false, value); }
        }


        /// <summary>
        /// The number of pixels to the left of the scrolled view. Sets the horizontal scroll bar's position.
        /// </summary>
        public int AbsoluteHorizontalScrollPosition
        {
            get { return GetScrollAbsolutePosition(true); }
            set { SetScrollAbsolutePosition(true, value); }
        }

        /// <summary>
        /// The number of pixels above the scrolled view. Sets the vertical scroll bar's position.
        /// </summary>
        public int AbsoluteVerticalScrollPosition
        {
            get { return GetScrollAbsolutePosition(false); }
            set { SetScrollAbsolutePosition(false, value); }
        }

        #endregion ScrollBar


        #region Visible Lines

        /// <summary>
        /// The number of lines that can fit into the text box with its current height.
        /// </summary>
        public double MaxVisibleLines
        {
            get
            {
                double margin = managedRichTextBox.ZoomFactor;
                return (managedRichTextBox.ClientSize.Height - margin) / (double)LineHeight;
            }
        }

        public double LineHeight
        {
            get
            {
                return managedRichTextBox.Font.Height * (double)managedRichTextBox.ZoomFactor;
            }
        }

        /// <summary>
        /// Number of lines as shown by the RichTextBox. If word wrap is on this can be more than the lines in the RichTextBox's text.
        /// </summary>
        public int TotalShownLines
        {
            get
            {
                var firstPos = managedRichTextBox.GetPositionFromCharIndex(0);

                int lines;
                if (managedRichTextBox.WordWrap)
                {
                    int textLength = managedRichTextBox.Text.Length;
                    var lastPos = managedRichTextBox.GetPositionFromCharIndex(textLength == 0 ? 0 : textLength - 1);
                    lines = (int)Math.Round((lastPos.Y - firstPos.Y) / (double)LineHeight) + 1;
                }
                else
                {
                    lines = managedRichTextBox.Lines.Length;
                }

                if (lines < 1)
                    lines = 1;

                return lines;
            }
        }


        /// <summary>
        /// The 0-based index of the first line in the RichTextBox whose height is fully visible. This value can be greater than the number of lines in the RichTextBox's text if word wrap is on.
        /// </summary>
        public int IndexOfFirstVisibleShownLine
        {
            get
            {
                int lines = (int)Math.Ceiling(LinesBeforeStartOfVisableArea);

                var max = TotalShownLines;
                if (lines > max)
                    lines = max;

                return lines;
            }
            set
            {
                if (managedRichTextBox.Lines.Length == 0)
                    return;

                if (value < 0)
                    value = 0;
                var max = TotalShownLines;
                if (value >= max)
                    value = max - 1;


                bool useScrollCalculation = true;

                if (useScrollCalculation)
                {
                    // Scroll calculation approach:
                    Rectangle visible = VisibleTextRegion;
                    var temp = LinesBeforeStartOfVisableArea;
                    visible.Y = (int)(LineHeight * value);
                    var scroll = new RelativeScrollInfo(visible, TotalTextArea).ToAbsolute(managedRichTextBox);
                    AbsoluteScroll = new AbsoluteScrollInfo(AbsoluteScroll.HorizontalScroll, scroll.VerticalScroll);
                }
                else
                {
                    // Selection based approach:
                    var wantedSelection = Selection;
                    managedRichTextBox.Select(managedRichTextBox.Text.Length - 1, 0);
                    managedRichTextBox.Select(managedRichTextBox.GetFirstCharIndexFromLine(value), 0);
                    var wantedScroll = AbsoluteScroll;
                    Selection = wantedSelection;
                    AbsoluteScroll = wantedScroll;
                }
            }
        }

        /// <summary>
        /// The 0-based index of the last line in the RichTextBox whose height is fully visible. This value can be greater than the number of lines in the RichTextBox's text if word wrap is on.
        /// </summary>
        public int IndexOfLastVisibleShownLine
        {
            get
            {
                int lines = (int)Math.Truncate(LinesBeforeEndOfVisableArea);
                if (lines > 0)
                    return lines - 1;
                else
                    return lines;
            }
        }


        public double LinesBeforeStartOfVisableArea
        {
            get
            {
                double lines = VisibleTextRegion.Y / (double)(LineHeight);

                var max = TotalShownLines;
                if (lines > max)
                    lines = max;

                return lines;
            }
        }

        public double LinesBeforeEndOfVisableArea
        {
            get
            {
                double lines = VisibleTextRegion.Bottom / (double)(LineHeight);

                var max = TotalShownLines;
                if (lines > max)
                    lines = max;

                return lines;
            }
        }

        #endregion Visible Lines


        #region Visible Characters

        /// <summary>
        /// Index of the first visible character. Height is entirely visible but width might be only partially visible or not visable at all.
        /// </summary>
        public int IndexOfFirstVisibleCharacter
        {
            get
            {
                int firstChar = managedRichTextBox.GetCharIndexFromPosition(new Point(0, 0));
                int lineYPos = managedRichTextBox.GetPositionFromCharIndex(firstChar).Y;

                // if only part of the line's height is visible then select next line if possible.
                if (lineYPos >= 0)
                    return firstChar;
                else
                {
                    int nextLineYPos = (int)Math.Ceiling(lineYPos + LineHeight);

                    if (nextLineYPos + LineHeight > VisibleTextArea.Height)
                        return firstChar;
                    else
                        return managedRichTextBox.GetCharIndexFromPosition(new Point(0, nextLineYPos + (int)(LineHeight * 0.5)));
                }
            }
        }

        /// <summary>
        /// Index of the last visible character. Height is entirely visible but width might be only partially visible or not visable at all.
        /// </summary>
        public int IndexOfLastVisibleCharacter
        {
            get
            {
                Size visableArea = VisibleTextArea;
                int lastChar = managedRichTextBox.GetCharIndexFromPosition(new Point(visableArea));
                int lineYPos = managedRichTextBox.GetPositionFromCharIndex(lastChar).Y;

                if (lineYPos + LineHeight <= visableArea.Height)
                    return lastChar;
                else
                {
                    int previousLineYPos = (int)Math.Truncate(lineYPos - LineHeight);
                    if (previousLineYPos < 0)
                        return lastChar;
                    else
                        return managedRichTextBox.GetCharIndexFromPosition(new Point(visableArea.Width, previousLineYPos + (int)(LineHeight * 0.5)));
                }
            }
        }

        #endregion  Visible Characters


        #region Visible Area

        /// <summary>
        /// X-coordinate of the scroll view.
        /// </summary>
        public int XPositionOfScrollView
        {
            get { return PositionOfScrollView.X; }
            set { PositionOfScrollView = new Point(value, AbsoluteScroll.VerticalScroll); }
        }

        /// <summary>
        /// Y-coordinate of the scroll view.
        /// </summary>
        public int YPositionOfScrollView
        {
            get { return PositionOfScrollView.Y; }
            set { PositionOfScrollView = new Point(AbsoluteScroll.HorizontalScroll, value); }
        }

        /// <summary>
        /// The top-left corner of the visible area of the text box.
        /// </summary>
        public Point PositionOfScrollView
        {
            get
            {
                Point textOrigoInCurrentCoordinateSystem = managedRichTextBox.GetPositionFromCharIndex(0);
                return new Point(0 - textOrigoInCurrentCoordinateSystem.X, 0 - textOrigoInCurrentCoordinateSystem.Y);
            }
            set
            {
                Scroll = new AbsoluteScrollInfo(value);
            }
        }

        /// <summary>
        /// Region of total text area that is currently visible in the text box.
        /// </summary>
        public Rectangle VisibleTextRegion
        {
            get
            {
                return new Rectangle(PositionOfScrollView, VisibleTextArea);
            }
        }


        /// <summary>
        /// Size of the currently visible text area. Can be bigger than total text area if the text doesn't cover all of the available text box space.
        /// </summary>
        public Size VisibleTextArea
        {
            get
            {
                return managedRichTextBox.ClientSize;
            }
        }

        /// <summary>
        /// Size of the text in the text box. This area can be bigger then the text box itself.
        /// </summary>
        public Size TotalScrollableArea
        {
            get
            {
                // Wanted Size for unzoomed text box (includes extra space for scroll bars) (if word wrap is on then no extra space for horizontal scroll bar):
                Size prefered = managedRichTextBox.PreferredSize;
                // Current Size:
                Size size = managedRichTextBox.Size;
                // Current available space for text:
                Size client = managedRichTextBox.ClientSize;
                // Size of scrollbars (included in Size but not in clientSize):
                Size invislbeScrollBars = new Size(!VerticalScrollVisible ? SystemInformation.VerticalScrollBarWidth : 0, !HorizontalScrollVisible && !managedRichTextBox.WordWrap ? SystemInformation.HorizontalScrollBarHeight : 0);

                // Unused Client Size (not text area):
                Size unusedSize = Size.Subtract(size, client);
                // Unused client size including invisble scroll bars. (At prefered size there will always be scroll bars):
                Size unusedSizeWithScrollBars = Size.Add(unusedSize, invislbeScrollBars);

                // Text Area for unzoomed text box:
                Size unzoomedTextArea = Size.Subtract(prefered, unusedSizeWithScrollBars);

                // Text Area for current zoom (without word wrap):
                Size textAreaWithoutWordWrap = new Size((int)Math.Ceiling(unzoomedTextArea.Width * managedRichTextBox.ZoomFactor), (int)Math.Ceiling(unzoomedTextArea.Height * managedRichTextBox.ZoomFactor));

                if (managedRichTextBox.WordWrap)
                {
                    int lines = managedRichTextBox.Lines.Length;
                    if (lines < 1)
                        lines = 1;

                    int wordWrapedLines = TotalShownLines - lines;

                    return new Size(VisibleTextArea.Width, textAreaWithoutWordWrap.Height + (int)Math.Ceiling(wordWrapedLines * LineHeight));
                }
                else
                    return textAreaWithoutWordWrap;
            }
        }

        /// <summary>
        /// Size of the scrollable area where text is allowed.
        /// </summary>
        public Size TotalTextArea
        {
            get
            {
                return Size.Subtract(TotalScrollableArea, TextMargin);
            }
        }

        /// <summary>
        /// Size of the text area that is used as margin. The text lines aren't allowed to use all of the scrollable area and this margin specifies how much isn't usable.
        /// </summary>
        public Size TextMargin
        {
            get
            {
                Size scrollArea = TotalScrollableArea;

                int textHeight = (int)(LineHeight * TotalShownLines);
                int heightMargin = scrollArea.Height - textHeight;

                int maxWidth = 0;
                int longestLineIndex = 0;
                string[] lines = managedRichTextBox.Lines;
                for (int iii = 0; iii < lines.Length; iii++)
                {
                    string line = lines[iii];
                    if (maxWidth < line.Length)
                    {
                        maxWidth = line.Length;
                        longestLineIndex = iii;
                    }
                }

                int textWidth = 0;
                if (maxWidth > 0)
                {
                    bool calculateUsingFont = false;

                    if (calculateUsingFont || maxWidth == 1)
                    {
                        textWidth = (int)(TextManager.GetTextScreenSize(lines[longestLineIndex], managedRichTextBox.Font).Width * managedRichTextBox.ZoomFactor);
                    }
                    else
                    {
                        int linePosY;
                        if (managedRichTextBox.WordWrap)
                        {
                            string[] linesBeforeLongestLine = lines.Reverse().Skip(lines.Length - longestLineIndex).Reverse().ToArray();
                            string lineBreak = "\r";
                            string textBeforeLongestLine = TextManager.CombineStringCollection(linesBeforeLongestLine, lineBreak) + lineBreak;
                            int firstIndex = textBeforeLongestLine.Length;

                            linePosY = managedRichTextBox.GetPositionFromCharIndex(firstIndex).Y;
                        }
                        else
                        {
                            linePosY = managedRichTextBox.GetPositionFromCharIndex(managedRichTextBox.GetFirstCharIndexFromLine(longestLineIndex)).Y;
                        }

                        int minX = managedRichTextBox.GetPositionFromCharIndex(0).X;
                        int maxX = scrollArea.Width;

                        int firstChar = managedRichTextBox.GetCharIndexFromPosition(new Point(minX, linePosY));
                        int lastChar = managedRichTextBox.GetCharIndexFromPosition(new Point(maxX, linePosY));

                        int firstPosX = managedRichTextBox.GetPositionFromCharIndex(firstChar).X;
                        int lastPosX = managedRichTextBox.GetPositionFromCharIndex(lastChar).X;
                        if (lastPosX <= firstPosX)
                            lastPosX = managedRichTextBox.GetPositionFromCharIndex(lastChar - 1).X;

                        int diff = lastPosX - firstPosX;
                        //diff = (int)(diff * (1.0 + (1.0 / lines[longestLineIndex].Length)));    // Extrapolate the extra size for one more character
                        textWidth = diff;
                    }
                }

                int widthMargin = scrollArea.Width - textWidth + 7;

                return new Size(widthMargin, heightMargin);
            }
        }

        #endregion Visible Area


        #region Info Structures

        public ScrollInfo Scroll
        {
            get
            {
                return AbsoluteScroll;
            }
            set
            {
                if (value is AbsoluteScrollInfo)
                    AbsoluteScroll = value as AbsoluteScrollInfo;
                else if (value is RelativeScrollInfo)
                    RelativeScroll = value as RelativeScrollInfo;
            }
        }

        /// <summary>
        /// Set or get the scroll bars position. When scroll is set it will not be applied immediately instead it will be done in an invoke that will run when the ui thread is free to handle it (it will be queued on the ui thread).
        /// </summary>
        public RelativeScrollInfo RelativeScroll
        {
            get
            {
                return AbsoluteScroll.ToRelative(managedRichTextBox);
            }
            set
            {
                if (value == null)
                    return;

                AbsoluteScrollInfo absoluteScroll = value.ToAbsolute(managedRichTextBox);

                double xScroll = absoluteScroll.HorizontalScroll;
                double yScroll = absoluteScroll.VerticalScroll;

                var totalArea = TotalTextArea;
                var max = Size.Add(Size.Subtract(totalArea, VisibleTextArea), new Size(0, TextMargin.Height));


                if (xScroll > max.Width)
                    xScroll = max.Width;
                if (yScroll > max.Height)
                    yScroll = max.Height;

                HorizontalScrollPosition = xScroll / (double)totalArea.Width;
                VerticalScrollPosition = yScroll / (double)totalArea.Height;
            }
        }

        /// <summary>
        /// Set or get the scroll bars position. When scroll is set it will not be applied immediately instead it will be done in an invoke that will run when the ui thread is free to handle it (it will be queued on the ui thread).
        /// </summary>
        public AbsoluteScrollInfo AbsoluteScroll
        {
            get
            {
                if (ScrollValuesArePixels)
                    return new AbsoluteScrollInfo(AbsoluteHorizontalScrollPosition, AbsoluteVerticalScrollPosition);
                else
                {
                    GetScrollBarRange(true, out int min, out int max);

                    int range = max - min;
                    int compare = TotalTextArea.Width;
                    // DEBUG:
                    // Console.WriteLine(range + "/" + compare + " - diff: " + (int)Math.Abs(range - compare));
                    var totalArea = TotalTextArea;

                    return new AbsoluteScrollInfo((int)Math.Ceiling(HorizontalScrollPosition * totalArea.Width), (int)Math.Ceiling(VerticalScrollPosition * totalArea.Height));
                }
            }
            set
            {
                if (value == null)
                    return;

                if (ScrollValuesArePixels)
                {
                    AbsoluteHorizontalScrollPosition = value.HorizontalScroll;
                    AbsoluteVerticalScrollPosition = value.VerticalScroll;
                }
                else
                    RelativeScroll = value.ToRelative(managedRichTextBox);
            }
        }

        /// <summary>
        /// The current selection in the rich text box.
        /// </summary>
        public SelectionInfo Selection
        {
            get
            {
                return new SelectionInfo(managedRichTextBox.SelectionStart, managedRichTextBox.SelectionLength);
            }
            set
            {
                if (value == null)
                    return;

                managedRichTextBox.Select(value.StartIndex, value.Length);
            }
        }

        #endregion Info Structures

        #endregion Properties
    }
}