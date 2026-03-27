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

using BeebPerf.ux;
using System.Text;

namespace BeebPerf
{
    public interface IGridExporter
    {
        string[] GetHeaders();
        int GetRowCount();
        string[] GetRowValues(int rowIndex);
    }

    public class Exporter
    {
        static public void CopyToClipboard(BeebPerfForm form, Bitmap bitmap, float aspectRatio)
        {
            // round aspect ratio to nearest factor of two, so pixels end up square
            if (Math.Abs(aspectRatio - 0.5) < Math.Abs(aspectRatio - 1.0))
                aspectRatio = 0.5f;
            else if (Math.Abs(aspectRatio - 1.0) < Math.Abs(aspectRatio - 2.0))
                aspectRatio = 1.0f;
            else
                aspectRatio = 2.0f;

            // calc new width
            int newWidth = (int)double.Round(aspectRatio * bitmap.Width);

            // create new stretched bitmap
            var newBitmap = new Bitmap(newWidth, bitmap.Height);
            using (Graphics graphics = Graphics.FromImage(newBitmap))
            {
                var bitmapRect = new Rectangle(0, 0, newWidth, bitmap.Height);
                graphics.DrawImage(bitmap, bitmapRect);
            }

            Clipboard.SetImage(newBitmap);
        }

        static public void CopyToClipboard(BeebPerfForm form, IGridExporter gridExporter)
        {
            string text = FormatData(gridExporter, '\t');
            Clipboard.SetText(text);
        }

        static public void ExportCSVFile(BeebPerfForm form, IGridExporter gridExporter)
        {
            SaveFileDialog exportDialog = new()
            {
                Title = "Export",
                Filter = "CSV Files (*.csv)|*.csv|All Files (*.*)|*.*",
                DefaultExt = "csv",
                AddExtension = true,
                OverwritePrompt = true,
                InitialDirectory = form.RecentExportFolderPath
            };

            if (exportDialog.ShowDialog() == DialogResult.OK)
            {
                form.RecentExportFolderPath = Path.GetDirectoryName(exportDialog.FileName)!;

                string csvText = FormatData(gridExporter, ',');

                try
                {
                    File.WriteAllText(exportDialog.FileName, csvText);
                }
                catch (Exception e)
                {
                    MessageBox.Show(
                        form,
                        $"An error occurred writing to '{exportDialog.FileName}'.\nDetail: {e.Message}",
                        "File Error",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Error);
                }
            }
        }

        static private string FormatData(IGridExporter gridExporter, char delimeter)
        {
            var sb = new StringBuilder(4096);

            // headers
            string[] headers = gridExporter.GetHeaders();
            bool first = true;
            foreach (var header in headers)
            {
                if (!first)
                    sb.Append(delimeter);
                else
                    first = false;

                sb.Append(FormatValue(header, delimeter));
            }

            sb.Append("\r\n");

            // rows
            int rowCount = gridExporter.GetRowCount();
            for (int i = 0; i < rowCount; i++)
            {
                // row values
                string[] rowValues = gridExporter.GetRowValues(i);
                first = true;
                foreach (var rowValue in rowValues)
                {
                    if (!first)
                        sb.Append(delimeter);
                    else
                        first = false;

                    sb.Append(FormatValue(rowValue, delimeter));
                }

                sb.Append("\r\n");
            }

            return sb.ToString();
        }
        
        static private string FormatValue(string value, char delimiter)
        {
            value = value.Trim();

            if (value.StartsWith('$'))
                value = '\'' + value;

            if (value.Length == 0)
                return "\"\"";

            if (value.Contains(delimiter) ||
                value.Contains('"') ||
                value.Contains('\r') ||
                value.Contains('\n'))
                return "\"" + value.Replace("\"", "\"\"") + "\"";

            return value;
        }
    }
}