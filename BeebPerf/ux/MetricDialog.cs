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
using System.Runtime.InteropServices;

namespace BeebPerf.ux
{
    public partial class MetricDialog : Form
    {
        public enum DialogMode
        {
            New,
            Edit
        }

        public MetricDialog(
            DialogMode dialogMode, 
            Metric metric, 
            List<string> reservedNames, 
            Instruction[] instructions,
            BeebPerfForm form)
        {
            _DialogMode = dialogMode;
            _ReservedNames = reservedNames;
            _FrameSettings = metric;
            _Instructions = instructions;

            InitializeComponent();
            Owner = form;

            Text = dialogMode switch
            {
                DialogMode.New => "New Metric",
                DialogMode.Edit => "Edit Metric",
                _ => throw new InvalidOperationException()
            };

            resetButton.Visible = (_DialogMode == DialogMode.Edit);

            SetCueText(nameTextBox, "<name>");
            SetCueText(startTextBox, "<address>");
            SetCueText(endTextBox, "<address>");

            PopulateControls();
        }

        private void PopulateControls()
        {
            _IgnoreChanges = true;

            // name
            nameTextBox.Text = _FrameSettings.Name;

            // type
            typeComboBox.Items.Add("Start & end addresses");
            typeComboBox.Items.Add("Routine address");
            typeComboBox.Items.Add("JSR address");
            typeComboBox.SelectedIndex = (int)_FrameSettings.Type - 1;

            // start and end addresses
            if (_FrameSettings.StartAddress.Address != 0)
                startTextBox.Text = FormatAddress(_FrameSettings.StartAddress);
            if (_FrameSettings.EndAddress.Address != 0 && _FrameSettings.Type == Metric.MetricType.StartAndEndAddresses)
                endTextBox.Text = FormatAddress(_FrameSettings.EndAddress);

            // page
            foreach (var name in Enum.GetNames(typeof(MemoryPage)))
                if (name != "Count")
                    pageComboBox.Items.Add(name);
            pageComboBox.SelectedIndex = (int)_FrameSettings.StartAddress.Page;

            // duration & cycles
            durationComboBox.Items.Add("None");
            durationComboBox.Items.Add("1/50 second");
            durationComboBox.Items.Add("Custom");
            durationComboBox.SelectedIndex = _FrameSettings.ThresholdCycles switch
            {
                0 => 0,
                40000 => 1,
                _ => 2
            };
            cyclesTextBox.Text = _FrameSettings.ThresholdCycles.ToString();

            _IgnoreChanges = false;

            CancelAddressValidation();
            UpdateState();
        }

        private void UpdateState()
        {
            // name
            string nameText = nameTextBox.Text.Trim();
            bool validName =
                nameText.Length > 0 &&
                !nameText.Contains(';') &&
                !nameText.Contains('|') &&
                !_ReservedNames.Contains(nameText);

            nameErrorLabel.Visible = _ReservedNames.Contains(nameText) || nameText.Contains(';') || nameText.Contains('|');
            if (_ReservedNames.Contains(nameText))
                nameErrorLabel.Text = "Name is not unique";
            else if (nameText.Contains(';') || nameText.Contains('|'))
                nameErrorLabel.Text = "Cannot contain ';' or '|'";
            else
                nameErrorLabel.Text = string.Empty;

            // addresses
            var startAddress = ParseAddress(startTextBox);
            var endAddress = ParseAddress(endTextBox);

            var frameType = (Metric.MetricType)(typeComboBox.SelectedIndex + 1);
            switch (frameType)
            {
                case Metric.MetricType.StartAndEndAddresses:
                    startLabel.Text = "Start address";
                    endLabel.Text = "End address";
                    break;

                case Metric.MetricType.RoutineAddress:
                    startLabel.Text = "Address";
                    break;

                case Metric.MetricType.JSRAddress:
                    startLabel.Text = "Address";
                    break;
            }

            string hexPrefix = GetHexPrefix();
            startHexLabel.Text = hexPrefix;
            endHexLabel.Text = hexPrefix;

            bool showEndAddress = (frameType == Metric.MetricType.StartAndEndAddresses);
            endLabel.Visible = showEndAddress;
            endHexLabel.Visible = showEndAddress;
            endTextBox.Visible = showEndAddress;

            // threshold
            bool validThreshold = (durationComboBox.SelectedIndex == 0 || ParseCycles() > 0);
            cyclesErrorLabel.Visible = !validThreshold;

            // reset button
            if (_DialogMode == DialogMode.Edit)
            {
                resetButton.Enabled = nameText != _FrameSettings.Name ||
                    frameType != _FrameSettings.Type ||
                    ParseAddress(startTextBox).Address != _FrameSettings.StartAddress.Address ||
                    ParseAddress(startTextBox).Page != _FrameSettings.StartAddress.Page ||
                    ParseAddress(endTextBox).Address != _FrameSettings.EndAddress.Address ||
                    ParseAddress(endTextBox).Page != _FrameSettings.EndAddress.Page ||
                    ParseCycles() != _FrameSettings.ThresholdCycles;
            }

            // validate address start and end addresses (asynchonously)
            okButton.Enabled = false;
            ValidateAddressesAsync(startAddress, endAddress, frameType)
                .ContinueWith((task) =>
                {
                    if (task.IsCanceled)
                        return;

                    var (startResult, endResult) = task.Result;

                    if (IsDisposed)
                        return;

                    startErrorLabel.Text = startResult.ErrorMessage;
                    startErrorLabel.Visible = (!startResult.IsValid && HasValue(startTextBox));

                    endErrorLabel.Text = endResult.ErrorMessage;
                    endErrorLabel.Visible = (!endResult.IsValid && HasValue(endTextBox));

                    // ok button
                    okButton.Enabled = validName && startResult.IsValid && endResult.IsValid && validThreshold;
                }, TaskScheduler.FromCurrentSynchronizationContext());
        }

