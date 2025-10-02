using System.Windows.Forms;

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
            DataGridViewCellStyle dataGridViewCellStyle = new DataGridViewCellStyle();
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
            hotRoutinesDataGrid = new RoutineGridView();
            callTreeTabPage = new TabPage();
            callTreeControl = new CallTreeGridView();
            callerCalleeTabPage = new TabPage();
            routinesTabPage = new TabPage();
            routinesDataGrid = new RoutineGridView();
            hotGraphTabPage = new TabPage();
            interruptsTabPage = new TabPage();
            codeView = new CodeGridView();
            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            tabControl.SuspendLayout();
            hotRoutinesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)hotRoutinesDataGrid).BeginInit();
            callTreeTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)callTreeControl).BeginInit();
            routinesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)routinesDataGrid).BeginInit();
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
            // 
            // splitContainer.Panel2
            // 
            splitContainer.Panel2.Controls.Add(codeView);
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
            hotRoutinesTabPage.Controls.Add(hotRoutinesDataGrid);
            hotRoutinesTabPage.Location = new Point(4, 34);
            hotRoutinesTabPage.Name = "hotRoutinesTabPage";
            hotRoutinesTabPage.Padding = new Padding(3);
            hotRoutinesTabPage.Size = new Size(1737, 448);
            hotRoutinesTabPage.TabIndex = 0;
            hotRoutinesTabPage.Text = "HotPath Routines";
            hotRoutinesTabPage.UseVisualStyleBackColor = true;
            // 
            // hotRoutinesDataGrid
            // 
            hotRoutinesDataGrid.ColumnHeadersHeight = 34;
            dataGridViewCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle.BackColor = SystemColors.Window;
            dataGridViewCellStyle.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle.Format = "N2";
            dataGridViewCellStyle.NullValue = null;
            dataGridViewCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle.WrapMode = DataGridViewTriState.False;
            hotRoutinesDataGrid.DefaultCellStyle = dataGridViewCellStyle;
            hotRoutinesDataGrid.Dock = DockStyle.Fill;
            hotRoutinesDataGrid.Location = new Point(3, 3);
            hotRoutinesDataGrid.Name = "hotRoutinesDataGrid";
            hotRoutinesDataGrid.RowHeadersWidth = 62;
            hotRoutinesDataGrid.Size = new Size(1731, 442);
            hotRoutinesDataGrid.TabIndex = 0;
            // 
            // callTreeTabPage
            // 
            callTreeTabPage.Controls.Add(callTreeControl);
            callTreeTabPage.Location = new Point(4, 34);
            callTreeTabPage.Name = "callTreeTabPage";
            callTreeTabPage.Padding = new Padding(3);
            callTreeTabPage.Size = new Size(1737, 448);
            callTreeTabPage.TabIndex = 1;
            callTreeTabPage.Text = "Call Tree";
            callTreeTabPage.UseVisualStyleBackColor = true;
            // 
            // callTreeControl
            // 
            callTreeControl.ColumnHeadersHeight = 34;
            callTreeControl.DefaultCellStyle = dataGridViewCellStyle;
            callTreeControl.Dock = DockStyle.Fill;
            callTreeControl.Location = new Point(3, 3);
            callTreeControl.Name = "callTreeControl";
            callTreeControl.RowHeadersWidth = 62;
            callTreeControl.Size = new Size(1731, 442);
            callTreeControl.TabIndex = 0;
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
            routinesTabPage.Controls.Add(routinesDataGrid);
            routinesTabPage.Location = new Point(4, 34);
            routinesTabPage.Name = "routinesTabPage";
            routinesTabPage.Padding = new Padding(3);
            routinesTabPage.Size = new Size(1737, 448);
            routinesTabPage.TabIndex = 3;
            routinesTabPage.Text = "Routines";
            routinesTabPage.UseVisualStyleBackColor = true;
            // 
            // routinesDataGrid
            // 
            routinesDataGrid.ColumnHeadersHeight = 34;
            routinesDataGrid.DefaultCellStyle = dataGridViewCellStyle;
            routinesDataGrid.Dock = DockStyle.Fill;
            routinesDataGrid.Location = new Point(3, 3);
            routinesDataGrid.Name = "routinesDataGrid";
            routinesDataGrid.RowHeadersWidth = 62;
            routinesDataGrid.Size = new Size(1731, 442);
            routinesDataGrid.TabIndex = 0;
            // 
            // hotGraphTabPage
            // 
            hotGraphTabPage.Location = new Point(4, 34);
            hotGraphTabPage.Name = "hotGraphTabPage";
            hotGraphTabPage.Padding = new Padding(3);
            hotGraphTabPage.Size = new Size(1737, 448);
            hotGraphTabPage.TabIndex = 4;
            hotGraphTabPage.Text = "HotPath Graph";
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
            // codeView
            // 
            codeView.Dock = DockStyle.Fill;
            codeView.Location = new Point(0, 0);
            codeView.Name = "codeView";
            codeView.Size = new Size(1745, 487);
            codeView.TabIndex = 0;
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
            Text = "BeebPerf";
            Load += BeebPerfForm_Load;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            splitContainer.Panel1.ResumeLayout(false);
            splitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitContainer).EndInit();
            splitContainer.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            hotRoutinesTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)hotRoutinesDataGrid).EndInit();
            callTreeTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)callTreeControl).EndInit();
            routinesTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)routinesDataGrid).EndInit();
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
        private RoutineGridView hotRoutinesDataGrid;
        private RoutineGridView routinesDataGrid;
        private CallTreeGridView callTreeControl;
        private CodeGridView codeView;
    }
}