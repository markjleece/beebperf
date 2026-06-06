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
    // Remove metric operation
    //
    class RemoveMetricOperation : Operation
    {
        public RemoveMetricOperation(BeebPerfForm form, Metric metric, List<Metric> metrics)
        {
            _Form = form;
            _Metrics = metrics;
            _Metric = metric;
        }

        public override bool Execute()
        {
            Redo();
            return true;
        }

        public override void Redo()
        {
            _Metrics.Remove(_Metric);
            _Form.ClearSelectedMetricInternal();
        }

        public override void Undo()
        {
            _Metrics.Add(_Metric);
            _Form.SetSelectedMetricInternal(_Metric);
        }

        private BeebPerfForm _Form;
        private Metric _Metric;
        private List<Metric> _Metrics;
    }
}
