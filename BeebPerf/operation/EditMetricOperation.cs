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
    // Edit metric UX operation
    //
    class EditMetricOperation : Operation
    {
        public EditMetricOperation(
            BeebPerfForm form, 
            Metric metric, 
            List<Metric> metrics,
            Instruction[] instructions)
        {
            _Form = form;
            _Metrcis = metrics;
            _Instructions = instructions;

            _ReservedNames = [];
            foreach (Metric settings in metrics)
                if (settings != metric)
                    _ReservedNames.Add(settings.Name);

            _OldMetric = metric;
            _NewMetric = metric.Clone();
        }

        public override bool Execute()
        {
            var dialog = new MetricDialog(
                MetricDialog.DialogMode.Edit, 
                _NewMetric, 
                _ReservedNames, 
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
            _Metrcis.Remove(_OldMetric);
            _Metrcis.Add(_NewMetric);
            _Form.ClearSelectedMetricInternal();
            _Form.SetSelectedMetricInternal(_NewMetric!);
        }

        public override void Undo()
        {
            _Metrcis.Remove(_NewMetric);
            _Metrcis.Add(_OldMetric);
            _Form.ClearSelectedMetricInternal();
            _Form.SetSelectedMetricInternal(_OldMetric);
        }

        private BeebPerfForm _Form;
        private List<Metric> _Metrcis;
        private Instruction[] _Instructions;
        private List<string> _ReservedNames;
        private Metric _OldMetric;
        private Metric _NewMetric;
    }
}
