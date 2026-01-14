using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using Telhai.DotNet.PlayerProject.Models;


namespace Telhai.DotNet.PlayerProject
{
    public partial class MainWindow : Window
    {
        // Global Variables
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";

        public MainWindow()
        {
            InitializeComponent();
            // Initialize things here later
        }

        // --- EMPTY PLACEHOLDERS TO MAKE IT BUILD ---
        private void BtnPlay_Click(object sender, RoutedEventArgs e) { }
        private void BtnPause_Click(object sender, RoutedEventArgs e) { }
        private void BtnStop_Click(object sender, RoutedEventArgs e) { }
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e) { }

        private void Slider_DragStarted(object sender, MouseButtonEventArgs e) { }
        private void Slider_DragCompleted(object sender, MouseButtonEventArgs e) { }

        private void BtnAdd_Click(object sender, RoutedEventArgs e) { }
        private void BtnRemove_Click(object sender, RoutedEventArgs e) { }
        private void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e) { }
    }




}
