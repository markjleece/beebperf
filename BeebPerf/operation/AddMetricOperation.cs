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
using BeebPerf.ux;

namespace BeebPerf.operation
{
    //
    // Add metric UX operation
    //
    class AddMetricOperation : Operation
    {
        public AddMetricOperation(
            BeebPerfForm form, 
            Metric? selectedMetric, 
            List<Metric> metrics,
            Instruction[] instructions)
        {
            _Form = form;
            _SelectedMetric = selectedMetric;
            _Metrics = metrics;
            _Instructions = instructions;
            _NewMetric = new()
            {
                Name = string.Empty,
                Type = Metric.MetricType.StartAndEndAddresses,
                StartAddress = new CanonicalAddress(0, MemoryPage.WholeRam),
                EndAddress = new CanonicalAddress(0, MemoryPage.WholeRam),
                ThresholdCycles = 40000,
                DisplayAnalysis = true
            };
        }

        public override bool Execute()
        {
            List<string> reservedNames = [];
            foreach (Metric metric in _Metrics)
                reservedNames.Add(metric.Name);

            var dialog = new MetricDialog(
                MetricDialog.DialogMode.New, 
                _NewMetric, 
                reservedNames,
                _Instructions,
                _Form);

            if (dialog.ShowDialog() == DialogResult.OK)
            {
                Redo();
                return true;
            }

            return false;
        }

        public override void Redo()
        {
            _Metrics.Add(_NewMetric);
            _Form.SetSelectedMetricInternal(_NewMetric!);
        }

        public override void Undo()
        {
            _Metrics.Remove(_NewMetric!);
            if (_SelectedMetric != null)
                _Form.SetSelectedMetricInternal(_SelectedMetric);
            else
                _Form.ClearSelectedMetricInternal();
        }

        private BeebPerfForm _Form;
        private Metric _NewMetric;
        private Metric? _SelectedMetric;
        private List<Metric> _Metrics;
        private Instruction[] _Instructions;
    }
}
