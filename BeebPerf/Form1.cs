using System.Diagnostics;

namespace BeebPerf
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();
            try
            {
                var perfReader = new PerfReader();
                Model? model = perfReader.ReadFile("C:\\Users\\markl\\BeebEm\\marks.perf");
                if (model != null)
                {
                    var analysis = new Analysis((Model)model);
                    analysis.StaticCPUAnalysis();
                    analysis.DynamicCPUAnalysis(startCycleCount:0, endCycleCount:int.MaxValue);
                }
            }
            catch (Exception ex)
            {
                Debug.Assert(false);
            }
        }
    }
}
