namespace BeebPerf.ux
{
    partial class SelectionDialog
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
            secondsLabel = new Label();
            cyclesLabel = new Label();
            fromLabel = new Label();
            fromSecondsTextBox = new TextBox();
            fromCyclesTextBox = new TextBox();
            toLabel = new Label();
            toSecondsTextBox = new TextBox();
            toCyclesTextBox = new TextBox();
            durationLabel = new Label();
            durationSecondsTextBox = new TextBox();
            durationCyclesTextBox = new TextBox();
            resetButton = new Button();
            okButton = new Button();
            cancelButton = new Button();
            SuspendLayout();
            // 
            // secondsLabel
            // 
            secondsLabel.AutoSize = true;
            secondsLabel.Location = new Point(119, 9);
            secondsLabel.Name = "secondsLabel";
            secondsLabel.Size = new Size(79, 25);
            secondsLabel.TabIndex = 0;
            secondsLabel.Text = "Seconds";
            // 
            // cyclesLabel
            // 
            cyclesLabel.AutoSize = true;
            cyclesLabel.Location = new Point(317, 9);
            cyclesLabel.Name = "cyclesLabel";
            cyclesLabel.Size = new Size(61, 25);
            cyclesLabel.TabIndex = 0;
            cyclesLabel.Text = "Cycles";
            // 
            // fromLabel
            // 
            fromLabel.AutoSize = true;
            fromLabel.Location = new Point(12, 40);
            fromLabel.Name = "fromLabel";
            fromLabel.Size = new Size(58, 25);
            fromLabel.TabIndex = 0;
            fromLabel.Text = "From:";
            // 
            // fromSecondsTextBox
            // 
            fromSecondsTextBox.Location = new Point(119, 37);
            fromSecondsTextBox.Name = "fromSecondsTextBox";
            fromSecondsTextBox.Size = new Size(150, 31);
            fromSecondsTextBox.TabIndex = 0;
            fromSecondsTextBox.TextChanged += fromSecondsTextBox_TextChanged;
            // 
            // fromCyclesTextBox
            // 
            fromCyclesTextBox.Location = new Point(317, 37);
            fromCyclesTextBox.Name = "fromCyclesTextBox";
            fromCyclesTextBox.Size = new Size(150, 31);
            fromCyclesTextBox.TabIndex = 0;
            fromCyclesTextBox.TextChanged += fromCyclesTextBox_TextChanged;
            // 
            // toLabel
            // 
            toLabel.AutoSize = true;
            toLabel.Location = new Point(12, 76);
            toLabel.Name = "toLabel";
            toLabel.Size = new Size(34, 25);
            toLabel.TabIndex = 0;
            toLabel.Text = "To:";
            // 
            // toSecondsTextBox
            // 
            toSecondsTextBox.Location = new Point(119, 73);
            toSecondsTextBox.Name = "toSecondsTextBox";
            toSecondsTextBox.Size = new Size(150, 31);
            toSecondsTextBox.TabIndex = 0;
            toSecondsTextBox.TextChanged += toSecondsTextBox_TextChanged;
            // 
            // toCyclesTextBox
            // 
            toCyclesTextBox.Location = new Point(317, 74);
            toCyclesTextBox.Name = "toCyclesTextBox";
            toCyclesTextBox.Size = new Size(150, 31);
            toCyclesTextBox.TabIndex = 0;
            toCyclesTextBox.TextChanged += toCyclesTextBox_TextChanged;
            // 
            // durationLabel
            // 
            durationLabel.AutoSize = true;
            durationLabel.Location = new Point(12, 114);
            durationLabel.Name = "durationLabel";
            durationLabel.Size = new Size(85, 25);
            durationLabel.TabIndex = 0;
            durationLabel.Text = "Duration:";
            // 
            // durationSecondsTextBox
            // 
            durationSecondsTextBox.Location = new Point(119, 111);
            durationSecondsTextBox.Name = "durationSecondsTextBox";
            durationSecondsTextBox.Size = new Size(150, 31);
            durationSecondsTextBox.TabIndex = 0;
            durationSecondsTextBox.TextChanged += durationSecondsTextBox_TextChanged;
            // 
            // durationCyclesTextBox
            // 
            durationCyclesTextBox.Location = new Point(317, 111);
            durationCyclesTextBox.Name = "durationCyclesTextBox";
            durationCyclesTextBox.Size = new Size(150, 31);
            durationCyclesTextBox.TabIndex = 0;
            durationCyclesTextBox.TextChanged += durationCyclesTextBox_TextChanged;
            // 
            // resetButton
            // 
            resetButton.Location = new Point(119, 164);
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
            okButton.Location = new Point(237, 164);
            okButton.Name = "okButton";
            okButton.Size = new Size(112, 34);
            okButton.TabIndex = 0;
            okButton.Text = "Ok";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += OkButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(355, 164);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(112, 34);
            cancelButton.TabIndex = 0;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            // 
            // SelectionDialog
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(477, 206);
            Controls.Add(secondsLabel);
            Controls.Add(cyclesLabel);
            Controls.Add(fromLabel);
            Controls.Add(fromSecondsTextBox);
            Controls.Add(fromCyclesTextBox);
            Controls.Add(toLabel);
            Controls.Add(toSecondsTextBox);
            Controls.Add(toCyclesTextBox);
            Controls.Add(durationLabel);
            Controls.Add(durationSecondsTextBox);
            Controls.Add(durationCyclesTextBox);
            Controls.Add(resetButton);
            Controls.Add(okButton);
            Controls.Add(cancelButton);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "SelectionDialog";
            Text = "Selection";
            Shown += SelectionDialog_Shown;
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label secondsLabel;
        private Label cyclesLabel;
        private Label fromLabel;
        private Label toLabel;
        private Label durationLabel;
        private TextBox fromSecondsTextBox;
        private TextBox fromCyclesTextBox;
        private TextBox toSecondsTextBox;
        private TextBox toCyclesTextBox;
        private TextBox durationSecondsTextBox;
        private TextBox durationCyclesTextBox;
        private Button resetButton;
        private Button okButton;
        private Button cancelButton;
    }
}