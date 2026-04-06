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
using System.Text;
using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm() : base()
        {
            _LabelResolver = new();
            _CPUAnalysis = new(_LabelResolver);
            _MemoryAnalysis = new(_LabelResolver);
            _FrameAnalysis = new();
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
                OpenPerfFile(_RecentPerfFilePathName);
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

                    InsertPerfFileLabels(_LabelsFiles, filePathName, _Model.Labels);
                    _LabelResolver.Initialize(_LabelsFiles);

                    // defer frame analysis if its dependent on static analysis
                    bool deferFrameAnalysis = 
                        (_SelectedFrameSettings != null && _SelectedFrameSettings.Type != FrameSettings.FrameType.StartAndEndAddresses);

                    StaticAnalysis(deferFrameAnalysis);

                    if (!deferFrameAnalysis)
                        FrameAnalysis();
                }));
            });
        }

        private void StaticAnalysis(bool performFrameAnalysis)
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

                    // execute frame analysis
                    if (performFrameAnalysis)
                        FrameAnalysis();
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
                        _CPUAnalysis.GetCallerMetrics,
                        _CPUAnalysis.GetCalleeMetrics,
                        _CPUAnalysis.EndCycleCount - _CPUAnalysis.StartCycleCount);

                    // code view
                    codeView.Initialize(
                        _CPUAnalysis.CalculateInstructionMetrics,
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

        private void FrameAnalysis()
        {
            // frame analysis...
            SetState(AppStateFlags.FrameAnalysis);

            var frameAnalysisTask = _FrameAnalysis.AnalysisAsync(
                _Model.Instructions,
                InstructionSet!,
                _Model,
                _SelectedFrameSettings,
                _CPUAnalysis.RootStackFrame).ContinueWith((success) =>
                {
                    this.Invoke((Action)(() =>
                    {
                        ClearState(AppStateFlags.FrameAnalysis);

                        timelineView.FrameBitmaps = _FrameAnalysis.DisplayFrames;
                        framesView.SetResults(_FrameAnalysis.Frames);
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

        private void selectAllButton_Click(object sender, EventArgs e)
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

        private void settingsButton_Click(object sender, EventArgs e)
        {
            var operation = new EditSettingsOperation(this, _BaseFont);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        private void helpButton_Click(object sender, EventArgs e)
        {
            var dialog = new HelpDialog(this);
            dialog.ShowDialog();
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
            codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null, memoryAccess);

            routinesView.SelectRoutine(routine);
            callerCalleeView.SelectRoutine(routine);
            callTreeView.SelectRoutine(routine, callStack!);
            flameGraphView.SelectRoutine(routine, callStack!);
            memoryRoutinesView.SelectRoutine(routine);
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

            framesView.SelectRange(analysisFrom, analysisTo);
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
                            codeView.SetCode(_SelectedRoutine, callStack: null, memoryAccesses);
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

        public void SetSelectedFrameSettings(FrameSettings frameSettings)
        {
            var operation = new SelectFrameSettingsOperation(this, frameSettings, _SelectedFrameSettings);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void SetSelectedFrameSettingsInternal(FrameSettings frameSettings)
        {
            _SelectedFrameSettings = frameSettings;
            framesView.SetSettings(_FrameSettingsList, _SelectedFrameSettings);
            FrameAnalysis();
        }

        public void ClearSelectedFrameSettings()
        {
            var operation = new SelectFrameSettingsOperation(this, null, _SelectedFrameSettings);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void ClearSelectedFrameSettingsInternal()
        {
            _SelectedFrameSettings = null;
            framesView.SetSettings(_FrameSettingsList, _SelectedFrameSettings);
            framesView.SetResults([]);
        }

        public void AddFrameSettings()
        {
            var operation = new AddFrameSettingsOperation(this, _SelectedFrameSettings!, _FrameSettingsList, _Model.Instructions);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void EditFrameSettings()
        {
            var operation = new EditFrameSettingsOperation(this, _SelectedFrameSettings!, _FrameSettingsList, _Model.Instructions);
            if (_UndoRedoHistory.Execute(operation))
                UpdateToolbarState();
        }

        public void RemoveFrameSettings()
        {
            var operation = new RemoveFrameSettingsOperation(this, _SelectedFrameSettings!, _FrameSettingsList);
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
                codeView.SetCode(_SelectedRoutine, callStackApplicable ? _SelectedCallStack : null, memoryAccess: null);
            }
        }

        public void SetLabelsFiles(List<LabelsFile> labelsFiles, string recentLabelsFilePathName)
        {
            // update preferences (saved when app closes)
            _RecentLabelsFilePathName = recentLabelsFilePathName;
            _LabelsFilesEncoding = EncodeLabels(labelsFiles);
 
            // update labels member
            _LabelsFiles = labelsFiles;
           
            // reinitialize resolver
            _LabelResolver.Initialize(labelsFiles);

            // refresh all the labels
            _CPUAnalysis.ResolveRoutineLabels();

            routinesView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            callTreeView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            memoryView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            memoryRoutinesView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            codeView.AutoResizeColumns(DataGridViewAutoSizeColumnsMode.DisplayedCells);
            Invalidate(true);
        }

        static private void InsertPerfFileLabels(
            List<LabelsFile> labelsFiles,
            string perfFileName,
            List<(string Name, ushort Address)> labels)
        {
            var pseudoLabelsFile = new LabelsFile()
            {
                FileName = $"<{perfFileName}>",
                Labels = labels,
                Status = LabelsFileStatus.Loaded,
                Enabled = true
            };

            if (labelsFiles.Count > 0 && labelsFiles[0].FileName.StartsWith('<'))
                labelsFiles.RemoveAt(0);

            labelsFiles.Insert(0, pseudoLabelsFile);
        }

        static private string EncodeLabels(List<LabelsFile> labelsFiles)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var labelsFile in labelsFiles)
            {
                if (!labelsFile.FileName.StartsWith('<'))
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

        static private string EncodeFrameSettings(List<FrameSettings> frameSettingsList)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var frameSettings in frameSettingsList)
            {
                if (sb.Length > 0) sb.Append('|');
                sb.Append(FrameSettings.Serialize(frameSettings));
            }
            return sb.ToString();
        }

        static private List<FrameSettings> DecodeFrameSettings(string value)
        {
            List<FrameSettings> result = [];
            var values = value.Split('|', StringSplitOptions.RemoveEmptyEntries);
            for (int i = 0; i < values.Length; i++)
                result.Add(FrameSettings.DeSerialize(values[i]));
            return result;
        }

        private void SaveAppState()
        {
            var bounds = (WindowState == FormWindowState.Normal) ? Bounds : RestoreBounds;
            var windowState = (WindowState == FormWindowState.Minimized) ? FormWindowState.Normal : WindowState;
            var displaySettings = DisplaySettings.Serialize(this.DisplaySettings!);
            var recentFrameSettings = _SelectedFrameSettings != null ? _SelectedFrameSettings.Name : string.Empty;
            Properties.Settings.Default.WindowLocation = bounds.Location;
            Properties.Settings.Default.WindowSize = bounds.Size;
            Properties.Settings.Default.WindowState = (int)windowState;
            Properties.Settings.Default.RecentPerfFilePathName = _RecentPerfFilePathName;
            Properties.Settings.Default.RecentLabelsFilePathName = _RecentLabelsFilePathName;
            Properties.Settings.Default.RecentStartCycleCount = _RecentStartCycleCount;
            Properties.Settings.Default.RecentEndCycleCount = _RecentEndCycleCount;
            Properties.Settings.Default.RecentExportFolderPath = RecentExportFolderPath;
            Properties.Settings.Default.LabelsFiles = _LabelsFilesEncoding;
            Properties.Settings.Default.WindowLayout = (int)secondarySplitContainer.Orientation;
            Properties.Settings.Default.PrimarySplitterDistance = primarySplitContainer.SplitterDistance;
            Properties.Settings.Default.SecondarySplitterDistance = secondarySplitContainer.SplitterDistance;
            Properties.Settings.Default.DisplaySettings = displaySettings;
            Properties.Settings.Default.ColorTheme = (int)ColorTheme.Get();
            Properties.Settings.Default.FrameSettingsList = EncodeFrameSettings(_FrameSettingsList);
            Properties.Settings.Default.RecentSelectedFrameSettings = recentFrameSettings;
            Properties.Settings.Default.Save();
        }

        private void RestoreAppState()
        {
            _RecentPerfFilePathName = Properties.Settings.Default.RecentPerfFilePathName;
            _RecentStartCycleCount = Properties.Settings.Default.RecentStartCycleCount;
            _RecentEndCycleCount = Properties.Settings.Default.RecentEndCycleCount;
            _RecentLabelsFilePathName = Properties.Settings.Default.RecentLabelsFilePathName;
            _LabelsFilesEncoding = Properties.Settings.Default.LabelsFiles;
            RecentExportFolderPath = Properties.Settings.Default.RecentExportFolderPath;

            _FrameSettingsList = DecodeFrameSettings(Properties.Settings.Default.FrameSettingsList);
            var recentFrameSettings = Properties.Settings.Default.RecentSelectedFrameSettings;
            foreach (var frameSettings in _FrameSettingsList)
            {
                if (frameSettings.Name == recentFrameSettings)
                {
                    _SelectedFrameSettings = frameSettings;
                    break;
                }
            }
            framesView.SetSettings(_FrameSettingsList, _SelectedFrameSettings);

            var location = Properties.Settings.Default.WindowLocation;
            var size = Properties.Settings.Default.WindowSize;
            var state = Properties.Settings.Default.WindowState;
            var orientation = Properties.Settings.Default.WindowLayout;
            var primarySplitterDistance = Properties.Settings.Default.PrimarySplitterDistance;
            var secondarySplitterDistance = Properties.Settings.Default.SecondarySplitterDistance;
            var colorTheme = Properties.Settings.Default.ColorTheme;

            ColorTheme.Set(this, (ColorThemeType)colorTheme);

            var displaySettings = Properties.Settings.Default.DisplaySettings;
            if (displaySettings.Length > 0)
                DisplaySettings = DisplaySettings.Deserialize(displaySettings)!;
            else
                DisplaySettings = new DisplaySettings();

            if (orientation < 0)
                orientation = (int)Orientation.Horizontal;

            if (primarySplitterDistance <= 0 || secondarySplitterDistance <= 0)
            {
                primarySplitterDistance = Height / 4;
                secondarySplitterDistance = 4 * Height / 8;
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

            var screenBounds = Screen.FromPoint(location).WorkingArea;
            if (!screenBounds.Contains(new Rectangle(location, size)))
                location = new Point(100, 100);

            StartPosition = FormStartPosition.Manual;
            Location = location;
            Size = size;
            WindowState = (FormWindowState)state;
            primarySplitContainer.SplitterDistance = primarySplitterDistance;
            secondarySplitContainer.Orientation = (Orientation)orientation;
            secondarySplitContainer.SplitterDistance = secondarySplitterDistance;

            ApplyFontScaling(this, DisplaySettings.FontScaling);
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
            openButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            undoButton.Enabled = (AppState == 0) && _UndoRedoHistory.CanUndo();
            redoButton.Enabled = (AppState == 0) && _UndoRedoHistory.CanRedo();
            selectAllButton.Enabled = timelineView.CanSelectAll();
            zoomInButton.Enabled = timelineView.CanZoomIn();
            zoomOutButton.Enabled = timelineView.CanZoomOut();
            fitSelectionButton.Enabled = timelineView.CanFitSelection();
            fitFramesButton.Enabled = timelineView.CanFitFrames();
            hotRoutinesButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            hotPathsButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
            memoryZeroPageCheckBox.Enabled = (AppState & AppStateFlags.DynamicMemoryAnalysis) == 0;
            labelsButton.Enabled = (AppState & AppStateFlags.Loading) == 0;
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
        public Image FlameImage;
        public InstructionSet? InstructionSet;
        public string RecentExportFolderPath = string.Empty;

        private Routine? _SelectedRoutine;
        private CallStack? _SelectedCallStack;
        private RoutineMemoryAccess? _SelectedMemoryAccess;
        private Panel? _SelectedTab;
        private CanonicalAddress? _SelectedMemoryAddress;
        private FrameSettings? _SelectedFrameSettings;

        private string _RecentPerfFilePathName = string.Empty;
        private int _RecentStartCycleCount = 0;
        private int _RecentEndCycleCount = 0;
        private string _RecentLabelsFilePathName = string.Empty;
        private string _LabelsFilesEncoding = string.Empty;
        private int _SuppressTabChange;
        private int _SuppressCheckBoxChange;
        private List<LabelsFile> _LabelsFiles = [];
        private List<FrameSettings> _FrameSettingsList = [];

        private UndoRedoHistory _UndoRedoHistory;
        private Model _Model;
        private LabelResolver _LabelResolver;
        private CPUAnalysis _CPUAnalysis;
        private MemoryAnalysis _MemoryAnalysis;
        private FrameAnalysis _FrameAnalysis;
        private Font _BaseFont;
    }
}
