using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;

namespace TurkiyeMap
{
    public partial class Form1 : Form
    {
        private GMapControl gmap;
        private GMapOverlay markerOverlay;
        private GMapOverlay polyOverlay; // Layer for distance measurement lines

        private TextBox txtLat, txtLng;
        private Button btnGo, btnZoomIn, btnZoomOut, btnClearMeasurement;
        private Label lblCoordValue, lblStatusBar, lblDistanceResult;
        private Panel panelLeft;

        // List to store points for distance measurement
        private List<PointLatLng> measurementPoints = new List<PointLatLng>();

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
            this.Text = "Turkiye Map - Bing Hybrid";
            this.Size = new Size(1280, 850);
            this.MinimumSize = new Size(1000, 700);
            this.BackColor = Color.FromArgb(18, 18, 28);

            // Status Bar
            lblStatusBar = new Label
            {
                Dock = DockStyle.Bottom,
                Height = 26,
                BackColor = Color.FromArgb(10, 10, 18),
                ForeColor = Color.FromArgb(100, 200, 255),
                Font = new Font("Courier New", 9),
                TextAlign = ContentAlignment.MiddleLeft,
                Padding = new Padding(10, 0, 0, 0),
                Text = ""
            };

            // Left Panel
            panelLeft = new Panel
            {
                Dock = DockStyle.Left,
                Width = 270,
                BackColor = Color.FromArgb(22, 22, 35),
            };

            int y = 20;

