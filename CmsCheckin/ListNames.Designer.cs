﻿namespace CmsCheckin
{
    partial class ListNames
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.GoBackButton = new System.Windows.Forms.Button();
            this.button1 = new System.Windows.Forms.Button();
            this.pgdn = new System.Windows.Forms.Button();
            this.pgup = new System.Windows.Forms.Button();
            this.button2 = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // GoBackButton
            // 
            this.GoBackButton.AutoSize = true;
            this.GoBackButton.BackColor = System.Drawing.Color.LightGreen;
            this.GoBackButton.FlatAppearance.BorderColor = System.Drawing.Color.DarkSlateBlue;
            this.GoBackButton.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.GoBackButton.Font = new System.Drawing.Font("Verdana", 24F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.GoBackButton.ForeColor = System.Drawing.Color.Black;
            this.GoBackButton.Location = new System.Drawing.Point(682, 689);
            this.GoBackButton.Margin = new System.Windows.Forms.Padding(4);
            this.GoBackButton.Name = "GoBackButton";
            this.GoBackButton.Size = new System.Drawing.Size(269, 66);
            this.GoBackButton.TabIndex = 10;
            this.GoBackButton.Text = "Go Back";
            this.GoBackButton.UseVisualStyleBackColor = false;
            this.GoBackButton.Click += new System.EventHandler(this.GoBack_Click);
            // 
            // button1
            // 
            this.button1.AutoSize = true;
            this.button1.BackColor = System.Drawing.Color.Coral;
            this.button1.FlatAppearance.BorderColor = System.Drawing.Color.DarkSlateBlue;
            this.button1.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button1.Font = new System.Drawing.Font("Verdana", 16.2F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.button1.ForeColor = System.Drawing.Color.Black;
            this.button1.Location = new System.Drawing.Point(984, 713);
            this.button1.Margin = new System.Windows.Forms.Padding(4);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(36, 29);
            this.button1.TabIndex = 11;
            this.button1.UseVisualStyleBackColor = false;
            this.button1.Click += new System.EventHandler(this.button1_Click);
            // 
            // pgdn
            // 
            this.pgdn.AutoSize = true;
            this.pgdn.BackColor = System.Drawing.Color.LightGray;
            this.pgdn.FlatAppearance.BorderColor = System.Drawing.Color.DarkSlateBlue;
            this.pgdn.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pgdn.Font = new System.Drawing.Font("Verdana", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pgdn.ForeColor = System.Drawing.Color.Black;
            this.pgdn.Location = new System.Drawing.Point(900, 628);
            this.pgdn.Margin = new System.Windows.Forms.Padding(4);
            this.pgdn.Name = "pgdn";
            this.pgdn.Size = new System.Drawing.Size(120, 53);
            this.pgdn.TabIndex = 14;
            this.pgdn.Text = "PgDn";
            this.pgdn.UseVisualStyleBackColor = false;
            this.pgdn.Click += new System.EventHandler(this.pgdn_Click);
            // 
            // pgup
            // 
            this.pgup.AutoSize = true;
            this.pgup.BackColor = System.Drawing.Color.LightGray;
            this.pgup.FlatAppearance.BorderColor = System.Drawing.Color.DarkSlateBlue;
            this.pgup.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.pgup.Font = new System.Drawing.Font("Verdana", 20F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.pgup.ForeColor = System.Drawing.Color.Black;
            this.pgup.Location = new System.Drawing.Point(902, 4);
            this.pgup.Margin = new System.Windows.Forms.Padding(4);
            this.pgup.Name = "pgup";
            this.pgup.Size = new System.Drawing.Size(118, 53);
            this.pgup.TabIndex = 13;
            this.pgup.Text = "PgUp";
            this.pgup.UseVisualStyleBackColor = false;
            this.pgup.Click += new System.EventHandler(this.pgup_Click);
            // 
            // button2
            // 
            this.button2.BackColor = System.Drawing.SystemColors.Menu;
            this.button2.FlatAppearance.BorderSize = 0;
            this.button2.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.button2.ForeColor = System.Drawing.SystemColors.GrayText;
            this.button2.Location = new System.Drawing.Point(14, 689);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(100, 66);
            this.button2.TabIndex = 15;
            this.button2.Text = ".";
            this.button2.UseVisualStyleBackColor = false;
            this.button2.Click += new System.EventHandler(this.button2_Click);
            // 
            // ListNames
            // 
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.None;
            this.BackColor = System.Drawing.SystemColors.Control;
            this.Controls.Add(this.button2);
            this.Controls.Add(this.pgdn);
            this.Controls.Add(this.pgup);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.GoBackButton);
            this.Margin = new System.Windows.Forms.Padding(4);
            this.Name = "ListNames";
            this.Size = new System.Drawing.Size(1024, 768);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        public System.Windows.Forms.Button GoBackButton;
        public System.Windows.Forms.Button button1;
        public System.Windows.Forms.Button pgdn;
        public System.Windows.Forms.Button pgup;
        private System.Windows.Forms.Button button2;

    }
}
