using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using Microsoft.Data.Sqlite;

namespace OfflineMapApp
{
    public partial class Form1 : Form
    {
        // .mbtiles dosyanı projenin bin\Debug klasörüne atmayı unutma!
        private string dbPath = "Data Source=harita.mbtiles";

        public Form1()
        {
            InitializeComponent();
        }

        private void btnGit_Click(object sender, EventArgs e)
        {
            // Kullanıcı girdilerini kontrol et
            if (!double.TryParse(txtLat.Text.Replace(".", ","), out double lat) || 
                !double.TryParse(txtLon.Text.Replace(".", ","), out double lon))
            {
                MessageBox.Show("Lütfen geçerli sayılar girin.");
                return;
            }

            int zoom = (int)numZoom.Value;

            // Enlem ve Boylamı X ve Y karo (tile) numaralarına çevirme (Slippy Map formülü)
            int n = 1 << zoom;
            int tileX = (int)Math.Floor((lon + 180.0) / 360.0 * n);
            double latRad = lat * Math.PI / 180.0;
            int tileY = (int)Math.Floor((1.0 - Math.Log(Math.Tan(latRad) + 1.0 / Math.Cos(latRad)) / Math.PI) / 2.0 * n);

            // MBTiles için Y eksenini ters çevirme (TMS Standardı)
            int tmsY = n - 1 - tileY;

            // Veritabanından resmi çekme
            using (var connection = new SqliteConnection(dbPath))
            {
                try
                {
                    connection.Open();
                    string sql = "SELECT tile_data FROM tiles WHERE zoom_level = @zoom AND tile_column = @x AND tile_row = @y";
                    
                    using (var command = new SqliteCommand(sql, connection))
                    {
                        command.Parameters.AddWithValue("@zoom", zoom);
                        command.Parameters.AddWithValue("@x", tileX);
                        command.Parameters.AddWithValue("@y", tmsY);

                        var result = command.ExecuteScalar();

                        if (result != null && result != DBNull.Value)
                        {
                            byte[] imageBytes = (byte[])result;
                            using (var ms = new MemoryStream(imageBytes))
                            {
                                pictureBox1.Image = Image.FromStream(ms);
                            }
                        }
                        else
                        {
                            pictureBox1.Image = null;
                            MessageBox.Show("Bu koordinat ve zoom için harita dosyası bulunamadı.");
                        }
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Hata oluştu: " + ex.Message);
                }
            }
        }
    }
}
