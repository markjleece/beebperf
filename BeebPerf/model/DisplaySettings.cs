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


using System.Text;

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

        public string FormatAddress(Object value, bool withPrefix = true)
        {
            int integer = Convert.ToInt32(value);
            bool zeroPage = value is byte;

            string text = string.Empty;
            switch (ColorThemeSettings.AddressSettings.Format)
            {
                case AddressFormat.AndUppercase:
                case AddressFormat.DollarUppercase:
                case AddressFormat.OxUppercase:
                    text = zeroPage ? $"{integer:X2}" : $"{integer:X4}";
                    break;

                case AddressFormat.AndLowercase:
                case AddressFormat.DollarLowercase:
                case AddressFormat.OxLowercase:
                    text = zeroPage ? $"{integer:x2}" : $"{integer:x4}";
                    break;
            }
            
            if (withPrefix)
                text = GetAddressPrefix() + text;
            
            return text;
        }

        public string GetAddressPrefix()
        {
            switch (ColorThemeSettings.AddressSettings.Format)
            {
                case AddressFormat.AndUppercase:
                case AddressFormat.AndLowercase:
                    return "&";

                case AddressFormat.DollarUppercase:
                case AddressFormat.DollarLowercase:
                    return "$";

                case AddressFormat.OxUppercase:
                case AddressFormat.OxLowercase:
                    return "0x";
            }
            return string.Empty;
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

            public Color Color
            {
                get => Color.FromArgb(_Color);
                set => _Color = value.ToArgb();
            }

            public required bool Bold { get; set; }
            public required bool Italic { get; set; }
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

            public required AddressFormat Format { get; set; }
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

            public required MnemonicFormat Format { get; set; }
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

            public required LiteralFormat Format { get; set; }
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

        // 
        // serialization...
        //
        static private void Serialize(List<string> stream, string value)
        {
            stream.Add(value);
        }

        static private void Serialize(List<string> stream, int value)
        {
            stream.Add(value.ToString());
        }

        static private void Serialize(List<string> stream, bool value)
        {
            stream.Add(value ? "1" : "0");
        }

        static private void Serialize(List<string> stream, CommonSettingsType value)
        {
            Serialize(stream, value.Bold);
            Serialize(stream, value.Italic);
            Serialize(stream, value._Color);
        }

        static private void Serialize(List<string> stream, AddressSettingsType value)
        {
            Serialize(stream, (CommonSettingsType)value);
            Serialize(stream, (int)value.Format);
        }

        static private void Serialize(List<string> stream, MnemonicSettingsType value)
        {
            Serialize(stream, (CommonSettingsType)value);
            Serialize(stream, (int)value.Format);
        }

        static private void Serialize(List<string> stream, LiteralSettingsType value)
        {
            Serialize(stream, (CommonSettingsType)value);
            Serialize(stream, (int)value.Format);
        }

        static private void Serialize(List<string> stream, ColorThemeSettingsType value)
        {
            Serialize(stream, (int)value.ColorTheme);
            Serialize(stream, value.MnemonicSettings);
            Serialize(stream, value.AddressSettings);
            Serialize(stream, value.LiteralSettings);
            Serialize(stream, value.PunctuationSettings);
            Serialize(stream, value.LabelSettings);
        }

        static public string Serialize(DisplaySettings settings)
        {
            var stream = new List<string>();
            Serialize(stream, settings.FontScaling);
            Serialize(stream, settings.LineSpacing);
            Serialize(stream, settings.CodeFont);
            Serialize(stream, settings.LightColorThemeSettings);
            Serialize(stream, settings.DarkColorThemeSettings);

            var sb = new StringBuilder();
            foreach (var element in stream)
            {
                sb.Append(element);
                sb.Append(',');
            }
            return sb.ToString(); 
        }

        // 
        // deserialization...
        //
        static private string DeserializeString(Queue<string> stream)
        {
            return stream.Dequeue();
        }

        static private int DeserializeInt(Queue<string> stream)
        {
            if (int.TryParse(stream.Dequeue(), out var value))
                return value;
            throw new Exception("display setting deserialization error");
        }

        static private bool DeserializeBool(Queue<string> stream)
        {
            var value = stream.Dequeue();
            if (value == "1")
                return true;
            else if (value == "0")
                return false;
            else
                throw new Exception("display setting deserialization error");
        }

        static private CommonSettingsType DeserializeCommonSettings(Queue<string> stream)
        {
            return new CommonSettingsType()
            {
                Bold = DeserializeBool(stream),
                Italic = DeserializeBool(stream),
                _Color = DeserializeInt(stream)
            };
        }

        static private MnemonicSettingsType DeserializeMnemonicSettings(Queue<string> stream)
        {
            return new MnemonicSettingsType()
            {
                Bold = DeserializeBool(stream),
                Italic = DeserializeBool(stream),
                _Color = DeserializeInt(stream),
                Format = (MnemonicFormat)DeserializeInt(stream)
            };
        }

        static private AddressSettingsType DeserializeAddressSettings(Queue<string> stream)
        {
            return new AddressSettingsType()
            {
                Bold = DeserializeBool(stream),
                Italic = DeserializeBool(stream),
                _Color = DeserializeInt(stream),
                Format = (AddressFormat)DeserializeInt(stream)
            };
        }

        static private LiteralSettingsType DeserializeLiteralSettings(Queue<string> stream)
        {
            return new LiteralSettingsType()
            {
                Bold = DeserializeBool(stream),
                Italic = DeserializeBool(stream),
                _Color = DeserializeInt(stream),
                Format = (LiteralFormat)DeserializeInt(stream)
            };
        }

        static private ColorThemeSettingsType DeserializeColorThemeSettings(Queue<string> stream)
        {
            return new ColorThemeSettingsType()
            {
                ColorTheme = (ColorThemeType)DeserializeInt(stream),
                MnemonicSettings = DeserializeMnemonicSettings(stream),
                AddressSettings = DeserializeAddressSettings(stream),
                LiteralSettings = DeserializeLiteralSettings(stream),
                PunctuationSettings = DeserializeCommonSettings(stream),
                LabelSettings = DeserializeCommonSettings(stream),
            };
        }

        static public DisplaySettings Deserialize(string encoding)
        {
            var stream = new Queue<string>(encoding.Split(','));
            return new DisplaySettings()
            {
                FontScaling = DeserializeInt(stream),
                LineSpacing = DeserializeInt(stream),
                CodeFont = DeserializeString(stream),
                LightColorThemeSettings = DeserializeColorThemeSettings(stream),
                DarkColorThemeSettings = DeserializeColorThemeSettings(stream)
            };
        }

        public int FontScaling { get; set; }
        public int LineSpacing { get; set; }
        public string CodeFont { get; set; }
        public ColorThemeSettingsType LightColorThemeSettings { get; set; }
        public ColorThemeSettingsType DarkColorThemeSettings { get; set; }
    }
}