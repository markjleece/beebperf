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
using static BeebPerf.MemoryAnalysis;

namespace BeebPerf.operation
{
    class SelectRoutineOperation : Operation
    {
        public SelectRoutineOperation(
            BeebPerfForm form,
            Routine? newRoutine, CallStack? newCallStack, RoutineMemoryAccess? newMemoryAccess,
            Routine? prevRoutine, CallStack? prevCallStack, RoutineMemoryAccess? prevMemoryAccess)
        {
            _Form = form;
            _NewRoutine = newRoutine;
            _PrevRoutine = prevRoutine;
            _NewCallStack = newCallStack;
            _PrevCallStack = prevCallStack;
            _NewMemoryAccess = newMemoryAccess;
            _PrevMemoryAccess = prevMemoryAccess;
        }

        public override bool Execute()
        {
            Redo();
            return true;
        }

        public override void Redo()
        {
            if (_NewRoutine != null)
                _Form.SetSelectedRoutineInternal(_NewRoutine, _NewCallStack, _NewMemoryAccess);
            else
                _Form.ClearSelectedRoutineInternal();
        }

        public override void Undo()
        {
            if (_PrevRoutine != null)
                _Form.SetSelectedRoutineInternal(_PrevRoutine, _PrevCallStack, _PrevMemoryAccess);
            else
                _Form.ClearSelectedRoutineInternal();
        }

        private BeebPerfForm _Form;
        private Routine? _NewRoutine;
        private Routine? _PrevRoutine;
        private CallStack? _NewCallStack;
        private CallStack? _PrevCallStack;
        private RoutineMemoryAccess? _NewMemoryAccess;
        private RoutineMemoryAccess? _PrevMemoryAccess;
    }
}
