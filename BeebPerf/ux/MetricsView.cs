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
    internal class MetricsView : Panel
    {
        public MetricsView() : base()
        {
            InitializeComponent();
            UpdateState();
        }

        public void SetMetrics(
            List<Metric> metrics,
            Metric? selectedMetric)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _Metrics = metrics;
            _SelectedMetric = selectedMetric;
            UpdateState();
        }

        public void SetIteractions(List<FrameAnalysis.MetricIteration> iterations)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _Iterations = iterations;

            // determine whether to highlight writes before or writes after
            bool highlightWritesBeforeDisplay = false;
            bool highlightWritesAfterDisplay = false;

            if (_SelectedMetric != null && _SelectedMetric.DisplayAnalysis)
            {
                int totalWritesBeforeDisplay = 0;
                int totalWritesAfterDisplay = 0;
                foreach (var iteration in iterations)
                {
                    totalWritesBeforeDisplay += iteration.WritesBeforeDisplayRead;
                    totalWritesAfterDisplay += iteration.WritesAfterDisplayRead;
                }

                if (totalWritesBeforeDisplay > 0 && totalWritesAfterDisplay > 0)
                {
                    highlightWritesBeforeDisplay = totalWritesBeforeDisplay <= totalWritesAfterDisplay;
                    highlightWritesAfterDisplay = totalWritesAfterDisplay <= totalWritesBeforeDisplay;
                }
            }

            // initialize grid
            _GridView.Initialize(_Iterations, _SelectedMetric, highlightWritesBeforeDisplay, highlightWritesAfterDisplay);

            UpdateSummaryText(highlightWritesBeforeDisplay, highlightWritesAfterDisplay);
            UpdateState();
        }

        private void UpdateSummaryText(
            bool highlightWritesBeforeDisplay,
            bool highlightWritesAfterDisplay)
        {
            string text = string.Empty;
            if (_Iterations.Count > 0)
            {
                if (_SelectedMetric != null && _SelectedMetric.ThresholdCycles > 0)
                {
                    int iterationDurationExceedsThresholdCount = 0;
                    foreach (var iteration in _Iterations)
                        if (iteration.EndCycleCount - iteration.StartCycleCount > _SelectedMetric.ThresholdCycles)
                            iterationDurationExceedsThresholdCount++;

                    double iterationDurationExceedsThresholdPercentage = 100.0 * (double)iterationDurationExceedsThresholdCount / (double)_Iterations.Count;
                    text = $"Iterations exceeding threshold: {iterationDurationExceedsThresholdPercentage:F2}%. ";
                }

                if (_SelectedMetric != null && _SelectedMetric.DisplayAnalysis)
                {
                    int totalWriteCount = 0;
                    int totalMissTimedWriteCount = 0;
                    int iterationWithMissTimesWriteCount = 0;
                    foreach (var iteration in _Iterations)
                    {
                        totalWriteCount += iteration.WritesBeforeDisplayRead + iteration.WritesAfterDisplayRead;

                        if (highlightWritesBeforeDisplay && iteration.WritesBeforeDisplayRead > 0)
                        {
                            totalMissTimedWriteCount += iteration.WritesBeforeDisplayRead;
                            iterationWithMissTimesWriteCount++;
                        }

                        if (highlightWritesAfterDisplay && iteration.WritesAfterDisplayRead > 0)
                        {
                            totalMissTimedWriteCount += iteration.WritesAfterDisplayRead;
                            iterationWithMissTimesWriteCount++;
                        }
                    }

                    double iterationMissTimedWritePercentage = 100.0 * (double)iterationWithMissTimesWriteCount / (double)_Iterations.Count;
                    double overallMissTimedWritePercentage = 100.0 * (double)totalMissTimedWriteCount / (double)totalWriteCount;
                    text += $"Iterations with miss-timed writes: {iterationMissTimedWritePercentage:F2}%, " +
                            $"Total miss-timed writes: {overallMissTimedWritePercentage:F2}%";
                }
            }

            _StatusLabel.Text = text;
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _GridView.SelectRange(analysisFrom, analysisTo);
        }

        private void MetricComboBox_SelectedIndexChanged(object? sender, EventArgs e)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            var comboBox = sender as ComboBox;
            if (comboBox == null) return;
            
            foreach (var settings in _Metrics!)
            {
                if (settings.Name == comboBox.SelectedItem as string)
                {
                    _SelectedMetric = settings;
                    form.SetSelectedMetric(settings);
                    break;
                }
            }
        }

        private void AddButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.AddMetric();
        }

        private void EditButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.EditMetric();
        }

        private void RemoveButton_Click(object? sender, EventArgs e)
        {
            var form = FindForm() as BeebPerfForm;
            if (form == null) return;

            form.RemoveMetric();
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

        [MemberNotNull(nameof(_MetricLabel), nameof(_MetricComboBox), nameof(_MetricComboBoxPanel), nameof(_AddButton), nameof(_EditButton), nameof(_StatusLabel), nameof(_RemoveButton), nameof(_CopyButton), nameof(_ExportButton), nameof(_GridView))]
        private void InitializeComponent()
        {
            // 
            // _MetricLabel
            // 
            _MetricLabel = new();
            _MetricLabel.AutoSize = true;
            _MetricLabel.Font = new Font("Segoe UI", 9F);
            _MetricLabel.Name = "settingsLabel";
            _MetricLabel.Size = new Size(101, 25);
            _MetricLabel.TabIndex = 0;
            _MetricLabel.Text = "Metric:";
            // 
            // _SettingComboBox
            // 
            _MetricComboBox = new();
            _MetricComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            _MetricComboBox.Font = new Font("Segoe UI", 9F);
            _MetricComboBox.FormattingEnabled = true;
            _MetricComboBox.Name = "settingsComboBox";
            _MetricComboBox.TabIndex = 0;
            _MetricComboBox.Location = new Point(0, 0);
            _MetricComboBox.Size = new Size(200, 33);
            _MetricComboBox.SelectedIndexChanged += MetricComboBox_SelectedIndexChanged;
            //
            // _MetricComboBoxPanel
            //
            _MetricComboBoxPanel = new();
            _MetricComboBoxPanel.BorderStyle = BorderStyle.FixedSingle;
            _MetricComboBoxPanel.BackColor = SystemColors.Window;
            _MetricComboBoxPanel.Size = new Size(200, 33);
            _MetricComboBoxPanel.AutoSize = false;
            _MetricComboBoxPanel.TabIndex = 0;
            _MetricComboBoxPanel.Controls.Add(_MetricComboBox);
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
            _GridView = new MetricsGridView();
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
            Controls.Add(_MetricLabel);
            Controls.Add(_MetricComboBoxPanel);
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
                _MetricLabel,
                _MetricComboBoxPanel,
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
            _MetricComboBox.Items.Clear();

            bool hasMetrics = _Metrics != null && _Metrics.Count > 0;
            if (hasMetrics)
            {
                int selectedIndex = -1;
                foreach (var metric in _Metrics!)
                {
                    _MetricComboBox.Items.Add(metric.Name);
                    if (_SelectedMetric != null && metric.Name == _SelectedMetric.Name)
                        selectedIndex = _MetricComboBox.Items.Count - 1;
                }
                _MetricComboBox.SelectedIndex = selectedIndex;
            }

            _MetricComboBoxPanel.Visible = hasMetrics;

            _EditButton.Visible = hasMetrics;
            _EditButton.Enabled = _SelectedMetric != null;

            _RemoveButton.Visible = hasMetrics;
            _RemoveButton.Enabled = _SelectedMetric != null;

            _StatusLabel.Visible = _StatusLabel.Text.Length > 0;

            _CopyButton.Visible = _Iterations.Count > 0;
            _ExportButton.Visible = _Iterations.Count > 0;
        }

        private List<FrameAnalysis.MetricIteration> _Iterations = [];
        private List<Metric>? _Metrics = [];
        private Metric? _SelectedMetric = null;
        private ReentrancyGuard _ReentrancyGuard = new();

        // controls
        private Label _MetricLabel;
        private Panel _MetricComboBoxPanel;
        private ComboBox _MetricComboBox;
        private Button _AddButton;
        private Button _EditButton;
        private Button _RemoveButton;
        private Label _StatusLabel;
        private ButtonEx _CopyButton;
        private ButtonEx _ExportButton;
        private MetricsGridView _GridView;
    }
}
