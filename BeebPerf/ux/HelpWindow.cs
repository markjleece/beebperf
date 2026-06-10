// --------------------------------------------------------------
// BeebPerf - A BBC Micro Profiler
//
// Copyright (C) 2026  Mark John Leece
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

using System.Runtime.InteropServices;

namespace BeebPerf.ux
{
    public partial class HelpWindow : Form
    {
        [DllImport("kernel32.dll", SetLastError = true, CharSet = CharSet.Unicode)]
        private static extern bool MoveFileEx(string lpExistingFileName, string? lpNewFileName, int dwFlags); 
        private const int MOVEFILE_DELAY_UNTIL_REBOOT = 0x4;

        public HelpWindow(BeebPerfForm form)
        {
            InitializeComponent();

            // read PDF from resource
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(HelpWindow));
            var bytes = (byte[])resources.GetObject("help.PDF")!;

            // create temp PDF file, which is cleaned up after crash when Windows reboots
            string tempFilePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid() + ".pdf");
            File.WriteAllBytes(tempFilePath, bytes);
            MoveFileEx(tempFilePath, null, MOVEFILE_DELAY_UNTIL_REBOOT);

            // initialize WebView2 control and load the temp file
            Load += async (s, e) =>
            {
                await webView21.EnsureCoreWebView2Async();
                webView21.Source = new Uri(tempFilePath);
            };

            // cleanup temp file on close
            FormClosing += async (s, e) =>
            {
                try
                {
                    await webView21.EnsureCoreWebView2Async();
                    webView21.CoreWebView2.NavigateToString("<html></html>");

                    await Task.Delay(200);
                    if (File.Exists(tempFilePath))
                        File.Delete(tempFilePath);
                }
                catch
                {
                    // ignore cleanup errors
                }
            };
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            var workingArea = Screen.PrimaryScreen.WorkingArea;

            int width = (int)(workingArea.Width / 2);
            int height = workingArea.Height;
            int x = workingArea.Right - width;
            int y = workingArea.Top;

            this.Location = new Point(x, y);
            this.Size = new Size(width, height);
        }
    }
}
