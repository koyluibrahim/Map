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
        // --- Map Engine Controls ---
        private GMapControl gmap;                    // The main map controller
        private GMapOverlay markerOverlay;          // Layer for red search/city markers
        private GMapOverlay polyOverlay;            // Layer for distance measurement points and lines

        // --- UI Components ---
        private TextBox txtLat, txtLng;             // Input fields for manual coordinate entry
        private Button btnGo, btnZoomIn, btnZoomOut, btnClearMeasurement;
        private Label lblCoordValue, lblStatusBar, lblDistanceResult;
        private Panel panelLeft;

        // --- Data Management ---
        // List to store points selected by the user for distance calculation
        private List<PointLatLng> measurementPoints = new List<PointLatLng>();
        
        // HttpClient instance for making Elevation API requests (static to prevent socket exhaustion)
        private static readonly HttpClient httpClient = new HttpClient();

        // Preset city coordinates for quick navigation
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
            InitializeUI();      // Programmatically build the interface
            InitializeMap();     // Configure the GMap engine
        }

        /// <summary>
        /// Builds the entire User Interface without using the Visual Studio Designer.
        /// This ensures high portability and precise control over UI elements.
        /// </summary>
        private void InitializeUI()
        {
            this.Text = "Turkey Map - Elevation & Analysis Tool";
            this.Size = new Size(1280, 850);
            this.MinimumSize = new Size(1000, 700);
            this.BackColor = Color.FromArgb(18, 18, 28);

            // Bottom bar for status messages and API feedback
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

            // Left panel containing all interactive controls
            panelLeft = new Panel { Dock = DockStyle.Left, Width = 270, BackColor = Color.FromArgb(22, 22, 35) };

            int y = 20;
            // Coordinate Input Section
            CreateLabel("COORDINATE INPUT", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("Latitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLat = CreateTextInput(y, "39.9334"); y += 32;
            CreateLabel("Longitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLng = CreateTextInput(y, "32.8597"); y += 36;

            btnGo = CreateStandardButton("▶   GO TO LOCATION", y, Color.FromArgb(0, 122, 204));
            btnGo.Click += async (s, e) => await HandleGoButtonClick();
            y += 48; DrawSeparator(y); y += 14;

            // Zoom Controls Section
            CreateLabel("MAP ZOOM", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            btnZoomIn = new Button { Text = "➕", Location = new Point(15, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomOut = new Button { Text = "➖", Location = new Point(140, y), Size = new Size(115, 32), BackMapColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomIn.Click += (s, e) => gmap.Zoom++;
            btnZoomOut.Click += (s, e) => gmap.Zoom--;
            panelLeft.Controls.Add(btnZoomIn); panelLeft.Controls.Add(btnZoomOut);
            y += 44; DrawSeparator(y); y += 14;

            // Quick City Navigation Section
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

            // Distance & Analysis Section
            CreateLabel("DISTANCE & ELEVATION", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("SHIFT+LeftClick: Add Point", 8, FontStyle.Italic, Color.FromArgb(180, 180, 100), y, 240); y += 20;
            lblDistanceResult = CreateLabel("Total Distance: 0.00 km", 9, FontStyle.Bold, Color.FromArgb(255, 180, 50), y, 240); y += 25;
            btnClearMeasurement = new Button { Text = "🗑 CLEAR MEASUREMENT", Location = new Point(15, y), Size = new Size(240, 30), Font = new Font("Segoe UI", 8, FontStyle.Bold), BackColor = Color.FromArgb(80, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClearMeasurement.Click += (s, e) => ClearDistance();
            panelLeft.Controls.Add(btnClearMeasurement); y += 45; DrawSeparator(y); y += 14;

            // Live Coordinate Tracking Section
            CreateLabel("LIVE COORDINATES (+)", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            lblCoordValue = CreateLabel("Fetching...", 9, FontStyle.Bold, Color.FromArgb(80, 220, 140), y, 240);
            lblCoordValue.Height = 85;

            // Main Map Control Initialization
            gmap = new GMapControl { Dock = DockStyle.Fill, MinZoom = 5, MaxZoom = 19, Zoom = 6, ShowCenter = true };
            gmap.MouseClick += async (s, e) => await HandleMapClick(e);
            gmap.OnPositionChanged += async (p) => await UpdateLiveStats(p); // Dynamic coordinate & elevation update

            this.Controls.Add(gmap); this.Controls.Add(panelLeft); this.Controls.Add(lblStatusBar);
        }

        /// <summary>
        /// Configures the GMap.NET engine settings, providers, and layers.
        /// </summary>
        private void InitializeMap()
        {
            GMapProvider.UserAgent = "TurkeyMapApp/1.0";
            gmap.MapProvider = GMapProviders.BingHybridMap; // Combined Satellite and Road view
            GMaps.Instance.Mode = AccessMode.ServerAndCache; // Enables offline capability
            gmap.Position = new PointLatLng(39.0, 35.5);    // Default start position (Turkey center)
            gmap.DragButton = MouseButtons.Left;
            
            // Initializing overlays for organizational structure
            markerOverlay = new GMapOverlay("markers");
            polyOverlay = new GMapOverlay("polygons");
            gmap.Overlays.Add(markerOverlay);
            gmap.Overlays.Add(polyOverlay);
        }

        /// <summary>
        /// Asynchronously updates the live coordinate and elevation label as the map moves.
        /// </summary>
        private async Task UpdateLiveStats(PointLatLng p)
        {
            string elev = await GetElevationAsync(p.Lat, p.Lng);
            lblCoordValue.Text = $"Lat: {p.Lat:F6}°\nLng: {p.Lng:F6}°\nAlt: {elev}";
        }

        /// <summary>
        /// Fetches elevation data from the Open-Elevation API using an HTTP GET request.
        /// </summary>
        private async Task<string> GetElevationAsync(double lat, double lng)
        {
            try
            {
                lblStatusBar.Text = "⏳ Fetching elevation...";
                // Formulating API URL with InvariantCulture to ensure dot decimal separator
                string url = $"https://api.open-elevation.com/api/v1/lookup?locations={lat.ToString(System.Globalization.CultureInfo.InvariantCulture)},{lng.ToString(System.Globalization.CultureInfo.InvariantCulture)}";
                
                string response = await httpClient.GetStringAsync(url);
                
                // Parsing JSON manually via Regex to avoid external dependencies like Newtonsoft
                var match = Regex.Match(response, @"""elevation"":\s*([\d.-]+)");
                
                lblStatusBar.Text = "✔ Elevation received";
                return match.Success ? $"{match.Groups[1].Value} m" : "N/A";
            }
            catch 
            { 
                lblStatusBar.Text = "❌ API Error"; 
                return "N/A"; 
            }
        }

        /// <summary>
        /// Handles mouse click events on the map for placing markers or measuring distance.
        /// </summary>
        private async Task HandleMapClick(MouseEventArgs e)
        {
            var p = gmap.FromLocalToLatLng(e.X, e.Y);
            
            // Shift + Left Click triggers point addition for distance measurement
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
            {
                string elev = await GetElevationAsync(p.Lat, p.Lng);
                measurementPoints.Add(p);
                DrawDistance(p, elev); 
            }
            // Right Click triggers single location pinpointing
            else if (e.Button == MouseButtons.Right)
            {
                string elev = await GetElevationAsync(p.Lat, p.Lng);
                txtLat.Text = p.Lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                txtLng.Text = p.Lng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                AddMarker(p, $"Lat: {p.Lat:F4}\nLng: {p.Lng:F4}\nAlt: {elev}");
            }
        }

        /// <summary>
        /// Renders the measurement points and the route line on the map.
        /// </summary>
        private void DrawDistance(PointLatLng lastPoint, string lastElev)
        {
            polyOverlay.Clear();
            
            // Re-rendering all points with tooltips showing Lat, Lng, and Alt
            foreach (var point in measurementPoints)
            {
                var circle = new GMarkerGoogle(point, GMarkerGoogleType.blue_small);
                circle.ToolTipText = $"Lat: {point.Lat:F4}\nLng: {point.Lng:F4}\nAlt: {lastElev}";
                circle.ToolTipMode = MarkerTooltipMode.Always;
                circle.ToolTip = new CustomToolTip(circle);
                polyOverlay.Markers.Add(circle);
            }

            // Drawing the polyline if at least two points exist
            if (measurementPoints.Count > 1)
            {
                var route = new GMapRoute(measurementPoints, "Dist");
                route.Stroke = new Pen(Color.FromArgb(255, 180, 50), 3); // High-visibility orange line
                polyOverlay.Routes.Add(route);
                
                // Calculating Great-Circle distance between all consecutive points
                double totalKm = 0;
                for (int i = 0; i < measurementPoints.Count - 1; i++)
                    totalKm += GMapProviders.EmptyProvider.Projection.GetDistance(measurementPoints[i], measurementPoints[i + 1]);
                
                lblDistanceResult.Text = $"Total Distance: {totalKm:F2} km";
            }
        }

        /// <summary>
        /// Navigates the map to the coordinates entered manually in the text boxes.
        /// </summary>
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

        /// <summary>
        /// Navigates the map to a preset city from the quick selection list.
        /// </summary>
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

        /// <summary>
        /// Adds a single red marker to the map and clears previous search markers.
        /// </summary>
        private void AddMarker(PointLatLng loc, string title)
        {
            markerOverlay.Markers.Clear();
            var m = new GMarkerGoogle(loc, GMarkerGoogleType.red);
            m.ToolTipText = title; 
            m.ToolTipMode = MarkerTooltipMode.Always;
            m.ToolTip = new CustomToolTip(m); 
            markerOverlay.Markers.Add(m);
        }

        /// <summary>
        /// Resets the distance measurement tool.
        /// </summary>
        private void ClearDistance() 
        { 
            measurementPoints.Clear(); 
            polyOverlay.Clear(); 
            lblDistanceResult.Text = "Total Distance: 0.00 km"; 
        }

        // --- UI Factory Helper Methods ---

        private Button CreateStandardButton(string t, int y, Color c) 
        { 
            var b = new Button { Text = t, Location = new Point(15, y), Size = new Size(240, 38), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = c, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand }; 
            panelLeft.Controls.Add(b); return b; 
        }
        
        private Label CreateLabel(string t, float s, FontStyle st, Color c, int y, int w) 
        { 
            var l = new Label { Text = t, ForeColor = c, Font = new Font("Segoe UI", s, st), Location = new Point(15, y), Size = new Size(w, 20), AutoSize = false }; 
            panelLeft.Controls.Add(l); return l; 
        }
        
        private TextBox CreateTextInput(int y, string d) 
        { 
            var t = new TextBox { Location = new Point(15, y), Size = new Size(240, 26), Font = new Font("Courier New", 10), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGreen, BorderStyle = BorderStyle.FixedSingle, Text = d }; 
            panelLeft.Controls.Add(t); return t; 
        }
        
        private void DrawSeparator(int y) => panelLeft.Controls.Add(new Label { Location = new Point(15, y), Size = new Size(240, 1), BackColor = Color.FromArgb(55, 55, 75) });
    }

    /// <summary>
    /// Custom rendering class for GMap tooltips to provide a clean, modern UI.
    /// Overrides default drawing behavior to center-align text and customize colors.
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
            // Measuring text size for dynamic box resizing
            var sz = g.MeasureString(Marker.ToolTipText, Font);
            int p = 8; // Padding
            int w = (int)sz.Width + (p * 2); 
            int h = (int)sz.Height + (p * 2);
            
            // Calculating the position to center the tooltip above the marker
            var rect = new Rectangle(Marker.LocalPosition.X - (w / 2), Marker.LocalPosition.Y - Marker.Size.Height - h - 2, w, h);
            
            g.FillRectangle(Fill, rect); 
            g.DrawRectangle(Stroke, rect);
            
            // Drawing the string with center alignment
            g.DrawString(Marker.ToolTipText, Font, Foreground, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }
}
