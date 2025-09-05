using System.Diagnostics;
using System.Runtime.InteropServices;

namespace BeebPerf
{
    public enum RoutineType
    {
        MaskableISR,
        NonMaskableISR,
        JSR,
        Pseudo,
        Unknown
    }

    public class Routine
    {
        public Routine()
        {
            StartAddress = new CanonicalAddress();
            EndAddress = new CanonicalAddress();
            Label = String.Empty;
        }

        public Routine(CanonicalAddress address, RoutineType routineType, string label)
        {
            Label = label;
            RoutineType = routineType;
            StartAddress = address;
            EndAddress = address;
        }

        public string Label;
        public RoutineType RoutineType;
        public CanonicalAddress StartAddress;
        public CanonicalAddress EndAddress;
        public Dictionary<StackFrame, CPUMetrics> CPUMetricsByStack = new();
        public CPUMetrics AggregateCPUMetrics = new();
    }
}