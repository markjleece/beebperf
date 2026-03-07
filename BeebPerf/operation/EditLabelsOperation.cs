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

using BeebPerf.model;
using BeebPerf.ux;

namespace BeebPerf.operation
{
    class EditLabelsOperation : Operation
    {
        public EditLabelsOperation(
            BeebPerfForm form,
            List<LabelsFile> labelsFiles,
            string recentLabelsFilePathName)
        {
            _Form = form;
            _OldLabelsFiles = labelsFiles;
            _NewLabelsFiles = [];
            _RecentLabelsFilePathName = recentLabelsFilePathName;
        }

        public override bool Execute()
        {
            LabelsDialog dialog = new(_OldLabelsFiles, _RecentLabelsFilePathName);
            dialog.Owner = _Form;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _NewLabelsFiles = dialog.LabelsFiles;
                _RecentLabelsFilePathName = dialog.RecentLabelsFilePathName;
                Redo();
                return true;
            }

            return false;
        }

        public override void Redo()
        {
            _Form.SetLabelsFiles(_NewLabelsFiles, _RecentLabelsFilePathName);
        }

        public override void Undo()
        {
            _Form.SetLabelsFiles(_OldLabelsFiles, _RecentLabelsFilePathName);
        }

        private string _RecentLabelsFilePathName;
        private List<LabelsFile> _OldLabelsFiles;
        private List<LabelsFile> _NewLabelsFiles;
        private BeebPerfForm _Form;
    }
}
