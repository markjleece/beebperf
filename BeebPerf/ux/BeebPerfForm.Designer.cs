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
            selectAllButton = new ToolStripButton();
            toolStripSeparator3 = new ToolStripSeparator();
            flipViewButton = new ToolStripButton();
            settingsButton = new ToolStripButton();
            helpButton = new ToolStripButton();
            timelineView = new TimelineView();
            splitContainer = new SplitContainer();
            tabControl = new TabControl();
            callTreeTabPage = new TabPage();
            callTreeControl = new CallTreeGridView();
            callerCalleeTabPage = new TabPage();
            routinesTabPage = new TabPage();
            routinesDataGrid = new RoutineGridView();
            flameGraphTabPage = new TabPage();
            interruptsTabPage = new TabPage();
            codeView = new CodeGridView();
            callerCalleeView = new CallerCalleeView();
            flameGraphView = new FlameGraphView();
            spinner = new Spinner();

            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitContainer).BeginInit();
            splitContainer.Panel1.SuspendLayout();
            splitContainer.Panel2.SuspendLayout();
            splitContainer.SuspendLayout();
            tabControl.SuspendLayout();
            callTreeTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)callTreeControl).BeginInit();
            routinesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)routinesDataGrid).BeginInit();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(24, 24);
            toolStrip.Items.AddRange(new ToolStripItem[] { openButton, toolStripSeparator1, undoButton, redoButton, toolStripSeparator2, zoomInButton, zoomOutButton, selectAllButton, toolStripSeparator3, flipViewButton, settingsButton, helpButton });
            toolStrip.Location = new Point(0, 0);
            toolStrip.Name = "toolStrip";
            toolStrip.Size = new Size(1745, 33);
            toolStrip.TabIndex = 0;
            toolStrip.Text = "toolStrip";
            // 
            // openButton
            // 
            openButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            openButton.Image = (Image)resources.GetObject("openButton.Image");
            openButton.ImageTransparentColor = Color.Magenta;
            openButton.Name = "openButton";
            openButton.Size = new Size(34, 28);
            openButton.Text = "Open";
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
            undoButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
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
            redoButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
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
            zoomInButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
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
            zoomOutButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            zoomOutButton.Image = (Image)resources.GetObject("zoomOutButton.Image");
            zoomOutButton.ImageTransparentColor = Color.Magenta;
            zoomOutButton.Name = "zoomOutButton";
            zoomOutButton.Size = new Size(34, 28);
            zoomOutButton.Text = "Zoom Out";
            zoomOutButton.Click += zoomOutButton_Click;
            // 
            // selectAllButton
            // 
            selectAllButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            selectAllButton.Image = (Image)resources.GetObject("selectAllButton.Image");
            selectAllButton.ImageTransparentColor = Color.Magenta;
            selectAllButton.Name = "selectAllButton";
            selectAllButton.Size = new Size(34, 28);
            selectAllButton.Text = "Select All";
            selectAllButton.Click += selectAllButton_Click;
            // 
            // toolStripSeparator3
            // 
            toolStripSeparator3.Name = "toolStripSeparator3";
            toolStripSeparator3.Size = new Size(6, 33);
            // 
            // flipViewButton
            // 
            flipViewButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            flipViewButton.Image = (Image)resources.GetObject("flipViewButton.Image");
            flipViewButton.ImageTransparentColor = Color.Magenta;
            flipViewButton.Name = "flipViewButton";
            flipViewButton.Size = new Size(34, 28);
            flipViewButton.Text = "Flip View";
            flipViewButton.ToolTipText = "Flip View";
            flipViewButton.Click += flipViewButton_Click;
            // 
            // settingsButton
            // 
            settingsButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
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
            helpButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            helpButton.Image = (Image)resources.GetObject("helpButton.Image");
            helpButton.ImageTransparentColor = Color.Magenta;
            helpButton.Name = "helpButton";
            helpButton.Size = new Size(34, 28);
            helpButton.Text = "Help";
            helpButton.Click += helpButton_Click;
            // 
            // spinner
            // 
            spinner.BackColor = SystemColors.Window;
            spinner.Location = new Point(0, 0);
            spinner.Size = new Size(1, 1);
            // 
            // timelineView
            // 
            timelineView.Dock = DockStyle.Top;
            timelineView.Location = new Point(0, 33);
            timelineView.Name = "timelineView";
            timelineView.Size = new Size(1745, 84);
            timelineView.TabIndex = 1;
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
            tabControl.Controls.Add(routinesTabPage);
            tabControl.Controls.Add(callTreeTabPage);
            tabControl.Controls.Add(callerCalleeTabPage);
            tabControl.Controls.Add(flameGraphTabPage);
            tabControl.Controls.Add(interruptsTabPage);
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.SelectedIndex = 0;
            tabControl.Size = new Size(1745, 486);
            tabControl.TabIndex = 0;
            tabControl.TabStop = false;
            // 
            // dataGridViewCellStyle
            // 
            dataGridViewCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle.BackColor = SystemColors.Window;
            dataGridViewCellStyle.Font = new Font("Segoe UI", 9F);
            dataGridViewCellStyle.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle.Format = "N2";
            dataGridViewCellStyle.NullValue = null;
            dataGridViewCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle.WrapMode = DataGridViewTriState.False;
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
            callerCalleeTabPage.Controls.Add(callerCalleeView);
            callerCalleeTabPage.Location = new Point(4, 34);
            callerCalleeTabPage.Name = "callerCalleeTabPage";
            callerCalleeTabPage.Padding = new Padding(3);
            callerCalleeTabPage.Size = new Size(1737, 448);
            callerCalleeTabPage.TabIndex = 2;
            callerCalleeTabPage.Text = "Caller / Callee";
            callerCalleeTabPage.UseVisualStyleBackColor = true;
            // 
            // callerCalleeView
            // 
            callerCalleeView.BackColor = SystemColors.Window;
            callerCalleeView.Dock = DockStyle.Fill;
            callerCalleeView.Location = new Point(3, 3);
            callerCalleeView.Name = "callerCalleeView";
            callerCalleeView.Size = new Size(1731, 442);
            callerCalleeView.TabIndex = 0;
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
            // flameGraphTabPage
            // 
            flameGraphTabPage.Controls.Add(flameGraphView);
            flameGraphTabPage.Location = new Point(4, 34);
            flameGraphTabPage.Name = "flameGraphTabPage";
            flameGraphTabPage.Padding = new Padding(3);
            flameGraphTabPage.Size = new Size(1737, 448);
            flameGraphTabPage.TabIndex = 4;
            flameGraphTabPage.Text = "Flame Graph";
            flameGraphTabPage.UseVisualStyleBackColor = true;
            // 
            // callerCalleeView
            // 
            flameGraphView.BackColor = SystemColors.Window;
            flameGraphView.Dock = DockStyle.Fill;
            flameGraphView.Location = new Point(3, 3);
            flameGraphView.Name = "flameGraphView";
            flameGraphView.Size = new Size(1731, 442);
            flameGraphView.TabIndex = 0;
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
            Controls.Add(spinner);
            Controls.Add(splitContainer);
            Controls.Add(timelineView);
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
        private ToolStripButton selectAllButton;
        private ToolStripSeparator toolStripSeparator3;
        private ToolStripButton flipViewButton;
        private ToolStripButton settingsButton;
        private ToolStripButton helpButton;
        private ToolStripButton openButton;
        private TimelineView timelineView;
        private SplitContainer splitContainer;
        private TabControl tabControl;
        private TabPage callTreeTabPage;
        private TabPage callerCalleeTabPage;
        private TabPage routinesTabPage;
        private TabPage flameGraphTabPage;
        private TabPage interruptsTabPage;
        private RoutineGridView routinesDataGrid;
        private CallTreeGridView callTreeControl;
        private CodeGridView codeView;
        private CallerCalleeView callerCalleeView;
        private FlameGraphView flameGraphView;
        private Spinner spinner;
    }
}