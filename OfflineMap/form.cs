using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

public class MapControl : Panel
{
    private SqliteConnection _conn;
    private double _lat = 39.0, _lon = 35.0;
    private int _zoom = 6;
    private Point? _dragStart;
    private int _offsetX, _offsetY;

    public MapControl()
    {
        DoubleBuffered = true;
        MouseWheel += (s, e) =>
        {
            // Math.Clamp yok, manuel yazıyoruz
            _zoom = Math.Max(1, Math.Min(18, _zoom + (e.Delta > 0 ? 1 : -1)));
            _offsetX = _offsetY = 0;
            Invalidate();
        };
        MouseDown += (s, e) => { if (e.Button == MouseButtons.Left) _dragStart = e.Location; };
        MouseMove += (s, e) =>
        {
            if (_dragStart == null) return;
            _offsetX += e.X - _dragStart.Value.X;
            _offsetY += e.Y - _dragStart.Value.Y;
            _dragStart = e.Location;
            Invalidate();
        };
        MouseUp += (s, e) => _dragStart = null;
    }

    public void LoadMBTiles(string path)
    {
        if (_conn != null) _conn.Close();
        _conn = new SqliteConnection("Data Source=" + path + ";Mode=ReadOnly");
        _conn.Open();
        Invalidate();
    }

    public void GoTo(double lat, double lon, int zoom)
    {
        _lat = lat; _lon = lon; _zoom = zoom;
        _offsetX = _offsetY = 0;
        Invalidate();
    }

    private static void LatLonToTile(double lat, double lon, int z, out int tx, out int ty)
    {
        int n = 1 << z;
        tx = (int)((lon + 180.0) / 360.0 * n);
        double rad = lat * Math.PI / 180.0;
        ty = (int)((1.0 - Math.Log(Math.Tan(rad) + 1.0 / Math.Cos(rad)) / Math.PI) / 2.0 * n);
    }

    private static int FlipY(int y, int z) => (1 << z) - 1 - y;

    private Bitmap GetTile(int z, int x, int y)
    {
        if (_conn == null) return null;
        using (var cmd = _conn.CreateCommand())
        {
            cmd.CommandText = "SELECT tile_data FROM tiles WHERE zoom_level=@z AND tile_column=@x AND tile_row=@y";
            cmd.Parameters.AddWithValue("@z", z);
            cmd.Parameters.AddWithValue("@x", x);
            cmd.Parameters.AddWithValue("@y", FlipY(y, z));
            using (var reader = cmd.ExecuteReader())
            {
                if (!reader.Read()) return null;
                return new Bitmap(new MemoryStream((byte[])reader[0]));
            }
        }
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        base.OnPaint(e);
        Graphics g = e.Graphics;
        g.Clear(Color.FromArgb(40, 40, 40));

        if (_conn == null)
        {
            g.DrawString("MBTiles dosyası yükleyin.", Font, Brushes.Gray, 10, 10);
            return;
        }

        const int S = 256;
        int cx, cy;
        LatLonToTile(_lat, _lon, _zoom, out cx, out cy);
        int baseX = Width / 2 - S / 2 + _offsetX;
        int baseY = Height / 2 - S / 2 + _offsetY;
        int r = Width / S / 2 + 2;

        for (int dx = -r; dx <= r; dx++)
        for (int dy = -r - 1; dy <= r + 1; dy++)
        {
            int tx = cx + dx, ty = cy + dy;
            int px = baseX + dx * S, py = baseY + dy * S;
            using (Bitmap bmp = GetTile(_zoom, tx, ty))
            {
                if (bmp != null) g.DrawImage(bmp, px, py, S, S);
                else g.FillRectangle(Brushes.DimGray, px, py, S - 1, S - 1);
            }
        }

        int mx = Width / 2, my = Height / 2;
        g.DrawLine(Pens.Red, mx - 12, my, mx + 12, my);
        g.DrawLine(Pens.Red, mx, my - 12, mx, my + 12);
    }
}

public partial class Form1 : Form
{
    private MapControl _map;

    public Form1()
    {
        InitializeComponent();
        _map = new MapControl { Dock = DockStyle.Fill };
        panel1.Controls.Add(_map);
    }

    private void btnOpen_Click(object sender, EventArgs e)
    {
        using (var dlg = new OpenFileDialog { Filter = "MBTiles|*.mbtiles" })
        {
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                _map.LoadMBTiles(dlg.FileName);
                lblStatus.Text = "Yüklendi: " + Path.GetFileName(dlg.FileName);
            }
        }
    }

    private void btnGo_Click(object sender, EventArgs e)
    {
        double lat, lon;
        int zoom;
        bool ok =
            double.TryParse(txtLat.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out lat) &&
            double.TryParse(txtLon.Text.Replace(',', '.'),
                System.Globalization.NumberStyles.Float,
                System.Globalization.CultureInfo.InvariantCulture, out lon) &&
            int.TryParse(txtZoom.Text, out zoom);

        if (!ok)
        {
            MessageBox.Show("Geçersiz değer.", "Hata", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        _map.GoTo(lat, lon, zoom);
        lblStatus.Text = string.Format("Konum: {0:F4}, {1:F4}  |  Zoom: {2}", lat, lon, zoom);
    }

    private void txtCoord_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.KeyCode == Keys.Enter) btnGo_Click(sender, e);
    }
}
