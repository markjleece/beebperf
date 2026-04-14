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
    // Select tab UX operation
    //
    class SelectTabOperation : Operation
    {
        public SelectTabOperation(BeebPerfForm form, Panel? newTab, Panel? prevTab)
        {
            _Form = form;
            _NewTab = newTab;
            _PrevTab = prevTab;
        }

        public override bool Execute()
        {
            Redo();
            return true;
        }

        public override void Redo()
        {
            _Form.SelectTabInternal(_NewTab);
        }

        public override void Undo()
        {
            _Form.SelectTabInternal(_PrevTab);
        }

        private BeebPerfForm _Form;
        private Panel? _NewTab;
        private Panel? _PrevTab;
    }
}
