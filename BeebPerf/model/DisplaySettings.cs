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

using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeebPerf.model
{
    public class DisplaySettings
    {
        public const int ThemeCount = 3;

        public DisplaySettings()
        {
            // default settings
            FontScaling = 100;
            LineSpacing = 120;
            CodeFont = string.Empty;
            LightColorThemeSettings = new()
            {
                ColorTheme = ColorThemeType.Light,
                LabelSettings = new() { Color = Color.Black, Bold = false, Italic = false },
                AddressSettings = new() { Color = Color.DarkCyan, Bold = false, Italic = false, Format = AddressFormat.AndUppercase },
                LiteralSettings = new() { Color = Color.DarkMagenta, Bold = false, Italic = false, Format = LiteralFormat.Hexadecimal },
                MnemonicSettings = new() { Color = Color.Blue, Bold = false, Italic = false, Format = MnemonicFormat.Uppercase },
                PunctuationSettings = new() { Color = Color.Gray, Bold = false, Italic = false }
            };
            DarkColorThemeSettings = new()
            {
                ColorTheme = ColorThemeType.Dark,
                LabelSettings = new() { Color = Color.White, Bold = false, Italic = false },
                AddressSettings = new() { Color = Color.Cyan, Bold = false, Italic = false, Format = AddressFormat.AndUppercase },
                LiteralSettings = new() { Color = Color.Magenta, Bold = false, Italic = false, Format = LiteralFormat.Hexadecimal },
                MnemonicSettings = new() { Color = Color.LightBlue, Bold = false, Italic = false, Format = MnemonicFormat.Uppercase },
                PunctuationSettings = new() { Color = Color.LightGray, Bold = false, Italic = false }
            };
        }

        public DisplaySettings Clone()
        {
            return new()
            {
                FontScaling = FontScaling,
                LineSpacing = LineSpacing,
                CodeFont = CodeFont,
                LightColorThemeSettings = LightColorThemeSettings.Clone(),
                DarkColorThemeSettings = DarkColorThemeSettings.Clone(),
            };
        }

        public ColorThemeSettingsType ColorThemeSettings
        {
            get => ColorTheme.IsLightMode() ? LightColorThemeSettings : DarkColorThemeSettings;
        }

        public Font GetFont(Setting setting, Font baseFont)
        {
            var settings = ColorThemeSettings[setting];
            FontStyle fontStyle = FontStyle.Regular;
            if (settings!.Bold)
                fontStyle |= FontStyle.Bold;
            if (settings!.Italic)
                fontStyle |= FontStyle.Italic;

            string fontName = baseFont.Name;
            if (CodeFont.Length > 0)
                fontName = CodeFont;

            return new Font(fontName, baseFont.SizeInPoints, fontStyle);
        }

        public Color GetColor(Setting setting)
        {
            return ColorThemeSettings[setting]!.Color;
        }

        public string Format(Setting setting, object value)
        {
            switch (setting)
            {
                case Setting.Address:
                    return FormatAddress(value);

                case Setting.Mnemonic:
                    var text = (string)value;
                    text = ColorThemeSettings.MnemonicSettings.Format switch
                    {
                        MnemonicFormat.Uppercase => text.ToUpper(),
                        MnemonicFormat.Lowercase => text.ToLower(),
                        _ => throw new ArgumentOutOfRangeException()
                    };
                    return text.PadRight(5, ' ');

                case Setting.Literal:
                    int integer = Convert.ToInt32(value);
                    return ColorThemeSettings.LiteralSettings.Format switch
                    {
                        LiteralFormat.Hexadecimal => FormatAddress(value),
                        LiteralFormat.Decimal => integer.ToString(),
                        LiteralFormat.Binary => Convert.ToString(integer, 2).PadLeft(8, '0'),
                        _ => throw new ArgumentOutOfRangeException()
                    };

                case Setting.Label:
                case Setting.Punctuation:
                    return (string)value;

                default:
                    return string.Empty;
            }
        }

        private string FormatAddress(Object value)
        {
            int integer = Convert.ToInt32(value);
            bool zeroPage = value is byte;
            return ColorThemeSettings.AddressSettings.Format switch
            {
                AddressFormat.AndUppercase => zeroPage ? $"&{integer:X2}" : $"&{integer:X4}",
                AddressFormat.AndLowercase => zeroPage ? $"&{integer:x2}" : $"&{integer:x4}",
                AddressFormat.DollarUppercase => zeroPage ? $"${integer:X2}" : $"${integer:X4}",
                AddressFormat.DollarLowercase => zeroPage ? $"${integer:x2}" : $"${integer:x4}",
                AddressFormat.OxUppercase => zeroPage ? $"0x{integer:X2}" : $"0x{integer:X4}",
                AddressFormat.OxLowercase => zeroPage ? $"0x{integer:x2}" : $"0x{integer:x4}",
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public enum Setting
        { 
            Label,
            Address,
            Mnemonic,
            Literal,
            Punctuation
        }

        public enum AddressFormat
        {
            AndUppercase = 0,
            AndLowercase = 1,
            DollarUppercase = 2,
            DollarLowercase = 3,
            OxUppercase = 4,
            OxLowercase = 5,
        }

        public enum MnemonicFormat
        {
            Uppercase,
            Lowercase
        }

        public enum LiteralFormat
        {
            Hexadecimal,
            Decimal,
            Binary
        }

        public class CommonSettingsType
        {
            public CommonSettingsType Clone()
            {
                return new CommonSettingsType
                {
                    Color = Color,
                    Bold = Bold,
                    Italic = Italic
                };
            }

            [JsonIgnore]
            public Color Color
            {
                get => Color.FromArgb(_Color);
                set => _Color = value.ToArgb();
            }

            public bool Bold { get; set; }
            public bool Italic { get; set; }
            public int _Color { get; set; }
        }

        public class AddressSettingsType : CommonSettingsType
        {
            public new AddressSettingsType Clone()
            {
                return new AddressSettingsType
                {
                    Format = Format,
                    Color = Color,
                    Bold = Bold,
                    Italic = Italic
                };
            }

            public AddressFormat Format { get; set; }
        }

        public class MnemonicSettingsType : CommonSettingsType
        {
            public new MnemonicSettingsType Clone()
            {
                return new MnemonicSettingsType
                {
                    Format = Format,
                    Color = Color,
                    Bold = Bold,
                    Italic = Italic
                };
            }

            public MnemonicFormat Format { get; set; }
        }

        public class LiteralSettingsType : CommonSettingsType
        {
            public new LiteralSettingsType Clone()
            {
                return new LiteralSettingsType
                {
                    Format = Format,
                    Color = Color,
                    Bold = Bold,
                    Italic = Italic
                };
            }

            public LiteralFormat Format { get; set; }
        }

        public class ColorThemeSettingsType
        {
            public ColorThemeSettingsType Clone()
            {
                return new ColorThemeSettingsType
                {
                    ColorTheme = ColorTheme,
                    MnemonicSettings = MnemonicSettings.Clone(),
                    AddressSettings = AddressSettings.Clone(),
                    LiteralSettings = LiteralSettings.Clone(),
                    PunctuationSettings = PunctuationSettings.Clone(),
                    LabelSettings = LabelSettings.Clone(),
                };
            }

            public CommonSettingsType? this[Setting indexer]
            {
                get
                {
                    return indexer switch
                    {
                        Setting.Label => LabelSettings,
                        Setting.Address => AddressSettings,
                        Setting.Mnemonic => MnemonicSettings,
                        Setting.Literal => LiteralSettings,
                        Setting.Punctuation => PunctuationSettings,
                        _ => throw new ArgumentOutOfRangeException()
                    };
                }
            }

            public required ColorThemeType ColorTheme { get; set; }
            public required MnemonicSettingsType MnemonicSettings { get; set; }
            public required AddressSettingsType AddressSettings { get; set; }
            public required LiteralSettingsType LiteralSettings { get; set; }
            public required CommonSettingsType PunctuationSettings { get; set; }
            public required CommonSettingsType LabelSettings { get; set; }
        }

        static public string Serialize(DisplaySettings settings)
        {
            return JsonSerializer.Serialize(settings);
        }

        static public DisplaySettings? Deserialize(string json)
        {
            try
            {
                return JsonSerializer.Deserialize<DisplaySettings>(json);
            }
            catch
            {
                return new DisplaySettings();
            }
        }

        public int FontScaling { get; set; }
        public int LineSpacing { get; set; }
        public string CodeFont { get; set; }
        public ColorThemeSettingsType LightColorThemeSettings { get; set; }
        public ColorThemeSettingsType DarkColorThemeSettings { get; set; }
    }
}