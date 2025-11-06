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

using Microsoft.Win32;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BeebPerf.model
{
    public class DisplaySettings
    {
        public const int ThemeCount = 3;

        static public string Serialize(DisplaySettings settings)
        {
            return JsonSerializer.Serialize(settings);
        }

        static public DisplaySettings? Deserialize(string json)
        {
            return JsonSerializer.Deserialize<DisplaySettings>(json);
        }

        public DisplaySettings()
        {
            Theme = ThemeManager.ThemeType.System;
            FontScaling = 100;
            CodeFont = string.Empty;
            LightSettings = new()
            {
                ThemeType = ThemeManager.ThemeType.Light,
                LabelSettings = new() { Color = Color.Black, Bold = false, Italic = false },
                AddressSettings = new() { Color = Color.DarkCyan, Bold = false, Italic = false, Format = AddressFormat.AndUppercase },
                LiteralSettings = new() { Color = Color.DarkMagenta, Bold = false, Italic = false, Format = LiteralFormat.Hexadecimal },
                MnemonicSettings = new() { Color = Color.Blue, Bold = false, Italic = false, Format = MnemonicFormat.Uppercase },
                PunctuationSettings = new() { Color = Color.Gray, Bold = false, Italic = false }
            };
            DarkSettings = new()
            {
                ThemeType = ThemeManager.ThemeType.Dark,
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
                Theme = Theme,
                FontScaling = FontScaling,
                CodeFont = CodeFont,
                LightSettings = LightSettings.Clone(),
                DarkSettings = DarkSettings.Clone(),
            };
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

        public class ThemeSettingsType
        {
            public ThemeSettingsType Clone()
            {
                return new ThemeSettingsType
                {
                    ThemeType = ThemeType,
                    MnemonicSettings = MnemonicSettings.Clone(),
                    AddressSettings = AddressSettings.Clone(),
                    LiteralSettings = LiteralSettings.Clone(),
                    PunctuationSettings = PunctuationSettings.Clone(),
                    LabelSettings = LabelSettings.Clone(),
                };
            }

            public required ThemeManager.ThemeType ThemeType { get; set; }
            public required MnemonicSettingsType MnemonicSettings { get; set; }
            public required AddressSettingsType AddressSettings { get; set; }
            public required LiteralSettingsType LiteralSettings { get; set; }
            public required CommonSettingsType PunctuationSettings { get; set; }
            public required CommonSettingsType LabelSettings { get; set; }
        }

        public ThemeSettingsType ThemeSettings
        {
            get
            {
                if (Theme == ThemeManager.ThemeType.System)
                    return IsLightSystemTheme() ? LightSettings : DarkSettings;
                else 
                    return (Theme == ThemeManager.ThemeType.Light) ? LightSettings : DarkSettings;
            }
        }

        private bool IsLightSystemTheme()
        {
            const string keyPath = @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize";
            using var key = Registry.CurrentUser.OpenSubKey(keyPath);
            var value = key?.GetValue("AppsUseLightTheme");
            return (value is int intValue && intValue == 1);
        }

        public ThemeManager.ThemeType Theme { get; set; }
        public int FontScaling { get; set; }
        public string CodeFont { get; set; }
        public ThemeSettingsType LightSettings { get; set; }
        public ThemeSettingsType DarkSettings { get; set; }
    }
}