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
    internal class FramesGridView : GridView<FrameAnalysis.Frame>, IGridExporter
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

        public FramesGridView() : base(System.Windows.Forms.SelectionMode.One, (ButtonType)0)
        {
            var cellTemplate = new CellTemplate();
            AddColumn("FrameNumber", "Frame [#]", cellTemplate);
            AddColumn("Duration", "Duration [#, %]", cellTemplate);
            AddColumn("DisplayFrameOffset", "Offset [#]", cellTemplate);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes before display [#, %]", cellTemplate);
            AddColumn("ScreenWritesBeforeDisplayRead", "Writes after display [#, %]", cellTemplate);
            AddColumn("Visualization", "Visualization", cellTemplate);

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
            List<FrameAnalysis.Frame> frames, 
            FrameSettings? frameSettings,
            bool highlightWritesBeforeDisplay,
            bool highlightWritesAfterDisplay)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            _FrameSettings = frameSettings;
            _HighlightWritesBeforeDisplay = highlightWritesBeforeDisplay;
            _HighlightWritesAfterDisplay = highlightWritesAfterDisplay;
            _MaxDisplayedCycleCount = CalcMaxDisplayedCycleCount(frames);

            // set data
            if (frames.Count > 0)
                base.SetRowsData(frames);
            else
                base.Clear();
        }

        public void SelectRange(int analysisFrom, int analysisTo)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            foreach (var frame in base._DataRows)
            {
                if (frame.StartCycleCount == analysisFrom &&
                    frame.EndCycleCount == analysisTo)
                {
                    SelectRow(frame, scrollIntoView: true);
                    return;
                }
            }

            base.ClearSelection();
        }

        protected override void OnSelectionChange(object? sender, FrameAnalysis.Frame? frame)
        {
            using var token = _ReentrancyGuard.TryEnter();
            if (token == null) return;

            var form = FindForm() as BeebPerfForm;
            if (form is null) return;

            if (frame != null)
                form.SetAnalysisRange(frame.StartCycleCount, frame.EndCycleCount);
            else
                form.SetAnalysisRange(0, int.MaxValue);
        }

        protected override int OnSortCompare(FrameAnalysis.Frame a, FrameAnalysis.Frame b, int columnIndex)
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

        protected override string OnFormatRowData(FrameAnalysis.Frame frame, int columnIndex, int rowIndex)
        {
            return columnIndex switch
            {
                FrameNumberColumnIndex => frame.FrameNumber.ToString(),
                DurationColumnIndex => FormatDuration(frame),
                WritesBeforeDisplayColumnIndex => $"{frame.WritesBeforeDisplayRead:N0}",
                WritesAfterDisplayColumnIndex => $"{frame.WritesAfterDisplayRead:N0}",
                DisplayOffsetColumnIndex => $"{frame.DisplayFrameOffset:N0}",
                
                _ => FormatCountAndRange(frame, columnIndex)
            };
        }

        protected override (int value, int range, bool clamp) OnRowDataCountAndRange(FrameAnalysis.Frame frame, int columnIndex)
        {
            switch (columnIndex)
            {
                case WritesBeforeDisplayColumnIndex:
                    if (_HighlightWritesBeforeDisplay && frame.WritesBeforeDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case WritesAfterDisplayColumnIndex:
                    if (_HighlightWritesAfterDisplay && frame.WritesAfterDisplayRead > 0)
                        return (value: 2, range: 1, clamp: false); // highlight cell
                    else
                        return (value: -1, range: 1, clamp: false); // no highlight

                case DurationColumnIndex:
                    int cycles = frame.EndCycleCount - frame.StartCycleCount;
                    return (value: cycles, range: _FrameSettings!.ThresholdCycles, clamp: false);

                default:
                    return (value: -1, range: 1, clamp: false);
            }
        }

        private string FormatDuration(FrameAnalysis.Frame frame)
        {
            int duration = frame.EndCycleCount - frame.StartCycleCount;
            int range = _FrameSettings!.ThresholdCycles;
            double percentage = (int)double.Round(100.0 * duration / range);
            return $"{duration:N0} ({percentage:F2}%)";
        }

        private int CalcMaxDisplayedCycleCount(List<FrameAnalysis.Frame> frames)
        {
            if (frames.Count == 0) return 0;

            // calculate mean
            double sum = 0.0;
            foreach (var frame in frames)
                sum += (frame.EndCycleCount - frame.StartCycleCount);
            double mean = sum / frames.Count;

            // calculate standard deviation
            double sumSq = 0.0;
            foreach (var frame in frames)
            {
                double d = (frame.EndCycleCount - frame.StartCycleCount) - mean;
                sumSq += d * d;
            }
            double sd = Math.Sqrt(sumSq / frames.Count);

            // find largest duration, ignoring outliers
            int maxDisplayCycleCount = 0;
            foreach (var frame in frames)
            {
                int duration = frame.EndCycleCount - frame.StartCycleCount;
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
            string[] headers = [
                "Frame Number",
                "Start cycle count",
                "End cycle count",
                "Duration cycles [#]",
                "Duration cycles [%]",
                "Display frame offset",
                "Writes before display [#]",
                "Writes before display [%]",
                "Writes after display [#]",
                "Writes after display [%]"
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

        private string FormatExportCell(FrameAnalysis.Frame frame, int columnIndex)
        {
            return columnIndex switch
            {
                ExportFrameNumberColumnIndex => frame.FrameNumber.ToString(),
                ExportStartCycleCountColumnIndex => frame.StartCycleCount.ToString(),
                ExportEndCycleCountColumnIndex => frame.EndCycleCount.ToString(),
                ExportDurationColumnIndex => (frame.EndCycleCount - frame.StartCycleCount).ToString(),
                ExportDurationPercentageColumnIndex => FormatExportPercentage(frame.EndCycleCount - frame.StartCycleCount, _FrameSettings!.ThresholdCycles),
                ExportWriteCountBeforeDisplayColumnIndex => frame.WritesBeforeDisplayRead.ToString(),
                ExportWritePercentageBeforeDisplayColumnIndex => FormatExportPercentage(frame.WritesBeforeDisplayRead, frame.WritesBeforeDisplayRead + frame.WritesAfterDisplayRead),
                ExportWriteCountAfterDisplayColumnIndex => frame.WritesAfterDisplayRead.ToString(),
                ExportWritePercentageAfterDisplayColumnIndex => FormatExportPercentage(frame.WritesAfterDisplayRead, frame.WritesBeforeDisplayRead + frame.WritesAfterDisplayRead),
                ExportDisplayOffsetColumnIndex => frame.DisplayFrameOffset.ToString(),
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

                var gridView = (FramesGridView)DataGridView!;
                var frame = (FrameAnalysis.Frame)gridView._DataRows[rowIndex];
                var frameSettings = gridView._FrameSettings;
                bool selected = (cellState & DataGridViewElementStates.Selected) != 0;

                // calc cycles extents
                int maxCycleCount = gridView._MaxDisplayedCycleCount;
                if (frameSettings != null && frameSettings.ThresholdCycles > 0)
                    maxCycleCount = int.Max(gridView._MaxDisplayedCycleCount, frameSettings.ThresholdCycles);

                // draw background
                PaintBackground(graphics, cellBounds, cellStyle, selected);

                // save state & constrain painting within the cell bounds
                var graphicsState = graphics.Save();
                graphics.IntersectClip(cellBounds);

                // draw foreground
                PaintFrame(graphics, cellBounds, cellStyle, gridView, frame, maxCycleCount);
                PaintDisplayFrames(graphics, cellBounds, cellStyle, gridView, frame, maxCycleCount);
                PaintThreshold(graphics, cellBounds, cellStyle, gridView, frameSettings, maxCycleCount);

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

            private static void PaintFrame(
                Graphics graphics,
                Rectangle cellBounds,
                DataGridViewCellStyle cellStyle,
                FramesGridView gridView, 
                FrameAnalysis.Frame frame, 
                int maxCycleCount)
            {
                // measure
                int width = CyclesToCell(frame.EndCycleCount - frame.StartCycleCount, maxCycleCount, cellBounds);
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
                FramesGridView gridView, 
                FrameAnalysis.Frame frame, 
                int maxCycleCount)
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
                var color = cellStyle.ForeColor;
                using var thinPen = new Pen(color, 1);
                using var thickPen = new Pen(color, 3);
                using var brush = new SolidBrush(color);

                foreach (var displayFrameSpan in frame.DisplayFrameSpans)
                {
                    // measure
                    int startCycleCount = displayFrameSpan.StartCycleCount - frame.StartCycleCount;
                    int endCycleCount = displayFrameSpan.EndCycleCount - frame.StartCycleCount;

                    int arrowLeft = cellBounds.Left + CyclesToCell(startCycleCount, maxCycleCount, cellBounds);
                    int arrowRight = cellBounds.Left + CyclesToCell(endCycleCount, maxCycleCount, cellBounds);
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
                FramesGridView gridView,
                FrameSettings? frameSettings,
                int maxCycleCount)
            {
                if (frameSettings != null && frameSettings.ThresholdCycles > 0)
                {
                    // measure
                    int thresholdX = cellBounds.Left + CyclesToCell(frameSettings.ThresholdCycles, maxCycleCount, cellBounds);

                    // paint
                    var backColor = cellStyle.BackColor;
                    var foreColor = backColor.GetBrightness() > 0.5 ? Color.Red : Color.DarkRed;
                    using var pen = new Pen(foreColor, 2);
                    graphics.DrawLine(pen, thresholdX, cellBounds.Top, thresholdX, cellBounds.Bottom);
                }
            }

            private static int CyclesToCell(int value, int range, Rectangle cellBounds)
            {
                int padding = cellBounds.Height / 2;
                return (int)double.Round((double)(cellBounds.Width - padding) * (double)value / (double)range);
            }
        }

        private FrameSettings? _FrameSettings = null;
        private bool _HighlightWritesBeforeDisplay = false;
        private bool _HighlightWritesAfterDisplay = false;
        private int _MaxDisplayedCycleCount = 0;
        private ReentrancyGuard _ReentrancyGuard = new();
    }
}
