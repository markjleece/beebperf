// --------------------------------------------------------------
// An Adventure In Time - A Doctor Who fan game for the BBC Micro
// Model B
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

using BeebPerf.model;
using BeebPerf.ux;

namespace BeebPerf.operation
{
    class EditSettingsOperation : Operation
    {
        public EditSettingsOperation(BeebPerfForm form)
        {
            _Form = form;
            _PrevSettings = form.DisplaySettings;
            _PrevColorTheme = ColorTheme.Get();
            _NewSettings = new();
        }

        public override bool Execute()
        {
            EditSettingsDialog dialog = new(_PrevSettings);
            dialog.Owner = _Form;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _NewSettings = dialog.Settings!;
                _NewColorTheme = ColorTheme.Get();
                Redo();
                return true;
            }

            ColorTheme.Set(_Form, _PrevColorTheme);
            return false;
        }

        public override void Redo()
        {
            _Form.DisplaySettings = _NewSettings;
            ColorTheme.Set(_Form, _NewColorTheme);
        }

        public override void Undo()
        {
            _Form.DisplaySettings = _PrevSettings;
            ColorTheme.Set(_Form, _PrevColorTheme);
        }

        private BeebPerfForm _Form;
        private ColorThemeType _NewColorTheme;
        private ColorThemeType _PrevColorTheme;
        private DisplaySettings _NewSettings;
        private DisplaySettings _PrevSettings;
    }
}
