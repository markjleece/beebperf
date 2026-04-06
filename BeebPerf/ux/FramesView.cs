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

using BeebPerf.model;
using System.Diagnostics.CodeAnalysis;

namespace BeebPerf.ux
{
    internal class FramesView : Panel
    {
        public FramesView() : base()
        {
            InitializeComponent();
            UpdateState();
        }

        public void SetSettings(
            List<FrameSettings> frameSettingsList,
            FrameSettings? selectedFrameSettings)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _FrameSettingsList = frameSettingsList;
            _SelectedFrameSettings = selectedFrameSettings;
            UpdateState();
        }

        public void SetResults(List<FrameAnalysis.Frame> frames)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _Frames = frames;

            // determine whether to highlight writes before or writes after
            int totalWritesBeforeDisplay = 0;
            int totalWritesAfterDisplay = 0;
            foreach (var frame in frames)
            {
                totalWritesBeforeDisplay += frame.WritesBeforeDisplayRead;
                totalWritesAfterDisplay += frame.WritesAfterDisplayRead;
            }

            bool highlightWritesBeforeDisplay = false;
            bool highlightWritesAfterDisplay = false;
            if (totalWritesBeforeDisplay > 0 && totalWritesAfterDisplay > 0)
            {
                highlightWritesBeforeDisplay = totalWritesBeforeDisplay <= totalWritesAfterDisplay;
                highlightWritesAfterDisplay = totalWritesAfterDisplay <= totalWritesBeforeDisplay;
            }

            // initialize grid
            _GridView.Initialize(_Frames, _SelectedFrameSettings, highlightWritesBeforeDisplay, highlightWritesAfterDisplay);

