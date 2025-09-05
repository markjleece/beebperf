namespace BeebPerf
{
    public class CPUMetrics
    {
        public CPUMetrics()
        {
        }

        public CPUMetrics(CPUMetrics other)
        {
            Count = other.SelfCycleCount;
            SelfCycleCount = other.SelfCycleCount;
            InclusiveCycleCount = other.InclusiveCycleCount;
            ElapsedCycleCount = other.ElapsedCycleCount;
        }

        public void Clear()
        {
            Count = 0;
            SelfCycleCount = 0;
            InclusiveCycleCount = 0;
            ElapsedCycleCount = 0;
        }

        public void Add(CPUMetrics other)
        {
            Count += other.Count;
            SelfCycleCount += other.SelfCycleCount;
            InclusiveCycleCount += other.InclusiveCycleCount;
            ElapsedCycleCount += other.ElapsedCycleCount;
        }

        public int Count;
        public int SelfCycleCount;
        public int InclusiveCycleCount;
        public int ElapsedCycleCount;
    }
}