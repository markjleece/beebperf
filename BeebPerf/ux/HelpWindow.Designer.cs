namespace BeebPerf.ux
{
    partial class HelpWindow
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
            richTextBox = new RichTextBox();
            SuspendLayout();
            // 
            // richTextBox
            // 
            richTextBox.BorderStyle = BorderStyle.None;
            richTextBox.Dock = DockStyle.Fill;
            richTextBox.ImeMode = ImeMode.NoControl;
            richTextBox.Location = new Point(16, 0);
            richTextBox.Name = "richTextBox";
            richTextBox.ReadOnly = true;
            richTextBox.Size = new Size(1242, 744);
            richTextBox.TabIndex = 0;
            richTextBox.TabStop = false;
            richTextBox.Text = "";
            // 
            // HelpWindow
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1258, 744);
            Controls.Add(richTextBox);
            Name = "HelpWindow";
            Padding = new Padding(16, 0, 0, 0);
            Text = "BeebPerf - Help";
            ResumeLayout(false);
        }

        #endregion

        private RichTextBox richTextBox;
    }
}