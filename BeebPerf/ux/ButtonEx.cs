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

namespace BeebPerf.ux
{
    public class ButtonEx : Button
    {
        public string? ImageResourceName
        {
            set
            {
                if (value != null)
                {
                    var resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
                    Image = (Image?)resources.GetObject(value);
                }
            }
        }

        public string? ToolTipText = null;

        public ButtonEx() : base()
        {
            Size = new Size(34, 28);
            Visible = false;

            _ToolTipTimer = new();
            _ToolTipTimer.Interval = 500;
            _ToolTipTimer.Tick += ToolTipTimer_Tick;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (Image == null)
                return;

            // compute innerRect rectangle
            var innerRect = new Rectangle(
                ClientRectangle.Left + Padding.Left,
                ClientRectangle.Top + Padding.Top,
                ClientRectangle.Width - Padding.Horizontal,
                ClientRectangle.Height - Padding.Vertical);

            // compute aspect ratio preserving scale
            float scaleX = (float)innerRect.Width / Image.Width;
            float scaleY = (float)innerRect.Height / Image.Height;
            float scale = Math.Min(scaleX, scaleY);

            // scale image if more than 20% different from original newSize
            var newSize = (scale >= 0.8f && scale <= 1.2f)
                ? Image.Size
                : new Size((int)(Image.Width * scale), (int)(Image.Height * scale));

            // center the image
            int left = innerRect.Left + (innerRect.Width - newSize.Width) / 2;
            int top = innerRect.Top + (innerRect.Height - newSize.Height) / 2;

            // draw the image
            e.Graphics.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
            e.Graphics.DrawImage(Image, left, top, newSize.Width, newSize.Height);
        }

        protected override void OnMouseMove(MouseEventArgs e)
        {
            base.OnMouseMove(e);

            Cursor = Cursors.Default;
            _ToolTipTimer.Stop();
            _ToolTipTimer.Start();
        }

        protected override void OnMouseEnter(EventArgs e)
        {
            base.OnMouseEnter(e);

            Cursor = Cursors.Default;
            _ToolTipTimer.Stop();
            _ToolTipTimer.Start();
        }

        protected override void OnMouseLeave(EventArgs e)
        {
            base.OnMouseLeave(e);

            _ToolTipTimer.Stop();
            _ToolTip.Hide(this);
        }

        private void ToolTipTimer_Tick(object? sender, EventArgs e)
        {
            _ToolTipTimer.Stop();
            if (Visible & ToolTipText != null)
                _ToolTip.Show(ToolTipText, this);
        }

        private ToolTip _ToolTip = new ToolTip();
        private System.Windows.Forms.Timer _ToolTipTimer = new();
    }
}