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
using System.Diagnostics;

namespace BeebPerf.ux
{
    internal class FramesGridView : GridView<FrameMetrics>, IGridExporter
    {
        private const int FrameNumberColumnIndex = 0;
        private const int DurationColumnIndex = 1;
        private const int DisplayOffsetColumnIndex = 2;
        private const int WritesBeforeDisplayColumnIndex = 3;
        private const int WritesAfterDisplayColumnIndex = 4;
        private const int VisualizationColumnIndex = 5;

        private const int ExportFrameNumberColumnIndex = 0;
        private const int ExportStartCycleCountColumnIndex = 1;
        private const int ExportEndCycleCountColumnIndex = 2;
        private const int ExportDurationColumnIndex = 3;
        private const int ExportDurationPercentageColumnIndex = 4;
        private const int ExportDisplayOffsetColumnIndex = 5;
        private const int ExportWriteCountBeforeDisplayColumnIndex = 6;
        private const int ExportWritePercentageBeforeDisplayColumnIndex = 7;
        private const int ExportWriteCountAfterDisplayColumnIndex = 8;
        private const int ExportWritePercentageAfterDisplayColumnIndex = 9;
        private const int ExportColumnCount = 10;

        public FramesGridView(FramesView framesView) : base(System.Windows.Forms.SelectionMode.One, (ButtonType)0)
        {
            _FramesView = framesView;

            AddColumn("FrameNumber", "Frame [#]", cellTemplate: null);
            AddColumn("Duration", "Duration [#, %]", cellTemplate: null);
            AddColumn("DisplayFrameOffset", "Offset [#]", cellTemplate: null);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes before display [#, %]", cellTemplate: null);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes after display [#, %]", cellTemplate: null);
            AddColumn("Visualization", "Visualization", cellTemplate: null);

            SetColumnAutoSize(VisualizationColumnIndex, DataGridViewAutoSizeColumnMode.Fill);

            SetColumnAlignment(FrameNumberColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(DurationColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(DisplayOffsetColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(WritesBeforeDisplayColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WritesAfterDisplayColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(FrameNumberColumnIndex, "Frame number");
            SetColumnHeaderToolTip(DurationColumnIndex, "Number of cycles, percentage of cycles to threshold");
            SetColumnHeaderToolTip(DisplayOffsetColumnIndex, "Cycles before display scan starts");
            SetColumnHeaderToolTip(WritesBeforeDisplayColumnIndex, "Number of screen memory writes before the display memory read");
            SetColumnHeaderToolTip(WritesBeforeDisplayColumnIndex, "Nunber of screen memory writes after the display memory read");
            SetColumnHeaderToolTip(VisualizationColumnIndex, "Visualization");

            SetColumnSortMode(FrameNumberColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(DurationColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(DisplayOffsetColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(VisualizationColumnIndex, DataGridViewColumnSortMode.NotSortable);
        }

        public void Initialize(
            List<FrameMetrics> frameMetricsList,
            FrameSettings? frameSettings,
            bool highlightWritesBeforeDisplay,
            bool highlightWritesAfterDisplay)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _FrameSettings = frameSettings;
            _HighlightWritesBeforeDisplay = highlightWritesBeforeDisplay;
            _HighlightWritesAfterDisplay = highlightWritesAfterDisplay;

            if (frameMetricsList.Count > 0)
                base.SetRowsData(frameMetricsList);
            else
                base.Clear();
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            foreach (var frameMetrics in base._DataRows)
            {
                if (frameMetrics.StartCycleCount == analysisFrom && 
                    frameMetrics.EndCycleCount == analysisTo)
                {
                    SelectRow(frameMetrics, scrollIntoView: true);
                    return;
                }
            }

            base.ClearSelection();
        }

        protected override void OnSelectionChange(object? sender, FrameMetrics? frameMetrics)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            if (frameMetrics != null)
                form.SetAnalysisRange(frameMetrics.StartCycleCount, frameMetrics.EndCycleCount);
            else
                form.SetAnalysisRange(0, int.MaxValue);
        }

        protected override int OnSortCompare(FrameMetrics a, FrameMetrics b, int columnIndex)
        {
            int result = columnIndex switch
            {
                FrameNumberColumnIndex => a.FrameNumber.CompareTo(b.FrameNumber),
                DurationColumnIndex => (a.EndCycleCount - a.StartCycleCount).CompareTo(b.EndCycleCount - b.StartCycleCount),
                WritesBeforeDisplayColumnIndex => a.WritesBeforeDisplayRead - b.WritesBeforeDisplayRead,
                WritesAfterDisplayColumnIndex => a.WritesAfterDisplayRead - b.WritesAfterDisplayRead,
                DisplayOffsetColumnIndex => a.DisplayFrameOffset.CompareTo(b.DisplayFrameOffset),
                _ => 0
            };

            if (result == 0)
                result = a.FrameNumber.CompareTo(b.FrameNumber);

            return result;
        }

        protected override string OnFormatRowData(FrameMetrics frameMetrics, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                FrameNumberColumnIndex => frameMetrics.FrameNumber.ToString(),
                DurationColumnIndex => FormatDuration(frameMetrics),
                WritesBeforeDisplayColumnIndex => $"{frameMetrics.WritesBeforeDisplayRead:N0}",
                WritesAfterDisplayColumnIndex => $"{frameMetrics.WritesAfterDisplayRead:N0}",
                DisplayOffsetColumnIndex => $"{frameMetrics.DisplayFrameOffset:N0}",
                
                _ => FormatCountAndRange(frameMetrics, columnIndex)
            };
        }

        protected override (int value, int range, bool clamp) OnRowDataCountAndRange(FrameMetrics frameMetrics, int columnIndex)
        {
            switch (columnIndex)
            {
                case WritesBeforeDisplayColumnIndex:
                    if (_HighlightWritesBeforeDisplay && frameMetrics.WritesBeforeDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case WritesAfterDisplayColumnIndex:
                    if (_HighlightWritesAfterDisplay && frameMetrics.WritesAfterDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case DurationColumnIndex:
                    int cycles = frameMetrics.EndCycleCount - frameMetrics.StartCycleCount;
                    return (value: cycles, range: _FrameSettings!.ThresholdCycles, clamp: false);

                default:
                    return (value: -1, range: 1, clamp: false);
            }
        }

        private string FormatDuration(FrameMetrics frameMetrics)
        {
            int duration = frameMetrics.EndCycleCount - frameMetrics.StartCycleCount;
            int range = _FrameSettings!.ThresholdCycles;
            double percentage = (int)double.Round(100.0 * duration / range);
            return $"{duration:N0} ({percentage:F2}%)";
        }

        string[] IGridExporter.GetHeaders()
        {
            string[] headers = [
                "Frame Number",
                "Start cycle count",
                "End cycle count",
                "Duration cycles [#]",
                "Duration cycles [%]",
                "Writes before display [#]",
                "Writes before display [%]",
                "Writes after display [#]",
                "Writes after display [%]",
                "Display frame offset"
            ];

            Debug.Assert(headers.Length == ExportColumnCount);
            return headers;
        }

        int IGridExporter.GetRowCount()
        {
            return _DataRows.Count;
        }

        string[] IGridExporter.GetRowValues(int rowIndex)
        {
            List<string> rowValues = new();

            var rowData = _DataRows[rowIndex];

            for (int columnIndex = 0; columnIndex < ExportColumnCount; columnIndex++)
                rowValues.Add(FormatExportCell(rowData, columnIndex));

            return rowValues.ToArray();
        }

        private string FormatExportCell(FrameMetrics frameMetrics, int columnIndex)
        {
            return columnIndex switch
            {
                ExportFrameNumberColumnIndex => frameMetrics.FrameNumber.ToString(),
                ExportStartCycleCountColumnIndex => frameMetrics.StartCycleCount.ToString(),
                ExportEndCycleCountColumnIndex => frameMetrics.EndCycleCount.ToString(),
                ExportDurationColumnIndex => (frameMetrics.EndCycleCount - frameMetrics.StartCycleCount).ToString(),
                ExportDurationPercentageColumnIndex => FormatExportPercentage(frameMetrics.EndCycleCount - frameMetrics.StartCycleCount, _FrameSettings!.ThresholdCycles),
                ExportWriteCountBeforeDisplayColumnIndex => frameMetrics.WritesBeforeDisplayRead.ToString(),
                ExportWritePercentageBeforeDisplayColumnIndex => FormatExportPercentage(frameMetrics.WritesBeforeDisplayRead, frameMetrics.WritesBeforeDisplayRead + frameMetrics.WritesAfterDisplayRead),
                ExportWriteCountAfterDisplayColumnIndex => frameMetrics.WritesAfterDisplayRead.ToString(),
                ExportWritePercentageAfterDisplayColumnIndex => FormatExportPercentage(frameMetrics.WritesAfterDisplayRead, frameMetrics.WritesBeforeDisplayRead + frameMetrics.WritesAfterDisplayRead),
                ExportDisplayOffsetColumnIndex => frameMetrics.DisplayFrameOffset.ToString(),
                _ => string.Empty
            };
        }

        private FramesView _FramesView;
        private FrameSettings? _FrameSettings = null;
        private bool _HighlightWritesBeforeDisplay = false;
        private bool _HighlightWritesAfterDisplay = false;
        private ReentrancyGuard _ReentrancyGuard = new();
    }
}
