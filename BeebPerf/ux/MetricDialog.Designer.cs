namespace BeebPerf.ux
{
    partial class MetricDialog
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            nameLabel = new Label();
            nameTextBox = new TextBox();
            nameErrorLabel = new Label();
            spanGroupBox = new GroupBox();
            typeLabel = new Label();
            typeComboBox = new ComboBox();
            pageLabel = new Label();
            pageComboBox = new ComboBox();
            startLabel = new Label();
            startHexLabel = new Label();
            startTextBox = new TextBox();
            startErrorLabel = new Label();
            endLabel = new Label();
            endHexLabel = new Label();
            endTextBox = new TextBox();
            endErrorLabel = new Label();
            thresholdGroupBox = new GroupBox();
            durationLabel = new Label();
            durationComboBox = new ComboBox();
            cyclesLabel = new Label();
            cyclesTextBox = new TextBox();
            cyclesErrorLabel = new Label();
            resetButton = new Button();
            okButton = new Button();
            cancelButton = new Button();
            spanGroupBox.SuspendLayout();
            thresholdGroupBox.SuspendLayout();
            SuspendLayout();
            // 
            // nameLabel
            // 
            nameLabel.AutoSize = true;
            nameLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            nameLabel.Location = new Point(18, 15);
            nameLabel.Name = "nameLabel";
            nameLabel.Size = new Size(62, 25);
            nameLabel.TabIndex = 0;
            nameLabel.Text = "Name";
            // 
            // nameTextBox
            // 
            nameTextBox.Location = new Point(137, 12);
            nameTextBox.Name = "nameTextBox";
            nameTextBox.Size = new Size(278, 31);
            nameTextBox.TabIndex = 0;
            nameTextBox.TextChanged += nameTextBox_TextChanged;
            // 
            // nameErrorLabel
            // 
            nameErrorLabel.AutoSize = true;
            nameErrorLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Italic);
            nameErrorLabel.Location = new Point(423, 15);
            nameErrorLabel.Name = "nameErrorLabel";
            nameErrorLabel.Size = new Size(58, 25);
            nameErrorLabel.TabIndex = 0;
            nameErrorLabel.Text = "error!";
            // 
            // spanGroupBox
            // 
            spanGroupBox.Controls.Add(typeLabel);
            spanGroupBox.Controls.Add(typeComboBox);
            spanGroupBox.Controls.Add(pageLabel);
            spanGroupBox.Controls.Add(pageComboBox);
            spanGroupBox.Controls.Add(startLabel);
            spanGroupBox.Controls.Add(startHexLabel);
            spanGroupBox.Controls.Add(startTextBox);
            spanGroupBox.Controls.Add(startErrorLabel);
            spanGroupBox.Controls.Add(endLabel);
            spanGroupBox.Controls.Add(endHexLabel);
            spanGroupBox.Controls.Add(endTextBox);
            spanGroupBox.Controls.Add(endErrorLabel);
            spanGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            spanGroupBox.Location = new Point(12, 49);
            spanGroupBox.Name = "spanGroupBox";
            spanGroupBox.Size = new Size(656, 186);
            spanGroupBox.TabIndex = 0;
            spanGroupBox.TabStop = false;
            spanGroupBox.Text = "Span";
            // 
            // typeLabel
            // 
            typeLabel.AutoSize = true;
            typeLabel.Font = new Font("Segoe UI", 9F);
            typeLabel.Location = new Point(6, 28);
            typeLabel.Name = "typeLabel";
            typeLabel.Size = new Size(49, 25);
            typeLabel.TabIndex = 0;
            typeLabel.Text = "Type";
            // 
            // typeComboBox
            // 
            typeComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            typeComboBox.Font = new Font("Segoe UI", 9F);
            typeComboBox.FormattingEnabled = true;
            typeComboBox.Location = new Point(125, 25);
            typeComboBox.Name = "typeComboBox";
            typeComboBox.Size = new Size(278, 33);
            typeComboBox.TabIndex = 0;
            typeComboBox.SelectedIndexChanged += typeComboBox_SelectedIndexChanged;
            // 
            // pageLabel
            // 
            pageLabel.AutoSize = true;
            pageLabel.Font = new Font("Segoe UI", 9F);
            pageLabel.Location = new Point(6, 67);
            pageLabel.Name = "pageLabel";
            pageLabel.Size = new Size(50, 25);
            pageLabel.TabIndex = 0;
            pageLabel.Text = "Page";
            // 
            // pageComboBox
            // 
            pageComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            pageComboBox.Font = new Font("Segoe UI", 9F);
            pageComboBox.FormattingEnabled = true;
            pageComboBox.Location = new Point(125, 64);
            pageComboBox.Name = "pageComboBox";
            pageComboBox.Size = new Size(204, 33);
            pageComboBox.TabIndex = 0;
            pageComboBox.SelectedIndexChanged += pageComboBox_SelectedIndexChanged;
            // 
            // startLabel
            // 
            startLabel.AutoSize = true;
            startLabel.Font = new Font("Segoe UI", 9F);
            startLabel.Location = new Point(6, 106);
            startLabel.Name = "startLabel";
            startLabel.Size = new Size(115, 25);
            startLabel.TabIndex = 0;
            startLabel.Text = "Start address";
            // 
            // startHexLabel
            // 
            startHexLabel.AutoSize = true;
            startHexLabel.Font = new Font("Segoe UI", 9F);
            startHexLabel.Location = new Point(125, 109);
            startHexLabel.Name = "startHexLabel";
            startHexLabel.Size = new Size(26, 25);
            startHexLabel.TabIndex = 0;
            startHexLabel.Text = "&";
            startHexLabel.UseMnemonic = false;
            // 
            // startTextBox
            // 
            startTextBox.Font = new Font("Segoe UI", 9F);
            startTextBox.Location = new Point(151, 103);
            startTextBox.Name = "startTextBox";
            startTextBox.Size = new Size(178, 31);
            startTextBox.TabIndex = 0;
            startTextBox.TextChanged += startTextBox_TextChanged;
            // 
            // startErrorLabel
            // 
            startErrorLabel.AutoSize = true;
            startErrorLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Italic);
            startErrorLabel.Location = new Point(335, 109);
            startErrorLabel.Name = "startErrorLabel";
            startErrorLabel.Size = new Size(149, 25);
            startErrorLabel.TabIndex = 0;
            startErrorLabel.Text = "invalid address!";
            // 
            // endLabel
            // 
            endLabel.AutoSize = true;
            endLabel.Font = new Font("Segoe UI", 9F);
            endLabel.Location = new Point(6, 143);
            endLabel.Name = "endLabel";
            endLabel.Size = new Size(109, 25);
            endLabel.TabIndex = 0;
            endLabel.Text = "End address";
            // 
            // endHexLabel
            // 
            endHexLabel.AutoSize = true;
            endHexLabel.Font = new Font("Segoe UI", 9F);
            endHexLabel.Location = new Point(125, 143);
            endHexLabel.Name = "endHexLabel";
            endHexLabel.Size = new Size(26, 25);
            endHexLabel.TabIndex = 0;
            endHexLabel.Text = "&";
            endHexLabel.UseMnemonic = false;
            // 
            // endTextBox
            // 
            endTextBox.Font = new Font("Segoe UI", 9F);
            endTextBox.Location = new Point(151, 140);
            endTextBox.Name = "endTextBox";
            endTextBox.Size = new Size(178, 31);
            endTextBox.TabIndex = 0;
            endTextBox.TextChanged += endTextBox_TextChanged;
            // 
            // endErrorLabel
            // 
            endErrorLabel.AutoSize = true;
            endErrorLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Italic);
            endErrorLabel.Location = new Point(335, 143);
            endErrorLabel.Name = "endErrorLabel";
            endErrorLabel.Size = new Size(149, 25);
            endErrorLabel.TabIndex = 0;
            endErrorLabel.Text = "invalid address!";
            // 
            // thresholdGroupBox
            // 
            thresholdGroupBox.Controls.Add(durationLabel);
            thresholdGroupBox.Controls.Add(durationComboBox);
            thresholdGroupBox.Controls.Add(cyclesLabel);
            thresholdGroupBox.Controls.Add(cyclesTextBox);
            thresholdGroupBox.Controls.Add(cyclesErrorLabel);
            thresholdGroupBox.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
            thresholdGroupBox.Location = new Point(12, 241);
            thresholdGroupBox.Name = "thresholdGroupBox";
            thresholdGroupBox.Size = new Size(656, 117);
            thresholdGroupBox.TabIndex = 0;
            thresholdGroupBox.TabStop = false;
            thresholdGroupBox.Text = "Threshold";
            // 
            // durationLabel
            // 
            durationLabel.AutoSize = true;
            durationLabel.Font = new Font("Segoe UI", 9F);
            durationLabel.Location = new Point(9, 33);
            durationLabel.Name = "durationLabel";
            durationLabel.Size = new Size(81, 25);
            durationLabel.TabIndex = 0;
            durationLabel.Text = "Duration";
            // 
            // durationComboBox
            // 
            durationComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
            durationComboBox.Font = new Font("Segoe UI", 9F);
            durationComboBox.FormattingEnabled = true;
            durationComboBox.Location = new Point(125, 30);
            durationComboBox.Name = "durationComboBox";
            durationComboBox.Size = new Size(204, 33);
            durationComboBox.TabIndex = 0;
            durationComboBox.SelectedIndexChanged += durationComboBox_SelectedIndexChanged;
            // 
            // cyclesLabel
            // 
            cyclesLabel.AutoSize = true;
            cyclesLabel.Font = new Font("Segoe UI", 9F);
            cyclesLabel.Location = new Point(9, 72);
            cyclesLabel.Name = "cyclesLabel";
            cyclesLabel.Size = new Size(61, 25);
            cyclesLabel.TabIndex = 0;
            cyclesLabel.Text = "Cycles";
            // 
            // cyclesTextBox
            // 
            cyclesTextBox.Font = new Font("Segoe UI", 9F);
            cyclesTextBox.Location = new Point(125, 69);
            cyclesTextBox.Name = "cyclesTextBox";
            cyclesTextBox.Size = new Size(204, 31);
            cyclesTextBox.TabIndex = 0;
            cyclesTextBox.TextChanged += cyclesTextBox_TextChanged;
            // 
            // cyclesErrorLabel
            // 
            cyclesErrorLabel.AutoSize = true;
            cyclesErrorLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold | FontStyle.Italic);
            cyclesErrorLabel.Location = new Point(335, 72);
            cyclesErrorLabel.Name = "cyclesErrorLabel";
            cyclesErrorLabel.Size = new Size(131, 25);
            cyclesErrorLabel.TabIndex = 0;
            cyclesErrorLabel.Text = "invalid value!";
            // 
            // resetButton
            // 
            resetButton.Location = new Point(320, 371);
            resetButton.Name = "resetButton";
            resetButton.Size = new Size(112, 34);
            resetButton.TabIndex = 0;
            resetButton.Text = "Reset";
            resetButton.UseVisualStyleBackColor = true;
            resetButton.Click += resetButton_Click;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(438, 371);
            okButton.Name = "okButton";
            okButton.Size = new Size(112, 34);
            okButton.TabIndex = 0;
            okButton.Text = "Ok";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += okButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(556, 371);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(112, 34);
            cancelButton.TabIndex = 0;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // MetricsDialog
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(680, 417);
            Controls.Add(nameLabel);
            Controls.Add(nameTextBox);
            Controls.Add(nameErrorLabel);
            Controls.Add(spanGroupBox);
            Controls.Add(thresholdGroupBox);
            Controls.Add(resetButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "MetricsDialog";
            Text = "MetricIteration Settings";
            spanGroupBox.ResumeLayout(false);
            spanGroupBox.PerformLayout();
            thresholdGroupBox.ResumeLayout(false);
            thresholdGroupBox.PerformLayout();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label nameLabel;
        private TextBox nameTextBox;
        private Label nameErrorLabel;
        private GroupBox spanGroupBox;
        private Label typeLabel;
        private ComboBox typeComboBox;
        private Label pageLabel;
        private ComboBox pageComboBox;
        private Label startLabel;
        private Label startHexLabel;
        private TextBox startTextBox;
        private Label startErrorLabel;
        private Label endLabel;
        private Label endHexLabel;
        private TextBox endTextBox;
        private Label endErrorLabel;
        private GroupBox thresholdGroupBox;
        private Label durationLabel;
        private ComboBox durationComboBox;
        private Label cyclesLabel;
        private TextBox cyclesTextBox;
        private Label cyclesErrorLabel;
        private Button resetButton;
        private Button okButton;
        private Button cancelButton;
    }
}