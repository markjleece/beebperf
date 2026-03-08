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
    class SelectMemoryAddressOperation : Operation
    {
        public SelectMemoryAddressOperation(BeebPerfForm form, CanonicalAddress? newAddress, CanonicalAddress? prevAddress)
        {
            _Form = form;
            _NewAddress = newAddress;
            _PrevAddress = prevAddress;
        }

        public override bool Execute()
        {
            Redo();
            return true;
        }

        public override void Redo()
        {
            if (_NewAddress != null)
                _Form.SetSelectedMemoryAddressInternal((CanonicalAddress)_NewAddress);
            else
                _Form.ClearSelectedMemoryAddressInternal();
        }

        public override void Undo()
        {
            if (_PrevAddress != null)
                _Form.SetSelectedMemoryAddressInternal((CanonicalAddress)_PrevAddress);
            else
                _Form.ClearSelectedMemoryAddressInternal();
        }

        private BeebPerfForm _Form;
        private CanonicalAddress? _NewAddress;
        private CanonicalAddress? _PrevAddress;
    }
}
