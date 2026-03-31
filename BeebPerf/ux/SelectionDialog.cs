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

using System.Diagnostics;

namespace BeebPerf.ux
{
    public partial class SelectionDialog : Form
    {
        public int AnalysisFrom;
        public int AnalysisTo;

        public SelectionDialog(int analysisFrom, int analysisTo, BeebPerfForm form)
        {
            InitializeComponent();
            Owner = form;

            AnalysisFrom = analysisFrom;
            AnalysisTo = analysisTo;
        }

        private void SelectionDialog_Shown(object sender, EventArgs e)
        {
            Reset();
        }

        private void OkButton_Click(object sender, EventArgs e)
        {
            AnalysisFrom = ParseCycles(fromCyclesTextBox.Text);
            AnalysisTo = ParseCycles(toCyclesTextBox.Text);
        }

        private void Reset()
        {
            _IgnoreTextChanges = true;
            fromCyclesTextBox.Text = AnalysisFrom.ToString();
            toCyclesTextBox.Text = AnalysisTo.ToString();
            _IgnoreTextChanges = false;

            _InputsFlags = [FromCyclesFlag, ToCyclesFlag];
            UpdateState();

            fromCyclesTextBox.SelectionStart = fromCyclesTextBox.TextLength;
            fromCyclesTextBox.SelectionLength = 0;
            fromCyclesTextBox.Focus();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            Reset();
        }

        private void fromSecondsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(FromSecondsFlag);
        }