        private Task<(AddressValidationResult startResult, AddressValidationResult endResult)> ValidateAddressesAsync(
            CanonicalAddress startAddress, 
            CanonicalAddress endAddress, 
            Metric.MetricType frameType)
        {
            CancelAddressValidation();

            var cts = new CancellationTokenSource();
            _AddressValidationCts = cts;

            return Task.Run(() =>
            {
                AddressValidationResult startResult;
                AddressValidationResult endResult;

                switch (frameType)
                {
                    case Metric.MetricType.StartAndEndAddresses:
                        startResult = ValidateAddressForInstruction(startAddress, cts);
                        endResult = ValidateAddressForInstruction(endAddress, cts);
                        break;

                    case Metric.MetricType.RoutineAddress:
                        startResult = ValidateAddressForInstruction(startAddress, cts);
                        endResult = new AddressValidationResult(true, string.Empty);
                        break;

                    case Metric.MetricType.JSRAddress:
                        startResult = ValidateAddressForJSR(startAddress, cts);
                        endResult = new AddressValidationResult(true, string.Empty);
                        break;

                    default:
                        startResult = new AddressValidationResult(false, string.Empty);
                        endResult = new AddressValidationResult(false, string.Empty);
                        break;
                }

                cts.Token.ThrowIfCancellationRequested();

                return (startResult, endResult);

            }, cts.Token);
        }

        private AddressValidationResult ValidateAddressForInstruction(CanonicalAddress address, CancellationTokenSource cts)
        {
            if (address.Address == 0)
                return new AddressValidationResult(false, "Invalid address format");

            if (!address.IsValid())
                return new AddressValidationResult(false, "Address outside page range");

            if (!AddressMatchesInstruction(address, cts))
                return new AddressValidationResult(false, "No instruction at address");

            return new AddressValidationResult(true, string.Empty);
        }

        private AddressValidationResult ValidateAddressForJSR(CanonicalAddress address, CancellationTokenSource cts)
        {
            if (address.Address == 0)
                return new AddressValidationResult(false, "Invalid address format");

            if (!address.IsValid())
                return new AddressValidationResult(false, "Address outside page range");

            if (!AddressMatchesJSRInstruction(address, cts))
                return new AddressValidationResult(false, "No JSR instruction at address");

            return new AddressValidationResult(true, string.Empty);
        }

        private void CancelAddressValidation()
        {
            try
            {
                _AddressValidationCts?.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // cancellation token disposed, ignore
            }

            _AddressValidationCts = null; // don't dispose here - let it be garbage collected
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            CancelAddressValidation();
            _AddressValidationCts?.Dispose();
            _AddressValidationCts = null;

            base.OnFormClosing(e);
        }

        private void nameTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            UpdateState();
        }

