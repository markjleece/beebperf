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

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm() : base()
        {
            InitializeComponent();
            UpdateToolbarState();

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
            FlameImage = (Image)resources.GetObject("flame.Image")!;

            FormClosing += BeebPerfForm_FormClosing;
            Resize += BeebPerfForm_Resize;

            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;
        }

        private void BeebPerfForm_Resize(object? sender, EventArgs e)
        {
            ResizeSpinner();
        }

        private void BeebPerfForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveAppState();
        }

        private void BeebPerfForm_Load(object sender, EventArgs e)
        {
            RestoreAppState();

            if (_RecentFilePathName.Length > 0 && File.Exists(_RecentFilePathName))
            {
                OpenPerfFile(_RecentFilePathName);
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Path.GetDirectoryName(_RecentFilePathName),
                Filter = "Beeb .perf files (*.perf)|*.perf",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                _RecentFilePathName = openFileDialog.FileName;
                OpenPerfFile(openFileDialog.FileName);
            }
        }

        private async void OpenPerfFile(string filePathName)
        {
            var openOperation = new OpenOperation(filePathName, _Model);

            SetState(AppStateFlags.Loading);
            bool success = await _UndoRedoHistory.Execute(openOperation);
            ClearState(AppStateFlags.Loading);

            if (success)
            {
                InstructionSet = _Model.InstructionSet;

                SetState(AppStateFlags.StaticCPUAnalysis);
                success = await _CPUAnalysis.StaticAnalysis(_Model);
                ClearState(AppStateFlags.StaticCPUAnalysis);

                if (success)
                {
                    timelineView.SetDuration(_CPUAnalysis.EndCycleCount);
                    DynamicAnalysis(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);
                }
            }
        }

        public async void DynamicAnalysis(int startCycleCount, int endCycleCount)
        {
            SetState(AppStateFlags.DynamicCPUAnalysis);
            var success = await _CPUAnalysis.DynamicAnalysis(startCycleCount, endCycleCount);
            ClearState(AppStateFlags.DynamicCPUAnalysis);

            // populate routines
            foreach (var routine in _CPUAnalysis.HotRoutines)
            {
                routinesView.AddRoutine(routine);
            }

            int maxExecutionCount = 0;
            foreach (var routine in _CPUAnalysis.HotRoutines)
            {
                if (maxExecutionCount < routine.AggregateMetrics.ExecutionCount)
                    maxExecutionCount = routine.AggregateMetrics.ExecutionCount;
            }

            routinesView.MaxExecutionCount = maxExecutionCount;
            routinesView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;

            // populate call tree
            callTreeView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;

            if (_CPUAnalysis!.ProgramCallTree != null)
                callTreeView.AddCallTree(_CPUAnalysis!.ProgramCallTree!);

            if (_CPUAnalysis!.NonMaskableInterruptCallTree != null)
                callTreeView.AddCallTree(_CPUAnalysis!.NonMaskableInterruptCallTree!);

            if (_CPUAnalysis!.MaskableInterruptCallTree != null)
                callTreeView.AddCallTree(_CPUAnalysis!.MaskableInterruptCallTree!);

            callTreeView.ShowHotPaths();

            // populate flame graph
            if (_CPUAnalysis!.ProgramCallTree != null)
                flameGraphView.AddCallTree(_CPUAnalysis!.ProgramCallTree!);

            if (_CPUAnalysis!.NonMaskableInterruptCallTree != null)
                flameGraphView.AddCallTree(_CPUAnalysis!.NonMaskableInterruptCallTree!);

            if (_CPUAnalysis!.MaskableInterruptCallTree != null)
                flameGraphView.AddCallTree(_CPUAnalysis!.MaskableInterruptCallTree!);

            // caller/callee
            callerCalleeView.Initialize(
                _CPUAnalysis.GetCallerMetrics,
                _CPUAnalysis.GetCalleeMetrics,
                _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount);

            // code view
            codeView.Initialize(
                _CPUAnalysis.CalculateInstructionMetrics,
                _CPUAnalysis.RoutinesByAddress,
                _Model.Labels,
                _Model.InstructionSet!);
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanUndo())
                _UndoRedoHistory.Undo();
            UpdateToolbarState();
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanRedo())
                _UndoRedoHistory.Redo();
            UpdateToolbarState();
        }

        private void zoomInButton_Click(object sender, EventArgs e)
        {
            timelineView.ZoomIn();
            UpdateToolbarState();
        }

        private void zoomOutButton_Click(object sender, EventArgs e)
        {
            timelineView.ZoomOut();
            UpdateToolbarState();
        }

        private void selectAllButton_Click(object sender, EventArgs e)
        {
            timelineView.SelectAll();
            UpdateToolbarState();
        }

        private void flipViewButton_Click(object sender, EventArgs e)
        {
            flameGraphView.FlipView();
        }

        private void settingsButton_Click(object sender, EventArgs e)
        {

        }

        private void helpButton_Click(object sender, EventArgs e)
        {

        }

        private UndoRedoHistory _UndoRedoHistory = new();
        private Model _Model = new();
        private CPUAnalysis _CPUAnalysis = new();

        public void SetSelectedRoutine(Object? sender, Routine routine, CallStack? callStack)
        {
            if (routine == _SelectedRoutine && callStack == _SelectedCallStack)
                return;

            if (callStack == null)
            {
                var elements = routine.MetricsByStack.ToList();
                elements.Sort((a, b) => (b.Value.InclusiveCycleCount - a.Value.InclusiveCycleCount));
                callStack = elements.First().Key;
            }

            _SelectedRoutine = routine;
            _SelectedCallStack = callStack;

            bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
            codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null);

            if (sender != routinesView)
                routinesView.SelectRoutine(routine);

            if (sender != callerCalleeView)
                callerCalleeView.SelectRoutine(routine);

            if (sender != callTreeView)
                callTreeView.SelectRoutine(routine, callStack);

            if (sender != flameGraphView)
                flameGraphView.SelectRoutine(routine, callStack);
        }

        public void ClearSelectedRoutine(Object? sender)
        {
            if (_SelectedRoutine == null && _SelectedCallStack == null)
                return;

            _SelectedRoutine = null;
            _SelectedCallStack = null;

            if (sender != routinesView)
                routinesView.ClearSelection();

            if (sender != callerCalleeView)
                callerCalleeView.Clear();

            if (sender != callTreeView)
                callTreeView.ClearSelection();

            if (sender != flameGraphView)
                flameGraphView.ClearSelection();

            codeView.Clear();
        }


        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            UpdateToolbarState();

            if (_SelectedRoutine != null)
            {
                bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
                codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null);
            }
        }

        private void SaveAppState()
        {
            var bounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
            var windowState = (WindowState == FormWindowState.Minimized) ? FormWindowState.Normal : WindowState;
            Properties.Settings.Default.WindowLocation = bounds.Location;
            Properties.Settings.Default.WindowSize = bounds.Size;
            Properties.Settings.Default.WindowState = (int)windowState;
            Properties.Settings.Default.RecentFilePathName = _RecentFilePathName;
            Properties.Settings.Default.Save();
        }

        private void RestoreAppState()
        {
            _RecentFilePathName = Properties.Settings.Default.RecentFilePathName;

            var location = Properties.Settings.Default.WindowLocation;
            var size = Properties.Settings.Default.WindowSize;
            var state = Properties.Settings.Default.WindowState;

            var screenBounds = Screen.FromPoint(location).WorkingArea;
            if (!screenBounds.Contains(new Rectangle(location, size)))
                location = new Point(100, 100);

            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = size;
            WindowState = (FormWindowState)state;
        }

        private void UpdateToolbarState()
        {
            openButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            undoButton.Enabled = _UndoRedoHistory.CanUndo();
            redoButton.Enabled = _UndoRedoHistory.CanRedo();
            zoomInButton.Enabled = timelineView.CanZoomIn();
            zoomOutButton.Enabled = timelineView.CanZoomOut();
            selectAllButton.Enabled = timelineView.CanSelectAll();
            flipViewButton.Enabled = (tabControl.SelectedTab == flameGraphTabPage);
        }

        private void SetState(AppStateFlags state)
        {
            if (state == AppStateFlags.Loading)
            {
                timelineView.SetDuration(0);
                routinesView.Clear();
                callTreeView.Clear();
                flameGraphView.Clear();
                codeView.Clear();
            }

            if (state == AppStateFlags.DynamicCPUAnalysis)
            {
                routinesView.Clear();
                callTreeView.Clear();
                flameGraphView.Clear();
                codeView.Clear();
            }

            if (state == AppStateFlags.Loading ||
                state == AppStateFlags.StaticCPUAnalysis ||
                state == AppStateFlags.DynamicCPUAnalysis)
                spinner.Visible = true;

            AppState |= state;
            UpdateToolbarState();
        }

        private void ClearState(AppStateFlags state)
        {
            AppState &= ~state;
            UpdateToolbarState();

            if ((AppState & (AppStateFlags.Loading | AppStateFlags.StaticCPUAnalysis | AppStateFlags.DynamicCPUAnalysis)) == 0)
                spinner.Visible = false;
        }

        public enum AppStateFlags
        {
            Loading = 0x01,
            StaticCPUAnalysis = 0x02,
            DynamicCPUAnalysis = 0x04,
        }

        private void ResizeSpinner()
        {
            spinner.Size = new Size(DeviceDpi, DeviceDpi);
            spinner.Location = splitContainer.Location + (tabControl.Size / 2) - (spinner.Size / 2);
        }

        public AppStateFlags AppState;
        public Image FlameImage;
        public InstructionSet? InstructionSet;

        private Routine? _SelectedRoutine;
        private CallStack? _SelectedCallStack;
        private string _RecentFilePathName = string.Empty;
    }
}
