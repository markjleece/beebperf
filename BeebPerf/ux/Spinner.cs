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

using System.Drawing.Drawing2D;

namespace BeebPerf.ux
{
    internal class Spinner : Panel
    {
        public Spinner() : base()
        {
            _Timer = new System.Windows.Forms.Timer();
            _Timer.Interval = 20;
            _Timer.Tick += Timer_Tick;
            VisibleChanged += OnVisibleChanged;
        }

        private void OnVisibleChanged(object? sender, EventArgs e)
        {
            if (Visible)
                _Timer.Start();
            else
                _Timer.Stop();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _Angle += (Math.PI / 16.0);
            if (_Angle > (2.0 * Math.PI))
                _Angle = 0.0;

            int size = Math.Min(Width, Height);

            double majorRadius = 0.35 * size;
            double minorRadius = 0.08 * size;

            int centerX = (Width / 2) - (int)minorRadius;
            int centerY = (Height / 2) - (int)minorRadius;

            int circleSize = (int)(minorRadius * 2);

            using Graphics graphics = CreateGraphics();

            for (double angle = 0.0; angle <= (2.0 * Math.PI); angle += Math.PI / 6.0)
            {
                int x = centerX + (int)(Math.Sin(angle) * majorRadius);
                int y = centerY - (int)(Math.Cos(angle) * majorRadius);

                double delta = _Angle - angle;
                if (delta < 0)
                    delta = (2.0 * Math.PI) + delta;

                double ratio = Math.Max(1.0 - delta * (1.25 / (2.0 * Math.PI)), 0);

                using Pen backPen = new Pen(BackColor, 2);
                graphics.SmoothingMode = SmoothingMode.None;
                graphics.DrawEllipse(backPen, x, y, circleSize, circleSize);

                using Brush foreBrush = new SolidBrush(Blend(BackColor, ForeColor, ratio));
                graphics.SmoothingMode = SmoothingMode.HighQuality;
                graphics.FillEllipse(foreBrush, x, y, circleSize, circleSize);
            }
        }

        private Color Blend(Color first, Color second, double ratio)
        {
            int r = (int)(first.R * (1 - ratio) + second.R * ratio);
            int g = (int)(first.G * (1 - ratio) + second.G * ratio);
            int b = (int)(first.B * (1 - ratio) + second.B * ratio);
            return Color.FromArgb(r, g, b);
        }

        private double _Angle = 0;
        private System.Windows.Forms.Timer _Timer;
    }
}