        private void typeComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            UpdateState();
        }

        private void pageComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            UpdateState();
        }

        private void startTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            UpdateState();
        }

        private void endTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            UpdateState();
        }

        private void cyclesTextBox_TextChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            _IgnoreChanges = true;

            int cycles = ParseCycles();
            if (cycles == 0)
                durationComboBox.SelectedIndex = 0;
            else if (cycles == 40000)
                durationComboBox.SelectedIndex = 1;
            else
                durationComboBox.SelectedIndex = 2;

            _IgnoreChanges = false;
            UpdateState();
        }

        private void durationComboBox_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_IgnoreChanges) return;
            _IgnoreChanges = true;

            switch (durationComboBox.SelectedIndex)
            {
                case 0:
                    cyclesTextBox.Text = "0";
                    break;

                case 1:
                    cyclesTextBox.Text = "40000";
                    break;

                case 2:
                    cyclesTextBox.SelectionStart = cyclesTextBox.TextLength;
                    cyclesTextBox.SelectionLength = 0;
                    cyclesTextBox.Focus();
                    break;
            }

            _IgnoreChanges = false;
            UpdateState();
        }

        private void resetButton_Click(object sender, EventArgs e)
        {
            PopulateControls();
        }

        private void okButton_Click(object sender, EventArgs e)
        {
            // commit changes
            _FrameSettings.Name = nameTextBox.Text;
            _FrameSettings.Type = (Metric.MetricType)(typeComboBox.SelectedIndex + 1);
            _FrameSettings.StartAddress = ParseAddress(startTextBox);
            if (_FrameSettings.Type == Metric.MetricType.StartAndEndAddresses)
                _FrameSettings.EndAddress = ParseAddress(endTextBox);
            else
                _FrameSettings.EndAddress = new CanonicalAddress();
            _FrameSettings.ThresholdCycles = ParseCycles();
        }

        private bool HasValue(TextBox textBox)
        {
            return textBox.Text.Trim().Length > 0;
        }

        private CanonicalAddress ParseAddress(TextBox textBox)
        {
            var page = (MemoryPage)pageComboBox.SelectedIndex;

            string text = textBox.Text.Trim();
            
            ushort address;
            try
            {
                address = ushort.Parse(text, System.Globalization.NumberStyles.HexNumber);
            }
            catch
            {
                return new CanonicalAddress();
            }
            
            return new CanonicalAddress(address, page);
        }

        private int ParseCycles()
        {
            try
            {
                return int.Parse(cyclesTextBox.Text.Trim());
            }
            catch
            {
                return 0;
            }
        }

        private string GetHexPrefix()
        {
            var form = Owner as BeebPerfForm;
            if (form is null) return string.Empty;

            return form.DisplaySettings.GetAddressPrefix();
        }

        private string FormatAddress(CanonicalAddress address)
        {
            var form = Owner as BeebPerfForm;
            if (form is null) return string.Empty;

            return form.DisplaySettings.FormatAddress(address.Address, withPrefix: false);
        }

        private bool AddressMatchesInstruction(CanonicalAddress address, CancellationTokenSource cts)
        {
            for (int i = 0; i < _Instructions.Length; i++)
            {
                if (i % 1024 == 0)
                    cts.Token.ThrowIfCancellationRequested();

                ref var instruction = ref _Instructions[i];
                if (instruction.IsInstruction &&
                    instruction.OpcodeAddress.Equals(address))
                    return true;
            }
            return false;
        }

        private bool AddressMatchesJSRInstruction(CanonicalAddress address, CancellationTokenSource cts)
        {
            for (int i = 0; i < _Instructions.Length; i++)
            {
                if (i % 1024 == 0)
                    cts.Token.ThrowIfCancellationRequested();

                ref var instruction = ref _Instructions[i];
                if (instruction.IsInstruction &&
                    instruction.OpcodeAddress.Equals(address) &&
                    instruction.Opcode == 0x20/*JSR*/)
                    return true;
            }
            return false;
        }

        static private void SetCueText(TextBox textBox, string cue)
        {
            SendMessage(textBox.Handle, 0x1501/*EM_SETCUEBANNER*/, (IntPtr)1, cue);
        }

        private record AddressValidationResult(
            bool IsValid,
            string ErrorMessage);

        private DialogMode _DialogMode;
        private Metric _FrameSettings;
        private List<string> _ReservedNames;
        private Instruction[] _Instructions;
        private bool _IgnoreChanges = false;
        private CancellationTokenSource? _AddressValidationCts;

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        private static extern IntPtr SendMessage(IntPtr hWnd, int msg, IntPtr wParam, string lParam);
    }
}
