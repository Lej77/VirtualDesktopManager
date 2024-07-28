using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

using VirtualDesktopManager.Properties;

namespace VirtualDesktopManager
{
    static class IconManager
    {
        #region Classes

        private class Native
        {
            /// <summary>
            /// Destroys an icon and frees any memory the icon occupied. 
            /// </summary>
            /// <param name="handle">A handle to the icon to be destroyed. The icon must not be in use.</param>
            /// <returns>If the function succeeds, the return value is nonzero.
            /// If the function fails, the return value is zero.To get extended error information, call GetLastError. </returns>
            [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
            public static extern bool DestroyIcon(IntPtr handle);
        }

        #endregion Classes


        #region Methods

        public static Icon CreateIconWithNumber(int number)
        {
            switch (number)
            {
                case 1:
                    return Resources.triangle1;
                case 2:
                    return Resources.triangle2;
                case 3:
                    return Resources.triangle3;
                case 4:
                    return Resources.triangle4;
                case 5:
                    return Resources.triangle5;
                case 6:
                    return Resources.triangle6;
                case 7:
                    return Resources.triangle7;
                case 8:
                    return Resources.triangle8;
                case 9:
                    return Resources.triangle9;
                    // */
            }

            using (Bitmap image = CreateImageWithNumber(number))
            {
                return ConvertIconFromBitmap(image);
            }
        }

        public static Bitmap CreateImageWithNumber(int number)
        {
            var fontSize = 140;
            var xPlacement = 100;
            var yPlacement = 50;

            int letters = number.ToString().Length;

            if (letters == 2)
            {
                fontSize = 125;
                xPlacement = 75;
                yPlacement = 65;
            }
            else if (letters >= 3)
            {
                fontSize = 80;
                xPlacement = 90;
                yPlacement = 100;

                if (letters > 3)
                {
                    number = number < 0 ? -99 : 999;
                }
            }

            Bitmap newIcon = null;
            try
            {
                newIcon = Properties.Resources.triangleEmptyImage;

                using (Font desktopNumberFont = new Font("Segoe UI", fontSize, FontStyle.Bold, GraphicsUnit.Pixel))
                {
                    using (var gr = Graphics.FromImage(newIcon))
                    {
                        using (Brush brush = new SolidBrush(Color.White))
                        {
                            gr.DrawString(number.ToString(), desktopNumberFont, brush, xPlacement, yPlacement);
                        }
                    }
                }
                return newIcon;
            }
            catch
            {
                if (newIcon != null)
                    newIcon.Dispose();

                throw;
            }
        }

        public static Icon ConvertIconFromBitmap(Bitmap bitmap)
        {
            Icon unmangedIcon = null;
            try
            {
                unmangedIcon = Icon.FromHandle(bitmap.GetHicon());
                return unmangedIcon.Clone() as Icon;
            }
            finally
            {
                if (unmangedIcon != null)
                {
                    DestroyIcon(unmangedIcon);
                }
            }
        }

        private static void DestroyIcon(Icon icon)
        {
            // false is equal to zero.
            if (!Native.DestroyIcon(icon.Handle))
            {
                Marshal.ThrowExceptionForHR(Marshal.GetHRForLastWin32Error());
            }
        }

        #endregion Methods


        #region Properties

        #endregion Properties
    }
}
