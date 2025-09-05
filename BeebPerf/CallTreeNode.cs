namespace BeebPerf
{
    public class CallTreeNode
    {
        public CallTreeNode(StackFrame stackFrame)
        {
            Context = stackFrame;
            Routine = stackFrame.Routine;
            CPUMetrics = stackFrame.Routine.CPUMetricsByStack[stackFrame];
        }

        public readonly Routine Routine;
        public readonly StackFrame Context;
        public readonly CPUMetrics CPUMetrics;
        public List<CallTreeNode> Children = new();
    }
}