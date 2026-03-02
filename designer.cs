namespace TurkiyeHarita
{
    partial class Form1
    {
        private System.ComponentModel.IContainer components = null;

        private System.Windows.Forms.Panel controlPanel;
        private System.Windows.Forms.Label latitudeLabel;
        private System.Windows.Forms.TextBox latitudeTextBox;
        private System.Windows.Forms.Label longitudeLabel;
        private System.Windows.Forms.TextBox longitudeTextBox;
        private System.Windows.Forms.Button goButton;

        private GMap.NET.WindowsForms.GMapControl mapControl;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.controlPanel = new System.Windows.Forms.Panel();
            this.latitudeLabel = new System.Windows.Forms.Label();
            this.latitudeTextBox = new System.Windows.Forms.TextBox();
            this.longitudeLabel = new System.Windows.Forms.Label();
            this.longitudeTextBox = new System.Windows.Forms.TextBox();
            this.goButton = new System.Windows.Forms.Button();
            this.mapControl = new GMap.NET.WindowsForms.GMapControl();

            this.controlPanel.SuspendLayout();
            this.SuspendLayout();

            // 
            // controlPanel
            // 
            this.controlPanel.Controls.Add(this.goButton);
            this.controlPanel.Controls.Add(this.longitudeTextBox);
            this.controlPanel.Controls.Add(this.longitudeLabel);
            this.controlPanel.Controls.Add(this.latitudeTextBox);
            this.controlPanel.Controls.Add(this.latitudeLabel);
            this.controlPanel.Dock = System.Windows.Forms.DockStyle.Top;
            this.controlPanel.Location = new System.Drawing.Point(0, 0);
            this.controlPanel.Name = "controlPanel";
            this.controlPanel.Size = new System.Drawing.Size(984, 60);
            this.controlPanel.TabIndex = 0;

            // 
            // latitudeLabel
            // 
            this.latitudeLabel.AutoSize = true;
            this.latitudeLabel.Location = new System.Drawing.Point(12, 20);
            this.latitudeLabel.Name = "latitudeLabel";
            this.latitudeLabel.Size = new System.Drawing.Size(59, 15);
            this.latitudeLabel.TabIndex = 0;
            this.latitudeLabel.Text = "Latitude";

            // 
            // latitudeTextBox
            // 
            this.latitudeTextBox.Location = new System.Drawing.Point(80, 16);
            this.latitudeTextBox.Name = "latitudeTextBox";
            this.latitudeTextBox.Size = new System.Drawing.Size(120, 23);
            this.latitudeTextBox.TabIndex = 1;
            this.latitudeTextBox.Text = "39.0";

            // 
            // longitudeLabel
            // 
            this.longitudeLabel.AutoSize = true;
            this.longitudeLabel.Location = new System.Drawing.Point(220, 20);
            this.longitudeLabel.Name = "longitudeLabel";
            this.longitudeLabel.Size = new System.Drawing.Size(72, 15);
            this.longitudeLabel.TabIndex = 2;
            this.longitudeLabel.Text = "Longitude";

            // 
            // longitudeTextBox
            // 
            this.longitudeTextBox.Location = new System.Drawing.Point(300, 16);
            this.longitudeTextBox.Name = "longitudeTextBox";
            this.longitudeTextBox.Size = new System.Drawing.Size(120, 23);
            this.longitudeTextBox.TabIndex = 3;
            this.longitudeTextBox.Text = "35.0";

            // 
            // goButton
            // 
            this.goButton.Location = new System.Drawing.Point(440, 15);
            this.goButton.Name = "goButton";
            this.goButton.Size = new System.Drawing.Size(90, 25);
            this.goButton.TabIndex = 4;
            this.goButton.Text = "Go";
            this.goButton.UseVisualStyleBackColor = true;

            // 
            // mapControl
            // 
            this.mapControl.Bearing = 0F;
            this.mapControl.CanDragMap = true;
            this.mapControl.DragButton = System.Windows.Forms.MouseButtons.Left;
            this.mapControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.mapControl.EmptyTileColor = System.Drawing.Color.Navy;
            this.mapControl.GrayScaleMode = false;
            this.mapControl.HelperLineOption = GMap.NET.WindowsForms.HelperLineOptions.DontShow;
            this.mapControl.LevelsKeepInMemmory = 5;
            this.mapControl.Location = new System.Drawing.Point(0, 60);
            this.mapControl.MarkersEnabled = true;
            this.mapControl.MaxZoom = 18;
            this.mapControl.MinZoom = 2;
            this.mapControl.MouseWheelZoomEnabled = true;
            this.mapControl.MouseWheelZoomType = GMap.NET.MouseWheelZoomType.MousePositionAndCenter;
            this.mapControl.Name = "mapControl";
            this.mapControl.NegativeMode = false;
            this.mapControl.PolygonsEnabled = true;
            this.mapControl.RetryLoadTile = 0;
            this.mapControl.RoutesEnabled = true;
            this.mapControl.ScaleMode = GMap.NET.WindowsForms.ScaleModes.Integer;
            this.mapControl.SelectedAreaFillColor = System.Drawing.Color.FromArgb(33, 65, 105, 225);
            this.mapControl.ShowTileGridLines = false;
            this.mapControl.Size = new System.Drawing.Size(984, 701);
            this.mapControl.TabIndex = 1;
            this.mapControl.Zoom = 6D;

            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(984, 761);
            this.Controls.Add(this.mapControl);
            this.Controls.Add(this.controlPanel);
            this.Name = "Form1";
            this.Text = "Turkiye Harita";

            this.controlPanel.ResumeLayout(false);
            this.controlPanel.PerformLayout();
            this.ResumeLayout(false);
        }

        #endregion
    }
}
