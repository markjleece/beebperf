using System.Diagnostics;
using System.Drawing;
using System.Runtime.Intrinsics.X86;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm()
        {
            InitializeComponent();
            UpdateState();
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
    }
}
