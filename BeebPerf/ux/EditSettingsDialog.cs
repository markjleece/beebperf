// --------------------------------------------------------------
// An Adventure In Time - A Doctor Who fan game for the BBC Micro
// Model B
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
using System.Drawing.Text;
using System.Text.RegularExpressions;

namespace BeebPerf.ux
{
    public partial class EditSettingsDialog : Form
    {
        public DisplaySettings? Settings;

        internal EditSettingsDialog(DisplaySettings settings, Font baseFont)
        {
            _SuppressChangeEvents++;

            _BaseFont = baseFont;
            _Settings = settings.Clone();

            InitializeComponent();

            // initialize controls
            sampleCodePanel.DisplaySettings = _Settings;

            List<string> codeFontNames = GetCodeFontNames();
            codeFontNames.Insert(0, "Default");
            SetComboBoxOptions(codeFontComboBox, codeFontNames.ToArray());
            if (settings.CodeFont.Length > 0)
                codeFontComboBox.SelectedItem = settings.CodeFont;
            else
                codeFontComboBox.SelectedIndex = 0;

            SetComboBoxOptions(textScalingComboBox, ["50%", "60%", "70%", "80%", "90%", "100%", "110%", "120%", "130%", "140%", "150%"]);
            textScalingComboBox.SelectedItem = settings.FontScaling.ToString() + '%';

            SetComboBoxOptions(lineSpacingComboBox, ["80%", "90%", "100%", "110%", "120%", "130%", "140%"]);
            lineSpacingComboBox.SelectedItem = settings.LineSpacing.ToString() + '%';

            SetComboBoxOptions(addressFormatComboBox, ["&A42B", "&a42b", "$A42B", "$a42b", "0xA42B", "0xa42b"]);
            SetComboBoxOptions(mnemonicFormatComboBox, ["Uppercase", "Lowercase"]);
            SetComboBoxOptions(literalFormatComboBox, ["Hexadecimal", "Decimal", "Binary"]);

            SetComboBoxOptions(colorThemeComboBox, ["System", "Dark", "Light"]);
            _SuppressChangeEvents--;

            colorThemeComboBox.SelectedIndex = (int)ColorTheme.Get();
            colorThemeComboBox.Enabled = ColorTheme.CanSet();

            sampleCodePanel.Invalidate();

            ApplyFontScaling(this, settings.FontScaling);
        }

        private void ApplyFontScaling(Control control, int fontScaling)
        {
            int fontSize = (int)float.Round(_BaseFont.SizeInPoints * fontScaling / 100.0f);
            Font font = new Font(_BaseFont.Name, fontSize, FontStyle.Regular);
            ApplyFontToAllControls(control, font);
        }

        private void ApplyFontToAllControls(Control control, Font font)
        {
            if (control.Font.Style != font.Style)
                control.Font = new Font(font, control.Font.Style);
            else
                control.Font = font;

            foreach (Control child in control.Controls)
                ApplyFontToAllControls(child, font);
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            Settings = _Settings;
        }

        private void TextScalingComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var value = (string)textScalingComboBox.SelectedItem!;
            var match = Regex.Match(value, @"\d+");
            var textScaling = match.Success ? int.Parse(match.Value) : 100;
            _Settings.FontScaling = textScaling;

            ApplyFontScaling(sampleCodePanel, textScaling);
            sampleCodePanel.Invalidate();
        }

