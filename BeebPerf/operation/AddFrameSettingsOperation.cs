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
    class AddFrameSettingsOperation : Operation
    {
        public AddFrameSettingsOperation(
            BeebPerfForm form, 
            FrameSettings? selectedFrameSettings, 
            List<FrameSettings> frameSettingsList,
            Instruction[] instructions)
        {
            _Form = form;
            _SelectedFrameSettings = selectedFrameSettings;
            _FrameSettingsList = frameSettingsList;
            _Instructions = instructions;
            _NewFrameSettings = new()
            {
                Name = string.Empty,
                Type = FrameSettings.FrameType.StartAndEndAddresses,
                StartAddress = new CanonicalAddress(0, MemoryPage.WholeRam),
                EndAddress = new CanonicalAddress(0, MemoryPage.WholeRam),
                ThresholdCycles = 40000
            };
        }

        public override bool Execute()
        {
            List<string> reservedNames = [];
            foreach (FrameSettings frameSettings in _FrameSettingsList)
                reservedNames.Add(frameSettings.Name);

            var dialog = new FrameSettingsDialog(
                FrameSettingsDialog.DialogMode.New, 
                _NewFrameSettings, 
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
            _FrameSettingsList.Add(_NewFrameSettings);
            _Form.SetSelectedFrameSettingsInternal(_NewFrameSettings!);
        }

        public override void Undo()
        {
            _FrameSettingsList.Remove(_NewFrameSettings!);
            if (_SelectedFrameSettings != null)
                _Form.SetSelectedFrameSettingsInternal(_SelectedFrameSettings);
            else
                _Form.ClearSelectedFrameSettingsInternal();
        }

        private BeebPerfForm _Form;
        private FrameSettings _NewFrameSettings;
        private FrameSettings? _SelectedFrameSettings;
        private List<FrameSettings> _FrameSettingsList;
        private Instruction[] _Instructions;
    }
}
