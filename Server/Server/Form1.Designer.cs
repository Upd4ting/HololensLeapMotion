namespace Server
{
    partial class Form1
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
            this.components = new System.ComponentModel.Container();
            this.label1 = new System.Windows.Forms.Label();
            this.portText = new System.Windows.Forms.TextBox();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.button = new System.Windows.Forms.Button();
            this.informationText = new System.Windows.Forms.RichTextBox();
            this.label2 = new System.Windows.Forms.Label();
            this.ipText = new System.Windows.Forms.TextBox();
            this.SuspendLayout();
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(31, 30);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(42, 20);
            this.label1.TabIndex = 0;
            this.label1.Text = "Port:";
            // 
            // portText
            // 
            this.portText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.portText.Location = new System.Drawing.Point(88, 24);
            this.portText.Name = "portText";
            this.portText.Size = new System.Drawing.Size(100, 26);
            this.portText.TabIndex = 1;
            this.portText.Text = "4269";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // button
            // 
            this.button.Location = new System.Drawing.Point(173, 249);
            this.button.Name = "button";
            this.button.Size = new System.Drawing.Size(102, 38);
            this.button.TabIndex = 2;
            this.button.Text = "Start";
            this.button.UseVisualStyleBackColor = true;
            this.button.Click += new System.EventHandler(this.button_Click);
            // 
            // informationText
            // 
            this.informationText.Location = new System.Drawing.Point(35, 90);
            this.informationText.Name = "informationText";
            this.informationText.Size = new System.Drawing.Size(423, 141);
            this.informationText.TabIndex = 3;
            this.informationText.Text = "";
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(221, 27);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(112, 20);
            this.label2.TabIndex = 4;
            this.label2.Text = "Websocket IP:";
            // 
            // ipText
            // 
            this.ipText.Font = new System.Drawing.Font("Microsoft Sans Serif", 12F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.ipText.Location = new System.Drawing.Point(339, 21);
            this.ipText.Name = "ipText";
            this.ipText.Size = new System.Drawing.Size(142, 26);
            this.ipText.TabIndex = 5;
            this.ipText.Text = "4269";
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(493, 313);
            this.Controls.Add(this.ipText);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.informationText);
            this.Controls.Add(this.button);
            this.Controls.Add(this.portText);
            this.Controls.Add(this.label1);
            this.Name = "Form1";
            this.Text = "LeapMotion - Server";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Form1_FormClosing);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox portText;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Button button;
        private System.Windows.Forms.RichTextBox informationText;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.TextBox ipText;
    }
}

