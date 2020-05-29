using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace SoftEngineNC
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private Device device;
        Mesh[] meshes;
        Camera mera = new Camera();
        private DateTime previousDate;
        private List<double> lastFPSValues = new List<double>();

        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Window_Loaded(object sender, RoutedEventArgs e)
        {
            WriteableBitmap bmp = new WriteableBitmap(1600, 900, 96, 96, PixelFormats.Bgra32, null);

            device = new Device(bmp);

            // Our XAML Image control
            frontBuffer.Source = bmp;

            meshes = await ModelLoader.LoadBabylonModel(Environment.CurrentDirectory + @"/models/icosphere.babylon");

            mera.Position = new Vector3(0, 0, 10.0f);
            mera.Target = Vector3.Zero;

            CompositionTarget.Rendering += CompositionTarget_Rendering;
        }

        private void CompositionTarget_Rendering(object sender, EventArgs e)
        {
            // Compute current and average fps
            var now = DateTime.Now;
            var currentFps = 1000.0 / (now - previousDate).TotalMilliseconds;
            previousDate = now;
            Title = $"Triangles: {meshes[0].Faces.Length}, Current: {currentFps:0.00} fps";

            if (lastFPSValues.Count < 60)
            {
                lastFPSValues.Add(currentFps);
            }
            else
            {
                lastFPSValues.RemoveAt(0);
                lastFPSValues.Add(currentFps);
                var totalValues = 0d;
                for (var i = 0; i < lastFPSValues.Count; i++)
                {
                    totalValues += lastFPSValues[i];
                }

                var averageFPS = totalValues / lastFPSValues.Count;
                Title += string.Format(", Average {0:0.00} fps", averageFPS);
            }

            device.Clear(0, 0, 0, 255);

            foreach (var mesh in meshes)
            {
                mesh.Rotation = new Vector3(mesh.Rotation.X + 0.01f, mesh.Rotation.Y + 0.01f, mesh.Rotation.Z);
            }

            //Doing the various matrix operations
            device.Render(mera, meshes);

            // Flushing the back buffer into the front buffer
            device.Present();
        }
    }
}
