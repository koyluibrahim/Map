using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TurkiyeHarita
{
    public partial class Form1 : Form
    {
        // --- Core GMap Engine Components ---
        private GMapControl gmap;                    // Main map controller object
        private GMapOverlay markerOverlay;          // Overlay for search and city markers (Red)
        private GMapOverlay polyOverlay;            // Overlay for distance measurement (Blue markers & Orange lines)

        // --- UI Control Elements ---
        private TextBox txtLat, txtLng;             // Input fields for manual coordinates
        private Button btnGo, btnZoomIn, btnZoomOut, btnClearMeasurement;
        private Label lblCoordValue, lblStatusBar, lblDistanceResult;
        private Panel panelLeft;

        // --- Data Management ---
        // Stores points selected by the user (Shift+Left Click) for distance calculation
        private List<PointLatLng> measurementPoints = new List<PointLatLng>();

        // Preset coordinates for major Turkish cities for quick navigation
        private readonly string[,] cities = {
            { "Istanbul",   "41.0082", "28.9784" },
            { "Ankara",     "39.9334", "32.8597" },
            { "Izmir",      "38.4192", "27.1287" },
            { "Bursa",      "40.1885", "29.0610" },
            { "Antalya",    "36.8969", "30.7133" },
            { "Adana",      "37.0000", "35.3213" },
            { "Trabzon",    "41.0015", "39.7178" },
            { "Erzurum",    "39.9043", "41.2679" },
            { "Diyarbakir", "37.9144", "40.2306" },
            { "Konya",      "37.8713", "32.4846" },
        };

        public Form1()
        {
            InitializeComponent();
            InitializeUI();      // Build the interface programmatically
            InitializeMap();     // Setup GMap engine configurations
        }

        /// <summary>
        /// Programmatically creates the Sidebar, Labels, Buttons, and Layout.
        /// No Visual Studio Designer (Designer.cs) is required.
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "Turkey Map - Coordinate & Distance Analysis Tool";
            this.Size = new Size(1280, 850);
            this.MinimumSize = new Size(1000, 700);
            this.BackColor = Color.FromArgb(18, 18, 28); // Dark theme background

            // Bottom bar for application status
            lblStatusBar = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(10, 10, 18),
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Courier New", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "System Ready"
            };

            // Sidebar Panel for controls
            panelLeft = new Panel { Dock = DockStyle.Left, Width = 270, BackColor = Color.FromArgb(22, 22, 35) };

            int y = 20;

            // --- Coordinate Input Section ---
            CreateLabel("COORDINATE INPUT", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("Latitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLat = CreateTextInput(y, "39.9334"); y += 32;
            CreateLabel("Longitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLng = CreateTextInput(y, "32.8597"); y += 36;

            btnGo = CreateStandardButton("▶   GO TO LOCATION", y, Color.FromArgb(0, 122, 204));
            btnGo.Click += (s, e) => HandleGoButtonClick();
            y += 48; DrawSeparator(y); y += 14;

            // --- Map Navigation Controls ---
            CreateLabel("MAP ZOOM", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            btnZoomIn = new Button { Text = "➕", Location = new Point(15, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomOut = new Button { Text = "➖", Location = new Point(140, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomIn.Click += (s, e) => gmap.Zoom++;
            btnZoomOut.Click += (s, e) => gmap.Zoom--;
            panelLeft.Controls.Add(btnZoomIn); panelLeft.Controls.Add(btnZoomOut);
            y += 44; DrawSeparator(y); y += 14;

            // --- Quick City Buttons ---
            CreateLabel("QUICK CITIES", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            for (int i = 0; i < cities.GetLength(0); i++)
            {
                string name = cities[i, 0]; string lat = cities[i, 1]; string lng = cities[i, 2];
                int col = (i % 2 == 0) ? 15 : 135; if (i % 2 == 0 && i > 0) y += 34;
                var btn = new Button { Text = name, Location = new Point(col, y), Size = new Size(112, 28), Font = new Font("Segoe UI", 8.5f), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = new string[] { lat, lng } };
                btn.Click += (s, e) => HandleCityButtonClick(s);
                panelLeft.Controls.Add(btn);
            }
            y += 46; DrawSeparator(y); y += 14;

            // --- Measurement Tools ---
            CreateLabel("DISTANCE MEASUREMENT", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("SHIFT + LeftClick: Add Point", 8, FontStyle.Italic, Color.FromArgb(180, 180, 100), y, 240); y += 20;
            lblDistanceResult = CreateLabel("Total Distance: 0.00 km", 9, FontStyle.Bold, Color.FromArgb(255, 180, 50), y, 240); y += 25;
            btnClearMeasurement = new Button { Text = "🗑 CLEAR MEASUREMENT", Location = new Point(15, y), Size = new Size(240, 30), Font = new Font("Segoe UI", 8, FontStyle.Bold), BackColor = Color.FromArgb(80, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClearMeasurement.Click += (s, e) => ClearDistance();
            panelLeft.Controls.Add(btnClearMeasurement); y += 45; DrawSeparator(y); y += 14;

            // --- Live Crosshair Tracker ---
            CreateLabel("LIVE COORDINATES (+)", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            lblCoordValue = CreateLabel("Loading...", 9, FontStyle.Bold, Color.FromArgb(80, 220, 140), y, 240);
            lblCoordValue.Height = 50;

            // --- Map View Configuration ---
            gmap = new GMapControl { Dock = DockStyle.Fill, MinZoom = 5, MaxZoom = 19, Zoom = 6, ShowCenter = true };
            gmap.MouseClick += (s, e) => HandleMapClick(e);
            gmap.OnPositionChanged += (p) => lblCoordValue.Text = $"Lat: {p.Lat:F6}°\nLng: {p.Lng:F6}°";

            this.Controls.Add(gmap); this.Controls.Add(panelLeft); this.Controls.Add(lblStatusBar);
        }

        private void InitializeMap()
        {
            // Set UserAgent to prevent 403 Forbidden errors from tile servers
            GMapProvider.UserAgent = "TurkeyMapApp/1.0";
            gmap.MapProvider = GMapProviders.BingHybridMap; // Combined Satellite and Road data
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Enables offline caching
            gmap.Position = new PointLatLng(39.0, 35.5);    // Center of Turkey
            gmap.DragButton = MouseButtons.Left;
            
            // Setup layers
            markerOverlay = new GMapOverlay("markers");
            polyOverlay = new GMapOverlay("polygons");
            gmap.Overlays.Add(markerOverlay);
            gmap.Overlays.Add(polyOverlay);
            
            lblStatusBar.Text = "Map Engine Initialized: Bing Hybrid Mode";
        }

        /// <summary>
        /// Handles mouse interactions on the map.
        /// Right Click: Single point marker.
        /// Shift + Left Click: Multi-point distance measurement.
        /// </summary>
        private void HandleMapClick(MouseEventArgs e)
        {
            var p = gmap.FromLocalToLatLng(e.X, e.Y);

            // Distance Measurement Logic
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
            {
                measurementPoints.Add(p);
                DrawDistance();
                lblStatusBar.Text = $"Added point: {p.Lat:F4}, {p.Lng:F4}";
            }
            // Manual Pin Logic
            else if (e.Button == MouseButtons.Right)
            {
                txtLat.Text = p.Lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                txtLng.Text = p.Lng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                AddMarker(p, $"Lat: {p.Lat:F4}\nLng: {p.Lng:F4}");
            }
        }

        /// <summary>
        /// Updates the polyline and measurement markers on the map.
        /// Calculates the total Great-Circle distance in kilometers.
        /// </summary>
        private void DrawDistance()
        {
            polyOverlay.Clear();
            
            foreach (var point in measurementPoints)
            {
                var circle = new GMarkerGoogle(point, GMarkerGoogleType.blue_small);
                circle.ToolTipText = $"Lat: {point.Lat:F4}\nLng: {point.Lng:F4}";
                circle.ToolTipMode = MarkerTooltipMode.Always;
                circle.ToolTip = new CustomToolTip(circle);
                polyOverlay.Markers.Add(circle);
            }

            if (measurementPoints.Count > 1)
            {
                var route = new GMapRoute(measurementPoints, "DistRoute");
                route.Stroke = new Pen(Color.FromArgb(255, 180, 50), 3); // Orange path
                polyOverlay.Routes.Add(route);
                
                double totalKm = 0;
                for (int i = 0; i < measurementPoints.Count - 1; i++)
                    totalKm += GMapProviders.EmptyProvider.Projection.GetDistance(measurementPoints[i], measurementPoints[i + 1]);
                
                lblDistanceResult.Text = $"Total Distance: {totalKm:F2} km";
            }
        }

        private void HandleGoButtonClick()
        {
            if (double.TryParse(txtLat.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(txtLng.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                gmap.Position = new PointLatLng(lat, lng);
                AddMarker(gmap.Position, $"Lat: {lat:F4}\nLng: {lng:F4}");
            }
            else { MessageBox.Show("Please enter valid numeric coordinates.", "Input Error"); }
        }

        private void HandleCityButtonClick(object sender)
        {
            var coords = (string[])((Button)sender).Tag;
            double lat = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
            double lng = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
            
            txtLat.Text = coords[0]; txtLng.Text = coords[1];
            gmap.Position = new PointLatLng(lat, lng);
            AddMarker(gmap.Position, $"City: {((Button)sender).Text}\nLat: {lat:F4}\nLng: {lng:F4}");
        }

        private void AddMarker(PointLatLng loc, string title)
        {
            markerOverlay.Markers.Clear();
            var m = new GMarkerGoogle(loc, GMarkerGoogleType.red);
            m.ToolTipText = title; 
            m.ToolTipMode = MarkerTooltipMode.Always;
            m.ToolTip = new CustomToolTip(m); 
            markerOverlay.Markers.Add(m);
        }

        private void ClearDistance() 
        { 
            measurementPoints.Clear(); 
            polyOverlay.Clear(); 
            lblDistanceResult.Text = "Total Distance: 0.00 km"; 
            lblStatusBar.Text = "Measurement cleared.";
        }

        // --- UI Element Factory Methods ---
        private Button CreateStandardButton(string t, int y, Color c) { var b = new Button { Text = t, Location = new Point(15, y), Size = new Size(240, 38), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; panelLeft.Controls.Add(b); return b; }
        private Label CreateLabel(string t, float s, FontStyle st, Color c, int y, int w) { var l = new Label { Text = t, ForeColor = c, Font = new Font("Segoe UI", s, st), Location = new Point(15, y), Size = new Size(w, 20), AutoSize = false }; panelLeft.Controls.Add(l); return l; }
        private TextBox CreateTextInput(int y, string d) { var t = new TextBox { Location = new Point(15, y), Size = new Size(240, 26), Font = new Font("Courier New", 10), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGreen, BorderStyle = BorderStyle.FixedSingle, Text = d }; panelLeft.Controls.Add(t); return t; }
        private void DrawSeparator(int y) => panelLeft.Controls.Add(new Label { Location = new Point(15, y), Size = new Size(240, 1), BackColor = Color.FromArgb(55, 55, 75) });
    }

    /// <summary>
    /// Custom Renderer for GMap ToolTips.
    /// Provides a clean, modern, and centered tooltip box.
    /// </summary>
    public class CustomToolTip : GMapToolTip
    {
        public CustomToolTip(GMapMarker m) : base(m) 
        { 
            Stroke = new Pen(Color.Gray, 1); 
            Fill = Brushes.White; 
            Foreground = Brushes.Black; 
            Font = new Font("Segoe UI", 9); 
        }

        public override void OnRender(Graphics g)
        {
            var sz = g.MeasureString(Marker.ToolTipText, Font);
            int padding = 8;
            int width = (int)sz.Width + (padding * 2); 
            int height = (int)sz.Height + (padding * 2);
            
            // Positioning the box above the marker
            var rect = new Rectangle(Marker.LocalPosition.X - (width / 2), Marker.LocalPosition.Y - Marker.Size.Height - height - 2, width, height);
            
            g.FillRectangle(Fill, rect); 
            g.DrawRectangle(Stroke, rect);
            g.DrawString(Marker.ToolTipText, Font, Foreground, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }
}
