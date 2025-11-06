// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2025  Mark John Leece
//
// This program is free software; you can redistribute it and/or
// modify it under the terms of the GNU General Public License
// as published by the Free Software Foundation; either version 2
// of the License, or (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public
// License along with this program; if not, write to the Free
// Software Foundation, Inc., 51 Franklin Street, Fifth Floor,
// Boston, MA  02110-1301, USA.
// --------------------------------------------------------------

using BeebPerf.model;
using Microsoft.Win32;
using System.Runtime.InteropServices;

namespace BeebPerf
{
    public static class ThemeManager
    {
        public enum ThemeType
        {
            System = 0,
            Dark = 1,
            Light = 2
        }

        [DllImport("user32.dll")]
        static public extern bool RedrawWindow([In] IntPtr hWnd, [In] IntPtr lprcUpdate, [In] IntPtr hrgnUpdate, [In] uint flags);

        [DllImport("dwmapi.dll")]
        private static extern int DwmSetWindowAttribute(IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        [DllImport("uxtheme.dll", CharSet = CharSet.Unicode)]
        private static extern int SetWindowTheme(IntPtr hWnd, string pszSubAppName, string? pszSubIdList);


        public static bool CanSetTheme()
        {
            return (Environment.OSVersion.Version.Major >= 10);
        }

        public static void SetTheme(Form form, ThemeType theme)
        {
            if (!CanSetTheme())
                return;

            bool darkMode = (theme == ThemeType.Dark) || (theme == ThemeType.System && IsSystemInDarkMode());

#pragma warning disable WFO5001
            var colorMode = darkMode ? SystemColorMode.Dark : SystemColorMode.Classic;
            Application.SetColorMode(colorMode);
#pragma warning restore WFO5001

            ApplyTheme(form, darkMode);

            if (form.Owner != null)
                ApplyTheme(form.Owner, darkMode);
        }
        private static bool IsSystemInDarkMode()
        {
            using var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme")?.ToString() == "0";
        }

        private static void ApplyTheme(Control control, bool darkMode)
        {
            if (control == null)
                return;

            string windowTheme = darkMode ? "DarkMode_Explorer" : "Explorer";
            SetWindowTheme(control.Handle, windowTheme, null);

            int mode = darkMode ? 1 : 0;
            const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;
            DwmSetWindowAttribute(control.Handle, DWMWA_USE_IMMERSIVE_DARK_MODE, ref mode, sizeof(int));

            const uint RDW_FRAME = 0x0400;
            const uint RDW_INVALIDATE = 0x0001;
            const uint RDW_UPDATENOW = 0x0100;
            RedrawWindow(control.Handle, IntPtr.Zero, IntPtr.Zero, RDW_FRAME | RDW_INVALIDATE | RDW_UPDATENOW);

            if (control is ComboBox)
            {
                var comboBox = (ComboBox)control;
                comboBox.FlatStyle = FlatStyle.Flat;
                var foreColor = comboBox.ForeColor;
                var backColor = comboBox.BackColor;
                comboBox.ForeColor = backColor;
                comboBox.ForeColor = foreColor;
                comboBox.BackColor = foreColor;
                comboBox.BackColor = backColor;
            }

            control.Invalidate();

            foreach (Control child in control.Controls)
                ApplyTheme(child, darkMode);
        }
    }
}