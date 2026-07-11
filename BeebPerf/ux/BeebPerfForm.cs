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
using BeebPerf.operation;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using static BeebPerf.CPUAnalysis;
using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm() : base()
        {
            Instruction.CheckSizeOf();

            _LabelResolver = new();
            _CPUAnalysis = new(_LabelResolver);
            _MemoryAnalysis = new(_LabelResolver);
            _VideoAnalysis = new();
            _UndoRedoHistory = new();
            _Model = new();

            InitializeComponent();
            UpdateToolbarState();

            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
            FlameImage = (Image)resources.GetObject("flame.Image")!;

            FormClosing += BeebPerfForm_FormClosing;
            Resize += Spinner_Resize;
            tabControl.Resize += Spinner_Resize;
            tabControl.SelectedIndexChanged += TabControl_SelectedIndexChanged;

            _SelectedTab = tabControl.SelectedTab;
            _BaseFont = Font;
        }

        protected override void OnHandleCreated(EventArgs e)
        {
            base.OnHandleCreated(e);
            RestoreAppState();
            ResizeSpinner();
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            BeginInvoke(new Action(() =>
            {
                this.WindowState = _InitialFormWindowState;
            }));
        }

        private void Spinner_Resize(object? sender, EventArgs e)
        {
            ResizeSpinner();
        }

        private void BeebPerfForm_FormClosing(object? sender, FormClosingEventArgs e)
        {
            SaveAppState();
        }

        private void BeebPerfForm_Load(object sender, EventArgs e)
        {
            ClearState(AppStateFlags.All);

            // load labels files (order not preserved)
            foreach (var labelsFile in DecodeLabels(_LabelsFilesEncoding))
                ReadLabelsFileAsync(labelsFile.FileName, labelsFile.LabelsEnabled);

            // reopen last .perf file
            if (_RecentPerfFilePathName.Length > 0 && File.Exists(_RecentPerfFilePathName))
            {
                var openRecentFile = DialogResult.Yes;
                if (_UnexpectedClose)
                {
                    openRecentFile = MessageBox.Show(
                        this,
                        $"BeebPerf closed unexpectedly last session. Do you want to reopen '{_RecentPerfFilePathName}' ?",
                        "BeebPerf",
                        MessageBoxButtons.YesNo,
                        MessageBoxIcon.Error);
                }

                if (openRecentFile == DialogResult.Yes)
                    OpenPerfFile(_RecentPerfFilePathName);
            }
        }

        private void ReadLabelsFileAsync(string fileName, bool labelsEnabled)
        {
            var task = new LabelsFileReader().ReadFileAsync(fileName);
            task.ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    var labelsFile = task.Result;
                    if (labelsFile.Status == LabelsFileStatus.Loaded)
                        labelsFile.Enabled = labelsEnabled;
                    _LabelsFiles.Add(labelsFile);
                }));
            });
        }

        private void openButton_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new()
            {
                InitialDirectory = Path.GetDirectoryName(_RecentPerfFilePathName),
                Filter = "Beeb .perf files (*.perf)|*.perf",
                FilterIndex = 1,
                RestoreDirectory = true
            };

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string filePathName = openFileDialog.FileName;
                _RecentPerfFilePathName = filePathName;
                _RecentStartCycleCount = 0;
                _RecentEndCycleCount = 0;
                OpenPerfFile(openFileDialog.FileName);
            }
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
                    FilePathName = filePathName;
                    InstructionSet = _Model.InstructionSet;

                    UpdateLabelFiles(filePathName, _Model.Labels);

                    // defer video analysis if the current metric is dependent on the stack frames
                    // created during static analysis. Metrics are evaluated during video analysis
                    bool deferVideoAnalysis = 
                        (_SelectedMetric != null && _SelectedMetric.Type != Metric.MetricType.StartAndEndAddresses);

                    StaticAnalysis(deferVideoAnalysis);

                    if (!deferVideoAnalysis)
                        VideoAnalysis();
                }));
            });
        }

        private void StaticAnalysis(bool performVideoAnalysis)
        {
            // CPU analysis...
            SetState(AppStateFlags.StaticCPUAnalysis);

            var staticAnalysisTask = _CPUAnalysis.StaticAnalysisAsync(_Model).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.StaticCPUAnalysis);

                    timelineView.Duration = _CPUAnalysis.EndCycleCount;

                    if (_RecentStartCycleCount != 0 || _RecentEndCycleCount != 0)
                    {
                        _CPUAnalysis.StartCycleCount = Math.Max(_RecentStartCycleCount, 0);
                        _CPUAnalysis.EndCycleCount = Math.Min(_RecentEndCycleCount, _CPUAnalysis.EndCycleCount);
                        timelineView.SelectRange(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);
                        timelineView.FitSelection();
                    }

                    // execute dynamic analysis for the selected range (or whole range if no selection)
                    DynamicAnalysis(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);

                    // execute video analysis
                    if (performVideoAnalysis)
                        VideoAnalysis();
                }));
            });
        }

        private void DynamicAnalysis(int startCycleCount, int endCycleCount)
        {
            // CPU analysis...
            SetState(AppStateFlags.DynamicCPUAnalysis);

            var dynamicAnalysisTask = _CPUAnalysis.DynamicAnalysisAsync(startCycleCount, endCycleCount).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicCPUAnalysis);

                    // remember selection
                    _RecentStartCycleCount = _CPUAnalysis.StartCycleCount;
                    _RecentEndCycleCount = _CPUAnalysis.EndCycleCount;

                    // populate routines
                    routinesView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                    routinesView.SetRoutines(_CPUAnalysis.HotRoutines);

                    // populate call tree
                    callTreeView.TotalCycleCount = _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount;
                    callTreeView.SetCallTrees([
                        _CPUAnalysis!.ProgramCallTree,
                        _CPUAnalysis!.NMICallTree,
                        _CPUAnalysis!.IRQBRKCallTree ]);
                    callTreeView.ShowHotPaths();

                    // populate flame graph
                    if (_CPUAnalysis!.ProgramCallTree != null)
                        flameGraphView.AddCallTree(_CPUAnalysis!.ProgramCallTree!);

                    if (_CPUAnalysis!.NMICallTree != null)
                        flameGraphView.AddCallTree(_CPUAnalysis!.NMICallTree!);

                    if (_CPUAnalysis!.IRQBRKCallTree != null)
                        flameGraphView.AddCallTree(_CPUAnalysis!.IRQBRKCallTree!);

                    // caller/callee
                    callerCalleeView.Initialize(
                        _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount);

                    // code view
                    codeView.Initialize(
                        _CPUAnalysis.RoutinesByAddress,
                        _LabelResolver,
                        _Model.InstructionSet!);
                }));
            });

            // memory analysis
            SetState(AppStateFlags.DynamicMemoryAnalysis);
            _MemoryAnalysis.Initialize(
                _CPUAnalysis.RootStackFrame,
                _Model.Instructions,
                _Model.InstructionSet!,
                _Model.Snapshot.Memory);

            var memoryAnalysisTask = _MemoryAnalysis.DynamicAnalysisAsync(startCycleCount, endCycleCount, memoryZeroPageCheckBox.Checked).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAnalysis);
                    memoryView.SetMemoryAccesses(_MemoryAnalysis.MemoryAccesses, _LabelResolver);
                    memoryRoutinesView.Clear();
                }));
            });
        }

        private void VideoAnalysis()
        {
            // video analysis...
            SetState(AppStateFlags.FrameAnalysis);

            var videoAnalysisTask = _VideoAnalysis.AnalysisAsync(
                _Model.Instructions,
                InstructionSet!,
                _Model,
                _SelectedMetric,
                _CPUAnalysis.RootStackFrame).ContinueWith((success) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        ClearState(AppStateFlags.FrameAnalysis);

                        timelineView.FrameBitmaps = _VideoAnalysis.DisplayFrames;
                        metricsView.SetIteractions(_VideoAnalysis.MetricIterations);
                    }));
                });
        }

        private void undoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanUndo())
            {
                _UndoRedoHistory.Undo();
                UpdateToolbarState();
            }
        }

        private void redoButton_Click(object sender, EventArgs e)
        {
            if (_UndoRedoHistory.CanRedo())
            {
                _UndoRedoHistory.Redo();
                UpdateToolbarState();
            }
        }

        private void resetAllButton_Click(object sender, EventArgs e)
        {
            SetAnalysisRange(analysisFrom: 0, analysisTo: timelineView.Duration);
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

        private void fitSelectionButton_Click(object sender, EventArgs e)
        {
            timelineView.FitSelection();
            UpdateToolbarState();
        }

        private void fitFramesButton_Click(object sender, EventArgs e)
        {
            timelineView.FitFrames();
            UpdateToolbarState();
        }

        private void hotRoutinesButton_Click(object sender, EventArgs e)
        {
            var operation = new ShowHotRoutinesOperation(
                this,
                tabControl.SelectedTab,
                _SelectedRoutine,
                _SelectedCallStack,
                _SelectedMemoryAccess,
                _SelectedMemoryAddress);

            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ShowHotRoutinesInternal()
        {
            ClearSelectedRoutine();
            if (tabControl.SelectedTab != routinesTabPage)
                tabControl.SelectedTab = routinesTabPage;
            routinesView.ShowHotRoutines();
        }

        private void hotPathsButton_Click(object sender, EventArgs e)
        {
            var operation = new ShowHotPathsOperation(
                this,
                tabControl.SelectedTab,
                _SelectedRoutine,
                _SelectedCallStack,
                _SelectedMemoryAccess,
                _SelectedMemoryAddress);

            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ShowHotPathsInternal()
        {
            ClearSelectedRoutine();
            if (tabControl.SelectedTab != callTreeTabPage)
                tabControl.SelectedTab = callTreeTabPage;
            callTreeView.ShowHotPaths();
        }

        private void labelsButton_Click(object sender, EventArgs e)
        {
            var operation = new EditLabelsOperation(this, _LabelsFiles, _RecentLabelsFilePathName);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        private void displaySettingsButton_Click(object sender, EventArgs e)
        {
            var operation = new EditDisplaySettingsOperation(this, _BaseFont);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            if (HelpWindow != null)
            {
                HelpWindow.BringToFront();
                return;
            }

            HelpWindow = new HelpWindow(this);
            HelpWindow.Show();

            HelpWindow.FormClosed += (s, args) =>
            {
                HelpWindow.Dispose();
                HelpWindow = null;
            };
        }

        private void MemoryZeroPageCheckBox_CheckedChanged(object sender, EventArgs e)
        {
            if (_SuppressCheckBoxChange > 0)
                return;

            var operation = new ShowZeroPageAddressesOperation(this, memoryZeroPageCheckBox.Checked);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SelectZeroPageMemoryAnalysis(bool zeroPageAnalysis)
        {
            _SuppressCheckBoxChange++;
            memoryZeroPageCheckBox.Checked = zeroPageAnalysis;
            _SuppressCheckBoxChange--;

            SetState(AppStateFlags.DynamicMemoryAnalysis);
            var memoryAnalysisTask = _MemoryAnalysis.DynamicAnalysisAsync(_CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount, zeroPageAnalysis).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAnalysis);
                    memoryView.SetMemoryAccesses(_MemoryAnalysis.MemoryAccesses, _LabelResolver);
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

        public void SetSelectedRoutineInternal(
            Routine routine,
            CallStack? callStack,
            RoutineMemoryAccess? memoryAccess)
        {
            if (callStack == null)
            {
                var elements = routine.MetricsByStack.ToList();
                elements.Sort((a, b) => (b.Value.InclusiveCycleCount - a.Value.InclusiveCycleCount));
                callStack = elements.Count > 0 ? elements[0].Key : null;
            }

            _SelectedRoutine = routine;
            _SelectedCallStack = callStack;
            _SelectedMemoryAccess = memoryAccess;

            bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
            bool memoryAccessApplicable = (tabControl.SelectedTab == memoryTabPage);
            SetCodeAsync(
                _SelectedRoutine, 
                callStackApplicable ? _SelectedCallStack : null, 
                memoryAccessApplicable ? _SelectedMemoryAccess : null);
            SetCallerCalleeAsync(_SelectedRoutine);

            routinesView.SelectRoutine(_SelectedRoutine);
            callTreeView.SelectRoutine(_SelectedRoutine, _SelectedCallStack!);
            flameGraphView.SelectRoutine(_SelectedRoutine, _SelectedCallStack!);
            memoryRoutinesView.SelectRoutine(_SelectedRoutine);
        }

        private void SetCodeAsync(Routine routine, CallStack? callStack, RoutineMemoryAccess? memoryAccess)
        {
            codeView.Clear();

            _CPUAnalysis.CalculateInstructionMetricsAsync(routine, callStack).ContinueWith((task) =>
            {
                this.Invoke((Action)(() =>
                {
                    var instructionMetrics = task.Result as List<InstructionMetrics>;
                    codeView.SetCode(routine!, callStack, instructionMetrics, memoryAccess);
                }));
            });
        }

        private void SetCallerCalleeAsync(Routine routine)
        {
            callerCalleeView.Clear();

            _CPUAnalysis.CalculateCallerCalleeMetricsAsync(routine).ContinueWith((task) =>
            {
                this.Invoke((Action)(() =>
                {
                    var callerMetrics = task.Result.Item1 as List<RoutineMetrics>;
                    var calleeMetrics = task.Result.Item2 as List<RoutineMetrics>;
                    callerCalleeView.SelectRoutine(routine, callerMetrics, calleeMetrics);
                }));
            });
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
            analysisFrom = Math.Max(analysisFrom, 0);
            analysisTo = Math.Min(analysisTo, timelineView.Duration);

            var operation = new SelectAnalysisRangeOperation(this, analysisFrom, analysisTo, _CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetAnalysisRangeInternal(int analysisFrom, int analysisTo)
        {
            DynamicAnalysis(analysisFrom, analysisTo);

            if (analysisFrom == 0 && analysisTo == timelineView.Duration)
                timelineView.SelectAll();
            else
                timelineView.SelectRange(analysisFrom, analysisTo);

            metricsView.SelectRange(analysisFrom, analysisTo);
        }

        public void SetSelectedMemoryAddress(CanonicalAddress address)
        {
            var operation = new SelectMemoryAddressOperation(this, address, _SelectedMemoryAddress);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetSelectedMemoryAddressInternal(CanonicalAddress address)
        {
            _SelectedMemoryAddress = address;
            memoryView.SelectMemoryAddress(address, _LabelResolver);
            SetState(AppStateFlags.DynamicMemoryAddressAnalysis);
            var memoryAnalysisTask = _MemoryAnalysis.DynamicAddressAnalysisAsync(address, _CPUAnalysis.StartCycleCount, _CPUAnalysis.EndCycleCount).ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    ClearState(AppStateFlags.DynamicMemoryAddressAnalysis);
                    memoryRoutinesView.SetMemoryAccesses(_MemoryAnalysis.RoutineAccesses);
                    if (_SelectedRoutine != null)
                    {
                        var memoryAccesses = _MemoryAnalysis.RoutineAccesses.Find(routineAccesses => _SelectedRoutine == routineAccesses.Routine);
                        if (memoryAccesses != null)
                        {
                            memoryRoutinesView.SelectRoutine(_SelectedRoutine);
                            SetCodeAsync(_SelectedRoutine, callStack: null, memoryAccesses);
                        }
                    }
                }));
            });
        }

        public void ClearSelectedMemoryAddress()
        {
            var operation = new SelectMemoryAddressOperation(this, null, _SelectedMemoryAddress);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ClearSelectedMemoryAddressInternal()
        {
            _SelectedMemoryAddress = null;

            memoryView.ClearSelection();
            if (_SelectedRoutine != null)
                SetSelectedRoutineInternal(_SelectedRoutine, _SelectedCallStack, memoryAccess: null);

            memoryRoutinesView.Clear();
        }

        public void SetSelectedMetric(Metric metric)
        {
            var operation = new SelectMetricOperation(this, metric, _SelectedMetric);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetSelectedMetricInternal(Metric metric)
        {
            _SelectedMetric = metric;
            metricsView.SetMetrics(_Metrics, _SelectedMetric);
            VideoAnalysis();
        }

        public void ClearSelectedMetric()
        {
            var operation = new SelectMetricOperation(this, null, _SelectedMetric);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ClearSelectedMetricInternal()
        {
            _SelectedMetric = null;
            metricsView.SetMetrics(_Metrics, _SelectedMetric);
            metricsView.SetIteractions([]);
        }

        public void AddMetric()
        {
            var operation = new AddMetricOperation(this, _SelectedMetric!, _Metrics, _Model.Instructions);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void EditMetric()
        {
            var operation = new EditMetricOperation(this, _SelectedMetric!, _Metrics, _Model.Instructions);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void RemoveMetric()
        {
            var operation = new RemoveMetricOperation(this, _SelectedMetric!, _Metrics);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        private void TabControl_SelectedIndexChanged(object? sender, EventArgs e)
        {
            if (_SuppressTabChange > 0)
                return;

            var operation = new SelectTabOperation(this, tabControl.SelectedTab, _SelectedTab);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SelectTabInternal(Panel? tab)
        {
            _SuppressTabChange++;
            tabControl.SelectedTab = tab;
            _SuppressTabChange--;

            _SelectedTab = tab;

            UpdateToolbarState();

            if (_SelectedRoutine != null)
            {
                bool callStackApplicable = (tabControl.SelectedTab == callTreeTabPage) || (tabControl.SelectedTab == flameGraphTabPage);
                bool memoryAccessApplicable = (tabControl.SelectedTab == memoryTabPage);
                SetCodeAsync(
                    _SelectedRoutine,
                    callStackApplicable ? _SelectedCallStack : null,
                    memoryAccessApplicable ? _SelectedMemoryAccess : null);
            }
        }

        public void SetLabelsFiles(List<LabelsFile> labelsFiles, string recentLabelsFilePathName)
        {
            // update preferences (saved when app closes)
            _RecentLabelsFilePathName = recentLabelsFilePathName;
            _LabelsFilesEncoding = EncodeLabels(labelsFiles);

            // update labels member
            _LabelsFiles = labelsFiles;

            // refresh labels in the UI
            RefreshUILabels();
        }

        private void RefreshUILabels()
        {
            // reinitialize resolver
            _LabelResolver.Initialize(_LabelsFiles);

            // refresh all the labels
            _CPUAnalysis.ResolveRoutineLabels();

            routinesView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            callTreeView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            memoryView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            memoryRoutinesView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            codeView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            Invalidate(true);
        }

        private void UpdateLabelFiles(
            string perfFileName,
            List<(string Name, ushort Address)> perfFileLabels)
        {
            // remove any transient label files
            for (int i = _LabelsFiles.Count - 1; i >= 0; i--)
            {
                if (_LabelsFiles[i].Transient)
                    _LabelsFiles.RemoveAt(i);
            }

            // insert MOS labels
            (var mosName, var mosLabels) = LoadMOSLabels();
            if (mosName != string.Empty)
            {
                _LabelsFiles.Insert(0, new LabelsFile()
                {
                    FileName = mosName,
                    Labels = mosLabels,
                    Status = LabelsFileStatus.Loaded,
                    Enabled = true,
                    Transient = true
                });
            }

            // insert .perf file labels, if any exist
            if (perfFileLabels.Count > 0)
            {
                _LabelsFiles.Insert(0, new LabelsFile()
                {
                    FileName = $"{perfFileName} (embedded labels)",
                    Labels = perfFileLabels,
                    Status = LabelsFileStatus.Loaded,
                    Enabled = true,
                    Transient = true
                });
            }

            // reload label files
            var loadLabelsTask = ReloadLabelsAsync().ContinueWith((success) =>
            {
                this.Invoke((Action)(() =>
                {
                    RefreshUILabels();
                }));
            });
        }

        private async Task ReloadLabelsAsync()
        {
            for (int i = 0; i < _LabelsFiles.Count; i++)
            {
                if (_LabelsFiles[i].Transient)
                    continue;

                LabelsFile labelsFile = await new LabelsFileReader().ReadFileAsync(_LabelsFiles[i].FileName);
                if (labelsFile.Status != LabelsFileStatus.Loaded)
                    continue;

                labelsFile.Enabled = _LabelsFiles[i].Enabled;
                _LabelsFiles[i] = labelsFile;
            }
        }

        private (string mosName, List<(string Name, ushort Address)> mosLabels) LoadMOSLabels() 
        {
            // finger print MOS memory, excluding FRED, JIM, and SHEILA
            byte[] mosMemory = new byte[0x4000 - 0x300];
            Buffer.BlockCopy(_Model.Snapshot.Memory[(int)MemoryPage.WholeRam], 0xC000, mosMemory, 0, 0xFC00 - 0xC000);
            Buffer.BlockCopy(_Model.Snapshot.Memory[(int)MemoryPage.WholeRam], 0xFF00, mosMemory, 0xFC00 - 0xC000, 0x10);
            byte[] mosHash = MD5.HashData(mosMemory);
            // Debug.WriteLine($"mosHash: {Convert.ToHexString(mosHash)}");

            // match against known MOS fingerprints
            string mosResource, mosName;
            var labels = new List<(string Name, ushort Address)>();
            if (mosHash.SequenceEqual(Convert.FromHexString("4EAFE9B5D17DFA80C213EAB71CDED9FC")))
            {
                mosResource = "mos120_labels";
                mosName = "MOS 1.2";
            }
            else if (mosHash.SequenceEqual(Convert.FromHexString("2A763CFC810035C09B8AA76C21466F46")))
            {
                mosResource = "mos200_labels";
                mosName = "MOS 2.0";
            }
            else
            {
                return (string.Empty, labels);
            }

            // load labels from resource
            var resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
            var text = (String)resources.GetObject(mosResource)!;

            // parse labels. Format is [{'LabelName':AddressL, ...}]
            text = text.Trim([' ', '\t', '\n', '\r']);
            text = text.Substring(2, text.Length - 4); // remove surrounding [{ and }]

            var nameAndAddressRegex = new Regex(@"^\'(?<name>.[A-Za-z_][.A-Za-z0-9_]*)'\:(?<address>\d+)L$", RegexOptions.Compiled);

            foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
            {
                string nameAndAddress = part.Trim();
                var m = nameAndAddressRegex.Match(nameAndAddress);
                Debug.Assert(m.Success);

                string name = m.Groups["name"].Value;
                long address = long.Parse(m.Groups["address"].Value);
                Debug.Assert(name.Length >= 2 && address >= 0 && address <= 0xFFFF);

                labels.Add((name, (ushort)address));
            }

            return (mosName, labels);
        }

        static private string EncodeLabels(List<LabelsFile> labelsFiles)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var labelsFile in labelsFiles)
            {
                if (!labelsFile.Transient)
                {
                    if (sb.Length > 0) sb.Append('|');
                    sb.Append(labelsFile.FileName);
                    sb.Append('|');
                    sb.Append(labelsFile.Enabled ? "true" : "false");
                }
            }
            return sb.ToString();
        }

        static private List<(string FileName, bool LabelsEnabled)> DecodeLabels(string value)
        {
            List<(string FileName, bool LabelsEnabled)> result = [];
            var values = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i += 2)
                result.Add((FileName: values[i], LabelsEnabled: values[i + 1].Equals("true")));
            return result;
        }

        static private string EncodeMetrics(List<Metric> frameSettingsList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var metric in frameSettingsList)
            {
                if (sb.Length > 0) sb.Append('|');
                sb.Append(Metric.Serialize(metric));
            }
            return sb.ToString();
        }

        static private List<Metric> DecodeMetrics(string value)
        {
            List<Metric> result = [];
            var values = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
                result.Add(Metric.DeSerialize(values[i]));
            return result;
        }

        private void SaveAppState()
        {
            var bounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
            var windowState = (WindowState == FormWindowState.Minimized) ? FormWindowState.Normal : WindowState;
            var displaySettings = DisplaySettings.Serialize(this.DisplaySettings!);
            var recentFrameSettings = _SelectedMetric != null ? _SelectedMetric.Name : string.Empty;
            Properties.Settings.Default.WindowLocation = bounds.Location;
            Properties.Settings.Default.WindowSize = bounds.Size;
            Properties.Settings.Default.WindowState = (int)windowState;
            Properties.Settings.Default.RecentPerfFilePathName = _RecentPerfFilePathName;
            Properties.Settings.Default.RecentLabelsFilePathName = _RecentLabelsFilePathName;
            Properties.Settings.Default.RecentStartCycleCount = _RecentStartCycleCount;
            Properties.Settings.Default.RecentEndCycleCount = _RecentEndCycleCount;
            Properties.Settings.Default.RecentExportFolderPath = RecentExportFolderPath;
            Properties.Settings.Default.LabelsFiles = _LabelsFilesEncoding;
            Properties.Settings.Default.PrimarySplitterDistance = primarySplitContainer.SplitterDistance;
            Properties.Settings.Default.SecondarySplitterDistance = secondarySplitContainer.SplitterDistance;
            Properties.Settings.Default.DisplaySettings = displaySettings;
            Properties.Settings.Default.ColorTheme = (int)ColorTheme.Get();
            Properties.Settings.Default.FrameSettingsList = EncodeMetrics(_Metrics);
            Properties.Settings.Default.RecentSelectedFrameSettings = recentFrameSettings;
            Properties.Settings.Default.UnexpectedClose = false;
            Properties.Settings.Default.Save();
        }

        private void RestoreAppState()
        {
            _UnexpectedClose = Properties.Settings.Default.UnexpectedClose;
            _RecentPerfFilePathName = Properties.Settings.Default.RecentPerfFilePathName;
            _RecentStartCycleCount = Properties.Settings.Default.RecentStartCycleCount;
            _RecentEndCycleCount = Properties.Settings.Default.RecentEndCycleCount;
            _RecentLabelsFilePathName = Properties.Settings.Default.RecentLabelsFilePathName;
            _LabelsFilesEncoding = Properties.Settings.Default.LabelsFiles;
            RecentExportFolderPath = Properties.Settings.Default.RecentExportFolderPath;

            // metrics
            _Metrics = DecodeMetrics(Properties.Settings.Default.FrameSettingsList);
            var recentFrameSettings = Properties.Settings.Default.RecentSelectedFrameSettings;
            foreach (var metric in _Metrics)
            {
                if (metric.Name == recentFrameSettings)
                {
                    _SelectedMetric = metric;
                    break;
                }
            }
            metricsView.SetMetrics(_Metrics, _SelectedMetric);

            // color theme
            var colorTheme = Properties.Settings.Default.ColorTheme;
            ColorTheme.Set(this, (ColorThemeType)colorTheme);

            // display settings
            var displaySettings = Properties.Settings.Default.DisplaySettings;
            if (displaySettings.Length > 0)
                DisplaySettings = DisplaySettings.Deserialize(displaySettings)!;
            else
                DisplaySettings = new DisplaySettings();

            // windows size and position
            var windowLocation = Properties.Settings.Default.WindowLocation;
            var windowSize = Properties.Settings.Default.WindowSize;
            var windowState = Properties.Settings.Default.WindowState;

            var screenBounds = Screen.FromPoint(windowLocation).WorkingArea;
            if (windowSize.Width == 0 || windowSize.Height == 0)
            {
                windowLocation.X = screenBounds.Size.Width / 12;
                windowLocation.Y = screenBounds.Size.Height / 12;
                windowSize.Width = 10 * screenBounds.Size.Width / 12;
                windowSize.Height = 10 * screenBounds.Size.Height / 12;
            }

            if (!screenBounds.Contains(new Rectangle(windowLocation, windowSize)))
                windowLocation = new Point(100, 100);

            Location = windowLocation;
            Size = windowSize;

            // window state set after form is visible
            _InitialFormWindowState = (FormWindowState)windowState;

            // splitter positions
            var primarySplitterDistance = Properties.Settings.Default.PrimarySplitterDistance;
            var secondarySplitterDistance = Properties.Settings.Default.SecondarySplitterDistance;

            if (primarySplitterDistance <= 0 || secondarySplitterDistance <= 0)
            {
                primarySplitterDistance = Height / 4;
                secondarySplitterDistance = 3 * Height / 8;
            }
            else
            {
                if (primarySplitterDistance > Height / 2)
                {
                    primarySplitterDistance = Height / 2;
                    secondarySplitterDistance = Height / 3;
                }
                else if (secondarySplitterDistance > Height / 2)
                {
                    secondarySplitterDistance = Height / 2;
                    primarySplitterDistance = Height / 4;
                }
            }

            primarySplitContainer.SplitterDistance = primarySplitterDistance;
            secondarySplitContainer.SplitterDistance = secondarySplitterDistance;

            ApplyFontScaling(this, DisplaySettings.FontScaling);

            // set unexpected close. This is overwritten when app data is saved during shutdown
            Properties.Settings.Default.UnexpectedClose = true;
            Properties.Settings.Default.Save();
        }

        public void ApplyFontScaling(Control control, int fontScaling)
        {
            int fontSize = (int)float.Round(_BaseFont.SizeInPoints * fontScaling / 100.0f);
            Font font = new Font(_BaseFont.Name, fontSize, FontStyle.Regular);
            primarySplitContainer.Panel1MinSize = font.Height * TimelineView.MinHeight;
            SuspendLayout();
            ApplyFontToAllControls(control, font);
            ResumeLayout();
            PerformLayout();
        }

        private void ApplyFontToAllControls(Control control, Font font)
        {
            if (control.Font.Style != font.Style)
                control.Font = new Font(font, control.Font.Style);
            else
                control.Font = font;

            if (control is IGridView)
            {
                var gridView = (IGridView)control;

                int fontHeight = TextRenderer.MeasureText("Sample", font).Height;
                int rowHeight;
                if (control is CodeView)
                    rowHeight = (int)float.Round(fontHeight * DisplaySettings.LineSpacing / 100.0f);
                else
                    rowHeight = fontHeight + 6;

                gridView.SetRowHeight(rowHeight);
                gridView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            }

            foreach (Control child in control.Controls)
            {
                ApplyFontToAllControls(child, font);
            }
        }

        private void UpdateToolbarState()
        {
            bool fileLoaded = (timelineView.Duration > 0);
            openButton.Enabled = (AppState == 0);
            undoButton.Enabled = (AppState == 0) && _UndoRedoHistory.CanUndo();
            redoButton.Enabled = (AppState == 0) && _UndoRedoHistory.CanRedo();
            resetAllButton.Enabled = timelineView.CanSelectAll() || timelineView.CanZoomOut();
            zoomInButton.Enabled = timelineView.CanZoomIn();
            zoomOutButton.Enabled = timelineView.CanZoomOut();
            fitSelectionButton.Enabled = timelineView.CanFitSelection();
            fitFramesButton.Enabled = timelineView.CanFitFrames();
            hotRoutinesButton.Enabled = fileLoaded && (AppState == 0);
            hotPathsButton.Enabled = fileLoaded && (AppState == 0);
            memoryZeroPageCheckBox.Enabled = (AppState & AppStateFlags.DynamicMemoryAnalysis) == 0;
            labelsButton.Enabled = (AppState == 0);
        }

        private void SetState(AppStateFlags state)
        {
            if (state == AppStateFlags.Loading)
            {
                timelineView.Duration = 0;
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
            }

            if (state == AppStateFlags.DynamicMemoryAnalysis)
            {
                memoryView.Clear();
            }

            if (state == AppStateFlags.DynamicMemoryAddressAnalysis)
            {
                memoryRoutinesView.Clear();
                codeView.Clear();
            }

            spinner.Visible =
               (state == AppStateFlags.Loading ||
                state == AppStateFlags.StaticCPUAnalysis ||
                state == AppStateFlags.DynamicCPUAnalysis ||
                state == AppStateFlags.DynamicMemoryAnalysis ||
                state == AppStateFlags.DynamicMemoryAddressAnalysis ||
                state == AppStateFlags.FrameAnalysis);

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
                 AppStateFlags.DynamicMemoryAddressAnalysis |
                 AppStateFlags.FrameAnalysis)) == 0)
                spinner.Visible = false;
        }

        public enum AppStateFlags
        {
            Loading = 0x01,
            StaticCPUAnalysis = 0x02,
            DynamicCPUAnalysis = 0x04,
            DynamicMemoryAnalysis = 0x8,
            DynamicMemoryAddressAnalysis = 0x10,
            FrameAnalysis = 0x20,
            All = 0x3F
        }

        private void ResizeSpinner()
        {
            spinner.Size = new Size(DeviceDpi, DeviceDpi);
            var point = PointToClient(tabControl.PointToScreen(Point.Empty));
            spinner.Location = point + (tabControl.Size / 2) - (spinner.Size / 2);
        }

        public DisplaySettings DisplaySettings = new();

        public string? FilePathName
        {
            get => _FilePathName;
            set
            {
                if (value != _FilePathName)
                {
                    _FilePathName = value;
                    UpdateCaptionText();
                }
            }
        }

        public string? StatusText
        {
            get => _StatusText;
            set
            {
                if (value != _StatusText)
                {
                    _StatusText = value;
                    UpdateCaptionText();
                }
            }
        }

        private void UpdateCaptionText()
        {
            string caption = "BeebPerf";
            if (_FilePathName != null && _FilePathName.Length > 0)
                caption += $" - {_FilePathName}";
            if (_StatusText != null && _StatusText.Length > 0)
                caption += $" - {_StatusText}";
            Text = caption;
        }

        private string? _FilePathName;
        private string? _StatusText;

        public AppStateFlags AppState;
        public HelpWindow? HelpWindow;
        public Image FlameImage;
        public InstructionSet? InstructionSet;
        public string RecentExportFolderPath = string.Empty;

        private Routine? _SelectedRoutine;
        private CallStack? _SelectedCallStack;
        private RoutineMemoryAccess? _SelectedMemoryAccess;
        private Panel? _SelectedTab;
        private CanonicalAddress? _SelectedMemoryAddress;
        private Metric? _SelectedMetric;

        private string _RecentPerfFilePathName = string.Empty;
        private int _RecentStartCycleCount = 0;
        private int _RecentEndCycleCount = 0;
        private string _RecentLabelsFilePathName = string.Empty;
        private string _LabelsFilesEncoding = string.Empty;
        private int _SuppressTabChange;
        private int _SuppressCheckBoxChange;
        private List<LabelsFile> _LabelsFiles = [];
        private List<Metric> _Metrics = [];

        private UndoRedoHistory _UndoRedoHistory;
        private Model _Model;
        private LabelResolver _LabelResolver;
        private CPUAnalysis _CPUAnalysis;
        private MemoryAnalysis _MemoryAnalysis;
        private VideoAnalysis _VideoAnalysis;
        private Font _BaseFont;
        private bool _UnexpectedClose;
        private FormWindowState _InitialFormWindowState;
    }
}
