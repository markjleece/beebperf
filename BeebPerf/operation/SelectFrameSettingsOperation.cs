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
    class SelectFrameSettingsOperation : Operation
    {
        public SelectFrameSettingsOperation(BeebPerfForm form, FrameSettings? newFrameSettings, FrameSettings? oldFrameSettings)
        {
            _Form = form;
            _NewFrameSettings = newFrameSettings;
            _OldFrameSettings = oldFrameSettings;
        }

        public override bool Execute()
        {
            Redo();
            return true;
        }

        public override void Redo()
        {
            if (_NewFrameSettings != null)
                _Form.SetSelectedFrameSettingsInternal(_NewFrameSettings);
            else
                _Form.ClearSelectedFrameSettingsInternal();
        }

        public override void Undo()
        {
            if (_OldFrameSettings != null)
                _Form.SetSelectedFrameSettingsInternal(_OldFrameSettings);
            else
                _Form.ClearSelectedMemoryAddressInternal();
        }

        private BeebPerfForm _Form;
        private FrameSettings? _NewFrameSettings;
        private FrameSettings? _OldFrameSettings;
    }
}
