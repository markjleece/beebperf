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

using static System.Net.WebRequestMethods;

public sealed class FilesWatcher : IDisposable
{
    public void WatchFiles(IEnumerable<string> files, Action<string> fileChanged)
    {
        foreach (var w in _Watchers.Values)
            w.Dispose();

        _FileChanged = fileChanged;
        _Files = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        _Watchers = new Dictionary<string, FileSystemWatcher>(StringComparer.OrdinalIgnoreCase);

        foreach (var file in files)
        {
            var fullPath = Path.GetFullPath(file);
            _Files.Add(fullPath);

            var dir = Path.GetDirectoryName(fullPath)!;

            if (!_Watchers.ContainsKey(dir))
            {
                var watcher = new FileSystemWatcher(dir)
                {
                    NotifyFilter =
                        NotifyFilters.LastWrite |
                        NotifyFilters.Size |
                        NotifyFilters.FileName
                };

                watcher.Changed += OnChanged;
                watcher.Created += OnChanged;
                watcher.Renamed += OnRenamed;

                watcher.EnableRaisingEvents = true;
                _Watchers.Add(dir, watcher);
            }
        }
    }

    private void OnChanged(object sender, FileSystemEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);

        if (_Files.Contains(fullPath))
            Raise(fullPath);
    }

    private void OnRenamed(object sender, RenamedEventArgs e)
    {
        var fullPath = Path.GetFullPath(e.FullPath);

        if (_Files.Contains(fullPath))
            Raise(fullPath);
    }

    private void Raise(string file)
    {
        lock (_Lock)
        {
            _FileChanged?.Invoke(file);
        }
    }

    public void Dispose()
    {
        foreach (var w in _Watchers.Values)
            w.Dispose();
    }

    public event Action<string>? _FileChanged;
    private HashSet<string> _Files = new();
    private Dictionary<string, FileSystemWatcher> _Watchers = new();
    private object _Lock = new();
}
