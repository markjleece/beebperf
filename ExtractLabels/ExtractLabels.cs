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

using System.Text.RegularExpressions;

public class ExtractLabels
{
    // 
    // Extracts labels and their addresses from an assembler listing file
    // Supports BeebAsm, Baron, ACME, and other common BBC Micro assemblers
    //
    public static List<(string Name, ushort Address)> Extract(
        string[] lines, 
        bool includeAssignments, 
        int addressesFrom)
    {
        // strip comments
        for (int i = 0; i < lines.Length; i++)
            lines[i] = StripComment(lines[i]).Trim();

        // strip line numbers
        if (HasLineNumbers(lines))
            for (int i = 0; i < lines.Length; i++)
                lines[i] = StripLineNumber(lines[i]).Trim();

        // parse labels
        var labels = new Dictionary<string, long>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < lines.Length; i++)
        {
            string line = lines[i];
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // match label = value
            var eq = regexEquate.Match(line);
            if (eq.Success)
            {
                var label = eq.Groups["label"].Value;
                var address = Convert.ToInt64(eq.Groups["addr"].Value, 16);
                if (includeAssignments && address >= addressesFrom)
                    labels[label] = address;
                continue;
            }

            // match label following address
            var al = regexAddrThenLabel.Match(line);
            if (al.Success)
            {
                var label = al.Groups["label"].Value;
                var address = Convert.ToInt64(al.Groups["addr"].Value, 16);
                if (address >= addressesFrom)
                    labels[label] = address;
                continue;
            }

            // match label followed by address on next line
            var lo = regexLabelOnly.Match(line);
            if (lo.Success)
            {
                string label = lo.Groups["label"].Value;
                if (i + 1 < lines.Length)
                {
                    var m = regexFirstAddress.Match(lines[i + 1]);
                    if (m.Success)
                    {
                        var address = Convert.ToInt64(m.Groups["addr"].Value, 16);
                        if (address >= addressesFrom)
                            labels[label] = address;
                    }
                }
                continue;
            }
        }

        // sort labels by address, then name
        return labels.OrderBy(kv => kv.Value).ThenBy(kv => kv.Key, StringComparer.OrdinalIgnoreCase)
            .Select(kv => (kv.Key, (ushort)kv.Value)).ToList();
    }

    private static string StripComment(string s)
    {
        if (string.IsNullOrEmpty(s)) return s;

        bool inDouble = false, inSingle = false;

        for (int i = 0; i < s.Length; i++)
        {
            char c = s[i];

            if (c == '"' && !inSingle)
            {
                if (i + 1 < s.Length && s[i + 1] == '"') { i++; continue; }
                inDouble = !inDouble; continue;
            }

            if (c == '\'' && !inDouble)
            {
                if (i + 1 < s.Length && s[i + 1] == '\'') { i++; continue; }
                inSingle = !inSingle; continue;
            }

            if (i == 0 && c == '*') return "";

            if (c == ';' && !inDouble && !inSingle) return s[..i];

            if (!inDouble && !inSingle &&
                i + 3 <= s.Length &&
                s[i..].StartsWith("REM", StringComparison.OrdinalIgnoreCase))
                return s[..i];
        }

        return s;
    }

    private static bool HasLineNumbers(string[] lines)
    {
        // detect if listing has line numbers
        bool hasLineNumbers = false;
        int lastLineNumber = 0;
        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line))
                continue;

            // match a leading number (hex or decimal)
            var m = Regex.Match(line, @"^\s*(?:\$|&|0x)?(?<number>[0-9A-Fa-f]+)");
            if (!m.Success)
                continue;

            // if match purely decimal? if not its hex so no line numbers
            string token = m.Groups["number"].Value;
            if (!Regex.IsMatch(token, @"^[0-9]+$"))
                return false;

            // has line number, does it increment from 1?
            int lineNumber = int.Parse(token);
            if (lineNumber == lastLineNumber + 1)
            {
                hasLineNumbers = true;
                lastLineNumber = lineNumber;
            }
            else
            {
                return false;
            }
        }

        return hasLineNumbers;
    }

    private static string StripLineNumber(string line)
    { 
        var matchLineNumber = regexLineNumber.Match(line);
        return matchLineNumber.Success ? line[matchLineNumber.Length..].TrimStart() : line;
    }

    // regex patterns
    private static readonly Regex regexEquate =
        new (@"^\s*(?<label>\.[A-Za-z_][A-Za-z0-9_.]*)\s*=\s*(?:\$|&|0x)?(?<addr>[0-9A-Fa-f]+)\s*$");

    private static readonly Regex regexLabelOnly =
        new(@"^\s*(?<label>\.[A-Za-z_][A-Za-z0-9_.]*)\s*$");

    private static readonly Regex regexAddrThenLabel =
        new(@"^\s*(?:\$|&|0x)?(?<addr>[0-9A-Fa-f]+)\s+(?<label>\.[A-Za-z_][A-Za-z0-9_.]*)\s*$");

    private static readonly Regex regexFirstAddress =
        new(@"^\s*(?:\$|&|0x)?(?<addr>[0-9A-Fa-f]+)(?:\s+|$)");

    private static readonly Regex regexLineNumber =
        new(@"^\s*(?<lineNumber>[0-9]+)(?:\s+|$)");
}
