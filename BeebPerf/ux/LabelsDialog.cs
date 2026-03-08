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

namespace BeebPerf.ux
{
    public partial class LabelsDialog : Form
    {
        public List<model.LabelsFile> LabelsFiles;
        public string RecentLabelsFilePathName;

        public LabelsDialog(List<model.LabelsFile> labelsFiles, string recentLabelsFilePathName)
        {
            InitializeComponent();

            // deep copy arguments. The copies are modified by actions within the dialog
            LabelsFiles = new();
            foreach (var labelsFile in labelsFiles)
                LabelsFiles.Add(labelsFile.Clone());
            RecentLabelsFilePathName = recentLabelsFilePathName;

            // set button tool-tips
            _ButtonTooltip.SetToolTip(addButton, "Add a labels file. A BeebAsm.exe output or -labels option file.");
            _ButtonTooltip.SetToolTip(removeButton, "Remove selected labels file. The contained labels will be unloaded.");
            _ButtonTooltip.SetToolTip(reloadButton, "Reload selected labels file. The contained labels will be refreshed.");

            // update button states when grid selection changes
            labelsGridView.SelectionChanged += (s, e) =>
            {
                UpdateButtonStates();
            };

            // add grid columns
            labelsGridView.Columns.Add(new DataGridViewTextBoxColumn {
                ReadOnly = true,
                HeaderText = "Labels file",
                Name = "LabelsFileColumn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                HeaderCell = { ToolTipText = "Supports BeebAsm.exe output and -labels option files" }
            });

            labelsGridView.Columns.Add(new DataGridViewTextBoxColumn
            {
                ReadOnly = true,
                HeaderText = "Status",
                Name = "LabelsStatusColumn",
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                HeaderCell = { ToolTipText = "Labels file status" }
            });

            labelsGridView.Columns.Add(new DataGridViewCheckBoxColumn {
                ReadOnly = false,
                HeaderText = "Labels Enabled",
                Name = "LabelsEnabledColumn",
                ThreeState = false,
                SortMode = DataGridViewColumnSortMode.NotSortable,
                AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells,
                DefaultCellStyle = { Alignment = DataGridViewContentAlignment.MiddleCenter },
                HeaderCell = { Style = { Alignment = DataGridViewContentAlignment.MiddleCenter }, ToolTipText = "Enable/disable contained labels" }
            });

            // make the grid check boxes selectable
            labelsGridView.ReadOnly = false;
            labelsGridView.EditMode = DataGridViewEditMode.EditOnEnter;
            labelsGridView.CellContentClick += (s, e) =>
            {
                if (e.ColumnIndex == 2) 
                    labelsGridView.CommitEdit(DataGridViewDataErrorContexts.Commit);
            };

            labelsGridView.CellValueChanged += (s, e) =>
            {
                if (labelsGridView.Columns[e.ColumnIndex] is DataGridViewCheckBoxColumn)
                {
                    bool newValue = (bool)labelsGridView[e.ColumnIndex, e.RowIndex].Value!;
                    LabelsFiles[e.RowIndex].Enabled = newValue;
                }
            };

            // show tool-tips if the text does not fit
            labelsGridView.CellToolTipTextNeeded += (s, e) =>
            {
                if (e.RowIndex < 0 || e.ColumnIndex < 0)
                    return;

                var cell = labelsGridView[e.ColumnIndex, e.RowIndex];
                var value = cell.Value?.ToString();
                if (string.IsNullOrEmpty(value))
                    return;

                // measure rendered text width
                var style = cell.InheritedStyle;
                using var g = labelsGridView.CreateGraphics();
                var textSize = g.MeasureString(value, style.Font);

                // compare to actual cell width (minus padding)
                int cellWidth = cell.Size.Width - 4;

                if (textSize.Width > cellWidth)
                    e.ToolTipText = value;
            };

            // populate the grid rows
            foreach (var labelsFile in LabelsFiles)
                labelsGridView.Rows.Add(labelsFile.FileName, ToStatusString(labelsFile), labelsFile.Enabled);

            UpdateButtonStates();
        }

        private void UpdateButtonStates()
        {
            bool enabled = (labelsGridView.SelectedRows.Count == 1);
            if (enabled)
            {
                var labelsFile = LabelsFiles[labelsGridView.SelectedRows[0].Index];
                enabled = !labelsFile.FileName.StartsWith('<'); // embedded labels?
            }
            reloadButton.Enabled = enabled;
            removeButton.Enabled = enabled;
        }

        private void CloseButton_Click(object sender, EventArgs e)
        {
            Close();
        }

        private void AddButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Path.GetDirectoryName(RecentLabelsFilePathName),
                Filter = "BeebAsm.exe output or -labels option files (*.*)|*.*",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // update recent file path
                string filePathName = openFileDialog.FileName;
                RecentLabelsFilePathName = filePathName;

                // load labels file
                var labelsFile = new LabelsFileReader().ReadFile(filePathName);

                // add row and update public state
                labelsGridView.Rows.Add(filePathName, ToStatusString(labelsFile), labelsFile.Enabled);
                LabelsFiles.Add(labelsFile);
            }
        }

        private void RemoveButton_Click(object sender, EventArgs e)
        {
            if (labelsGridView.SelectedRows.Count == 1)
            {
                // remove row and update public state
                var rowIndex = labelsGridView.SelectedRows[0].Index;
                LabelsFiles.RemoveAt(rowIndex);
                labelsGridView.Rows.RemoveAt(rowIndex);
            }
        }

        private void ReloadButton_Click(object sender, EventArgs e)
        {
            if (labelsGridView.SelectedRows.Count == 1)
            {
                // reload labels file
                var rowIndex = labelsGridView.SelectedRows[0].Index;
                var reloadedLabelsFile = new LabelsFileReader().ReadFile(LabelsFiles[rowIndex].FileName);

                // replace selected row's cell values
                var row = labelsGridView.Rows[rowIndex];
                row.Cells[0].Value = reloadedLabelsFile.FileName;
                row.Cells[1].Value = ToStatusString(reloadedLabelsFile);
                row.Cells[2].Value = reloadedLabelsFile.Enabled;

                // update public state
                LabelsFiles[rowIndex] = reloadedLabelsFile;
            }
        }

        private string ToStatusString(model.LabelsFile labelsFile)
        {
            return labelsFile.Status switch
            {
                model.LabelsFileStatus.Loaded => $"{labelsFile.Labels.Count} labels",
                model.LabelsFileStatus.Error_FileNotFound => $"file not found!",
                model.LabelsFileStatus.Error_InvalidFileFormat => $"invalid file format!",
                model.LabelsFileStatus.Error_Other => $"file load error",
                _ => "unknown"
            };
        }

        private readonly ToolTip _ButtonTooltip = new ToolTip();
    }
}
