namespace Chess
{
    partial class fGame
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
            button4 = new Button();
            SuspendLayout();
            // 
            // button4
            // 
            button4.BackColor = Color.White;
            button4.Location = new Point(635, 652);
            button4.Margin = new Padding(4, 3, 4, 3);
            button4.Name = "button4";
            button4.Size = new Size(51, 27);
            button4.TabIndex = 3;
            button4.Text = "Menu";
            button4.UseVisualStyleBackColor = false;
            button4.Click += button4_Click;
            // 
            // fGame
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            BackColor = Color.FromArgb(50, 20, 0);
            ClientSize = new Size(700, 700);
            Controls.Add(button4);
            FormBorderStyle = FormBorderStyle.None;
            Margin = new Padding(4, 3, 4, 3);
            Name = "fGame";
            StartPosition = FormStartPosition.CenterScreen;
            Text = "Form1";
            Load += fGame_Load;
            Click += Form1_Click;
            ResumeLayout(false);
        }

        #endregion

        private System.Windows.Forms.Button button4;
    }
}

