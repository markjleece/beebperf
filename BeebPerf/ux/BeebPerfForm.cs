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

using BeebPerf.model;
using BeebPerf.operation;
using BeebPerf.Properties;
using System.Resources;

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm()
        {
            InitializeComponent();
            UpdateState();

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
            FlameImage = (Image)resources.GetObject("flame.Image")!;
        }

        private void BeebPerfForm_Load(object sender, EventArgs e)
        {
            string filePathName = AppSettings.Instance.RecentFilePathName;
            if (filePathName != string.Empty && File.Exists(filePathName))
            {
                OpenPerfFile(filePathName);
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Path.GetDirectoryName(AppSettings.Instance.RecentFilePathName),
                Filter = "Beeb .perf files (*.perf)|*.perf",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                AppSettings.Instance.RecentFilePathName = openFileDialog.FileName;
                OpenPerfFile(openFileDialog.FileName);
            }
        }

        private void OpenPerfFile(string filePathName)
        {
            var openOperation = new OpenOperation(filePathName, _Model);
            if (_UndoRedoHistory.Execute(openOperation))
            {
                InstructionSet = _Model.InstructionSet;

                _CPUAnalysis.StaticAnalysis(_Model);
                _CPUAnalysis.DynamicAnalysis(_Model, startCycleCount:0, endCycleCount: Int32.MaxValue);
                UpdateState();

                // populate hot routines
                hotRoutinesDataGrid.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                hotRoutinesDataGrid.Clear();
                foreach (var routine in _CPUAnalysis.HotRoutines)
                {
                    hotRoutinesDataGrid.AddRoutine(routine);
                }

                // populate routines
                routinesDataGrid.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                routinesDataGrid.Clear();
                foreach (var routine in _CPUAnalysis.HotRoutines)
                {
                    routinesDataGrid.AddRoutine(routine);
                }

                // populate call tree
                callTreeControl.Clear();

                if (_CPUAnalysis!.ProgramCallTree != null)
                    callTreeControl.AddCallTree(_CPUAnalysis!.ProgramCallTree!);

                if (_CPUAnalysis!.NonMaskableInterruptCallTree != null)
                    callTreeControl.AddCallTree(_CPUAnalysis!.NonMaskableInterruptCallTree!);

                if (_CPUAnalysis!.MaskableInterruptCallTree != null)
                    callTreeControl.AddCallTree(_CPUAnalysis!.MaskableInterruptCallTree!);

                callTreeControl.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
            }
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanUndo())
                _UndoRedoHistory.Undo();
            UpdateState();
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanRedo())
                _UndoRedoHistory.Redo();
            UpdateState();
        }

        private void zoomInButton_Click(object sender, EventArgs e)
        {
        }

        private void zoomOutButton_Click(object sender, EventArgs e)
        {

        }

        private void settingsButton_Click(object sender, EventArgs e)
        {

        }

        private void helpButton_Click(object sender, EventArgs e)
        {

        }

        private void UpdateState()
        {
            undoButton.Enabled = _UndoRedoHistory.CanUndo();
            redoButton.Enabled = _UndoRedoHistory.CanRedo();
        }

        private UndoRedoHistory _UndoRedoHistory = new();
        private Model _Model = new();
        private CPUAnalysis _CPUAnalysis = new();

        public void SetSelectedRoutine(Routine routine, CallStack? callStack)
        {
            _SelectedRoutine = routine;
            _SelectedCallStack = callStack;

            List<InstructionMetrics> instructionMetrics = _CPUAnalysis.CalculateInstructionMetrics(routine, callStack);
            codeView.SetCode(routine, instructionMetrics, _CPUAnalysis.RoutinesByAddress, _Model.Labels, _Model.InstructionSet!);
        }

        public void ClearSelectedRoutine()
        {
            _SelectedRoutine = null;
            _SelectedCallStack = null;
        }

        public Image FlameImage;
        public InstructionSet? InstructionSet;
        private Routine? _SelectedRoutine;
        private CallStack? _SelectedCallStack;
    }
}