        private void LineSpacingComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var value = (string)lineSpacingComboBox.SelectedItem!;
            var match = Regex.Match(value, @"\d+");
            var lineSpacing = match.Success ? int.Parse(match.Value) : 100;
            _Settings.LineSpacing = lineSpacing;
            sampleCodePanel.Invalidate();
        }
        

        private void CodeFontComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            if (codeFontComboBox.SelectedIndex == 0)
                _Settings.CodeFont = string.Empty;
            else
                _Settings.CodeFont = (string)codeFontComboBox.SelectedItem!;
           
            sampleCodePanel.Invalidate();
        }

        private void ColorThemeComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            ColorTheme.Set(this, (ColorThemeType)colorThemeComboBox.SelectedIndex);
            var themeSettings = _Settings.ColorThemeSettings;

            _SuppressChangeEvents++;

            addressFormatComboBox.SelectedIndex = (int)themeSettings.AddressSettings.Format;
            addressColorButton.BackColor = themeSettings.AddressSettings.Color;
            addressBoldCheckBox.Checked = themeSettings.AddressSettings.Bold;
            addressItalicCheckBox.Checked = themeSettings.AddressSettings.Italic;

            mnemonicFormatComboBox.SelectedIndex = (int)themeSettings.MnemonicSettings.Format;
            mnemonicColorButton.BackColor = themeSettings.MnemonicSettings.Color;
            mnemonicBoldCheckBox.Checked = themeSettings.MnemonicSettings.Bold;
            mnemonicItalicCheckBox.Checked = themeSettings.MnemonicSettings.Italic;

            literalFormatComboBox.SelectedIndex = (int)themeSettings.LiteralSettings.Format;
            literalColorButton.BackColor = themeSettings.LiteralSettings.Color;
            literalBoldCheckBox.Checked = themeSettings.LiteralSettings.Bold;
            literalItalicCheckBox.Checked = themeSettings.LiteralSettings.Italic;

            labelColorButton.BackColor = themeSettings.LabelSettings.Color;
            labelBoldCheckBox.Checked = themeSettings.LabelSettings.Bold;
            labelItalicCheckBox.Checked = themeSettings.LabelSettings.Italic;

            punctuationColorButton.BackColor = themeSettings.PunctuationSettings.Color;
            punctuationBoldCheckBox.Checked = themeSettings.PunctuationSettings.Bold;
            punctuationItalicCheckBox.Checked = themeSettings.PunctuationSettings.Italic;

            sampleCodePanel.Invalidate();

            _SuppressChangeEvents--;
        }

        private void AddressFormatComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var comboBox = (ComboBox)sender!;
            var themeSettings = _Settings.ColorThemeSettings;
            themeSettings.AddressSettings.Format = (DisplaySettings.AddressFormat)comboBox.SelectedIndex;

            sampleCodePanel.Invalidate();
        }

        private void MnemonicFormatComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var comboBox = (ComboBox)sender!;
            var themeSettings = _Settings.ColorThemeSettings;
            themeSettings.MnemonicSettings.Format = (DisplaySettings.MnemonicFormat)comboBox.SelectedIndex;

            sampleCodePanel.Invalidate();
        }

        private void LiteralFormatComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var comboBox = (ComboBox)sender!;
            var themeSettings = _Settings.ColorThemeSettings;
            themeSettings.LiteralSettings.Format = (DisplaySettings.LiteralFormat)comboBox.SelectedIndex;

            sampleCodePanel.Invalidate();
        }

        private void ColorButton_Click(object sender, EventArgs e)
        {
            var button = (Button)sender;
            using (ColorDialog colorDialog = new ColorDialog())
            {
                colorDialog.AllowFullOpen = true;
                colorDialog.ShowHelp = false;
                colorDialog.Color = button.BackColor;

                if (colorDialog.ShowDialog() == DialogResult.OK)
                {
                    button.BackColor = colorDialog.Color;

                    var color = colorDialog.Color;
                    var themeSettings = _Settings.ColorThemeSettings;
                    if (button == addressColorButton)
                        themeSettings.AddressSettings.Color = color;
                    else if (button == mnemonicColorButton)
                        themeSettings.MnemonicSettings.Color = color;
                    else if (button == literalColorButton)
                        themeSettings.LiteralSettings.Color = color;
                    else if (button == labelColorButton)
                        themeSettings.LabelSettings.Color = color;
                    else if (button == punctuationColorButton)
                        themeSettings.PunctuationSettings.Color = color;

                    sampleCodePanel.Invalidate();
                }
            }
        }

        private void BoldCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var checkBox = (CheckBox)sender;
            bool isChecked = checkBox.Checked;
            var themeSettings = _Settings.ColorThemeSettings;
            if (checkBox == addressBoldCheckBox)
                themeSettings.AddressSettings.Bold = isChecked;
            else if (checkBox == mnemonicBoldCheckBox)
                themeSettings.MnemonicSettings.Bold = isChecked;
            else if (checkBox == literalBoldCheckBox)
                themeSettings.LiteralSettings.Bold = isChecked;
            else if (checkBox == labelBoldCheckBox)
                themeSettings.LabelSettings.Bold = isChecked;
            else if (checkBox == punctuationBoldCheckBox)
                themeSettings.PunctuationSettings.Bold = isChecked;

            sampleCodePanel.Invalidate();
        }

        private void ItalicCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_SuppressChangeEvents > 0)
                return;

            var checkBox = (CheckBox)sender;
            bool isChecked = checkBox.Checked;
            var themeSettings = _Settings.ColorThemeSettings;
            if (checkBox == addressItalicCheckBox)
                themeSettings.AddressSettings.Italic = isChecked;
            else if (checkBox == mnemonicItalicCheckBox)
                themeSettings.MnemonicSettings.Italic = isChecked;
            else if (checkBox == literalItalicCheckBox)
                themeSettings.LiteralSettings.Italic = isChecked;
            else if (checkBox == labelItalicCheckBox)
                themeSettings.LabelSettings.Italic = isChecked;
            else if (checkBox == punctuationItalicCheckBox)
                themeSettings.PunctuationSettings.Italic = isChecked;

            sampleCodePanel.Invalidate();
        }

        private void SetComboBoxOptions(ComboBox comboBox, object[] options)
        {
            comboBox.Items.Clear();
            foreach (var option in options)
                comboBox.Items.Add(option);

            sampleCodePanel.Invalidate();
        }

        public static List<string> GetCodeFontNames()
        {
            List<string> codeFontNames = new();

            InstalledFontCollection fontCollection = new InstalledFontCollection();
            FontFamily[] families = fontCollection.Families;

            HashSet<string> knownCodeFonts = [
                "consolas", "courier new", "cascadia code", "fira code", "source code pro",
                "lucida console", "monaco", "inconsolata", "ibm plex mono", "jetbrains mono",
                "dejavu sans mono", "andale mono", "ocr a extended", "anonymous pro" ];

            for (int i = 0; i < families.Length; i++)
            {
                FontFamily family = families[i];
                if (!family.IsStyleAvailable(FontStyle.Regular))
                    continue;

                if (knownCodeFonts.Contains(family.Name.ToLower()))
                    codeFontNames.Add(family.Name);
            }

            return codeFontNames;
        }

        private void CodeFontComboBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) 
                return;

            var comboBox = (ComboBox)sender;
            string fontName = comboBox.Items[e.Index]!.ToString()!;
            Font font = comboBox.Font;
            if (e.Index > 0)
            {
                try
                {
                    font = new Font(fontName, comboBox.Font.Size);
                }
                catch {}
            }

            using Brush textBrush = new SolidBrush(e.ForeColor);
            StringFormat textFormat = new StringFormat
            {
                Alignment = StringAlignment.Near,
                LineAlignment = StringAlignment.Center,
                FormatFlags = StringFormatFlags.NoWrap
            };

            e.DrawBackground();
            e.Graphics.DrawString(fontName, font, textBrush, e.Bounds, textFormat);
            e.DrawFocusRectangle();
        }

        private DisplaySettings _Settings;
        private int _SuppressChangeEvents;
        private Font _BaseFont;
    }
}
