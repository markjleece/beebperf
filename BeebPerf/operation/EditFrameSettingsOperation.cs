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
    // Edit analysis frame settings UX operation
    //
    class EditFrameSettingsOperation : Operation
    {
        public EditFrameSettingsOperation(
            BeebPerfForm form, 
            FrameSettings frameSettings, 
            List<FrameSettings> frameSettingsList,
            Instruction[] instructions)
        {
            _Form = form;
            _FrameSettingsList = frameSettingsList;
            _Instructions = instructions;

            _ReservedNames = [];
            foreach (FrameSettings settings in frameSettingsList)
                if (settings != frameSettings)
                    _ReservedNames.Add(settings.Name);

            _OldFrameSettings = frameSettings;
            _NewFrameSettings = frameSettings.Clone();
        }

        public override bool Execute()
        {
            var dialog = new FrameSettingsDialog(
                FrameSettingsDialog.DialogMode.Edit, 
                _NewFrameSettings, 
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
            _FrameSettingsList.Remove(_OldFrameSettings);
            _FrameSettingsList.Add(_NewFrameSettings);
            _Form.ClearSelectedFrameSettingsInternal();
            _Form.SetSelectedFrameSettingsInternal(_NewFrameSettings!);
        }

        public override void Undo()
        {
            _FrameSettingsList.Remove(_NewFrameSettings);
            _FrameSettingsList.Add(_OldFrameSettings);
            _Form.ClearSelectedFrameSettingsInternal();
            _Form.SetSelectedFrameSettingsInternal(_OldFrameSettings);
        }

        private BeebPerfForm _Form;
        private List<FrameSettings> _FrameSettingsList;
        private Instruction[] _Instructions;
        private List<string> _ReservedNames;
        private FrameSettings _OldFrameSettings;
        private FrameSettings _NewFrameSettings;
    }
}
