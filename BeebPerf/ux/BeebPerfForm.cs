using System.Diagnostics;

namespace BeebPerf.ux
{
    public partial class BeebPerfForm : Form
    {
        public BeebPerfForm()
        {
            InitializeComponent();
        }

        private void BeebPerfForm_Load(object sender, EventArgs e)
        {
            string filePathName = AppSettings.Instance.RecentFilePathName;
            if (filePathName != string.Empty && File.Exists(filePathName))
            {
            }

            try
            {
                var perfReader = new PerfReader();
                Model? model = perfReader.ReadFile("C:\\Users\\markl\\BeebEm\\marks.perf");
                if (model != null)
                {
                    var analysis = new CPUAnalysis((Model)model);
                    analysis.StaticAnalysis();
                    analysis.DynamicAnalysis(startCycleCount: 0, endCycleCount: int.MaxValue);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);
            }
        }

        private void openButton_Click(object sender, EventArgs e)
        {
        }

        private void undoButton_Click(object sender, EventArgs e)
        {

        }

        private void redoButton_Click(object sender, EventArgs e)
        {

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
    }
}
