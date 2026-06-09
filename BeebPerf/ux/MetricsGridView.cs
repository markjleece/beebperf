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
    internal class MetricsGridView : GridView<MetricIteration>, IGridExporter
    {
        private const int IteractionNumberColumnIndex = 0;
        private const int DurationColumnIndex = 1;
        private const int DisplayOffsetColumnIndex = 2;
        private const int WritesBeforeDisplayColumnIndex = 3;
        private const int WritesAfterDisplayColumnIndex = 4;
        private const int VisualizationColumnIndex = 5;

        private const int ExportIteractionNumberColumnIndex = 0;
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

        public MetricsGridView() : base(System.Windows.Forms.SelectionMode.One, (ButtonType)0)
        {
            var cellTemplate = new CellTemplate();
            AddColumn("Iteration", "Iteration [#]", cellTemplate);
            AddColumn("Duration", "Duration [#, %]", cellTemplate);
            AddColumn("DisplayFrameOffset", "Display offset [#]", cellTemplate);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes before display [#, %]", cellTemplate);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes after display [#, %]", cellTemplate);
            AddColumn("Visualization", "Visualization", cellTemplate);

            SetColumnAutoSize(VisualizationColumnIndex, DataGridViewAutoSizeColumnMode.Fill);

            SetColumnAlignment(IteractionNumberColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(DurationColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(DisplayOffsetColumnIndex, DataGridViewContentAlignment.MiddleCenter);
            SetColumnAlignment(WritesBeforeDisplayColumnIndex, DataGridViewContentAlignment.MiddleRight);
            SetColumnAlignment(WritesAfterDisplayColumnIndex, DataGridViewContentAlignment.MiddleRight);

            SetColumnHeaderToolTip(IteractionNumberColumnIndex, "Iteration number");
            SetColumnHeaderToolTip(DurationColumnIndex, "Number of cycles, percentage of cycles to threshold");
            SetColumnHeaderToolTip(DisplayOffsetColumnIndex, "CPU cycles to the start of the display screen memory scan");
            SetColumnHeaderToolTip(WritesBeforeDisplayColumnIndex, "Number of screen memory writes before the screen memory was scanned for display");
            SetColumnHeaderToolTip(WritesBeforeDisplayColumnIndex, "Number of screen memory writes after the screen memory was scanned for display");
            SetColumnHeaderToolTip(VisualizationColumnIndex, "Iteration visualization");

            SetColumnSortMode(IteractionNumberColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(DurationColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(DisplayOffsetColumnIndex, DataGridViewColumnSortMode.Programmatic);
            SetColumnSortMode(VisualizationColumnIndex, DataGridViewColumnSortMode.NotSortable);
        }

        public void Initialize(
            List<MetricIteration> iterations, 
            Metric? metric,
            bool highlightWritesBeforeDisplay,
            bool highlightWritesAfterDisplay)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _Metric = metric;
            _HighlightWritesBeforeDisplay = highlightWritesBeforeDisplay;
            _HighlightWritesAfterDisplay = highlightWritesAfterDisplay;

            int maxCycleCount = 0;
            foreach (var iteration in iterations)
                maxCycleCount = Math.Max(maxCycleCount, iteration.EndCycleCount - iteration.StartCycleCount);
            _MaxCycleCount = maxCycleCount;

            int maxDisplayedCycleCount = GetMaxDisplayedCycleCount(iterations);
            if (metric != null && metric.ThresholdCycles > 0)
                maxDisplayedCycleCount = int.Max(maxDisplayedCycleCount, metric.ThresholdCycles);
            _MaxDisplayedCycleCount = maxDisplayedCycleCount;

            // set data
            if (iterations.Count > 0)
                base.SetRowsData(iterations);
            else
                base.Clear();

            // set column visibility
            bool displayAnalysis = (metric != null && metric.DisplayAnalysis);
            SetColumnVisibility(DisplayOffsetColumnIndex, displayAnalysis);
            SetColumnVisibility(WritesBeforeDisplayColumnIndex, displayAnalysis);
            SetColumnVisibility(WritesAfterDisplayColumnIndex, displayAnalysis);

            // set visualization column tool-tip
            string visualizationToolTip = string.Empty;
            if (metric != null && metric.DisplayAnalysis && metric.ThresholdCycles > 0)
                visualizationToolTip =
                    "Shows the iteration's duration (blue), threshold (red line), and CRTC screen memory scans (arrows)\n" +
                    "The arrow numbers identify the corresponding display frames, shown under the timeline";
            else if (metric != null && metric.DisplayAnalysis)
                visualizationToolTip =
                    "Shows the iteration's duration (blue) and CRTC screen memory scans (arrows)\n" +
                    "The arrow numbers identify the corresponding display frames, shown under the timeline";
            else if (metric != null && metric.ThresholdCycles > 0)
                visualizationToolTip = "Shows the iteration's duration (blue) and the threshold (red line)";
            else
                visualizationToolTip = "Shows the iteration's duration";

            SetColumnHeaderToolTip(VisualizationColumnIndex, visualizationToolTip);
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            foreach (var iteration in base._DataRows)
            {
                if (iteration.StartCycleCount == analysisFrom &&
                    iteration.EndCycleCount == analysisTo)
                {
                    SelectRow(iteration, scrollIntoView: true);
                    return;
                }
            }

            base.ClearSelection();
        }

        protected override void OnSelectionChange(object? sender, MetricIteration? iteration)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            if (iteration != null)
                form.SetAnalysisRange(iteration.StartCycleCount, iteration.EndCycleCount);
            else
                form.SetAnalysisRange(0, int.MaxValue);
        }

        protected override int OnSortCompare(MetricIteration a, MetricIteration b, int columnIndex)
        {
            int result = columnIndex switch
            {
                IteractionNumberColumnIndex => a.IterationNumber.CompareTo(b.IterationNumber),
                DurationColumnIndex => (a.EndCycleCount - a.StartCycleCount).CompareTo(b.EndCycleCount - b.StartCycleCount),
                WritesBeforeDisplayColumnIndex => a.WritesBeforeDisplayRead - b.WritesBeforeDisplayRead,
                WritesAfterDisplayColumnIndex => a.WritesAfterDisplayRead - b.WritesAfterDisplayRead,
                DisplayOffsetColumnIndex => a.DisplayFrameOffset.CompareTo(b.DisplayFrameOffset),
                _ => 0
            };

            if (result == 0)
                result = a.IterationNumber.CompareTo(b.IterationNumber);

            return result;
        }

        protected override string OnFormatRowData(MetricIteration iteration, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                IteractionNumberColumnIndex => iteration.IterationNumber.ToString(),
                DurationColumnIndex => FormatDuration(iteration),
                WritesBeforeDisplayColumnIndex => $"{iteration.WritesBeforeDisplayRead:N0}",
                WritesAfterDisplayColumnIndex => $"{iteration.WritesAfterDisplayRead:N0}",
                DisplayOffsetColumnIndex => $"{iteration.DisplayFrameOffset:N0}",
                
                _ => FormatCountAndRange(iteration, columnIndex)
            };
        }

        protected override (int value, int range, bool clamp) OnRowDataCountAndRange(MetricIteration iteration, int columnIndex)
        {
            switch (columnIndex)
            {
                case WritesBeforeDisplayColumnIndex:
                    if (_HighlightWritesBeforeDisplay && iteration.WritesBeforeDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case WritesAfterDisplayColumnIndex:
                    if (_HighlightWritesAfterDisplay && iteration.WritesAfterDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case DurationColumnIndex:
                    int range = (_Metric == null || _Metric.ThresholdCycles <= 0) ? _MaxCycleCount : _Metric.ThresholdCycles;
                    int cycles = iteration.EndCycleCount - iteration.StartCycleCount;
                    return (value: cycles, range: range, clamp: false);

                default:
                    return (value: -1, range: 1, clamp: false);
            }
        }

        private string FormatDuration(MetricIteration iteration)
        {
            int duration = iteration.EndCycleCount - iteration.StartCycleCount;

            int range = (_Metric == null || _Metric.ThresholdCycles <= 0) ? _MaxCycleCount : _Metric.ThresholdCycles;
            double percentage = (int)double.Round(100.0 * duration / range);
            return $"{duration:N0} ({percentage:F2}%)";
        }

        private static int GetMaxDisplayedCycleCount(List<MetricIteration> iterations)
        {
            if (iterations.Count == 0) return 0;

            // calculate mean
            double sum = 0.0;
            foreach (var iteration in iterations)
                sum += (iteration.EndCycleCount - iteration.StartCycleCount);
            double mean = sum / iterations.Count;

            // calculate standard deviation
            double sumSq = 0.0;
            foreach (var iteration in iterations)
            {
                double d = (iteration.EndCycleCount - iteration.StartCycleCount) - mean;
                sumSq += d * d;
            }
            double sd = Math.Sqrt(sumSq / iterations.Count);

            // find largest duration, ignoring outliers
            int maxDisplayCycleCount = 0;
            foreach (var iteration in iterations)
            {
                int duration = iteration.EndCycleCount - iteration.StartCycleCount;
                if (sd != 0.0)
                {
                    double zScore = (duration - mean) / sd;
                    if (zScore > 3.0)
                        continue; // outlier, so ignore
                }
                maxDisplayCycleCount = Math.Max(maxDisplayCycleCount, duration);
            }

            return maxDisplayCycleCount;
        }

        string[] IGridExporter.GetHeaders()
        {
            var headers = new List<string>();

            headers.Add("Iteration number");
            headers.Add("Start cycle count");
            headers.Add("End cycle count");
            headers.Add("Duration cycles [#]");
            headers.Add("Duration cycles [%]");

            if (_Metric != null && _Metric.DisplayAnalysis)
            {
                headers.Add("Display iteration offset");
                headers.Add("Writes before display [#]");
                headers.Add("Writes before display [%]");
                headers.Add("Writes after display [#]");
                headers.Add("Writes after display [%]");
            }

            int exportHeaderCount = (_Metric != null && _Metric.DisplayAnalysis) ? ExportColumnCount : ExportColumnCount - 5;
            Debug.Assert(headers.Count == exportHeaderCount);

            return headers.ToArray();
        }

        int IGridExporter.GetRowCount()
        {
            return _DataRows.Count;
        }

        string[] IGridExporter.GetRowValues(int rowIndex)
        {
            List<string> rowValues = new();

            var rowData = _DataRows[rowIndex];

            int exportHeaderCount = (_Metric != null && _Metric.DisplayAnalysis) ? ExportColumnCount : ExportColumnCount - 5;
            for (int columnIndex = 0; columnIndex < exportHeaderCount; columnIndex++)
                rowValues.Add(FormatExportCell(rowData, columnIndex));

            return rowValues.ToArray();
        }

        private string FormatExportCell(MetricIteration iteration, int columnIndex)
        {
            return columnIndex switch
            {
                ExportIteractionNumberColumnIndex => iteration.IterationNumber.ToString(),
                ExportStartCycleCountColumnIndex => iteration.StartCycleCount.ToString(),
                ExportEndCycleCountColumnIndex => iteration.EndCycleCount.ToString(),
                ExportDurationColumnIndex => (iteration.EndCycleCount - iteration.StartCycleCount).ToString(),
                ExportDurationPercentageColumnIndex => FormatExportPercentage(iteration.EndCycleCount - iteration.StartCycleCount, _Metric!.ThresholdCycles),
                ExportWriteCountBeforeDisplayColumnIndex => iteration.WritesBeforeDisplayRead.ToString(),
                ExportWritePercentageBeforeDisplayColumnIndex => FormatExportPercentage(iteration.WritesBeforeDisplayRead, iteration.WritesBeforeDisplayRead + iteration.WritesAfterDisplayRead),
                ExportWriteCountAfterDisplayColumnIndex => iteration.WritesAfterDisplayRead.ToString(),
                ExportWritePercentageAfterDisplayColumnIndex => FormatExportPercentage(iteration.WritesAfterDisplayRead, iteration.WritesBeforeDisplayRead + iteration.WritesAfterDisplayRead),
                ExportDisplayOffsetColumnIndex => iteration.DisplayFrameOffset.ToString(),
                _ => string.Empty
            };
        }

        public class CellTemplate : GridViewCellTemplate
        {
            protected override void Paint(
                Graphics graphics,
                Rectangle clipBounds,
                Rectangle cellBounds,
                int rowIndex,
                DataGridViewElementStates cellState,
                object? value,
                object? formattedValue,
                string? errorText,
                DataGridViewCellStyle cellStyle,
                DataGridViewAdvancedBorderStyle advancedBorderStyle,
                DataGridViewPaintParts paintParts)
            {
                if (ColumnIndex != VisualizationColumnIndex)
                {
                    base.Paint(
                        graphics, clipBounds, cellBounds, rowIndex, cellState, value, formattedValue, errorText,
                        cellStyle, advancedBorderStyle, paintParts);
                    return;
                }

                var gridView = (MetricsGridView)DataGridView!;
                var iteration = (MetricIteration)gridView._DataRows[rowIndex];
                var metric = gridView._Metric;
                bool selected = (cellState & DataGridViewElementStates.Selected) != 0;

                // calc cycles extents
                int maxDisplayedCycleCount = gridView._MaxDisplayedCycleCount;

                // draw background
                PaintBackground(graphics, cellBounds, cellStyle, selected);

                // save state & constrain painting within the cell bounds
                var graphicsState = graphics.Save();
                graphics.IntersectClip(cellBounds);

                // draw foreground
                PaintIteration(graphics, cellBounds, cellStyle, gridView, iteration, maxDisplayedCycleCount);

                if (metric != null && metric.DisplayAnalysis)
                    PaintDisplayFrames(graphics, cellBounds, cellStyle, gridView, iteration, maxDisplayedCycleCount);

                if (metric != null && metric.ThresholdCycles > 0)
                    PaintThreshold(graphics, cellBounds, cellStyle, gridView, metric, maxDisplayedCycleCount);

                // restore state
                graphics.Restore(graphicsState);
            }

            private static void PaintBackground(Graphics graphics, Rectangle cellBounds, DataGridViewCellStyle cellStyle, bool selected)
            {
                var color = cellStyle.BackColor;
                if (selected)
                    color = cellStyle.SelectionBackColor;
                var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, cellBounds);
            }

            private static void PaintIteration(
                Graphics graphics,
                Rectangle cellBounds,
                DataGridViewCellStyle cellStyle,
                MetricsGridView gridView, 
                MetricIteration iteration, 
                int maxDisplayedCycleCount)
            {
                // measure
                int width = CyclesToCell(iteration.EndCycleCount - iteration.StartCycleCount, maxDisplayedCycleCount, cellBounds);
                int margin = cellBounds.Height / 8;
                var rect = new Rectangle(
                    cellBounds.Left,
                    cellBounds.Top + margin,
                    width,
                    cellBounds.Height - margin - margin);

                // paint bar
                var color = Blend(cellStyle.SelectionBackColor, cellStyle.BackColor, 0.5f);
                using var brush = new SolidBrush(color);
                graphics.FillRectangle(brush, rect);
            }

            private static void PaintDisplayFrames(
                Graphics graphics, 
                Rectangle cellBounds,
                DataGridViewCellStyle cellStyle,
                MetricsGridView gridView, 
                MetricIteration iteration, 
                int maxDisplayedCycleCount)
            {
                // measure
                int arrowHeadLength = cellBounds.Height / 3;
                int arrowHeadHalfHeight = cellBounds.Height / 4;

                int arrowCenterY = cellBounds.Top + cellBounds.Height / 2;
                int arrowTop = arrowCenterY - arrowHeadHalfHeight;
                int arrowBottom = arrowCenterY + arrowHeadHalfHeight;

                // font
                int fontHeight = 2 * gridView.Font.Height / 3;
                using var font = new System.Drawing.Font(gridView.Font.FontFamily, fontHeight, GraphicsUnit.Pixel);
                StringFormat textFormat = new StringFormat
                {
                    Alignment = StringAlignment.Center,
                    LineAlignment = StringAlignment.Center,
                    Trimming = StringTrimming.EllipsisCharacter,
                    FormatFlags = StringFormatFlags.NoWrap
                };

                // pens & brushes
                var primaryColor = cellStyle.ForeColor;
                using var primaryThinPen = new Pen(primaryColor, 1);
                using var primaryThickPen = new Pen(primaryColor, 3);
                using var primaryBrush = new SolidBrush(primaryColor);

                var secondaryColor = Blend(cellStyle.ForeColor, cellStyle.BackColor, 0.5);
                using var secondaryThinPen = new Pen(secondaryColor, 1);
                using var secondaryThickPen = new Pen(secondaryColor, 3);
                using var secondaryBrush = new SolidBrush(secondaryColor);

                for (int i = 0; i < iteration.DisplayFrameSpans.Length; i++)
                {
                    var displayFrameSpan = iteration.DisplayFrameSpans[i];

                    // select pens and brushes
                    bool primaryDisplaySpan = (i == iteration.DisplayFrameIndex);
                    var thinPen = primaryDisplaySpan ? primaryThinPen : secondaryThinPen;
                    var thickPen = primaryDisplaySpan ? primaryThickPen : secondaryThickPen;
                    var brush = primaryDisplaySpan ? primaryBrush : secondaryBrush;

                    // measure
                    int startCycleCount = displayFrameSpan.StartCycleCount - iteration.StartCycleCount;
                    int endCycleCount = displayFrameSpan.EndCycleCount - iteration.StartCycleCount;

                    int arrowLeft = cellBounds.Left + CyclesToCell(startCycleCount, maxDisplayedCycleCount, cellBounds);
                    int arrowRight = cellBounds.Left + CyclesToCell(endCycleCount, maxDisplayedCycleCount, cellBounds);
                    int arrowCenterX = (arrowRight + arrowLeft) / 2;

                    string frameNumberText = displayFrameSpan.FrameNumber.ToString();
                    int textHalfWidth = TextRenderer.MeasureText(frameNumberText, font).Width / 2;
                    int arrowCenterLeft = arrowCenterX - textHalfWidth;
                    int arrowCenterRight = arrowCenterX + textHalfWidth;

                    // paint line
                    graphics.DrawLine(thickPen,
                        arrowLeft + arrowHeadLength, arrowCenterY,
                        arrowCenterLeft , arrowCenterY);

                    graphics.DrawLine(thickPen,
                        arrowCenterRight, arrowCenterY,
                        arrowRight - arrowHeadLength, arrowCenterY);

                    // paint left arrow head
                    Point[] leftArrowHeadPoints = [
                        new() { X = arrowLeft, Y = arrowCenterY },
                        new() { X = arrowLeft + arrowHeadLength, Y = arrowTop },
                        new() { X = arrowLeft + arrowHeadLength, Y = arrowBottom },
                    ];

                    graphics.FillPolygon(brush, leftArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawPolygon(thinPen, leftArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                    // paint right arrow head
                    Point[] rightArrowHeadPoints = [
                        new() { X = arrowRight, Y = arrowCenterY },
                        new() { X = arrowRight - arrowHeadLength, Y = arrowTop },
                        new() { X = arrowRight - arrowHeadLength, Y = arrowBottom },
                    ];

                    graphics.FillPolygon(brush, rightArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                    graphics.DrawPolygon(thinPen, rightArrowHeadPoints);
                    graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighSpeed;

                    // paint display frame number
                    var captionRect = new Rectangle(arrowLeft, cellBounds.Top, arrowRight - arrowLeft, cellBounds.Height);
                    graphics.DrawString(frameNumberText, font, brush, captionRect, textFormat);
                }
            }

            private static void PaintThreshold(
                Graphics graphics,
                Rectangle cellBounds,
                DataGridViewCellStyle cellStyle,
                MetricsGridView gridView,
                Metric? metric,
                int maxDisplayedCycleCount)
            {
                // measure
                int thresholdX = cellBounds.Left + CyclesToCell(metric!.ThresholdCycles, maxDisplayedCycleCount, cellBounds);

                // paint
                var backColor = cellStyle.BackColor;
                var foreColor = backColor.GetBrightness() > 0.5 ? Color.Red : Color.DarkRed;
                using var pen = new Pen(foreColor, 2);
                graphics.DrawLine(pen, thresholdX, cellBounds.Top, thresholdX, cellBounds.Bottom);
            }

            private static int CyclesToCell(int value, int range, Rectangle cellBounds)
            {
                int padding = cellBounds.Height / 2;
                return (int)double.Round((double)(cellBounds.Width - padding) * (double)value / (double)range);
            }
        }

        private Metric? _Metric = null;
        private bool _HighlightWritesBeforeDisplay = false;
        private bool _HighlightWritesAfterDisplay = false;
        private int _MaxCycleCount = 0;
        private int _MaxDisplayedCycleCount = 0;
        private ReentrancyGuard _ReentrancyGuard = new();
    }
}