            UpdateSummaryText(highlightWritesBeforeDisplay, highlightWritesAfterDisplay);
            UpdateState();
        }

        private void UpdateSummaryText(
            bool highlightWritesBeforeDisplay,
            bool highlightWritesAfterDisplay)
        {
            string text = string.Empty;
            if (_Frames.Count > 0)
            {
                if (_SelectedFrameSettings != null && _SelectedFrameSettings.ThresholdCycles > 0)
                {
                    int frameDuractionExceedsThresholdCount = 0;
                    foreach (var frame in _Frames)
                        if (frame.EndCycleCount - frame.StartCycleCount > _SelectedFrameSettings.ThresholdCycles)
                            frameDuractionExceedsThresholdCount++;

                    double frameDurationExceedsThresholdPercentage = 100.0 * (double)frameDuractionExceedsThresholdCount / (double)_Frames.Count;
                    text = $"Frames exceeding threshold: {frameDurationExceedsThresholdPercentage:F2}%. ";
                }

                int totalWriteCount = 0;
                int totalMissTimedWriteCount = 0;
                int frameWithMissTimesWriteCount = 0;
                foreach (var frame in _Frames)
                {
                    totalWriteCount += frame.WritesBeforeDisplayRead + frame.WritesAfterDisplayRead;

                    if (highlightWritesBeforeDisplay && frame.WritesBeforeDisplayRead > 0)
                    {
                        totalMissTimedWriteCount += frame.WritesBeforeDisplayRead;
                        frameWithMissTimesWriteCount++;
                    }

                    if (highlightWritesAfterDisplay && frame.WritesAfterDisplayRead > 0)
                    {
                        totalMissTimedWriteCount += frame.WritesAfterDisplayRead;
                        frameWithMissTimesWriteCount++;
                    }
                }

                double frameMissTimedWritePercentage = 100.0 * (double)frameWithMissTimesWriteCount / (double)_Frames.Count;
                double overallMissTimedWritePercentage = 100.0 * (double)totalMissTimedWriteCount / (double)totalWriteCount;
                text += $"Frames with miss-timed writes: {frameMissTimedWritePercentage:F2}%, " +
                        $"Total miss-timed writes: {overallMissTimedWritePercentage:F2}%";
            }

            _StatusLabel.Text = text;
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _GridView.SelectRange(analysisFrom, analysisTo);
        }

        private void SettingsComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            
            foreach (var settings in _FrameSettingsList!)
            {
                if (settings.Name == comboBox.SelectedItem as string)
                {
                    _SelectedFrameSettings = settings;
                    form.SetSelectedFrameSettings(settings);
                    break;
                }
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.AddFrameSettings();
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.EditFrameSettings();
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.RemoveFrameSettings();
        }

        private void CopyButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            Exporter.CopyToClipboard(form, _GridView);
        }

        private void ExportButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            Exporter.ExportCSVFile(form, _GridView);
        }

        [MemberNotNull(nameof(_SettingsLabel), nameof(_SettingsComboBox), nameof(_SettingsComboBoxPanel), nameof(_AddButton), nameof(_EditButton), nameof(_StatusLabel), nameof(_RemoveButton), nameof(_CopyButton), nameof(_ExportButton), nameof(_GridView))]
        private void InitializeComponent()
        {
            // 
            // _SettingsLabel
            // 
            _SettingsLabel = new();
            _SettingsLabel.AutoSize = true;
            _SettingsLabel.Font = new Font("Segoe UI", 9F);
            _SettingsLabel.Name = "settingsLabel";
            _SettingsLabel.Size = new Size(101, 25);
            _SettingsLabel.TabIndex = 0;
            _SettingsLabel.Text = "Settings:";
            // 
            // _SettingComboBox
            // 
            _SettingsComboBox = new();
            _SettingsComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _SettingsComboBox.Font = new Font("Segoe UI", 9F);
            _SettingsComboBox.FormattingEnabled = true;
            _SettingsComboBox.Name = "settingsComboBox";
            _SettingsComboBox.TabIndex = 0;
            _SettingsComboBox.Location = new Point(0, 0);
            _SettingsComboBox.Size = new Size(200, 33);
            _SettingsComboBox.SelectedIndexChanged += SettingsComboBox_SelectedIndexChanged;
            //
            // _SettingsComboBoxPanel
            //
            _SettingsComboBoxPanel = new();
            _SettingsComboBoxPanel.BorderStyle = BorderStyle.FixedSingle;
            _SettingsComboBoxPanel.BackColor = SystemColors.Window;
            _SettingsComboBoxPanel.Size = new Size(200, 33);
            _SettingsComboBoxPanel.AutoSize = false;
            _SettingsComboBoxPanel.TabIndex = 0;
            _SettingsComboBoxPanel.Controls.Add(_SettingsComboBox);
            // 
            // _AddButton
            // 
            _AddButton = new();
            _AddButton.DialogResult = DialogResult.OK;
            _AddButton.Name = "addButton";
            _AddButton.Size = new Size(112, 34);
            _AddButton.TabIndex = 0;
            _AddButton.Text = "Add...";
            _AddButton.BackColor = SystemColors.ControlLightLight;
            _AddButton.Click += AddButton_Click;
            // 
            // _EditButton
            // 
            _EditButton = new();
            _EditButton.DialogResult = DialogResult.OK;
            _EditButton.Name = "editButton";
            _EditButton.Size = new Size(130, 34);
            _EditButton.TabIndex = 0;
            _EditButton.Text = "Edit...";
            _EditButton.BackColor = SystemColors.ControlLightLight;
            _EditButton.Click += EditButton_Click;
            // 
            // _RemoveButton
            // 
            _RemoveButton = new();
            _RemoveButton.DialogResult = DialogResult.OK;
            _RemoveButton.Name = "removeButton";
            _RemoveButton.Size = new Size(112, 34);
            _RemoveButton.TabIndex = 0;
            _RemoveButton.Text = "Remove";
            _RemoveButton.BackColor = SystemColors.ControlLightLight;
            _RemoveButton.Click += RemoveButton_Click;
            //
            // _CopyButton
            //
            _CopyButton = new();
            _CopyButton.Name = "copyButton";
            _CopyButton.ImageResourceName = "copyButton.Image";
            _CopyButton.ToolTipText = "Copy";
            _CopyButton.BackColor = SystemColors.ControlLightLight;
            _CopyButton.Click += CopyButton_Click;
            //
            // _ExportButton
            //
            _ExportButton = new();
            _ExportButton.Name = "exportButton";
            _ExportButton.ImageResourceName = "exportButton.Image";
            _ExportButton.ToolTipText = "Export";
            _ExportButton.BackColor = SystemColors.ControlLightLight;
            _ExportButton.Click += ExportButton_Click;
            // 
            // _GridView
            // 
            _GridView = new FramesGridView();
            _GridView.BackColor = SystemColors.Control;
            // 
            // _StatusLabel
            // 
            _StatusLabel = new();
            _StatusLabel.AutoSize = true;
            _StatusLabel.Font = new Font("Segoe UI", 9F);
            _StatusLabel.Name = "summaryLabel";
            _StatusLabel.Size = new Size(101, 25);
            _StatusLabel.TabIndex = 0;
            // 
            // FramesView - Add controls directly
            // 
            Controls.Add(_SettingsLabel);
            Controls.Add(_SettingsComboBoxPanel);
            Controls.Add(_AddButton);
            Controls.Add(_EditButton);
            Controls.Add(_RemoveButton);
            Controls.Add(_CopyButton);
            Controls.Add(_ExportButton);
            Controls.Add(_GridView);
            Controls.Add(_StatusLabel);
        }

        protected override void OnLayout(LayoutEventArgs levent)
        {
            base.OnLayout(levent);

            // layout children
            Control[] leftAlignedControls = [
                _SettingsLabel,
                _SettingsComboBoxPanel,
                _AddButton,
                _EditButton,
                _RemoveButton ];

            Control[] rightAlignedControls = [
                _ExportButton,
                _CopyButton ];

            // calc max height of controls to align them vertically centered
            int maxHeight = 0;
            foreach (Control control in leftAlignedControls)
                if (maxHeight < control.Height && control.Visible)
                    maxHeight = control.Height;
            foreach (Control control in rightAlignedControls)
                if (maxHeight < control.Height && control.Visible)
                    maxHeight = control.Height;

            // layout left aligned controls
            int padding = 6;
            int left = padding;
            foreach (Control child in leftAlignedControls)
            {
                if (!child.Visible)
                    continue;

                Size size = child is Panel ? child.Size : child.GetPreferredSize(new Size(0, 0));
                child.Location = new Point(left, padding + (maxHeight - size.Height) / 2);
                child.Width = size.Width;
                child.Height = size.Height;
                left += child.Width + (child is Label ? 3 : 6);
            }

            // layout right aligned buttons
            left = Width - 6;
            foreach (Control child in rightAlignedControls)
            {
                left -= child.Width;
                child.Location = new Point(left, padding + (maxHeight - child.Height) / 2);
            }

            // layout grid view
            int toolbarHeight = maxHeight + padding * 2;
            int statusHeight = Font.Height + padding * 2;
            _GridView.Location = new Point(0, toolbarHeight);
            _GridView.Width = Width;
            _GridView.Height = Height - toolbarHeight - statusHeight;

            // layout status text
            _StatusLabel.Location = new Point(padding, Height - statusHeight + padding);
        }

        private void UpdateState()
        {
            _SettingsComboBox.Items.Clear();

            bool hasSettings = _FrameSettingsList != null && _FrameSettingsList.Count > 0;
            if (hasSettings)
            {
                int selectedIndex = -1;
                foreach (var setting in _FrameSettingsList!)
                {
                    _SettingsComboBox.Items.Add(setting.Name);
                    if (_SelectedFrameSettings != null && setting.Name == _SelectedFrameSettings.Name)
                        selectedIndex = _SettingsComboBox.Items.Count - 1;
                }
                _SettingsComboBox.SelectedIndex = selectedIndex;
            }

            _SettingsComboBoxPanel.Visible = hasSettings;

            _EditButton.Visible = hasSettings;
            _EditButton.Enabled = _SelectedFrameSettings != null;

            _RemoveButton.Visible = hasSettings;
            _RemoveButton.Enabled = _SelectedFrameSettings != null;

            _StatusLabel.Visible = _StatusLabel.Text.Length > 0;

            _CopyButton.Visible = _Frames.Count > 0;
            _ExportButton.Visible = _Frames.Count > 0;
        }

        private List<FrameAnalysis.Frame> _Frames = [];
        private List<FrameSettings>? _FrameSettingsList = [];
        private FrameSettings? _SelectedFrameSettings = null;
        private ReentrancyGuard _ReentrancyGuard = new();

        // controls
        private Label _SettingsLabel;
        private Panel _SettingsComboBoxPanel;
        private ComboBox _SettingsComboBox;
        private Button _AddButton;
        private Button _EditButton;
        private Button _RemoveButton;
        private Label _StatusLabel;
        private ButtonEx _CopyButton;
        private ButtonEx _ExportButton;
        private FramesGridView _GridView;
    }
}
