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

namespace BeebPerf.operation
{
    class OpenOperation : Operation
    {
        public OpenOperation(string filePathName, Model model)
        {
            _FilePathName = filePathName;
            _Model = model;
            _OldModel = model.Clone();
            _NewModel = new();
        }

        public override async Task<bool> Execute()
        {
            return await Task.Run(() =>
            {
                try
                {
                    var perfReader = new PerfReader();
                    Model? model = perfReader.ReadFile(_FilePathName);
                    if (model == null)
                        throw new Exception($"An error occurred reading {_FilePathName}");
                    _NewModel = model;
                    Redo();
                    return true;
                }
                catch (Exception ex)
                {
                    return false;
                }
            });
        }

        public override void Redo()
        {
            _Model.Set(_NewModel);
        }

        public override void Undo()
        {
            _Model.Set(_OldModel);
        }

        private string _FilePathName;
        private Model _NewModel;
        private Model _OldModel;
        private Model _Model;
    }
}
