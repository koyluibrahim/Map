public partial class Form1 : Form
{
    public Form1()
    {
        InitializeComponent();
        InitializeMap();

        goButton.Click += GoButton_Click;
    }

    private void InitializeMap()
    {
        mapControl.MapProvider = OpenStreetMapProvider.Instance;
        GMaps.Instance.Mode = AccessMode.ServerOnly;

        mapControl.MinZoom = 2;
        mapControl.MaxZoom = 18;
        mapControl.Zoom = 6;

        // Initial position: center of Turkey
        mapControl.Position = new PointLatLng(39.0, 35.0);
    }

    private void GoButton_Click(object sender, EventArgs e)
    {
        bool latitudeValid = double.TryParse(
            latitudeTextBox.Text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double latitude);

        bool longitudeValid = double.TryParse(
            longitudeTextBox.Text,
            NumberStyles.Float,
            CultureInfo.InvariantCulture,
            out double longitude);

        if (!latitudeValid || !longitudeValid)
        {
            MessageBox.Show("Please enter valid numeric coordinates using a dot. Example: 41.01");
            return;
        }

        if (latitude < -90 || latitude > 90 || longitude < -180 || longitude > 180)
        {
            MessageBox.Show("Coordinates are out of range.");
            return;
        }

        PointLatLng targetLocation = new PointLatLng(latitude, longitude);

        mapControl.Position = targetLocation;

        if (mapControl.Zoom < 12)
        {
            mapControl.Zoom = 12;
        }
    }
}