        private void fromCyclesTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(FromCyclesFlag);
        }

        private void toSecondsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(ToSecondsFlag);
        }

        private void toCyclesTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(ToCyclesFlag);
        }

        private void durationSecondsTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(DurationSecondsFlag);
        }

        private void durationCyclesTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreTextChanges) return;
            UpdateInputs(DurationCyclesFlag);
        }

        private void UpdateInputs(byte input)
        {
            // remove input and its sibling
            byte sibling = input switch
            {
                FromSecondsFlag => FromCyclesFlag,
                FromCyclesFlag => FromSecondsFlag,
                ToSecondsFlag => ToCyclesFlag,
                ToCyclesFlag => ToSecondsFlag,
                DurationSecondsFlag => DurationCyclesFlag,
                DurationCyclesFlag => DurationSecondsFlag,
                _ => 0
            };

            _InputsFlags.Remove(input);
            _InputsFlags.Remove(sibling);

            // insert input
            _InputsFlags.Insert(0, input);

            // ensure there are only two inputs
            while (_InputsFlags.Count > 2)
                _InputsFlags.RemoveAt(2);

            UpdateState();
        }

        private void UpdateState()
        {
            _IgnoreTextChanges = true;

            // convert input boxes pairs
            int inputsFlags = (_InputsFlags[0] | _InputsFlags[1]);

            if ((inputsFlags & FromSecondsFlag) != 0)
            {
                double fromSeconds = ParseSeconds(fromSecondsTextBox.Text);
                fromCyclesTextBox.Text = SecondsToCycles(fromSeconds).ToString();
            }

            if ((inputsFlags & FromCyclesFlag) != 0)
            {
                int fromCycles = ParseCycles(fromCyclesTextBox.Text);
                fromSecondsTextBox.Text = CyclesToSeconds(fromCycles).ToString();
            }

            if ((inputsFlags & ToSecondsFlag) != 0)
            {
                double toSeconds = ParseSeconds(toSecondsTextBox.Text);
                toCyclesTextBox.Text = SecondsToCycles(toSeconds).ToString();
            }

            if ((inputsFlags & ToCyclesFlag) != 0)
            {
                int toCycles = ParseCycles(toCyclesTextBox.Text);
                toSecondsTextBox.Text = CyclesToSeconds(toCycles).ToString();
            }

            if ((inputsFlags & DurationSecondsFlag) != 0)
            {
                double durationSeconds = ParseSeconds(durationSecondsTextBox.Text);
                durationCyclesTextBox.Text = SecondsToCycles(durationSeconds).ToString();
            }

            if ((inputsFlags & DurationCyclesFlag) != 0)
            {
                int durationCycles = ParseCycles(durationCyclesTextBox.Text);
                durationSecondsTextBox.Text = CyclesToSeconds(durationCycles).ToString();
            }

            // highlight inputs
            HighlightTextBox(fromSecondsTextBox, (inputsFlags & FromSecondsFlag) != 0);
            HighlightTextBox(fromCyclesTextBox, (inputsFlags & FromCyclesFlag) != 0);
            HighlightTextBox(toSecondsTextBox, (inputsFlags & ToSecondsFlag) != 0);
            HighlightTextBox(toCyclesTextBox, (inputsFlags & ToCyclesFlag) != 0);
            HighlightTextBox(durationSecondsTextBox, (inputsFlags & DurationSecondsFlag) != 0);
            HighlightTextBox(durationCyclesTextBox, (inputsFlags & DurationCyclesFlag) != 0);

            // compute non-input row
            if ((inputsFlags & (FromSecondsFlag | FromCyclesFlag)) == 0)
            {
                int fromCycles = ParseCycles(toCyclesTextBox.Text) - ParseCycles(durationCyclesTextBox.Text);
                fromCyclesTextBox.Text = fromCycles.ToString();
                fromSecondsTextBox.Text = CyclesToSeconds(fromCycles).ToString();
            }
            else if ((inputsFlags & (ToSecondsFlag | ToCyclesFlag)) == 0)
            {
                int toCycles = ParseCycles(fromCyclesTextBox.Text) + ParseCycles(durationCyclesTextBox.Text);
                toCyclesTextBox.Text = toCycles.ToString();
                toSecondsTextBox.Text = CyclesToSeconds(toCycles).ToString();
            }
            else if ((inputsFlags & (DurationSecondsFlag | DurationCyclesFlag)) == 0)
            {
                int durationCycles = ParseCycles(toCyclesTextBox.Text) - ParseCycles(fromCyclesTextBox.Text);
                durationCyclesTextBox.Text = durationCycles.ToString();
                durationSecondsTextBox.Text = CyclesToSeconds(durationCycles).ToString();
            }
            else
                Debug.Assert(false);

            // enable/disabe reset button
            resetButton.Enabled =
                (ParseCycles(fromCyclesTextBox.Text) != AnalysisFrom ||
                 ParseCycles(toCyclesTextBox.Text) != AnalysisTo);

            // enable/disable ok button
            okButton.Enabled = ParseCycles(fromCyclesTextBox.Text) < ParseCycles(toCyclesTextBox.Text);

            _IgnoreTextChanges = false;
        }

        private void HighlightTextBox(TextBox textBox, bool bold)
        {
            textBox.BorderStyle = bold ? BorderStyle.FixedSingle : BorderStyle.None;
        }

        private double CyclesToSeconds(int value)
        {
            return (double)value / 2000000.0;
        }

        private int SecondsToCycles(double value)
        {
            return (int)double.Round(value * 2000000.0);
        }

        private int ParseCycles(string text)
        {
            try
            {
                return int.Parse(text.Trim());
            }
            catch
            {
                return 0;
            }
        }

        private double ParseSeconds(string text)
        {
            try
            {
                return double.Parse(text.Trim());
            }
            catch
            {
                return 0.0;
            }
        }

        private bool _IgnoreTextChanges = true;
        private List<byte> _InputsFlags = [];

        private const byte FromSecondsFlag = 0x01;
        private const byte FromCyclesFlag = 0x02;
        private const byte ToSecondsFlag = 0x04;
        private const byte ToCyclesFlag = 0x08;
        private const byte DurationSecondsFlag = 0x10;
        private const byte DurationCyclesFlag = 0x20;
    }
}
