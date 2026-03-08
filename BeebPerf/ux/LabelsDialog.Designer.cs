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

namespace BeebPerf.ux
{
    partial class LabelsDialog
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(LabelsDialog));
            labelsGridView = new DataGridView();
            addButton = new Button();
            removeButton = new Button();
            okButton = new Button();
            cancelButton = new Button();
            reloadButton = new Button();
            ((System.ComponentModel.ISupportInitialize)labelsGridView).BeginInit();
            SuspendLayout();
            // 
            // labelsGridView
            // 
            labelsGridView.AllowUserToAddRows = false;
            labelsGridView.AllowUserToDeleteRows = false;
            labelsGridView.AllowUserToResizeRows = false;
            labelsGridView.ColumnHeadersHeight = 34;
            labelsGridView.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            labelsGridView.Location = new Point(12, 12);
            labelsGridView.MultiSelect = false;
            labelsGridView.Name = "labelsGridView";
            labelsGridView.ReadOnly = true;
            labelsGridView.RowHeadersVisible = false;
            labelsGridView.RowHeadersWidth = 62;
            labelsGridView.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            labelsGridView.Size = new Size(858, 290);
            labelsGridView.TabIndex = 0;
            // 
            // addButton
            // 
            addButton.Image = (Image)resources.GetObject("addButton.Image");
            addButton.Location = new Point(12, 308);
            addButton.Name = "addButton";
            addButton.Size = new Size(112, 34);
            addButton.TabIndex = 1;
            addButton.Text = "Add";
            addButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            addButton.UseVisualStyleBackColor = true;
            addButton.Click += AddButton_Click;
            // 
            // removeButton
            // 
            removeButton.Image = (Image)resources.GetObject("removeButton.Image");
            removeButton.Location = new Point(130, 308);
            removeButton.Name = "removeButton";
            removeButton.Size = new Size(122, 34);
            removeButton.TabIndex = 1;
            removeButton.Text = "Remove";
            removeButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            removeButton.UseVisualStyleBackColor = true;
            removeButton.Click += RemoveButton_Click;
            // 
            // okButton
            // 
            okButton.DialogResult = DialogResult.OK;
            okButton.Location = new Point(640, 308);
            okButton.Name = "okButton";
            okButton.Size = new Size(112, 34);
            okButton.TabIndex = 1;
            okButton.Text = "Ok";
            okButton.UseVisualStyleBackColor = true;
            okButton.Click += CloseButton_Click;
            // 
            // cancelButton
            // 
            cancelButton.DialogResult = DialogResult.Cancel;
            cancelButton.Location = new Point(758, 308);
            cancelButton.Name = "cancelButton";
            cancelButton.Size = new Size(112, 34);
            cancelButton.TabIndex = 1;
            cancelButton.Text = "Cancel";
            cancelButton.UseVisualStyleBackColor = true;
            cancelButton.Click += CloseButton_Click;
            // 
            // reloadButton
            // 
            reloadButton.Image = (Image)resources.GetObject("reloadButton.Image");
            reloadButton.Location = new Point(258, 308);
            reloadButton.Name = "reloadButton";
            reloadButton.Size = new Size(122, 34);
            reloadButton.TabIndex = 2;
            reloadButton.Text = "Reload";
            reloadButton.TextImageRelation = TextImageRelation.ImageBeforeText;
            reloadButton.UseVisualStyleBackColor = true;
            reloadButton.Click += ReloadButton_Click;
            // 
            // LabelsDialog
            // 
            AcceptButton = okButton;
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            CancelButton = cancelButton;
            ClientSize = new Size(882, 353);
            Controls.Add(cancelButton);
            Controls.Add(okButton);
            Controls.Add(reloadButton);
            Controls.Add(removeButton);
            Controls.Add(addButton);
            Controls.Add(labelsGridView);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;
            Name = "LabelsDialog";
            Text = "Labels";
            ((System.ComponentModel.ISupportInitialize)labelsGridView).EndInit();
            ResumeLayout(false);
        }
        #endregion

        private DataGridView labelsGridView;
        private Button addButton;
        private Button removeButton;
        private Button reloadButton;
        private Button okButton;
        private Button cancelButton;
    }
}