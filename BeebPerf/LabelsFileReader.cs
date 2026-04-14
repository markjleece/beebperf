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
using System.Text.RegularExpressions;

namespace BeebPerf
{
    // 
    // Label files reader. Extracts labels from assembler output
    // or from label files in the form: [{'label':addressL,...}]
    //
    public class LabelsFileReader
    {
        public async Task<LabelsFile> ReadFileAsync(string fileName)
        {
            return await Task.Run(() =>
            {
                return ReadFile(fileName);
            });
        }

        public LabelsFile ReadFile(string fileName)
        {
            var status = LabelsFileStatus.None;
            var labels = new List<(string Name, ushort Address)>();

            try
            {
                string text = File.ReadAllText(fileName).Trim([' ', '\t', '\n', '\r']);

                if (text.StartsWith("[{") && text.EndsWith("}]"))
                {
                    // try to parse a BeebAsm.exe -labels option file
                  
                    text = text.Substring(2, text.Length - 4); // remove surrounding [{ and }]

                    var nameAndAddressRegex = new Regex(@"^\'(?<name>.[A-Za-z_][.A-Za-z0-9_]*)'\:(?<address>\d+)L$", RegexOptions.Compiled);

                    foreach (var part in text.Split(',', StringSplitOptions.RemoveEmptyEntries))
                    {
                        string nameAndAddress = part.Trim();
                        var m = nameAndAddressRegex.Match(nameAndAddress);
                        if (!m.Success)
                        {
                            status = LabelsFileStatus.Error_InvalidFileFormat;
                            break;
                        }

                        string name = m.Groups["name"].Value;
                        long address = long.Parse(m.Groups["address"].Value);
                        if (name.Length < 2 || address < 0 || address > 0xFFFF)
                        {
                            status = LabelsFileStatus.Error_InvalidFileFormat;
                            break;
                        }
                        labels.Add((name, (ushort)address));
                    }
                }
                else
                {
                    // try to parse a BeebAsm.exe output file

                    var nameRegex = new Regex(@"(?<=^| )(?<name>\.[A-Za-z_][A-Za-z0-9_.]*)(?=$| )", RegexOptions.Compiled);
                    var addressRegex = new Regex(@"(?<=^| )(?<address>[A-Fa-f0-9]{4})(?=$| )", RegexOptions.Compiled);

                    string name = string.Empty;
                    foreach (var line in text.Split(['\n', '\r'], StringSplitOptions.RemoveEmptyEntries))
                    {
                        var nameMatch = nameRegex.Match(line);
                        if (nameMatch.Success)
                            name = nameMatch.Groups["name"].Value;

                        var addressMatch = addressRegex.Match(line);
                        if (addressMatch.Success && name.Length > 0)
                        {
                            ushort address = (ushort)Convert.ToInt32(addressMatch.Groups["address"].Value, 16);
                            labels.Add((name, address));
                            name = string.Empty;
                        }
                    }

                    if (labels.Count == 0)
                        status = LabelsFileStatus.Error_InvalidFileFormat;
                }

                if (status == LabelsFileStatus.None)
                    status = LabelsFileStatus.Loaded;
            }
            catch (DirectoryNotFoundException)
            {
                status = LabelsFileStatus.Error_FileNotFound;
            }
            catch (FileNotFoundException)
            {
                status = LabelsFileStatus.Error_FileNotFound;
            }
            catch (UnauthorizedAccessException)
            {
                status = LabelsFileStatus.Error_FileNotReadable;
            }
            catch (IOException)
            {
                status = LabelsFileStatus.Error_FileNotReadable;
            }
            catch (Exception)
            {
                status = LabelsFileStatus.Error_Other;
            }

            return new() {
                FileName = fileName,
                Labels = labels,
                Status = status,
                Enabled = (status == LabelsFileStatus.Loaded)
            };
        }
    }
}