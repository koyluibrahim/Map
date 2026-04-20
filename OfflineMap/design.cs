partial class Form1
{
    private System.ComponentModel.IContainer components = null;

    private System.Windows.Forms.Panel toolBar;
    private System.Windows.Forms.Panel panel1;
    private System.Windows.Forms.Button btnOpen;
    private System.Windows.Forms.Label lblLat;
    private System.Windows.Forms.TextBox txtLat;
    private System.Windows.Forms.Label lblLon;
    private System.Windows.Forms.TextBox txtLon;
    private System.Windows.Forms.Label lblZoom;
    private System.Windows.Forms.TextBox txtZoom;
    private System.Windows.Forms.Button btnGo;
    private System.Windows.Forms.Label lblStatus;

    protected override void Dispose(bool disposing)
    {
        if (disposing && (components != null)) components.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        toolBar   = new System.Windows.Forms.Panel();
        panel1    = new System.Windows.Forms.Panel();
        btnOpen   = new System.Windows.Forms.Button();
        lblLat    = new System.Windows.Forms.Label();
        txtLat    = new System.Windows.Forms.TextBox();
        lblLon    = new System.Windows.Forms.Label();
        txtLon    = new System.Windows.Forms.TextBox();
        lblZoom   = new System.Windows.Forms.Label();
        txtZoom   = new System.Windows.Forms.TextBox();
        btnGo     = new System.Windows.Forms.Button();
        lblStatus = new System.Windows.Forms.Label();

        // toolBar
        toolBar.Dock      = System.Windows.Forms.DockStyle.Top;
        toolBar.Height    = 38;
        toolBar.BackColor = System.Drawing.Color.FromArgb(50, 50, 50);

        // btnOpen
        btnOpen.Text     = "MBTiles Aç";
        btnOpen.Size     = new System.Drawing.Size(100, 26);
        btnOpen.Location = new System.Drawing.Point(6, 6);
        btnOpen.Click   += new System.EventHandler(this.btnOpen_Click);

        // lblLat
        lblLat.Text      = "Lat:";
        lblLat.ForeColor = System.Drawing.Color.White;
        lblLat.Size      = new System.Drawing.Size(28, 26);
        lblLat.Location  = new System.Drawing.Point(116, 9);
        lblLat.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // txtLat
        txtLat.Text     = "39.0";
        txtLat.Size     = new System.Drawing.Size(75, 22);
        txtLat.Location = new System.Drawing.Point(146, 8);
        txtLat.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCoord_KeyDown);

        // lblLon
        lblLon.Text      = "Lon:";
        lblLon.ForeColor = System.Drawing.Color.White;
        lblLon.Size      = new System.Drawing.Size(28, 26);
        lblLon.Location  = new System.Drawing.Point(230, 9);
        lblLon.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // txtLon
        txtLon.Text     = "35.0";
        txtLon.Size     = new System.Drawing.Size(75, 22);
        txtLon.Location = new System.Drawing.Point(260, 8);
        txtLon.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCoord_KeyDown);

        // lblZoom
        lblZoom.Text      = "Zoom:";
        lblZoom.ForeColor = System.Drawing.Color.White;
        lblZoom.Size      = new System.Drawing.Size(38, 26);
        lblZoom.Location  = new System.Drawing.Point(344, 9);
        lblZoom.TextAlign = System.Drawing.ContentAlignment.MiddleRight;

        // txtZoom
        txtZoom.Text     = "6";
        txtZoom.Size     = new System.Drawing.Size(36, 22);
        txtZoom.Location = new System.Drawing.Point(384, 8);
        txtZoom.KeyDown += new System.Windows.Forms.KeyEventHandler(this.txtCoord_KeyDown);

        // btnGo
        btnGo.Text     = "Git";
        btnGo.Size     = new System.Drawing.Size(55, 26);
        btnGo.Location = new System.Drawing.Point(428, 6);
        btnGo.Click   += new System.EventHandler(this.btnGo_Click);

        // lblStatus
        lblStatus.Text      = "Hazır";
        lblStatus.ForeColor = System.Drawing.Color.LightGray;
        lblStatus.Size      = new System.Drawing.Size(300, 26);
        lblStatus.Location  = new System.Drawing.Point(494, 9);

        toolBar.Controls.AddRange(new System.Windows.Forms.Control[] {
            btnOpen, lblLat, txtLat, lblLon, txtLon,
            lblZoom, txtZoom, btnGo, lblStatus
        });

        // panel1
        panel1.Dock      = System.Windows.Forms.DockStyle.Fill;
        panel1.BackColor = System.Drawing.Color.Black;

        // Form
        this.Text          = "MBTiles Viewer";
        this.Size          = new System.Drawing.Size(900, 650);
        this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
        this.Controls.Add(panel1);
        this.Controls.Add(toolBar);
    }
}
