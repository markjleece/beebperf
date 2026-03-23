namespace BeebPerf.ux
{
    partial class selectionDialog
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
            fromLabel = new Label();
            toRadioButton = new RadioButton();
            durationRadioButton = new RadioButton();
            secondsLabel = new Label();
            cyclesLabel = new Label();
            fromSecondsTextBox = new TextBox();
            fromCyclesTextBox = new TextBox();
            toSecondsTextBox = new TextBox();
            toCyclesTextBox = new TextBox();
            durationSecondsTextBox = new TextBox();
            durationCyclesTextBox = new TextBox();
            resetButton = new Button();
            okButton = new Button();
            canelButton = new Button();
            SuspendLayout();
            // 
            // fromLabel
            // 
            fromLabel.AutoSize = true;
            fromLabel.Location = new Point(36, 40);
            fromLabel.Name = "fromLabel";
            fromLabel.Size = new Size(58, 25);
            fromLabel.TabIndex = 2;
            fromLabel.Text = "From:";
            // 
            // toRadioButton
            // 
            toRadioButton.AutoSize = true;
            toRadioButton.Location = new Point(12, 75);
            toRadioButton.Name = "toRadioButton";
            toRadioButton.Size = new Size(59, 29);
            toRadioButton.TabIndex = 0;
            toRadioButton.TabStop = true;
            toRadioButton.Text = "To:";
            toRadioButton.UseVisualStyleBackColor = true;
            // 
            // durationRadioButton
            // 
            durationRadioButton.AutoSize = true;
            durationRadioButton.Location = new Point(12, 112);
            durationRadioButton.Name = "durationRadioButton";
            durationRadioButton.Size = new Size(110, 29);
            durationRadioButton.TabIndex = 1;
            durationRadioButton.TabStop = true;
            durationRadioButton.Text = "Duration:";
            durationRadioButton.UseVisualStyleBackColor = true;
            // 
            // secondsLabel
            // 
            secondsLabel.AutoSize = true;
            secondsLabel.Location = new Point(153, 9);
            secondsLabel.Name = "secondsLabel";
            secondsLabel.Size = new Size(79, 25);
            secondsLabel.TabIndex = 4;
            secondsLabel.Text = "Seconds";
            // 
            // cyclesLabel
            // 
            cyclesLabel.AutoSize = true;
            cyclesLabel.Location = new Point(351, 9);
            cyclesLabel.Name = "cyclesLabel";
            cyclesLabel.Size = new Size(61, 25);
            cyclesLabel.TabIndex = 5;
            cyclesLabel.Text = "Cycles";
            // 
            // fromSecondsTextBox
            // 
            fromSecondsTextBox.Location = new Point(153, 37);
            fromSecondsTextBox.Name = "fromSecondsTextBox";
            fromSecondsTextBox.Size = new Size(150, 31);
            fromSecondsTextBox.TabIndex = 3;
            // 
            // fromCyclesTextBox
            // 
            fromCyclesTextBox.Location = new Point(351, 37);
            fromCyclesTextBox.Name = "fromCyclesTextBox";
            fromCyclesTextBox.Size = new Size(150, 31);
            fromCyclesTextBox.TabIndex = 6;
            // 
            // toSecondsTextBox
            // 
            toSecondsTextBox.Location = new Point(153, 73);
            toSecondsTextBox.Name = "toSecondsTextBox";
            toSecondsTextBox.Size = new Size(150, 31);
            toSecondsTextBox.TabIndex = 13;
            // 
            // toCyclesTextBox
            // 
            toCyclesTextBox.Location = new Point(351, 74);
            toCyclesTextBox.Name = "toCyclesTextBox";
            toCyclesTextBox.Size = new Size(150, 31);
            toCyclesTextBox.TabIndex = 8;
            // 
            // durationSecondsTextBox
            // 
            durationSecondsTextBox.Location = new Point(153, 111);
            durationSecondsTextBox.Name = "durationSecondsTextBox";
            durationSecondsTextBox.Size = new Size(150, 31);
            durationSecondsTextBox.TabIndex = 9;
            // 
            // durationCyclesTextBox
            // 
            durationCyclesTextBox.Location = new Point(351, 111);
            durationCyclesTextBox.Name = "durationCyclesTextBox";
            durationCyclesTextBox.Size = new Size(150, 31);
            durationCyclesTextBox.TabIndex = 10;
            // 
            // resetButton
            // 
            resetButton.Location = new Point(153, 164);
            resetButton.Name = "resetButton";
            resetButton.Size = new Size(112, 34);
            resetButton.TabIndex = 0;
            resetButton.Text = "Reset";
            resetButton.UseVisualStyleBackColor = true;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(271, 164);
            okButton.Name = "okButton";
            okButton.Size = new Size(112, 34);
            okButton.TabIndex = 0;
            okButton.Text = "Ok";
            okButton.UseVisualStyleBackColor = true;
            // 
            // canelButton
            // 
            canelButton.DialogResult = DialogResult.Cancel;
            canelButton.Location = new Point(389, 164);
            canelButton.Name = "canelButton";
            canelButton.Size = new Size(112, 34);
            canelButton.TabIndex = 0;
            canelButton.Text = "Cancel";
            canelButton.UseVisualStyleBackColor = true;
            // 
            // selectionDialog
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(510, 206);
            Controls.Add(canelButton);
            Controls.Add(okButton);
            Controls.Add(resetButton);
            Controls.Add(durationRadioButton);
            Controls.Add(toRadioButton);
            Controls.Add(durationCyclesTextBox);
            Controls.Add(durationSecondsTextBox);
            Controls.Add(toCyclesTextBox);
            Controls.Add(toSecondsTextBox);
            Controls.Add(fromCyclesTextBox);
            Controls.Add(fromSecondsTextBox);
            Controls.Add(fromLabel);
            Controls.Add(cyclesLabel);
            Controls.Add(secondsLabel);
            Name = "selectionDialog";
            Text = "Selection";
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label secondsLabel;
        private Label cyclesLabel;
        private Label fromLabel;
        private RadioButton toRadioButton;
        private RadioButton durationRadioButton;
        private TextBox fromSecondsTextBox;
        private TextBox fromCyclesTextBox;
        private TextBox toSecondsTextBox;
        private TextBox toCyclesTextBox;
        private TextBox durationSecondsTextBox;
        private TextBox durationCyclesTextBox;
        private Button resetButton;
        private Button okButton;
        private Button canelButton;
    }
}