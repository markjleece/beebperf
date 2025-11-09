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
using static BeebPerf.model.DisplaySettings;

namespace BeebPerf.ux
{
    internal class SampleCodePanel : Panel
    {
        public DisplaySettings? DisplaySettings;

        protected override void OnPaintBackground(PaintEventArgs e)
        {
            if (DisplaySettings == null)
            {
                base.OnPaintBackground(e);
                return;
            }

            var themeSettings = DisplaySettings!.ColorThemeSettings;
            var color = (themeSettings.ColorTheme == ColorThemeType.Light) ? Color.White : Color.Black;
            using var brush = new SolidBrush(color);
            e.Graphics.FillRectangle(brush, e.ClipRectangle);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (DisplaySettings == null)
                return;

            var graphics = e.Graphics;

            // calculate column widths
            int addressColumnWidth = 0;
            int labelColumnWidth = 0;
            int padding = Font.Height / 2;
            foreach (var instruction in _Instructions)
            {
                addressColumnWidth = Math.Max(addressColumnWidth, MeasureSegmentWidth(instruction.AddressSegment) + padding);
                if (instruction.LabelSegment != null)
                    labelColumnWidth = Math.Max(labelColumnWidth, MeasureSegmentWidth((Segment)instruction.LabelSegment) + padding);
            }

            // paint instructions
            int indent = Font.Height / 2;
            int baseHeight = TextRenderer.MeasureText("Sample", Font).Height;
            int lineHeight = baseHeight + 6;
            var position = new Point(indent, Math.Max(0, (Height - _Instructions.Length * lineHeight) / 2));
            foreach (var instruction in _Instructions)
            {
                PaintSegments(graphics, position, [instruction.AddressSegment]);
                position.X += addressColumnWidth + padding;

                if (instruction.LabelSegment != null)
                    PaintSegments(graphics, position, [(Segment)instruction.LabelSegment]);
                position.X += labelColumnWidth + padding;

                PaintSegments(graphics, position, instruction.InstructionSegments);
                position.X = indent;
                position.Y += lineHeight;
            }
        }

        private int MeasureSegmentWidth(Segment segment)
        {
            var text = DisplaySettings!.Format(segment.Type, segment.Value);
            using var font = DisplaySettings!.GetFont(segment.Type, Font);

            Size measure = TextRenderer.MeasureText(
                text,
                font,
                new Size(int.MaxValue, int.MaxValue),
                TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

            return measure.Width;
        }

        private void PaintSegments(Graphics graphics, Point position, Segment[] segments)
        {
            foreach (var segment in segments)
            {
                var text = DisplaySettings!.Format(segment.Type, segment.Value);
                var color = DisplaySettings!.GetColor(segment.Type);
                using var font = DisplaySettings!.GetFont(segment.Type, Font);

                Size measure = TextRenderer.MeasureText(
                    text,
                    font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                using var brush = new SolidBrush(color);
                graphics.DrawString(text, font!, brush, position);

                position.X += measure.Width;
            }
        }

        private readonly struct Segment
        {
            public Setting Type { get; init; }
            public Object Value { get; init; }
        };

        private readonly struct Instruction
        {
            public Segment AddressSegment { get; init; }
            public Segment? LabelSegment { get; init; }
            public Segment[] InstructionSegments { get; init; }
        }

        private static readonly Instruction[] _Instructions = [
            new()
            {
                AddressSegment = new() { Type = Setting.Address, Value = 0x2A6E },
                LabelSegment = new() { Type = Setting.Label, Value = "loop" },
                InstructionSegments = [
                    new() { Type = Setting.Mnemonic, Value = "LDA" },
                    new() { Type = Setting.Label, Value = "gameObject" },
                    new() { Type = Setting.Punctuation, Value = "(" },
                    new() { Type = Setting.Address, Value = 0x08A0 },
                    new() { Type = Setting.Punctuation, Value = "),X" },
                ]
            },
            new()
            {
                AddressSegment = new Segment { Type = Setting.Address, Value = 0x2A71 },
                InstructionSegments = [
                    new() { Type = Setting.Mnemonic, Value = "AND" },
                    new() { Type = Setting.Punctuation, Value = "#" },
                    new() { Type = Setting.Literal, Value = (byte)192 }
                ]
            },
            new()
            {
                AddressSegment = new Segment { Type = Setting.Address, Value = 0x2A73 },
                InstructionSegments = [
                    new() { Type = Setting.Mnemonic, Value = "BEQ" },
                    new() { Type = Setting.Label, Value = "func" },
                    new() { Type = Setting.Punctuation, Value = "(" },
                    new() { Type = Setting.Address, Value = 0x21B2 },
                    new() { Type = Setting.Punctuation, Value = ")" },
                ]
            }
        ];
    }
}