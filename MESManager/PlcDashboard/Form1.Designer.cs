namespace PlcDashboard;

partial class Form1
{
    /// <summary>
    ///  Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    ///  Clean up any resources being used.
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
    ///  Required method for Designer support - do not modify
    ///  the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        dataGridView1 = new DataGridView();
        timer1 = new System.Windows.Forms.Timer(components);
        statusStrip1 = new StatusStrip();
        toolStripStatusLabel1 = new ToolStripStatusLabel();
        toolStripStatusLabel2 = new ToolStripStatusLabel();
        panel1 = new Panel();
        btnRefresh = new Button();
        chkAutoRefresh = new CheckBox();
        numRefreshInterval = new NumericUpDown();
        label1 = new Label();
        txtApiUrl = new TextBox();
        label2 = new Label();
        ((System.ComponentModel.ISupportInitialize)dataGridView1).BeginInit();
        statusStrip1.SuspendLayout();
        panel1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)numRefreshInterval).BeginInit();
        SuspendLayout();
        // 
        // dataGridView1
        // 
        dataGridView1.AllowUserToAddRows = false;
        dataGridView1.AllowUserToDeleteRows = false;
        dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        dataGridView1.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
        dataGridView1.Dock = DockStyle.Fill;
        dataGridView1.Location = new Point(0, 100);
        dataGridView1.Name = "dataGridView1";
        dataGridView1.ReadOnly = true;
        dataGridView1.Size = new Size(1400, 600);
        dataGridView1.TabIndex = 0;
        // 
        // timer1
        // 
        timer1.Interval = 4000;
        timer1.Tick += timer1_Tick;
        // 
        // statusStrip1
        // 
        statusStrip1.Items.AddRange(new ToolStripItem[] { toolStripStatusLabel1, toolStripStatusLabel2 });
        statusStrip1.Location = new Point(0, 700);
        statusStrip1.Name = "statusStrip1";
        statusStrip1.Size = new Size(1400, 22);
        statusStrip1.TabIndex = 1;
        statusStrip1.Text = "statusStrip1";
        // 
        // toolStripStatusLabel1
        // 
        toolStripStatusLabel1.Name = "toolStripStatusLabel1";
        toolStripStatusLabel1.Size = new Size(39, 17);
        toolStripStatusLabel1.Text = "Pronto";
        // 
        // toolStripStatusLabel2
        // 
        toolStripStatusLabel2.Name = "toolStripStatusLabel2";
        toolStripStatusLabel2.Size = new Size(0, 17);
        // 
        // panel1
        // 
        panel1.Controls.Add(label2);
        panel1.Controls.Add(txtApiUrl);
        panel1.Controls.Add(label1);
        panel1.Controls.Add(numRefreshInterval);
        panel1.Controls.Add(chkAutoRefresh);
        panel1.Controls.Add(btnRefresh);
        panel1.Dock = DockStyle.Top;
        panel1.Location = new Point(0, 0);
        panel1.Name = "panel1";
        panel1.Size = new Size(1400, 100);
        panel1.TabIndex = 2;
        // 
        // btnRefresh
        // 
        btnRefresh.Location = new Point(12, 60);
        btnRefresh.Name = "btnRefresh";
        btnRefresh.Size = new Size(100, 30);
        btnRefresh.TabIndex = 0;
        btnRefresh.Text = "Refresh";
        btnRefresh.UseVisualStyleBackColor = true;
        btnRefresh.Click += btnRefresh_Click;
        // 
        // chkAutoRefresh
        // 
        chkAutoRefresh.AutoSize = true;
        chkAutoRefresh.Checked = true;
        chkAutoRefresh.CheckState = CheckState.Checked;
        chkAutoRefresh.Location = new Point(130, 65);
        chkAutoRefresh.Name = "chkAutoRefresh";
        chkAutoRefresh.Size = new Size(95, 19);
        chkAutoRefresh.TabIndex = 1;
        chkAutoRefresh.Text = "Auto-refresh";
        chkAutoRefresh.UseVisualStyleBackColor = true;
        chkAutoRefresh.CheckedChanged += chkAutoRefresh_CheckedChanged;
        // 
        // numRefreshInterval
        // 
        numRefreshInterval.Location = new Point(350, 63);
        numRefreshInterval.Maximum = new decimal(new int[] { 60, 0, 0, 0 });
        numRefreshInterval.Minimum = new decimal(new int[] { 2, 0, 0, 0 });
        numRefreshInterval.Name = "numRefreshInterval";
        numRefreshInterval.Size = new Size(60, 23);
        numRefreshInterval.TabIndex = 2;
        numRefreshInterval.Value = new decimal(new int[] { 4, 0, 0, 0 });
        numRefreshInterval.ValueChanged += numRefreshInterval_ValueChanged;
        // 
        // label1
        // 
        label1.AutoSize = true;
        label1.Location = new Point(250, 67);
        label1.Name = "label1";
        label1.Size = new Size(94, 15);
        label1.TabIndex = 3;
        label1.Text = "Interval (secondi):";
        // 
        // txtApiUrl
        // 
        txtApiUrl.Location = new Point(80, 20);
        txtApiUrl.Name = "txtApiUrl";
        txtApiUrl.Size = new Size(500, 23);
        txtApiUrl.TabIndex = 4;
        txtApiUrl.Text = "http://localhost:5156";
        // 
        // label2
        // 
        label2.AutoSize = true;
        label2.Location = new Point(12, 23);
        label2.Name = "label2";
        label2.Size = new Size(52, 15);
        label2.TabIndex = 5;
        label2.Text = "API URL:";
        // 
        // Form1
        // 
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(1400, 722);
        Controls.Add(dataGridView1);
        Controls.Add(statusStrip1);
        Controls.Add(panel1);
        Name = "Form1";
        Text = "PLC Dashboard - Realtime Monitor";
        Load += Form1_Load;
        ((System.ComponentModel.ISupportInitialize)dataGridView1).EndInit();
        statusStrip1.ResumeLayout(false);
        statusStrip1.PerformLayout();
        panel1.ResumeLayout(false);
        panel1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)numRefreshInterval).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private DataGridView dataGridView1;
    private System.Windows.Forms.Timer timer1;
    private StatusStrip statusStrip1;
    private ToolStripStatusLabel toolStripStatusLabel1;
    private ToolStripStatusLabel toolStripStatusLabel2;
    private Panel panel1;
    private Button btnRefresh;
    private CheckBox chkAutoRefresh;
    private NumericUpDown numRefreshInterval;
    private Label label1;
    private TextBox txtApiUrl;
    private Label label2;
}