            // --- COORDINATE INPUT ---
            CreateLabel("COORDINATE INPUT", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            CreateLabel("Latitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLat = CreateTextInput(y, "39.9334"); y += 32;
            CreateLabel("Longitude:", 9, FontStyle.Regular, Color.White, y, 240); y += 20;
            txtLng = CreateTextInput(y, "32.8597"); y += 36;

            btnGo = CreateStandardButton("▶   GO TO LOCATION", y, Color.FromArgb(0, 122, 204));
            btnGo.Click += BtnGo_Click;
            txtLat.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnGo_Click(null, null); };
            txtLng.KeyDown += (s, e) => { if (e.KeyCode == Keys.Enter) BtnGo_Click(null, null); };
            y += 48;

            DrawSeparator(y); y += 14;

            // --- ZOOM CONTROLS ---
            CreateLabel("MAP ZOOM", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            btnZoomIn = new Button { Text = "➕ ZOOM IN", Location = new Point(15, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomOut = new Button { Text = "➖ ZOOM OUT", Location = new Point(140, y), Size = new Size(115, 32), BackColor = Color.FromArgb(45, 45, 65), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnZoomIn.Click += (s, e) => gmap.Zoom++;
            btnZoomOut.Click += (s, e) => gmap.Zoom--;
            panelLeft.Controls.Add(btnZoomIn); panelLeft.Controls.Add(btnZoomOut);
            y += 44;

            DrawSeparator(y); y += 14;

            // --- QUICK CITY SELECTION ---
            CreateLabel("QUICK CITY SELECTION", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 24;
            for (int i = 0; i < cities.GetLength(0); i++)
            {
                string name = cities[i, 0]; string lat = cities[i, 1]; string lng = cities[i, 2];
                int col = (i % 2 == 0) ? 15 : 135; if (i % 2 == 0 && i > 0) y += 34;
                var btn = new Button { Text = name, Location = new Point(col, y), Size = new Size(112, 28), Font = new Font("Segoe UI", 8.5f), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.LightGray, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand, Tag = new string[] { lat, lng } };
                btn.FlatAppearance.BorderColor = Color.FromArgb(60, 60, 85); btn.Click += CityBtn_Click;
                panelLeft.Controls.Add(btn);
            }
            y += 46;

            DrawSeparator(y); y += 14;

            // --- DISTANCE MEASUREMENT ---
            CreateLabel("DISTANCE MEASUREMENT TOOL", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            var lblInstructions = CreateLabel("• SHIFT + Left Click: Add point\n• Distance is calculated automatically.", 8, FontStyle.Italic, Color.FromArgb(180, 180, 100), y, 240);
            lblInstructions.Height = 35; y += 40;

            lblDistanceResult = CreateLabel("Total Distance: 0.00 km", 9, FontStyle.Bold, Color.FromArgb(255, 180, 50), y, 240); y += 25;
            btnClearMeasurement = new Button { Text = "🗑 CLEAR MEASUREMENT", Location = new Point(15, y), Size = new Size(240, 30), Font = new Font("Segoe UI", 8, FontStyle.Bold), BackColor = Color.FromArgb(80, 40, 40), ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btnClearMeasurement.Click += (s, e) => ClearDistance();
            panelLeft.Controls.Add(btnClearMeasurement); y += 45;

            DrawSeparator(y); y += 14;

            // --- CURRENT LOCATION ---
            CreateLabel("CURRENT LOCATION (+ MARKER)", 8, FontStyle.Bold, Color.FromArgb(130, 130, 160), y, 240); y += 22;
            lblCoordValue = CreateLabel("", 9, FontStyle.Bold, Color.FromArgb(80, 220, 140), y, 240);
            lblCoordValue.Height = 55;

            // Map Initialization
            gmap = new GMapControl { Dock = DockStyle.Fill, MinZoom = 5, MaxZoom = 19, Zoom = 6, ShowCenter = true };
            gmap.MouseClick += Gmap_MouseClick;
            gmap.OnPositionChanged += (p) => lblCoordValue.Text = $"Lat: {p.Lat:F6}°\nLng: {p.Lng:F6}°";

            this.Controls.Add(gmap);
            this.Controls.Add(panelLeft);
            this.Controls.Add(lblStatusBar);
        }

        private void InitializeMap()
        {
            GMapProvider.UserAgent = "TurkeyMapApp/1.0";
            gmap.MapProvider = GMapProviders.BingHybridMap;
            GMaps.Instance.Mode = AccessMode.ServerAndCache;

            gmap.Position = new PointLatLng(39.0, 35.5);
            gmap.DragButton = MouseButtons.Left;

            markerOverlay = new GMapOverlay("markers");
            polyOverlay = new GMapOverlay("polygons");
            gmap.Overlays.Add(markerOverlay);
            gmap.Overlays.Add(polyOverlay);

            lblCoordValue.Text = $"Lat: {gmap.Position.Lat:F6}°\nLng: {gmap.Position.Lng:F6}°";
        }

        private void Gmap_MouseClick(object sender, MouseEventArgs e)
        {
            var p = gmap.FromLocalToLatLng(e.X, e.Y);

            // SHIFT + Left Click: Measure Distance
            if (e.Button == MouseButtons.Left && ModifierKeys == Keys.Shift)
            {
                measurementPoints.Add(p);
                DrawDistance();
            }
            // Right Click: Standard Pin
            else if (e.Button == MouseButtons.Right)
            {
                txtLat.Text = p.Lat.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                txtLng.Text = p.Lng.ToString("F6", System.Globalization.CultureInfo.InvariantCulture);
                AddMarker(p, $"Lat: {p.Lat:F4}°\nLng: {p.Lng:F4}°");
            }
        }

        private void DrawDistance()
        {
            polyOverlay.Clear();
            if (measurementPoints.Count < 1) return;

            foreach (var point in measurementPoints)
            {
                var circle = new GMarkerGoogle(point, GMarkerGoogleType.blue_small);
                circle.ToolTipText = $"Lat: {point.Lat:F4}°\nLng: {point.Lng:F4}°";
                circle.ToolTipMode = MarkerTooltipMode.Always;
                circle.ToolTip = new CustomToolTip(circle);
                polyOverlay.Markers.Add(circle);
            }

            if (measurementPoints.Count > 1)
            {
                var route = new GMapRoute(measurementPoints, "Distance");
                route.Stroke = new Pen(Color.FromArgb(255, 180, 50), 3);
                polyOverlay.Routes.Add(route);

                double totalKm = 0;
                for (int i = 0; i < measurementPoints.Count - 1; i++)
                {
                    totalKm += GMapProviders.EmptyProvider.Projection.GetDistance(measurementPoints[i], measurementPoints[i + 1]);
                }
                lblDistanceResult.Text = $"Total Distance: {totalKm:F2} km";
            }
        }

        private void ClearDistance()
        {
            measurementPoints.Clear();
            polyOverlay.Clear();
            lblDistanceResult.Text = "Total Distance: 0.00 km";
        }

        private void GoToLocation(double lat, double lng, int zoom, string title)
        {
            gmap.Position = new PointLatLng(lat, lng);
            gmap.Zoom = zoom;
            AddMarker(new PointLatLng(lat, lng), title);
        }

        private void AddMarker(PointLatLng location, string title)
        {
            markerOverlay.Markers.Clear();
            var marker = new GMarkerGoogle(location, GMarkerGoogleType.red);
            marker.ToolTipText = title;
            marker.ToolTipMode = MarkerTooltipMode.Always;
            marker.ToolTip = new CustomToolTip(marker);
            markerOverlay.Markers.Add(marker);
        }

        private void BtnGo_Click(object sender, EventArgs e)
        {
            if (TryParseCoordinate(txtLat.Text, out double lat) && TryParseCoordinate(txtLng.Text, out double lng))
            {
                if (lat < 35.5 || lat > 42.5 || lng < 25.5 || lng > 45.0)
                {
                    ShowError("Coordinates are outside the borders of Turkey!\nLat: 35.5 – 42.5\nLng: 25.5 – 45.0");
                    return;
                }
                GoToLocation(lat, lng, 12, $"Lat: {lat:F4}°\nLng: {lng:F4}°");
            }
            else
            {
                ShowError("Invalid coordinate!\nExample:  Lat: 41.0082   Lng: 28.9784");
            }
        }

        private void CityBtn_Click(object sender, EventArgs e)
        {
            var coords = (string[])((Button)sender).Tag;
            double lat = double.Parse(coords[0], System.Globalization.CultureInfo.InvariantCulture);
            double lng = double.Parse(coords[1], System.Globalization.CultureInfo.InvariantCulture);
            txtLat.Text = coords[0]; txtLng.Text = coords[1];
            GoToLocation(lat, lng, 11, $"Lat: {lat:F4}°\nLng: {lng:F4}°");
        }

        private Button CreateStandardButton(string text, int y, Color color)
        {
            var btn = new Button { Text = text, Location = new Point(15, y), Size = new Size(240, 38), Font = new Font("Segoe UI", 10, FontStyle.Bold), BackColor = color, ForeColor = Color.White, FlatStyle = FlatStyle.Flat, Cursor = Cursors.Hand };
            btn.FlatAppearance.BorderSize = 0; panelLeft.Controls.Add(btn); return btn;
        }

        private Label CreateLabel(string text, float size, FontStyle style, Color color, int y, int width)
        {
            var lbl = new Label { Text = text, ForeColor = color, Font = new Font("Segoe UI", size, style), Location = new Point(15, y), Size = new Size(width, 20), AutoSize = false };
            panelLeft.Controls.Add(lbl); return lbl;
        }

        private TextBox CreateTextInput(int y, string defaultValue)
        {
            var txt = new TextBox { Location = new Point(15, y), Size = new Size(240, 26), Font = new Font("Courier New", 10), BackColor = Color.FromArgb(38, 38, 58), ForeColor = Color.FromArgb(130, 220, 130), BorderStyle = BorderStyle.FixedSingle, Text = defaultValue };
            panelLeft.Controls.Add(txt); return txt;
        }

        private void DrawSeparator(int y) => panelLeft.Controls.Add(new Label { Location = new Point(15, y), Size = new Size(240, 1), BackColor = Color.FromArgb(55, 55, 75) });

        private static bool TryParseCoordinate(string text, out double value) => double.TryParse(text.Replace(',', '.').Trim(), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out value);

        private static void ShowError(string message) => MessageBox.Show(message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    public class CustomToolTip : GMapToolTip
    {
        public CustomToolTip(GMapMarker marker) : base(marker)
        {
            Stroke = new Pen(Color.FromArgb(130, 130, 130), 1);
            Fill = new SolidBrush(Color.White);
            Foreground = new SolidBrush(Color.FromArgb(40, 40, 40));
            Font = new Font("Segoe UI", 9, FontStyle.Regular);
        }

        public override void OnRender(Graphics g)
        {
            SizeF size = g.MeasureString(Marker.ToolTipText, Font);
            int p = 8;
            int width = (int)size.Width + (p * 2);
            int height = (int)size.Height + (p * 2);
            int x = Marker.LocalPosition.X - (width / 2);
            int y = Marker.LocalPosition.Y - Marker.Size.Height - height - 2;
            Rectangle rect = new Rectangle(x, y, width, height);

            g.FillRectangle(Fill, rect);
            g.DrawRectangle(Stroke, rect);
            g.DrawString(Marker.ToolTipText, Font, Foreground, rect, new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center });
        }
    }
}
