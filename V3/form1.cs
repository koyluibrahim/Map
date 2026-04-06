using System;
using System.Collections.Generic;
using System.Drawing;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TurkiyeHarita
{
    public partial class Form1 : Form
    {
        // Core GMap components
        private GMapControl gmap;
        private GMapOverlay markerOverlay;
        private GMapOverlay polyOverlay;

        // UI Controls
        private TextBox txtLat, txtLng;
        private Button btnGo, btnZoomIn, btnZoomOut, btnClearMeasurement;
        private Label lblCoordValue, lblStatusBar, lblDistanceResult;
        private Panel panelLeft;

        // Data storage for distance tool and API requests
        private List<PointLatLng> measurementPoints = new List<PointLatLng>();
        private static readonly HttpClient httpClient = new HttpClient();

        // Preset city data
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
            InitializeUI();
            InitializeMap();
        }

        private void InitializeUI()
        {
            // Main Window Settings
            this.Text = "Turkey Map - Elevation & Analysis Tool";
            this.Size = new Size(1280, 850);
            this.MinimumSize = new Size(1000, 700);
            this.BackColor = Color.FromArgb(18, 18, 28);

            // Information bar at the bottom
            lblStatusBar = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(10, 10, 18),
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Courier New", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = "Ready"
            };

            // Sidebar Container
            panelLeft = new Panel { Dock = DockStyle.Left, Width = 270, BackColor = Color.FromArgb(22, 22, 35) };

            int y = 20;

            // Manual Entry Section
            CreateLabel("COORDINATE INPUT", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("Latitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLat = CreateTextInput(y, "39.9334"); y += 32;
            CreateLabel("Longitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLng = CreateTextInput(y, "32.8597"); y += 36;

            btnGo = CreateStandardButton("▶   GO TO LOCATION", y, Color.FromArgb(0, 122, 204));
            btnGo.Click += async (s, e) => await HandleGoButtonClick();
            y += 48; DrawSeparator(y); y += 14;

            // Zoom Control Section
            CreateLabel("MAP ZOOM", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            btnZoomIn = new Button { Text = "➕", Location = new Point(15, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomOut = new Button { Text = "➖", Location = new Point(140, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomIn.Click += (s, e) => gmap.Zoom++;
            btnZoomOut.Click += (s, e) => gmap.Zoom--;
            panelLeft.Controls.Add(btnZoomIn); panelLeft.Controls.Add(btnZoomOut);
            y += 44; DrawSeparator(y); y += 14;

            // City Selection List
            CreateLabel("QUICK CITIES", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            for (int i = 0; i < cities.GetLength(0); i++)
            {
                string name = cities[i, 0]; string lat = cities[i, 1]; string lng = cities[i, 2];
                int col = (i % 2 == 0) ? 15 : 135; if (i % 2 == 0 && i > 0) y += 34;
                var btn = new Button { Text = name, Location = new Point(col, y), Size = new Size(112, 28), Font = new Font("Segoe UI", 8.5f), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = new string[] { lat, lng } };
                btn.Click += async (s, e) => await HandleCityButtonClick(s);
                panelLeft.Controls.Add(btn);
            }
            y += 46; DrawSeparator(y); y += 14;

            // Analysis & Measurement Section
            CreateLabel("DISTANCE & ELEVATION", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("SHIFT+LeftClick: Add Point", 8, FontStyle.Italic, Color.FromArgb(180, 180, 100), y, 240); y += 20;
            lblDistanceResult = CreateLabel("Total Distance: 0.00 km", 9, FontStyle.Bold, Color.FromArgb(255, 180, 50), y, 240); y += 25;
            btnClearMeasurement = new Button { Text = "🗑 CLEAR MEASUREMENT", Location = new Point(15, y), Size = new Size(240, 30), Font = new Font("Segoe UI", 8, FontStyle.Bold), BackColor = Color.FromArgb(80, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClearMeasurement.Click += (s, e) => ClearDistance();
            panelLeft.Controls.Add(btnClearMeasurement); y += 45; DrawSeparator(y); y += 14;

            // Real-time Coordinate & Altitude Display
            CreateLabel("LIVE COORDINATES (+)", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            lblCoordValue = CreateLabel("Fetching...", 9, FontStyle.Bold, Color.FromArgb(80, 220, 140), y, 240);
            lblCoordValue.Height = 85;

            // Configure Map Component
            gmap = new GMapControl { Dock = DockStyle.Fill, MinZoom = 5, MaxZoom = 19, Zoom = 6, ShowCenter = true };
            gmap.MouseClick += async (s, e) => await HandleMapClick(e);
            gmap.OnPositionChanged += async (p) => await UpdateLiveStats(p);

            this.Controls.Add(gmap); this.Controls.Add(panelLeft); this.Controls.Add(lblStatusBar);
        }

        private void InitializeMap()
        {
            GMapProvider.UserAgent = "TurkeyMapApp/1.0";
            gmap.MapProvider = GMapProviders.BingHybridMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Allows offline use
            gmap.Position = new PointLatLng(39.0, 35.5);
            gmap.DragButton = MouseButtons.Left;
            
            markerOverlay = new GMapOverlay("markers");
            polyOverlay = new GMapOverlay("polygons");
            gmap.Overlays.Add(markerOverlay);
            gmap.Overlays.Add(polyOverlay);
        }

        // Triggered when map crosshair moves
        private async Task UpdateLiveStats(PointLatLng p)
        {
            string elev = await GetElevationAsync(p.Lat, p.Lng);
            lblCoordValue.Text = $"Lat: {p.Lat:F6}°\nLng: {p.Lng:F6}°\nAlt: {elev}";
        }

        // Requests elevation from Open-Elevation API
        private async Task<string> GetElevationAsync(double lat, double lng)
        {
            try
            {
                lblStatusBar.Text = "⏳ Fetching elevation...";
                string url = $"https://api.open-elevation.com/api/v1/lookup?locations={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                string response = await httpClient.GetStringAsync(url);
                var match = Regex.Match(response, @"""elevation"":\s*([\d.-]+)");
                lblStatusBar.Text = "✔ Elevation received";
                return match.Success ? $"{match.Groups[1].Value} m" : "N/A";
            }
            catch { lblStatusBar.Text = "❌ API Error"; return "N/A"; }
        }

        // Interaction Handler
        private async Task HandleMapClick(MouseEventArgs e)
        {
            var p = gmap.FromLocalToLatLng(e.X, e.Y);
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
            {
                string elev = await GetElevationAsync(p.Lat, p.Lng);
                measurementPoints.Add(p);
                DrawDistance(elev); 
            }
            else if (e.Button == MouseButtons.Right)
            {
                string elev = await GetElevationAsync(p.Lat, p.Lng);
                txtLat.Text = p.Lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                txtLng.Text = p.Lng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                AddMarker(p, $"Lat: {p.Lat:F4}\nLng: {p.Lng:F4}\nAlt: {elev}");
            }
        }

        // Updates distance line and point tooltips
        private void DrawDistance(string lastElev)
        {
            polyOverlay.Clear();
            foreach (var point in measurementPoints)
            {
                var circle = new GMarkerGoogle(point, GMarkerGoogleType.blue_small);
                circle.ToolTipText = $"Lat: {point.Lat:F4}\nLng: {point.Lng:F4}\nAlt: {lastElev}";
                circle.ToolTipMode = MarkerTooltipMode.Always;
                circle.ToolTip = new CustomToolTip(circle);
                polyOverlay.Markers.Add(circle);
            }

            if (measurementPoints.Count > 1)
            {
                var route = new GMapRoute(measurementPoints, "Dist");
                route.Stroke = new Pen(Color.FromArgb(255, 180, 50), 3);
                polyOverlay.Routes.Add(route);
                double totalKm = 0;
                for (int i = 0; i < measurementPoints.Count - 1; i++)
                    totalKm += GMapProviders.EmptyProvider.Projection.GetDistance(measurementPoints[i], measurementPoints[i + 1]);
                lblDistanceResult.Text = $"Total Distance: {totalKm:F2} km";
            }
        }

        private async Task HandleGoButtonClick()
        {
            if (double.TryParse(txtLat.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lat) &&
                double.TryParse(txtLng.Text.Replace(',', '.'), System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out double lng))
            {
                string elev = await GetElevationAsync(lat, lng);
                gmap.Position = new PointLatLng(lat, lng);
                AddMarker(gmap.Position, $"Lat: {lat:F4}\nLng: {lng:F4}\nAlt: {elev}");
            }
        }

        private async Task HandleCityButtonClick(object sender)
        {
            var coords = (string[])((Button)sender).Tag;
            double lat = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
            double lng = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
            txtLat.Text = coords[0]; txtLng.Text = coords[1];
            
            string elev = await GetElevationAsync(lat, lng);
            gmap.Position = new PointLatLng(lat, lng);
            AddMarker(gmap.Position, $"Lat: {lat:F4}\nLng: {lng:F4}\nAlt: {elev}");
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
        }

        // UI Factory Helpers
        private Button CreateStandardButton(string t, int y, Color c) { var b = new Button { Text = t, Location = new Point(15, y), Size = new Size(240, 38), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; panelLeft.Controls.Add(b); return b; }
        private Label CreateLabel(string t, float s, FontStyle st, Color c, int y, int w) { var l = new Label { Text = t, ForeColor = c, Font = new Font("Segoe UI", s, st), Location = new Point(15, y), Size = new Size(w, 20), AutoSize = false }; panelLeft.Controls.Add(l); return l; }
        private TextBox CreateTextInput(int y, string d) { var t = new TextBox { Location = new Point(15, y), Size = new Size(240, 26), Font = new Font("Courier New", 10), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGreen, BorderStyle = BorderStyle.FixedSingle, Text = d }; panelLeft.Controls.Add(t); return t; }
        private void DrawSeparator(int y) => panelLeft.Controls.Add(new Label { Location = new Point(15, y), Size = new Size(240, 1), BackColor = Color.FromArgb(55, 55, 75) });
    }

    // Custom Tooltip Renderer
    public class CustomToolTip : GMapToolTip
    {
        public CustomToolTip(GMapMarker m) : base(m) { Stroke = new Pen(Color.Gray, 1); Fill = Brushes.White; Foreground = Brushes.Black; Font = new Font("Segoe UI", 9); }
        public override void OnRender(Graphics g)
        {
            var sz = g.MeasureString(Marker.ToolTipText, Font);
            int p = 8; int w = (int)sz.Width + (p * 2); int h = (int)sz.Height + (p * 2);
            var rect = new Rectangle(Marker.LocalPosition.X - (w / 2), Marker.LocalPosition.Y - Marker.Size.Height - h - 2, w, h);
            g.FillRectangle(Fill, rect); g.DrawRectangle(Stroke, rect);
            g.DrawString(Marker.ToolTipText, Font, Foreground, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }
}
