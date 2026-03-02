using System;
using System.Globalization;
using System.Windows.Forms;
using GMap.NET;
using GMap.NET.MapProviders;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace TurkiyeHarita
{
    public partial class Form1 : Form
    {
        public Form1()
        {
            InitializeComponent();

            InitializeMap();

            // Connect button click event
            goButton.Click += GoButton_Click;
            this.AcceptButton = goButton;  // To go to coordinates with enter click
        }

        private void InitializeMap()
        {
            // Choosing map provider
            mapControl.MapProvider = GMap.NET.MapProviders.WikiMapiaMapProvider.Instance;
            GMap.NET.GMaps.Instance.Mode = GMap.NET.AccessMode.ServerAndCache;

            // Maximum and minimum zoom settings
            mapControl.MinZoom = 4;
            mapControl.MaxZoom = 14;
            mapControl.Zoom = 9;

            // Initial position
            mapControl.Position = new PointLatLng(39.0, 35.0);
        }

        private void GoButton_Click(object sender, EventArgs e)
        {
            // Take valid latitude value from input
            bool latitudeValid = double.TryParse(
                latitudeTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double latitude);

            // Take valid longitude value from input
            bool longitudeValid = double.TryParse(
                longitudeTextBox.Text,
                NumberStyles.Float,
                CultureInfo.InvariantCulture,
                out double longitude);

            // Gives warning if input coordinates is invalid
            if (!latitudeValid || !longitudeValid)
            {
                MessageBox.Show("Please enter valid coordinates using a dot. Example: 41.0151");
                return;
            }

            // Gives warning if it is outside of the Earth's borders
            if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
            {
                MessageBox.Show("Coordinates are out of range.");
                return;
            }

            // Gives warning if it is outside of the Turkiye's borders
            if (latitude < 36 || latitude > 42.5 || longitude < 26 || longitude > 45)
            {
                MessageBox.Show("Coordinates are outside Turkey.");
                return;
            }

            // New position according to the input coordinates
            mapControl.Position = new PointLatLng(latitude, longitude);

            if (mapControl.Zoom < 12)
            {
                mapControl.Zoom = 9;
            }
        }

    }

}
