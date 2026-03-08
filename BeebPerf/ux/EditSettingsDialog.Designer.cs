// --------------------------------------------------------------
// An Adventure In Time - A Doctor Who fan game for the BBC Micro
// Model B
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
    partial class EditSettingsDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            generalGroupBox = new GroupBox();
            colorThemeComboBox = new ComboBox();
            textScalingComboBox = new ComboBox();
            textScalingLabel = new Label();
            lineSpacingLabel = new Label();
            lineSpacingComboBox = new ComboBox();
            colorThemeLabel = new Label();
            codeFontComboBox = new ComboBox();
            codeFontLabel = new Label();
            addressGroupBox = new GroupBox();
            addressColorButton = new Button();
            addressColorLabel = new Label();
            addressItalicCheckBox = new CheckBox();
            addressBoldCheckBox = new CheckBox();
            addressFormatLabel = new Label();
            addressFormatComboBox = new ComboBox();
            mnemonicGroupBox = new GroupBox();
            mnemonicColorButton = new Button();
            mnemonicFormatComboBox = new ComboBox();
            mnemonicColorLabel = new Label();
            mnemonicItalicCheckBox = new CheckBox();
            mnemonicFormatLabel = new Label();
            mnemonicBoldCheckBox = new CheckBox();
            labelGroupBox = new GroupBox();
            labelColorButton = new Button();
            labelItalicCheckBox = new CheckBox();
            labelColorLabel = new Label();
            labelBoldCheckBox = new CheckBox();
            literalColorLabel = new Label();
            literalGroupBox = new GroupBox();
            literalColorButton = new Button();
            literalFormatComboBox = new ComboBox();
            literalItalicCheckBox = new CheckBox();
            literalFormatLabel = new Label();
            literalBoldCheckBox = new CheckBox();
            punctuationGroupBox = new GroupBox();
            punctuationColorButton = new Button();
            punctuationItalicCheckBox = new CheckBox();
            punctuationColorLabel = new Label();
            punctuationBoldCheckBox = new CheckBox();
            sampleCodePanel = new SampleCodePanel();
            okButton = new Button();
            cancelButton = new Button();
            generalGroupBox.SuspendLayout();
            addressGroupBox.SuspendLayout();
            mnemonicGroupBox.SuspendLayout();
            labelGroupBox.SuspendLayout();
            literalGroupBox.SuspendLayout();
            punctuationGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // generalGroupBox
            // 
            generalGroupBox.Controls.Add(lineSpacingComboBox);
            generalGroupBox.Controls.Add(lineSpacingLabel);
            generalGroupBox.Controls.Add(colorThemeComboBox);
            generalGroupBox.Controls.Add(textScalingComboBox);
            generalGroupBox.Controls.Add(textScalingLabel);
            generalGroupBox.Controls.Add(colorThemeLabel);
            generalGroupBox.Controls.Add(codeFontComboBox);
            generalGroupBox.Controls.Add(codeFontLabel);
            generalGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            generalGroupBox.Location = new Point(12, 12);
            generalGroupBox.Name = "generalGroupBox";
            generalGroupBox.Size = new Size(592, 187);
            generalGroupBox.TabIndex = 1;
            generalGroupBox.TabStop = false;
            generalGroupBox.Text = "General";
            // 
            // colorThemeComboBox
            // 
            colorThemeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            colorThemeComboBox.Font = new Font("Segoe UI", 9F);
            colorThemeComboBox.FormattingEnabled = true;
            colorThemeComboBox.Location = new Point(152, 63);
            colorThemeComboBox.Name = "colorThemeComboBox";
            colorThemeComboBox.Size = new Size(182, 33);
            colorThemeComboBox.TabIndex = 4;
            colorThemeComboBox.SelectedIndexChanged += ColorThemeComboBox_SelectedIndexChanged;
            // 
            // textScalingLabel
            // 
            textScalingLabel.AutoSize = true;
            textScalingLabel.Font = new Font("Segoe UI", 9F);
            textScalingLabel.Location = new Point(6, 104);
            textScalingLabel.Name = "textScalingLabel";
            textScalingLabel.Size = new Size(101, 25);
            textScalingLabel.TabIndex = 1;
            textScalingLabel.Text = "Text scaling";
            // 
            // textScalingComboBox
            // 
            textScalingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            textScalingComboBox.Font = new Font("Segoe UI", 9F);
            textScalingComboBox.FormattingEnabled = true;
            textScalingComboBox.Location = new Point(152, 102);
            textScalingComboBox.Name = "textScalingComboBox";
            textScalingComboBox.Size = new Size(182, 33);
            textScalingComboBox.TabIndex = 3;
            textScalingComboBox.SelectedIndexChanged += TextScalingComboBox_SelectedIndexChanged;
            // 
            // lineSpacingLabel
            // 
            lineSpacingLabel.AutoSize = true;
            lineSpacingLabel.Font = new Font("Segoe UI", 9F);
            lineSpacingLabel.Location = new Point(6, 144);
            lineSpacingLabel.Name = "lineSpacingLabel";
            lineSpacingLabel.Size = new Size(109, 25);
            lineSpacingLabel.TabIndex = 5;
            lineSpacingLabel.Text = "Line spacing";
            // 
            // lineSpacingComboBox
            // 
            lineSpacingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            lineSpacingComboBox.Font = new Font("Segoe UI", 9F);
            lineSpacingComboBox.FormattingEnabled = true;
            lineSpacingComboBox.Location = new Point(152, 141);
            lineSpacingComboBox.Name = "lineSpacingComboBox";
            lineSpacingComboBox.Size = new Size(182, 33);
            lineSpacingComboBox.TabIndex = 6;
            lineSpacingComboBox.SelectedIndexChanged += LineSpacingComboBox_SelectedIndexChanged;
            // 
            // colorThemeLabel
            // 
            colorThemeLabel.AutoSize = true;
            colorThemeLabel.Font = new Font("Segoe UI", 9F);
            colorThemeLabel.Location = new Point(6, 66);
            colorThemeLabel.Name = "colorThemeLabel";
            colorThemeLabel.Size = new Size(110, 25);
            colorThemeLabel.TabIndex = 0;
            colorThemeLabel.Text = "Color theme";
            // 
            // codeFontComboBox
            // 
            codeFontComboBox.DrawMode = DrawMode.OwnerDrawFixed;
            codeFontComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            codeFontComboBox.Font = new Font("Segoe UI", 9F);
            codeFontComboBox.FormattingEnabled = true;
            codeFontComboBox.Location = new Point(152, 24);
            codeFontComboBox.Name = "codeFontComboBox";
            codeFontComboBox.Size = new Size(322, 32);
            codeFontComboBox.TabIndex = 2;
            codeFontComboBox.DrawItem += CodeFontComboBox_DrawItem;
            codeFontComboBox.SelectedIndexChanged += CodeFontComboBox_SelectedIndexChanged;
            // 
            // codeFontLabel
            // 
            codeFontLabel.AutoSize = true;
            codeFontLabel.Font = new Font("Segoe UI", 9F);
            codeFontLabel.Location = new Point(6, 27);
            codeFontLabel.Name = "codeFontLabel";
            codeFontLabel.Size = new Size(92, 25);
            codeFontLabel.TabIndex = 1;
            codeFontLabel.Text = "Code font";
            // 
            // addressGroupBox
            // 
            addressGroupBox.Controls.Add(addressColorButton);
            addressGroupBox.Controls.Add(addressColorLabel);
            addressGroupBox.Controls.Add(addressItalicCheckBox);
            addressGroupBox.Controls.Add(addressBoldCheckBox);
            addressGroupBox.Controls.Add(addressFormatLabel);
            addressGroupBox.Controls.Add(addressFormatComboBox);
            addressGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            addressGroupBox.Location = new Point(12, 205);
            addressGroupBox.Name = "addressGroupBox";
            addressGroupBox.Size = new Size(592, 115);
            addressGroupBox.TabIndex = 2;
            addressGroupBox.TabStop = false;
            addressGroupBox.Text = "Addresses";
            // 
            // addressColorButton
            // 
            addressColorButton.Location = new Point(152, 66);
            addressColorButton.Name = "addressColorButton";
            addressColorButton.Size = new Size(34, 34);
            addressColorButton.TabIndex = 15;
            addressColorButton.UseVisualStyleBackColor = true;
            addressColorButton.Click += ColorButton_Click;
            // 
            // addressColorLabel
            // 
            addressColorLabel.AutoSize = true;
            addressColorLabel.Font = new Font("Segoe UI", 9F);
            addressColorLabel.Location = new Point(6, 71);
            addressColorLabel.Name = "addressColorLabel";
            addressColorLabel.Size = new Size(55, 25);
            addressColorLabel.TabIndex = 8;
            addressColorLabel.Text = "Color";
            // 
            // addressItalicCheckBox
            // 
            addressItalicCheckBox.AutoSize = true;
            addressItalicCheckBox.Font = new Font("Segoe UI", 9F);
            addressItalicCheckBox.Location = new Point(400, 64);
            addressItalicCheckBox.Name = "addressItalicCheckBox";
            addressItalicCheckBox.Size = new Size(74, 29);
            addressItalicCheckBox.TabIndex = 7;
            addressItalicCheckBox.Text = "Italic";
            addressItalicCheckBox.UseVisualStyleBackColor = true;
            addressItalicCheckBox.CheckedChanged += ItalicCheckBox_CheckedChanged;
            // 
            // addressBoldCheckBox
            // 
            addressBoldCheckBox.AutoSize = true;
            addressBoldCheckBox.Font = new Font("Segoe UI", 9F);
            addressBoldCheckBox.Location = new Point(400, 29);
            addressBoldCheckBox.Name = "addressBoldCheckBox";
            addressBoldCheckBox.Size = new Size(74, 29);
            addressBoldCheckBox.TabIndex = 6;
            addressBoldCheckBox.Text = "Bold";
            addressBoldCheckBox.UseVisualStyleBackColor = true;
            addressBoldCheckBox.CheckedChanged += BoldCheckBox_CheckedChanged;
            // 
            // addressFormatLabel
            // 
            addressFormatLabel.AutoSize = true;
            addressFormatLabel.Font = new Font("Segoe UI", 9F);
            addressFormatLabel.Location = new Point(6, 27);
            addressFormatLabel.Name = "addressFormatLabel";
            addressFormatLabel.Size = new Size(69, 25);
            addressFormatLabel.TabIndex = 5;
            addressFormatLabel.Text = "Format";
            // 
            // addressFormatComboBox
            // 
            addressFormatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            addressFormatComboBox.Font = new Font("Segoe UI", 9F);
            addressFormatComboBox.FormattingEnabled = true;
            addressFormatComboBox.Location = new Point(152, 27);
            addressFormatComboBox.Name = "addressFormatComboBox";
            addressFormatComboBox.Size = new Size(182, 33);
            addressFormatComboBox.TabIndex = 4;
            addressFormatComboBox.SelectedIndexChanged += AddressFormatComboBox_SelectedIndexChanged;
            // 
            // mnemonicGroupBox
            // 
            mnemonicGroupBox.Controls.Add(mnemonicColorButton);
            mnemonicGroupBox.Controls.Add(mnemonicFormatComboBox);
            mnemonicGroupBox.Controls.Add(mnemonicColorLabel);
            mnemonicGroupBox.Controls.Add(mnemonicItalicCheckBox);
            mnemonicGroupBox.Controls.Add(mnemonicFormatLabel);
            mnemonicGroupBox.Controls.Add(mnemonicBoldCheckBox);
            mnemonicGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            mnemonicGroupBox.Location = new Point(12, 326);
            mnemonicGroupBox.Name = "mnemonicGroupBox";
            mnemonicGroupBox.Size = new Size(592, 112);
            mnemonicGroupBox.TabIndex = 3;
            mnemonicGroupBox.TabStop = false;
            mnemonicGroupBox.Text = "Mnemonics";
            // 
            // mnemonicColorButton
            // 
            mnemonicColorButton.Location = new Point(152, 66);
            mnemonicColorButton.Name = "mnemonicColorButton";
            mnemonicColorButton.Size = new Size(34, 34);
            mnemonicColorButton.TabIndex = 14;
            mnemonicColorButton.UseVisualStyleBackColor = true;
            mnemonicColorButton.Click += ColorButton_Click;
            // 
            // mnemonicFormatComboBox
            // 
            mnemonicFormatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            mnemonicFormatComboBox.Font = new Font("Segoe UI", 9F);
            mnemonicFormatComboBox.FormattingEnabled = true;
            mnemonicFormatComboBox.Location = new Point(152, 27);
            mnemonicFormatComboBox.Name = "mnemonicFormatComboBox";
            mnemonicFormatComboBox.Size = new Size(182, 33);
            mnemonicFormatComboBox.TabIndex = 10;
            mnemonicFormatComboBox.SelectedIndexChanged += MnemonicFormatComboBox_SelectedIndexChanged;
            // 
            // mnemonicColorLabel
            // 
            mnemonicColorLabel.AutoSize = true;
            mnemonicColorLabel.Font = new Font("Segoe UI", 9F);
            mnemonicColorLabel.Location = new Point(20, 71);
            mnemonicColorLabel.Name = "mnemonicColorLabel";
            mnemonicColorLabel.Size = new Size(55, 25);
            mnemonicColorLabel.TabIndex = 10;
            mnemonicColorLabel.Text = "Color";
            // 
            // mnemonicItalicCheckBox
            // 
            mnemonicItalicCheckBox.AutoSize = true;
            mnemonicItalicCheckBox.Font = new Font("Segoe UI", 9F);
            mnemonicItalicCheckBox.Location = new Point(400, 64);
            mnemonicItalicCheckBox.Name = "mnemonicItalicCheckBox";
            mnemonicItalicCheckBox.Size = new Size(74, 29);
            mnemonicItalicCheckBox.TabIndex = 13;
            mnemonicItalicCheckBox.Text = "Italic";
            mnemonicItalicCheckBox.UseVisualStyleBackColor = true;
            mnemonicItalicCheckBox.CheckedChanged += ItalicCheckBox_CheckedChanged;
            // 
            // mnemonicFormatLabel
            // 
            mnemonicFormatLabel.AutoSize = true;
            mnemonicFormatLabel.Font = new Font("Segoe UI", 9F);
            mnemonicFormatLabel.Location = new Point(6, 27);
            mnemonicFormatLabel.Name = "mnemonicFormatLabel";
            mnemonicFormatLabel.Size = new Size(69, 25);
            mnemonicFormatLabel.TabIndex = 11;
            mnemonicFormatLabel.Text = "Format";
            // 
            // mnemonicBoldCheckBox
            // 
            mnemonicBoldCheckBox.AutoSize = true;
            mnemonicBoldCheckBox.Font = new Font("Segoe UI", 9F);
            mnemonicBoldCheckBox.Location = new Point(400, 29);
            mnemonicBoldCheckBox.Name = "mnemonicBoldCheckBox";
            mnemonicBoldCheckBox.Size = new Size(74, 29);
            mnemonicBoldCheckBox.TabIndex = 12;
            mnemonicBoldCheckBox.Text = "Bold";
            mnemonicBoldCheckBox.UseVisualStyleBackColor = true;
            mnemonicBoldCheckBox.CheckedChanged += BoldCheckBox_CheckedChanged;
            // 
            // labelGroupBox
            // 
            labelGroupBox.Controls.Add(labelColorButton);
            labelGroupBox.Controls.Add(labelItalicCheckBox);
            labelGroupBox.Controls.Add(labelColorLabel);
            labelGroupBox.Controls.Add(labelBoldCheckBox);
            labelGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            labelGroupBox.Location = new Point(12, 444);
            labelGroupBox.Name = "labelGroupBox";
            labelGroupBox.Size = new Size(592, 103);
            labelGroupBox.TabIndex = 16;
            labelGroupBox.TabStop = false;
            labelGroupBox.Text = "Labels";
            // 
            // labelColorButton
            // 
            labelColorButton.Location = new Point(152, 22);
            labelColorButton.Name = "labelColorButton";
            labelColorButton.Size = new Size(34, 34);
            labelColorButton.TabIndex = 15;
            labelColorButton.UseVisualStyleBackColor = true;
            labelColorButton.Click += ColorButton_Click;
            // 
            // labelItalicCheckBox
            // 
            labelItalicCheckBox.AutoSize = true;
            labelItalicCheckBox.Font = new Font("Segoe UI", 9F);
            labelItalicCheckBox.Location = new Point(400, 61);
            labelItalicCheckBox.Name = "labelItalicCheckBox";
            labelItalicCheckBox.Size = new Size(74, 29);
            labelItalicCheckBox.TabIndex = 13;
            labelItalicCheckBox.Text = "Italic";
            labelItalicCheckBox.UseVisualStyleBackColor = true;
            labelItalicCheckBox.CheckedChanged += ItalicCheckBox_CheckedChanged;
            // 
            // labelColorLabel
            // 
            labelColorLabel.AutoSize = true;
            labelColorLabel.Font = new Font("Segoe UI", 9F);
            labelColorLabel.Location = new Point(6, 27);
            labelColorLabel.Name = "labelColorLabel";
            labelColorLabel.Size = new Size(55, 25);
            labelColorLabel.TabIndex = 14;
            labelColorLabel.Text = "Color";
            // 
            // labelBoldCheckBox
            // 
            labelBoldCheckBox.AutoSize = true;
            labelBoldCheckBox.Font = new Font("Segoe UI", 9F);
            labelBoldCheckBox.Location = new Point(400, 26);
            labelBoldCheckBox.Name = "labelBoldCheckBox";
            labelBoldCheckBox.Size = new Size(74, 29);
            labelBoldCheckBox.TabIndex = 12;
            labelBoldCheckBox.Text = "Bold";
            labelBoldCheckBox.UseVisualStyleBackColor = true;
            labelBoldCheckBox.CheckedChanged += BoldCheckBox_CheckedChanged;
            // 
            // literalColorLabel
            // 
            literalColorLabel.AutoSize = true;
            literalColorLabel.Font = new Font("Segoe UI", 9F);
            literalColorLabel.Location = new Point(6, 71);
            literalColorLabel.Name = "literalColorLabel";
            literalColorLabel.Size = new Size(55, 25);
            literalColorLabel.TabIndex = 16;
            literalColorLabel.Text = "Color";
            // 
            // literalGroupBox
            // 
            literalGroupBox.Controls.Add(literalColorButton);
            literalGroupBox.Controls.Add(literalFormatComboBox);
            literalGroupBox.Controls.Add(literalColorLabel);
            literalGroupBox.Controls.Add(literalItalicCheckBox);
            literalGroupBox.Controls.Add(literalFormatLabel);
            literalGroupBox.Controls.Add(literalBoldCheckBox);
            literalGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            literalGroupBox.Location = new Point(12, 553);
            literalGroupBox.Name = "literalGroupBox";
            literalGroupBox.Size = new Size(592, 112);
            literalGroupBox.TabIndex = 16;
            literalGroupBox.TabStop = false;
            literalGroupBox.Text = "Literals";
            // 
            // literalColorButton
            // 
            literalColorButton.Location = new Point(152, 66);
            literalColorButton.Name = "literalColorButton";
            literalColorButton.Size = new Size(34, 34);
            literalColorButton.TabIndex = 16;
            literalColorButton.UseVisualStyleBackColor = true;
            literalColorButton.Click += ColorButton_Click;
            // 
            // literalFormatComboBox
            // 
            literalFormatComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            literalFormatComboBox.Font = new Font("Segoe UI", 9F);
            literalFormatComboBox.FormattingEnabled = true;
            literalFormatComboBox.Location = new Point(152, 27);
            literalFormatComboBox.Name = "literalFormatComboBox";
            literalFormatComboBox.Size = new Size(182, 33);
            literalFormatComboBox.TabIndex = 10;
            literalFormatComboBox.SelectedIndexChanged += LiteralFormatComboBox_SelectedIndexChanged;
            // 
            // literalItalicCheckBox
            // 
            literalItalicCheckBox.AutoSize = true;
            literalItalicCheckBox.Font = new Font("Segoe UI", 9F);
            literalItalicCheckBox.Location = new Point(400, 64);
            literalItalicCheckBox.Name = "literalItalicCheckBox";
            literalItalicCheckBox.Size = new Size(74, 29);
            literalItalicCheckBox.TabIndex = 13;
            literalItalicCheckBox.Text = "Italic";
            literalItalicCheckBox.UseVisualStyleBackColor = true;
            literalItalicCheckBox.CheckedChanged += ItalicCheckBox_CheckedChanged;
            // 
            // literalFormatLabel
            // 
            literalFormatLabel.AutoSize = true;
            literalFormatLabel.Font = new Font("Segoe UI", 9F);
            literalFormatLabel.Location = new Point(6, 27);
            literalFormatLabel.Name = "literalFormatLabel";
            literalFormatLabel.Size = new Size(69, 25);
            literalFormatLabel.TabIndex = 11;
            literalFormatLabel.Text = "Format";
            // 
            // literalBoldCheckBox
            // 
            literalBoldCheckBox.AutoSize = true;
            literalBoldCheckBox.Font = new Font("Segoe UI", 9F);
            literalBoldCheckBox.Location = new Point(400, 29);
            literalBoldCheckBox.Name = "literalBoldCheckBox";
            literalBoldCheckBox.Size = new Size(74, 29);
            literalBoldCheckBox.TabIndex = 12;
            literalBoldCheckBox.Text = "Bold";
            literalBoldCheckBox.UseVisualStyleBackColor = true;
            literalBoldCheckBox.CheckedChanged += BoldCheckBox_CheckedChanged;
            // 
            // punctuationGroupBox
            // 
            punctuationGroupBox.Controls.Add(punctuationColorButton);
            punctuationGroupBox.Controls.Add(punctuationItalicCheckBox);
            punctuationGroupBox.Controls.Add(punctuationColorLabel);
            punctuationGroupBox.Controls.Add(punctuationBoldCheckBox);
            punctuationGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            punctuationGroupBox.Location = new Point(12, 671);
            punctuationGroupBox.Name = "punctuationGroupBox";
            punctuationGroupBox.Size = new Size(592, 103);
            punctuationGroupBox.TabIndex = 17;
            punctuationGroupBox.TabStop = false;
            punctuationGroupBox.Text = "Punctuation";
            // 
            // punctuationColorButton
            // 
            punctuationColorButton.Location = new Point(152, 22);
            punctuationColorButton.Name = "punctuationColorButton";
            punctuationColorButton.Size = new Size(34, 34);
            punctuationColorButton.TabIndex = 17;
            punctuationColorButton.UseVisualStyleBackColor = true;
            punctuationColorButton.Click += ColorButton_Click;
            // 
            // punctuationItalicCheckBox
            // 
            punctuationItalicCheckBox.AutoSize = true;
            punctuationItalicCheckBox.Font = new Font("Segoe UI", 9F);
            punctuationItalicCheckBox.Location = new Point(400, 61);
            punctuationItalicCheckBox.Name = "punctuationItalicCheckBox";
            punctuationItalicCheckBox.Size = new Size(74, 29);
            punctuationItalicCheckBox.TabIndex = 13;
            punctuationItalicCheckBox.Text = "Italic";
            punctuationItalicCheckBox.UseVisualStyleBackColor = true;
            punctuationItalicCheckBox.CheckedChanged += ItalicCheckBox_CheckedChanged;
            // 
            // punctuationColorLabel
            // 
            punctuationColorLabel.AutoSize = true;
            punctuationColorLabel.Font = new Font("Segoe UI", 9F);
            punctuationColorLabel.Location = new Point(6, 27);
            punctuationColorLabel.Name = "punctuationColorLabel";
            punctuationColorLabel.Size = new Size(55, 25);
            punctuationColorLabel.TabIndex = 18;
            punctuationColorLabel.Text = "Color";
            // 
            // punctuationBoldCheckBox
            // 
            punctuationBoldCheckBox.AutoSize = true;
            punctuationBoldCheckBox.Font = new Font("Segoe UI", 9F);
            punctuationBoldCheckBox.Location = new Point(400, 26);
            punctuationBoldCheckBox.Name = "punctuationBoldCheckBox";
            punctuationBoldCheckBox.Size = new Size(74, 29);
            punctuationBoldCheckBox.TabIndex = 12;
            punctuationBoldCheckBox.Text = "Bold";
            punctuationBoldCheckBox.UseVisualStyleBackColor = true;
            punctuationBoldCheckBox.CheckedChanged += BoldCheckBox_CheckedChanged;
            // 
            // sampleCodePanel
            // 
            sampleCodePanel.BorderStyle = BorderStyle.FixedSingle;
            sampleCodePanel.Location = new Point(12, 780);
            sampleCodePanel.Name = "sampleCodePanel";
            sampleCodePanel.Size = new Size(592, 123);
            sampleCodePanel.TabIndex = 18;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(374, 909);
            okButton.Name = "okButton";
            okButton.Size = new Size(112, 34);
            okButton.TabIndex = 0;
            okButton.Text = "Ok";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += OkButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(492, 909);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(112, 34);
            cancelButton.TabIndex = 0;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // EditSettingsDialog
            //
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(616, 955);
            Controls.Add(sampleCodePanel);
            Controls.Add(punctuationGroupBox);
            Controls.Add(literalGroupBox);
            Controls.Add(labelGroupBox);
            Controls.Add(mnemonicGroupBox);
            Controls.Add(addressGroupBox);
            Controls.Add(generalGroupBox);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "EditSettingsDialog";
            Text = "Settings";
            generalGroupBox.ResumeLayout(false);
            generalGroupBox.PerformLayout();
            addressGroupBox.ResumeLayout(false);
            addressGroupBox.PerformLayout();
            mnemonicGroupBox.ResumeLayout(false);
            mnemonicGroupBox.PerformLayout();
            labelGroupBox.ResumeLayout(false);
            labelGroupBox.PerformLayout();
            literalGroupBox.ResumeLayout(false);
            literalGroupBox.PerformLayout();
            punctuationGroupBox.ResumeLayout(false);
            punctuationGroupBox.PerformLayout();
            ResumeLayout(false);
        }

        private void AddressItalicCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            throw new NotImplementedException();
        }

        #endregion
        private GroupBox generalGroupBox;
        private Label colorThemeLabel;
        private ComboBox colorThemeComboBox;
        private Label textScalingLabel;
        private ComboBox textScalingComboBox;
        private Label lineSpacingLabel;
        private ComboBox lineSpacingComboBox;
        private Label codeFontLabel;
        private ComboBox codeFontComboBox;

        private GroupBox addressGroupBox;
        private Label addressFormatLabel;
        private ComboBox addressFormatComboBox;
        private Label addressColorLabel;
        private Button addressColorButton;
        private CheckBox addressBoldCheckBox;
        private CheckBox addressItalicCheckBox;

        private GroupBox mnemonicGroupBox;
        private Label mnemonicFormatLabel;
        private ComboBox mnemonicFormatComboBox;
        private Label mnemonicColorLabel;
        private Button mnemonicColorButton;
        private CheckBox mnemonicBoldCheckBox;
        private CheckBox mnemonicItalicCheckBox;

        private GroupBox labelGroupBox;
        private Label labelColorLabel;
        private Button labelColorButton;
        private CheckBox labelBoldCheckBox;
        private CheckBox labelItalicCheckBox;

        private GroupBox literalGroupBox;
        private Label literalFormatLabel;
        private ComboBox literalFormatComboBox;
        private Label literalColorLabel;
        private Button literalColorButton;
        private CheckBox literalBoldCheckBox;
        private CheckBox literalItalicCheckBox;

        private GroupBox punctuationGroupBox;
        private Label punctuationColorLabel;
        private Button punctuationColorButton;
        private CheckBox punctuationBoldCheckBox;
        private CheckBox punctuationItalicCheckBox;
        
        private SampleCodePanel sampleCodePanel;

        private Button okButton;
        private Button cancelButton;
    }
}