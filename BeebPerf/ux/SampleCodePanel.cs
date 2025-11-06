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

            var themeSettings = DisplaySettings!.ThemeSettings;
            var color = (themeSettings.ThemeType == ThemeManager.ThemeType.Light) ? Color.White : Color.Black;
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
                labelColumnWidth = Math.Max(labelColumnWidth, MeasureSegmentWidth(instruction.LabelSegment) + padding);
            }

            // paint instructions
            int indent = Font.Height / 2;
            int lineHeight = (int)double.Round((double)Font.Height * DisplaySettings.FontScaling / 100.0);
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

        private int MeasureSegmentWidth(Segment? segment)
        {
            if (segment == null)
                return 0;

            var text = FormatSegmentText(segment.Value);
            using Font font = GetSegmentFont((Segment)segment);

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
                var text = FormatSegmentText(segment);
                using Font font = GetSegmentFont(segment);

                Size measure = TextRenderer.MeasureText(
                    text,
                    font,
                    new Size(int.MaxValue, int.MaxValue),
                    TextFormatFlags.NoPadding | TextFormatFlags.SingleLine | TextFormatFlags.NoPrefix);

                using var brush = new SolidBrush(GetSegmentColor(segment));
                graphics.DrawString(text, font, brush, position);

                position.X += measure.Width;
            }
        }

        private string FormatSegmentText(Segment segment)
        {
            return segment.Type switch 
            { 
                SegmentType.Label => (string)segment.Value,
                SegmentType.Address => FormatHexadecimal((int)segment.Value),
                SegmentType.Literal => FormatLiteral((int)segment.Value),
                SegmentType.Mnemonic => FormatMnemonic((string)segment.Value),
                SegmentType.Punctuation => (string)segment.Value,
                _ => string.Empty
            };
        }

        private Color GetSegmentColor(Segment segment)
        {
            var themeSettings = DisplaySettings!.ThemeSettings;
            return segment.Type switch
            {
                SegmentType.Label => themeSettings.LabelSettings.Color,
                SegmentType.Address => themeSettings.AddressSettings.Color,
                SegmentType.Literal => themeSettings.LiteralSettings.Color,
                SegmentType.Mnemonic => themeSettings.MnemonicSettings.Color,
                SegmentType.Punctuation => themeSettings.PunctuationSettings.Color,
                _ => Color.FromArgb(0),
            };
        }

        private Font GetSegmentFont(Segment segment)
        {
            int fontSize = (int)float.Round(Font.SizeInPoints * DisplaySettings!.FontScaling / 100.0f);

            var themeSettings = DisplaySettings!.ThemeSettings;
            bool bold = false;
            bool italic = false;
        
            switch (segment.Type)
            {
                case SegmentType.Label:
                    bold = themeSettings.LabelSettings.Bold;
                    italic = themeSettings.LabelSettings.Italic;
                    break;

                case SegmentType.Literal:
                    bold = themeSettings.LiteralSettings.Bold;
                    italic = themeSettings.LiteralSettings.Italic;
                    break;

                case SegmentType.Mnemonic:
                    bold = themeSettings.MnemonicSettings.Bold;
                    italic = themeSettings.MnemonicSettings.Italic;
                    break;

                case SegmentType.Address:
                    bold = themeSettings.AddressSettings.Bold;
                    italic = themeSettings.AddressSettings.Italic;
                    break;

                case SegmentType.Punctuation:
                    bold = themeSettings.PunctuationSettings.Bold;
                    italic = themeSettings.PunctuationSettings.Italic;
                    break;

                default:
                    break;
            }

            FontStyle fontStyle = FontStyle.Regular;
            if (bold)
                fontStyle |= FontStyle.Bold;
            if (italic)
                fontStyle |= FontStyle.Italic;

            string fontName = Font.FontFamily.Name;
            if (DisplaySettings.CodeFont.Length > 0)
                fontName = DisplaySettings.CodeFont;

            return new Font(fontName, fontSize, fontStyle);
        }

        private string FormatMnemonic(string value)
        {
            var themeSettings = DisplaySettings!.ThemeSettings;
            value = themeSettings.MnemonicSettings.Format switch
            {
                DisplaySettings.MnemonicFormat.Uppercase => value.ToUpper(),
                DisplaySettings.MnemonicFormat.Lowercase => value.ToLower(),
                _ => value
            };

            return value.PadRight(5, ' ');
        }

        private string FormatHexadecimal(int value)
        {
            var themeSettings = DisplaySettings!.ThemeSettings;
            return themeSettings.AddressSettings.Format switch
            {
                DisplaySettings.AddressFormat.AndUppercase => $"&{value:X2}",
                DisplaySettings.AddressFormat.AndLowercase => $"&{value:x2}",
                DisplaySettings.AddressFormat.DollarUppercase => $"${value:X2}",
                DisplaySettings.AddressFormat.DollarLowercase => $"${value:x2}",
                DisplaySettings.AddressFormat.OxUppercase => $"0x{value:X2}",
                DisplaySettings.AddressFormat.OxLowercase => $"0x{value:x2}",
                _ => string.Empty
            };
        }

        private string FormatLiteral(int value)
        {
            var themeSettings = DisplaySettings!.ThemeSettings;
            return themeSettings.LiteralSettings.Format switch
            {
                DisplaySettings.LiteralFormat.Hexadecimal => FormatHexadecimal(value),
                DisplaySettings.LiteralFormat.Decimal => value.ToString(),
                DisplaySettings.LiteralFormat.Binary => Convert.ToString(value, 2),
                _ => string.Empty
            };
        }

        private enum SegmentType { Label, Address, Mnemonic, Literal, Punctuation, }

        private readonly struct Segment
        {
            public SegmentType Type { get; init; }
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
                AddressSegment = new() { Type = SegmentType.Address, Value = 0x2A6E },
                LabelSegment = new() { Type = SegmentType.Label, Value = "loop" },
                InstructionSegments = [
                    new() { Type = SegmentType.Mnemonic, Value = "LDA" },
                    new() { Type = SegmentType.Label, Value = "gameObject" },
                    new() { Type = SegmentType.Punctuation, Value = "(" },
                    new() { Type = SegmentType.Address, Value = 0x08A0 },
                    new() { Type = SegmentType.Punctuation, Value = "),X" },
                ]
            },
            new()
            {
                AddressSegment = new Segment { Type = SegmentType.Address, Value = 0x2A71 },
                InstructionSegments = [
                    new() { Type = SegmentType.Mnemonic, Value = "AND" },
                    new() { Type = SegmentType.Punctuation, Value = "#" },
                    new() { Type = SegmentType.Literal, Value = 192 }
                ]
            },
            new()
            {
                AddressSegment = new Segment { Type = SegmentType.Address, Value = 0x2A73 },
                InstructionSegments = [
                    new() { Type = SegmentType.Mnemonic, Value = "BEQ" },
                    new() { Type = SegmentType.Label, Value = "func" },
                    new() { Type = SegmentType.Punctuation, Value = "(" },
                    new() { Type = SegmentType.Address, Value = 0x21B2 },
                    new() { Type = SegmentType.Punctuation, Value = ")" },
                ]
            }
        ];
    }
}