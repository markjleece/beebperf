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
using static BeebPerf.MemoryAnalysis;

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

            _SelectedTab = tabControl.SelectedTab;
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
                OpenPerfFile(_RecentFilePathName);
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
                string filePathName = openFileDialog.FileName;
                _RecentFilePathName = filePathName;
                OpenPerfFile(openFileDialog.FileName);
            }
        }

        private void OpenPerfFile(string filePathName)
        {
            SetState(AppStateFlags.Loading);

            var readPerfFileTask = ReadPerfFileAsync(filePathName).ContinueWith((model) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.Loading);

                    _UndoRedoHistory.Clear();
                    _Model = model.Result;
                    _FilePathName = filePathName;
                    InstructionSet = _Model.InstructionSet;

                    StaticAnalysis();
                }));
            });
        }

        private async Task<Model> ReadPerfFileAsync(string filePathName)
        {
            return await Task.Run(() =>
            {
                var perfReader = new PerfReader();

                Model? model = perfReader.ReadFile(filePathName);
                if (model == null)
                    throw new Exception($"An error occurred reading {filePathName}");

                return model;
            });
        }

        public void StaticAnalysis()
        {
            SetState(AppStateFlags.StaticCPUAnalysis);

            var staticAnalysisTask = _CPUAnalysis.StaticAnalysis(_Model).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.StaticCPUAnalysis);

                    timelineView.SetDuration(_CPUAnalysis.EndCycleCount);
                    DynamicAnalysis(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);
                }));
            });
        }

        public void DynamicAnalysis(int startCycleCount, int endCycleCount)
        {
            SetState(AppStateFlags.DynamicCPUAnalysis);

            var dynamicAnalysisTask = _CPUAnalysis.DynamicAnalysisAsync(startCycleCount, endCycleCount).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicCPUAnalysis);

                    // populate routines
                    routinesView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                    routinesView.SetRoutines(_CPUAnalysis.HotRoutines);

                    // populate call tree
                    callTreeView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                    callTreeView.SetCallTrees([
                        _CPUAnalysis!.ProgramCallTree,
                        _CPUAnalysis!.NonMaskableInterruptCallTree,
                        _CPUAnalysis!.MaskableInterruptCallTree ]);
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
                }));
            });

            SetState(AppStateFlags.DynamicMemoryAnalysis);
            _MemoryAnalysis.Initialize(
                _CPUAnalysis.RootStackFrame,
                _Model.Instructions,
                _Model.InstructionSet!,
                _Model.Labels,
                _Model.Snapshot.Memory);

            var memoryAnalysisTask = _MemoryAnalysis.DynamicAnalysisAsync(startCycleCount, endCycleCount, _ZeroPageMemoryAnalysis).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAnalysis);
                    memoryView.Labels = _Model.Labels;
                    memoryView.SetMemoryAccesses(_MemoryAnalysis.MemoryAccesses);
                }));
            });
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

        private void hotRoutinesButton_Click(object sender, EventArgs e)
        {
            ClearSelectedRoutine();
            if (tabControl.SelectedTab != routinesTabPage)
                tabControl.SelectedTab = routinesTabPage;
            routinesView.ShowHotRoutines();
        }

        private void hotPathsButton_Click(object sender, EventArgs e)
        {
            ClearSelectedRoutine();
            if (tabControl.SelectedTab != callTreeTabPage)
                tabControl.SelectedTab = callTreeTabPage;
            callTreeView.ShowHotPaths();
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

        private void MemoryZeroPageCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            _ZeroPageMemoryAnalysis = memoryZeroPageCheckBox.Checked;

            SetState(AppStateFlags.DynamicMemoryAnalysis);
            var memoryAnalysisTask = _MemoryAnalysis.DynamicAnalysisAsync(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount, _ZeroPageMemoryAnalysis).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAnalysis);
                    memoryView.Labels = _Model.Labels;
                    memoryView.SetMemoryAccesses(_MemoryAnalysis.MemoryAccesses);
                }));
            });
        }

        public void SetSelectedRoutine(
            Routine? routine,
            CallStack? callStack,
            RoutineMemoryAccess? memoryAccess)
        {
            if (routine == _SelectedRoutine && callStack == _SelectedCallStack)
                return;

            var operation = new SelectRoutineOperation(
                this,
                routine, callStack, memoryAccess,
                _SelectedRoutine, _SelectedCallStack, _SelectedMemoryAccess);

            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ClearSelectedRoutine()
        {
            if (_SelectedRoutine == null && _SelectedCallStack == null)
                return;

            var operation = new SelectRoutineOperation(
                this,
                null, null, null,
                _SelectedRoutine, _SelectedCallStack, _SelectedMemoryAccess);

            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetSelectedRoutineInternal(
            Routine routine, 
            CallStack? callStack,
            RoutineMemoryAccess? memoryAccess)
        {
            if (callStack == null)
            {
                var elements = routine.MetricsByStack.ToList();
                elements.Sort((a, b) => (b.Value.InclusiveCycleCount - a.Value.InclusiveCycleCount));
                callStack = elements[0].Key;
            }

            _SelectedRoutine = routine;
            _SelectedCallStack = callStack;
            _SelectedMemoryAccess = memoryAccess;

            bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
            codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null, memoryAccess); 

            routinesView.SelectRoutine(routine);
            callerCalleeView.SelectRoutine(routine);
            callTreeView.SelectRoutine(routine, callStack);
            flameGraphView.SelectRoutine(routine, callStack);
            memoryRoutinesView.SelectRoutine(routine);
        }

        public void ClearSelectedRoutineInternal()
        {
            _SelectedRoutine = null;
            _SelectedCallStack = null;

            routinesView.ClearSelection();
            callerCalleeView.Clear();
            callTreeView.ClearSelection();
            flameGraphView.ClearSelection();
            memoryRoutinesView.ClearSelection();
            codeView.Clear();
        }

        public void SetAnalysisRange(int analysisFrom, int analysisTo)
        {
            var operation = new SelectAnalysisRangeOperation(this, analysisFrom, analysisTo, _CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetAnalysisRangeInternal(int analysisFrom, int analysisTo)
        {
            DynamicAnalysis(analysisFrom, analysisTo);
            timelineView.SelectRange(analysisFrom, analysisTo);
        }

        public void SetSelectedMemoryAddress(CanonicalAddress address)
        {
            var operation = new SelectMemoryAddressOperation(this, address, _SelectedMemoryAddress);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ClearSelectedMemoryAddress()
        {
            var operation = new SelectMemoryAddressOperation(this, null, _SelectedMemoryAddress);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetSelectedMemoryAddressInternal(CanonicalAddress address)
        {
            _SelectedMemoryAddress = address;
            memoryView.SelectMemoryAddress(address);
            SetState(AppStateFlags.DynamicMemoryAddressAnalysis);
            var memoryAnalysisTask = _MemoryAnalysis.DynamicAddressAnalysisAsync(address, _CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAddressAnalysis);
                    memoryRoutinesView.SetMemoryAccesses(_MemoryAnalysis.RoutineAccesses);
                }));
            });
        }

        public void ClearSelectedMemoryAddressInternal()
        {
            _SelectedMemoryAddress = null;

            memoryView.ClearSelection();
            if (_SelectedRoutine != null)
                SetSelectedRoutine(_SelectedRoutine, _SelectedCallStack, memoryAccess: null);

            memoryRoutinesView.Clear();
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressTabChange > 0)
                return;

            var operation = new SelectTabOperation(this, tabControl.SelectedTab, _SelectedTab);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SelectTabInternal(TabPage? tab)
        {
            _SuppressTabChange++;
            tabControl.SelectedTab = tab;
            _SuppressTabChange--;

            _SelectedTab = tab;

            UpdateToolbarState();

            if (_SelectedRoutine != null)
            {
                bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
                codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null, memoryAccess: null);
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
            hotRoutinesButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            hotPathsButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            flipViewButton.Enabled = (tabControl.SelectedTab == flameGraphTabPage);
            memoryZeroPageCheckBox.Enabled = (AppState & AppStateFlags.DynamicMemoryAnalysis) == 0;
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
                memoryView.Clear();
            }

            if (state == AppStateFlags.DynamicCPUAnalysis)
            {
                routinesView.Clear();
                callTreeView.Clear();
                flameGraphView.Clear();
                codeView.Clear();
                memoryView.Clear();
            }

            if (state == AppStateFlags.DynamicMemoryAnalysis)
            {
                memoryView.Clear();
            }

            if (state == AppStateFlags.DynamicMemoryAddressAnalysis)
            {
                // TODO: disable memoryView selection
                memoryRoutinesView.Clear();
                codeView.Clear();
            }

            if (state == AppStateFlags.Loading ||
                state == AppStateFlags.StaticCPUAnalysis ||
                state == AppStateFlags.DynamicCPUAnalysis ||
                state == AppStateFlags.DynamicMemoryAnalysis ||
                state == AppStateFlags.DynamicMemoryAddressAnalysis)
                spinner.Visible = true;

            AppState |= state;
            UpdateToolbarState();
        }

        private void ClearState(AppStateFlags state)
        {
            AppState &= ~state;
            UpdateToolbarState();

            if ((AppState & 
                (AppStateFlags.Loading | 
                 AppStateFlags.StaticCPUAnalysis | 
                 AppStateFlags.DynamicCPUAnalysis | 
                 AppStateFlags.DynamicMemoryAnalysis |
                 AppStateFlags.DynamicMemoryAddressAnalysis)) == 0)
                spinner.Visible = false;
        }

        public enum AppStateFlags
        {
            Loading = 0x01,
            StaticCPUAnalysis = 0x02,
            DynamicCPUAnalysis = 0x04,
            DynamicMemoryAnalysis = 0x8,
            DynamicMemoryAddressAnalysis = 0x10
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
        private RoutineMemoryAccess? _SelectedMemoryAccess;
        private TabPage? _SelectedTab;
        private CanonicalAddress? _SelectedMemoryAddress;

        private string _RecentFilePathName = string.Empty;
        private bool _ZeroPageMemoryAnalysis;
        private int _SuppressTabChange;
        private string? _FilePathName;

        private UndoRedoHistory _UndoRedoHistory = new();
        private Model _Model = new();
        private CPUAnalysis _CPUAnalysis = new();
        private MemoryAnalysis _MemoryAnalysis = new();
    }
}
