namespace BeebPerf.ux
{
    partial class BeebPerfForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(BeebPerfForm));
            toolStrip = new ToolStrip();
            openButton = new ToolStripButton();
            toolStripSeparator1 = new ToolStripSeparator();
            undoButton = new ToolStripButton();
            redoButton = new ToolStripButton();
            toolStripSeparator2 = new ToolStripSeparator();
            zoomInButton = new ToolStripButton();
            zoomOutButton = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            settingsButton = new ToolStripButton();
            helpButton = new ToolStripButton();
            timelinePanel = new Panel();
            splitContainer = new SplitContainer();
            tabControl = new TabControl();
            hotRoutinesTabPage = new TabPage();
            callTreeTabPage = new TabPage();
            callerCalleeTabPage = new TabPage();
            routinesTabPage = new TabPage();
            hotGraphTabPage = new TabPage();
            interruptsTabPage = new TabPage();
            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.SuspendLayout();
            tabControl.SuspendLayout();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(24, 24);
            toolStrip.Items.AddRange(new ToolStripItem[] { openButton, toolStripSeparator1, undoButton, redoButton, toolStripSeparator2, zoomInButton, zoomOutButton, toolStripSeparator3, settingsButton, helpButton });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1745, 33);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip";
            // 
            // openButton
            // 
            openButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            openButton.Image = (Image)resources.GetObject("openButton.Image");
            openButton.ImageTransparentColor = Color.Magenta;
            openButton.Name = "openButton";
            openButton.Size = new Size(34, 28);
            openButton.Text = "openButton";
            openButton.ToolTipText = "Open";
            openButton.Click += openButton_Click;
            // 
            // toolStripSeparator1
            // 
            toolStripSeparator1.Name = "toolStripSeparator1";
            toolStripSeparator1.Size = new Size(6, 33);
            // 
            // undoButton
            // 
            undoButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            undoButton.Image = (Image)resources.GetObject("undoButton.Image");
            undoButton.ImageTransparentColor = Color.Magenta;
            undoButton.Name = "undoButton";
            undoButton.Size = new Size(34, 28);
            undoButton.Text = "Undo";
            undoButton.ToolTipText = "Undo (Ctrl+Z)";
            undoButton.Click += undoButton_Click;
            // 
            // redoButton
            // 
            redoButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            redoButton.Image = (Image)resources.GetObject("redoButton.Image");
            redoButton.ImageTransparentColor = Color.Magenta;
            redoButton.Name = "redoButton";
            redoButton.Size = new Size(34, 28);
            redoButton.Text = "Redo";
            redoButton.ToolTipText = "Redo (Ctrl+Y)";
            redoButton.Click += redoButton_Click;
            // 
            // toolStripSeparator2
            // 
            toolStripSeparator2.Name = "toolStripSeparator2";
            toolStripSeparator2.Size = new Size(6, 33);
            // 
            // zoomInButton
            // 
            zoomInButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            zoomInButton.Image = (Image)resources.GetObject("zoomInButton.Image");
            zoomInButton.ImageTransparentColor = Color.Magenta;
            zoomInButton.Name = "zoomInButton";
            zoomInButton.Size = new Size(34, 28);
            zoomInButton.Text = "Zoom In";
            zoomInButton.ToolTipText = "Zoom In";
            zoomInButton.Click += zoomInButton_Click;
            // 
            // zoomOutButton
            // 
            zoomOutButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            zoomOutButton.Image = (Image)resources.GetObject("zoomOutButton.Image");
            zoomOutButton.ImageTransparentColor = Color.Magenta;
            zoomOutButton.Name = "zoomOutButton";
            zoomOutButton.Size = new Size(34, 28);
            zoomOutButton.Text = "Zoom Out";
            zoomOutButton.Click += zoomOutButton_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 33);
            // 
            // settingsButton
            // 
            settingsButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            settingsButton.Image = (Image)resources.GetObject("settingsButton.Image");
            settingsButton.ImageTransparentColor = Color.Magenta;
            settingsButton.Name = "settingsButton";
            settingsButton.Size = new Size(34, 28);
            settingsButton.Text = "Settings";
            settingsButton.ToolTipText = "Settings";
            settingsButton.Click += settingsButton_Click;
            // 
            // helpButton
            // 
            helpButton.DisplayStyle = ToolStripItemDisplayStyle.Image;
            helpButton.Image = (Image)resources.GetObject("helpButton.Image");
            helpButton.ImageTransparentColor = Color.Magenta;
            helpButton.Name = "helpButton";
            helpButton.Size = new Size(34, 28);
            helpButton.Text = "Help";
            helpButton.Click += helpButton_Click;
            // 
            // timelinePanel
            // 
            timelinePanel.Dock = DockStyle.Top;
            timelinePanel.Location = new Point(0, 33);
            timelinePanel.Name = "timelinePanel";
            timelinePanel.Size = new Size(1745, 100);
            timelinePanel.TabIndex = 1;
            // 
            // splitContainer
            // 
            splitContainer.Dock = DockStyle.Fill;
            splitContainer.Location = new Point(0, 133);
            splitContainer.Name = "splitContainer";
            splitContainer.Orientation = Orientation.Horizontal;
            // 
            // splitContainer.Panel1
            // 
            splitContainer.Panel1.Controls.Add(tabControl);
            splitContainer.Size = new Size(1745, 977);
            splitContainer.SplitterDistance = 486;
            splitContainer.TabIndex = 2;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(hotRoutinesTabPage);
            tabControl.Controls.Add(callTreeTabPage);
            tabControl.Controls.Add(callerCalleeTabPage);
            tabControl.Controls.Add(routinesTabPage);
            tabControl.Controls.Add(hotGraphTabPage);
            tabControl.Controls.Add(interruptsTabPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1745, 486);
            tabControl.TabIndex = 0;
            tabControl.TabStop = false;
            // 
            // hotRoutinesTabPage
            // 
            hotRoutinesTabPage.Location = new Point(4, 34);
            hotRoutinesTabPage.Name = "hotRoutinesTabPage";
            hotRoutinesTabPage.Padding = new Padding(3);
            hotRoutinesTabPage.Size = new Size(1737, 448);
            hotRoutinesTabPage.TabIndex = 0;
            hotRoutinesTabPage.Text = "Hot Routines";
            hotRoutinesTabPage.UseVisualStyleBackColor = true;
            // 
            // callTreeTabPage
            // 
            callTreeTabPage.Location = new Point(4, 34);
            callTreeTabPage.Name = "callTreeTabPage";
            callTreeTabPage.Padding = new Padding(3);
            callTreeTabPage.Size = new Size(1737, 448);
            callTreeTabPage.TabIndex = 1;
            callTreeTabPage.Text = "Call Tree";
            callTreeTabPage.UseVisualStyleBackColor = true;
            // 
            // callerCalleeTabPage
            // 
            callerCalleeTabPage.Location = new Point(4, 34);
            callerCalleeTabPage.Name = "callerCalleeTabPage";
            callerCalleeTabPage.Padding = new Padding(3);
            callerCalleeTabPage.Size = new Size(1737, 448);
            callerCalleeTabPage.TabIndex = 2;
            callerCalleeTabPage.Text = "Caller / Callee";
            callerCalleeTabPage.UseVisualStyleBackColor = true;
            // 
            // routinesTabPage
            // 
            routinesTabPage.Location = new Point(4, 34);
            routinesTabPage.Name = "routinesTabPage";
            routinesTabPage.Padding = new Padding(3);
            routinesTabPage.Size = new Size(1737, 448);
            routinesTabPage.TabIndex = 3;
            routinesTabPage.Text = "Routines";
            routinesTabPage.UseVisualStyleBackColor = true;
            // 
            // hotGraphTabPage
            // 
            hotGraphTabPage.Location = new Point(4, 34);
            hotGraphTabPage.Name = "hotGraphTabPage";
            hotGraphTabPage.Padding = new Padding(3);
            hotGraphTabPage.Size = new Size(1737, 448);
            hotGraphTabPage.TabIndex = 4;
            hotGraphTabPage.Text = "Hot Graph";
            hotGraphTabPage.UseVisualStyleBackColor = true;
            // 
            // interruptsTabPage
            // 
            interruptsTabPage.Location = new Point(4, 34);
            interruptsTabPage.Name = "interruptsTabPage";
            interruptsTabPage.Padding = new Padding(3);
            interruptsTabPage.Size = new Size(1737, 448);
            interruptsTabPage.TabIndex = 5;
            interruptsTabPage.Text = "Interrupts";
            interruptsTabPage.UseVisualStyleBackColor = true;
            // 
            // BeebPerfForm
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1745, 1110);
            Controls.Add(splitContainer);
            Controls.Add(timelinePanel);
            Controls.Add(toolStrip);
            Name = "BeebPerfForm";
            Text = "WinPerfForm";
            Load += BeebPerfForm_Load;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private ToolStrip toolStrip;
        private ToolStripSeparator toolStripSeparator1;
        private ToolStripButton undoButton;
        private ToolStripButton redoButton;
        private ToolStripSeparator toolStripSeparator2;
        private ToolStripButton zoomInButton;
        private ToolStripButton zoomOutButton;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton settingsButton;
        private ToolStripButton helpButton;
        private ToolStripButton openButton;
        private Panel timelinePanel;
        private SplitContainer splitContainer;
        private TabControl tabControl;
        private TabPage hotRoutinesTabPage;
        private TabPage callTreeTabPage;
        private TabPage callerCalleeTabPage;
        private TabPage routinesTabPage;
        private TabPage hotGraphTabPage;
        private TabPage interruptsTabPage;
    }
}