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

namespace BeebPerf.model
{
    public class UndoRedoHistory
    {
        public bool Execute(Operation op)
        {
            bool success = op.Execute();
            if (success)
            {
                RedoHistory.Clear();
                UndoHistory.Push(op);
            }

            return success;
        }

        public bool CanUndo()
        {
            return UndoHistory.Count > 0;
        }

        public bool CanRedo()
        {
            return RedoHistory.Count > 0;
        }

        public void Undo()
        {
            Operation op = UndoHistory.Pop();
            op.Undo();
            RedoHistory.Push(op);
        }

        public void Redo()
        {
            Operation op = RedoHistory.Pop();
            op.Redo();
            UndoHistory.Push(op);
        }

        private readonly Stack<Operation> UndoHistory = new();
        private readonly Stack<Operation> RedoHistory = new();
    }
}
