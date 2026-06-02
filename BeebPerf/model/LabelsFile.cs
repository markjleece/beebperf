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

namespace BeebPerf.model
{
    //
    // Represents a labels file loaded from disk. It includes the
    // file name, the list of contained labels, the file’s load
    // status, and whether the labels are currently enabled.
    //
    public enum LabelsFileStatus
    {
        None = 0,
        Loaded = 1,
        Error_FileNotFound = 2,
        Error_FileNotReadable = 4,
        Error_InvalidFileFormat = 4,
        Error_Other = 5
    }

    public class LabelsFile
    {
        public LabelsFile Clone()
        {
            return new()
            {
                FileName = FileName,
                Labels = Labels.ToList(),
                Status = Status,
                Enabled = Enabled,
                Transient = Transient
            };
        }

        public required string FileName = string.Empty;
        public required List<(string Name, ushort Address)> Labels = new();
        public required LabelsFileStatus Status = LabelsFileStatus.None;
        public required bool Enabled = false;
        public required bool Transient = false;
    }
}