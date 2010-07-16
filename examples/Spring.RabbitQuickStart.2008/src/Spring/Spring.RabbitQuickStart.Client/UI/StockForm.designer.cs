namespace Spring.RabbitQuickStart.Client.UI
{
    partial class StockForm
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
            this.tradeRequestButton = new System.Windows.Forms.Button();
            this.tradeRequestStatusTextBox = new System.Windows.Forms.TextBox();
            this.marketDataListBox = new System.Windows.Forms.ListBox();
            this.label1 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // tradeRequestButton
            // 
            this.tradeRequestButton.Location = new System.Drawing.Point(16, 15);
            this.tradeRequestButton.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tradeRequestButton.Name = "tradeRequestButton";
            this.tradeRequestButton.Size = new System.Drawing.Size(180, 28);
            this.tradeRequestButton.TabIndex = 0;
            this.tradeRequestButton.Text = "Send Trade Request";
            this.tradeRequestButton.UseVisualStyleBackColor = true;
            this.tradeRequestButton.Click += new System.EventHandler(this.OnSendTradeRequest);
            // 
            // tradeRequestStatusTextBox
            // 
            this.tradeRequestStatusTextBox.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.tradeRequestStatusTextBox.Location = new System.Drawing.Point(205, 16);
            this.tradeRequestStatusTextBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.tradeRequestStatusTextBox.Name = "tradeRequestStatusTextBox";
            this.tradeRequestStatusTextBox.Size = new System.Drawing.Size(366, 22);
            this.tradeRequestStatusTextBox.TabIndex = 1;
            // 
            // marketDataListBox
            // 
            this.marketDataListBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.marketDataListBox.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(177)));
            this.marketDataListBox.FormattingEnabled = true;
            this.marketDataListBox.ItemHeight = 17;
            this.marketDataListBox.Location = new System.Drawing.Point(120, 53);
            this.marketDataListBox.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.marketDataListBox.Name = "marketDataListBox";
            this.marketDataListBox.Size = new System.Drawing.Size(451, 123);
            this.marketDataListBox.TabIndex = 4;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(16, 53);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(93, 17);
            this.label1.TabIndex = 5;
            this.label1.Text = "Market Data :";
            // 
            // StockForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(589, 200);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.marketDataListBox);
            this.Controls.Add(this.tradeRequestStatusTextBox);
            this.Controls.Add(this.tradeRequestButton);
            this.Margin = new System.Windows.Forms.Padding(4, 4, 4, 4);
            this.Name = "StockForm";
            this.Text = "TradeForm";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button tradeRequestButton;
        private System.Windows.Forms.TextBox tradeRequestStatusTextBox;
        private System.Windows.Forms.ListBox marketDataListBox;
        private System.Windows.Forms.Label label1;
    }
}