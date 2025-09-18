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

using System.Configuration;

namespace BeebPerf
{
    class AppSettings
    {
        static internal readonly AppSettings Instance = new();

        // Settings...
        internal string RecentFilePathName
        {
            get { return Get(RecentFilePathName_PropertyName, string.Empty/*defaultValue*/); }
            set { Set(RecentFilePathName_PropertyName, value); }
        }

        // Implementation...
        private AppSettings()
        {
            Config = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        private string Get(string name, string defaultValue)
        {
            var nameValue = Config.AppSettings.Settings[name];
            return nameValue != null ? nameValue.Value : defaultValue;
        }

        private void Set(string name, string value)
        {
            while (Config.AppSettings.Settings[name] != null)
            {
                Config.AppSettings.Settings.Remove(name);
            }

            Config.AppSettings.Settings.Add(name, value);
            Config.Save();
            ConfigurationManager.RefreshSection("appSettings");
        }

        private readonly Configuration Config;

        private const string RecentFilePathName_PropertyName = "RecentFilePathName";
    }
}
