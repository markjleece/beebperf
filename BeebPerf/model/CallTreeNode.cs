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

using System.Windows.Forms;

namespace BeebPerf
{
    public class CallTreeNode : TreeNode<CallTreeNode>
    {
        public CallTreeNode(CallStack callStack)
        {
            Context = callStack;
            Routine = callStack.Routine;
            CPUMetrics = callStack.Routine.MetricsByStack[callStack];
        }

        public enum SortField
        {
            SelfCPU,
            InclusiveCPU,
            ElapsedCPU,
            Count
        };

        public void Sort(SortField sortField, SortOrder sortOrder)
        {
            switch (sortField)
            {
                case SortField.SelfCPU:
                    if (sortOrder == SortOrder.Ascending)
                        Sort((a, b) => a.CPUMetrics.SelfCycleCount.CompareTo(b.CPUMetrics.SelfCycleCount));
                    else if (sortOrder == SortOrder.Descending)
                        Sort((a, b) => b.CPUMetrics.SelfCycleCount.CompareTo(a.CPUMetrics.SelfCycleCount));
                    break;

                case SortField.InclusiveCPU:
                    if (sortOrder == SortOrder.Ascending)
                        Sort((a, b) => a.CPUMetrics.InclusiveCycleCount.CompareTo(b.CPUMetrics.InclusiveCycleCount));
                    else if (sortOrder == SortOrder.Descending)
                        Sort((a, b) => b.CPUMetrics.InclusiveCycleCount.CompareTo(a.CPUMetrics.InclusiveCycleCount));
                    break;

                case SortField.ElapsedCPU:
                    if (sortOrder == SortOrder.Ascending)
                        Sort((a, b) => a.CPUMetrics.ElapsedCycleCount.CompareTo(b.CPUMetrics.ElapsedCycleCount));
                    else if (sortOrder == SortOrder.Descending)
                        Sort((a, b) => b.CPUMetrics.ElapsedCycleCount.CompareTo(a.CPUMetrics.ElapsedCycleCount));
                    break;

                case SortField.Count:
                    if (sortOrder == SortOrder.Ascending)
                        Sort((a, b) => a.CPUMetrics.ExecutionCount.CompareTo(b.CPUMetrics.ExecutionCount));
                    else if (sortOrder == SortOrder.Descending)
                        Sort((a, b) => b.CPUMetrics.ExecutionCount.CompareTo(a.CPUMetrics.ExecutionCount));
                    break;

                default:
                    break;
            }
        }

        public readonly Routine Routine;
        public readonly CallStack Context;
        public readonly RoutineMetrics CPUMetrics;
    }
}