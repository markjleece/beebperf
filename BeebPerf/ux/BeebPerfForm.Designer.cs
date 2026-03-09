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
            hotRoutinesButton = new ToolStripButton();
            hotPathsButton = new ToolStripButton();
            flipViewButton = new ToolStripButton();
            toolStripSeparator4 = new ToolStripSeparator();
            labelsButton = new ToolStripButton();
            settingsButton = new ToolStripButton();
            helpButton = new ToolStripButton();
            timelineView = new TimelineView();
            primarySplitContainer = new SplitContainer();
            secondarySplitContainer = new SplitContainer();
            memoryContainer = new Panel();
            tabControl = new TabControlEx();
            callTreeTabPage = new Panel();
            callTreeView = new CallTreeView();
            callerCalleeTabPage = new Panel();
            routinesTabPage = new Panel();
            routinesView = new RoutinesView();
            flameGraphTabPage = new Panel();
            memoryTabPage = new Panel();
            codeView = new CodeView();
            callerCalleeView = new CallerCalleeView();
            flameGraphView = new FlameGraphView();
            memoryZeroPageCheckBox = new CheckBox();
            memoryView = new MemoryView();
            memoryRoutinesView = new MemoryRoutinesView();
            spinner = new Spinner();

            toolStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)primarySplitContainer).BeginInit();
            primarySplitContainer.Panel1.SuspendLayout();
            primarySplitContainer.Panel2.SuspendLayout();
            primarySplitContainer.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)secondarySplitContainer).BeginInit();
            secondarySplitContainer.Panel1.SuspendLayout();
            secondarySplitContainer.Panel2.SuspendLayout();
            secondarySplitContainer.SuspendLayout();
            tabControl.SuspendLayout();
            callTreeTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)callTreeView).BeginInit();
            routinesTabPage.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)routinesView).BeginInit();
            SuspendLayout();
            // 
            // toolStrip
            // 
            toolStrip.ImageScalingSize = new Size(24, 24);
            toolStrip.Items.AddRange(new ToolStripItem[] 
            { 
                openButton, 
                toolStripSeparator1, 
                undoButton, 
                redoButton, 
                toolStripSeparator2, 
                zoomInButton, 
                zoomOutButton, 
                selectAllButton, 
                toolStripSeparator3, 
                hotRoutinesButton, 
                hotPathsButton, 
                flipViewButton, 
                toolStripSeparator4, 
                labelsButton,
                settingsButton, 
                helpButton
            });
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
            // hotRoutinesButton
            // 
            hotRoutinesButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            hotRoutinesButton.Image = (Image)resources.GetObject("hotRoutinesButton.Image");
            hotRoutinesButton.ImageTransparentColor = Color.Magenta;
            hotRoutinesButton.Name = "hotRoutinesButton";
            hotRoutinesButton.Size = new Size(34, 28);
            hotRoutinesButton.Text = "Hot Routines";
            hotRoutinesButton.ToolTipText = "Hot Routines";
            hotRoutinesButton.Click += hotRoutinesButton_Click;
            // 
            // hotPathsButton
            // 
            hotPathsButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            hotPathsButton.Image = (Image)resources.GetObject("hotPathsButton.Image");
            hotPathsButton.ImageTransparentColor = Color.Magenta;
            hotPathsButton.Name = "hotPathsButton";
            hotPathsButton.Size = new Size(34, 28);
            hotPathsButton.Text = "Hot Paths";
            hotPathsButton.ToolTipText = "Hot Paths";
            hotPathsButton.Click += hotPathsButton_Click;
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
            // toolStripSeparator3
            // 
            toolStripSeparator4.Name = "toolStripSeparator4";
            toolStripSeparator4.Size = new Size(6, 33);
            // 
            // labelsButton
            // 
            labelsButton.DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
            labelsButton.Image = (Image)resources.GetObject("labelsButton.Image");
            labelsButton.ImageTransparentColor = Color.Magenta;
            labelsButton.Name = "labelsButton";
            labelsButton.Size = new Size(34, 28);
            labelsButton.Text = "Labels";
            labelsButton.ToolTipText = "Labels";
            labelsButton.Click += labelsButton_Click;
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
            timelineView.BorderStyle = BorderStyle.FixedSingle;
            timelineView.Dock = DockStyle.Fill;
            timelineView.Name = "timelineView";
            timelineView.TabIndex = 1;
            // 
            // primarySplitContainer
            // 
            primarySplitContainer.Dock = DockStyle.Fill;
            primarySplitContainer.Location = new Point(0, 0);
            primarySplitContainer.Name = "primarySplitContainer";
            primarySplitContainer.Panel1.Controls.Add(timelineView);
            primarySplitContainer.Panel2.Controls.Add(secondarySplitContainer);
            primarySplitContainer.Orientation = Orientation.Horizontal;
            primarySplitContainer.TabIndex = 2;
            // 
            // secondarySplitContainer
            // 
            secondarySplitContainer.Dock = DockStyle.Fill;
            secondarySplitContainer.BorderStyle = BorderStyle.FixedSingle;
            secondarySplitContainer.Location = new Point(0, 0);
            secondarySplitContainer.Name = "secondarySplitContainer";
            secondarySplitContainer.Panel1.Controls.Add(tabControl);
            secondarySplitContainer.Panel2.Controls.Add(codeView);
            secondarySplitContainer.TabIndex = 2;
            // 
            // tabControl
            // 
            tabControl.Controls.Add(routinesTabPage);
            tabControl.Controls.Add(callerCalleeTabPage);
            tabControl.Controls.Add(callTreeTabPage);
            tabControl.Controls.Add(flameGraphTabPage);
            tabControl.Controls.Add(memoryTabPage);
            tabControl.SelectedIndex = 0;
            tabControl.Dock = DockStyle.Fill;
            tabControl.Location = new Point(0, 0);
            tabControl.Name = "tabControl";
            tabControl.Size = new Size(1745, 486);
            tabControl.TabIndex = 0;
            tabControl.TabStop = false;
            // 
            // dataGridViewCellStyle
            // 
            dataGridViewCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
            dataGridViewCellStyle.BackColor = SystemColors.Window;
            dataGridViewCellStyle.ForeColor = SystemColors.ControlText;
            dataGridViewCellStyle.Format = "N2";
            dataGridViewCellStyle.NullValue = null;
            dataGridViewCellStyle.SelectionBackColor = SystemColors.Highlight;
            dataGridViewCellStyle.SelectionForeColor = SystemColors.HighlightText;
            dataGridViewCellStyle.WrapMode = DataGridViewTriState.False;
            // 
            // callTreeTabPage
            // 
            callTreeTabPage.Controls.Add(callTreeView);
            callTreeTabPage.Name = "callTreeTabPage";
            callTreeTabPage.Padding = new Padding(3);
            callTreeTabPage.TabIndex = 1;
            callTreeTabPage.Text = "Call Tree";
            // 
            // callTreeView
            // 
            callTreeView.DefaultCellStyle = dataGridViewCellStyle;
            callTreeView.Dock = DockStyle.Fill;
            callTreeView.Location = new Point(3, 3);
            callTreeView.Name = "callTreeView";
            callTreeView.RowHeadersWidth = 62;
            callTreeView.Size = new Size(1731, 442);
            callTreeView.TabIndex = 0;
            // 
            // callerCalleeTabPage
            // 
            callerCalleeTabPage.Controls.Add(callerCalleeView);
            callerCalleeTabPage.Name = "callerCalleeTabPage";
            callerCalleeTabPage.Padding = new Padding(3);
            callerCalleeTabPage.TabIndex = 2;
            callerCalleeTabPage.Text = "Caller / Callee";
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
            routinesTabPage.Controls.Add(routinesView);
            routinesTabPage.Name = "routinesTabPage";
            routinesTabPage.Padding = new Padding(3);
            routinesTabPage.TabIndex = 3;
            routinesTabPage.Text = "Routines";
            // 
            // routinesView
            // 
            routinesView.DefaultCellStyle = dataGridViewCellStyle;
            routinesView.Dock = DockStyle.Fill;
            routinesView.Location = new Point(3, 3);
            routinesView.Name = "routinesView";
            routinesView.RowHeadersWidth = 62;
            routinesView.Size = new Size(1731, 442);
            routinesView.TabIndex = 0;
            // 
            // flameGraphTabPage
            // 
            flameGraphTabPage.Controls.Add(flameGraphView);
            flameGraphTabPage.Name = "flameGraphTabPage";
            flameGraphTabPage.Padding = new Padding(3);
            flameGraphTabPage.TabIndex = 4;
            flameGraphTabPage.Text = "Flame Graph";
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
            // memoryTabPage
            // 
            memoryTabPage.Controls.Add(memoryRoutinesView);
            memoryTabPage.Controls.Add(memoryContainer);
            memoryTabPage.Name = "memoryTabPage";
            memoryTabPage.Padding = new Padding(3);
            memoryTabPage.TabIndex = 5;
            memoryTabPage.Text = "Memory";
            // 
            // memoryContainer
            // 
            memoryContainer.AutoSize = true;
            memoryContainer.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            memoryContainer.Dock = DockStyle.Left;
            memoryContainer.Location = new Point(0, 133);
            memoryContainer.Name = "memoryContainer";
            memoryContainer.Controls.Add(memoryView);
            memoryContainer.Controls.Add(memoryZeroPageCheckBox);
            memoryContainer.TabIndex = 0;
            // 
            // memoryZeroPageCheckBox
            // 
            memoryZeroPageCheckBox.Dock = DockStyle.Top;
            memoryZeroPageCheckBox.Location = new Point(0, 0);
            memoryZeroPageCheckBox.AutoSize = true;
            memoryZeroPageCheckBox.Name = "memoryZeroPageCheckBox";
            memoryZeroPageCheckBox.Text = "Zero page addresses";
            memoryZeroPageCheckBox.TabIndex = 0;
            memoryZeroPageCheckBox.CheckedChanged += MemoryZeroPageCheckBox_CheckedChanged;
            // 
            // memoryView
            // 
            memoryView.AutoSize = true;
            memoryView.Dock = DockStyle.Fill;
            memoryView.Location = new Point(0, 0);
            memoryView.Name = "memoryView";
            memoryView.TabIndex = 0;
            // 
            // memoryRoutinesView
            // 
            memoryRoutinesView.Dock = DockStyle.Fill;
            memoryRoutinesView.Location = new Point(0, 0);
            memoryRoutinesView.Name = "memoryRoutinesView";
            memoryRoutinesView.Size = new Size(1745, 487);
            memoryRoutinesView.TabIndex = 0;
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
            Controls.Add(primarySplitContainer);
            Controls.Add(toolStrip);
            Name = "BeebPerfForm";
            Text = "BeebPerf";
            Load += BeebPerfForm_Load;
            toolStrip.ResumeLayout(false);
            toolStrip.PerformLayout();
            secondarySplitContainer.Panel1.ResumeLayout(false);
            secondarySplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)secondarySplitContainer).EndInit();
            secondarySplitContainer.ResumeLayout(false);
            primarySplitContainer.Panel1.ResumeLayout(false);
            primarySplitContainer.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)primarySplitContainer).EndInit();
            primarySplitContainer.ResumeLayout(false);
            tabControl.ResumeLayout(false);
            callTreeTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)callTreeView).EndInit();
            routinesTabPage.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)routinesView).EndInit();
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
        private ToolStripButton hotRoutinesButton;
        private ToolStripButton hotPathsButton;
        private ToolStripButton flipViewButton;
        private ToolStripSeparator toolStripSeparator4;
        private ToolStripButton labelsButton;
        private ToolStripButton settingsButton;
        private ToolStripButton helpButton;
        private ToolStripButton openButton;
        private TimelineView timelineView;
        private SplitContainer primarySplitContainer;
        private SplitContainer secondarySplitContainer;
        private TabControlEx tabControl;
        private Panel callTreeTabPage;
        private Panel callerCalleeTabPage;
        private Panel routinesTabPage;
        private Panel flameGraphTabPage;
        private Panel memoryTabPage;
        private RoutinesView routinesView;
        private CallTreeView callTreeView;
        private CodeView codeView;
        private CallerCalleeView callerCalleeView;
        private FlameGraphView flameGraphView;
        private Panel memoryContainer;
        private CheckBox memoryZeroPageCheckBox;
        private MemoryView memoryView;
        private MemoryRoutinesView memoryRoutinesView;
        private Spinner spinner;
    }
}