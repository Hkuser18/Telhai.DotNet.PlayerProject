using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Windows.Threading;
using Telhai.DotNet.PlayerProject.Models;
using Telhai.DotNet.PlayerProject.Services;

namespace Telhai.DotNet.PlayerProject
{
    /// <summary>
    /// Interaction logic for MusicPlayer.xaml
    /// </summary>
    public partial class MusicPlayer : Window
    {
        private const string PlaceholderImageUri = "pack://application:,,,/Assets/placeholder.jpg";
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";
        private readonly ItunesService _itunesService = new ItunesService();
        private CancellationTokenSource? _cts;
        private MusicTrack? currentTrack;

        public MusicPlayer()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += new EventHandler(Timer_Tick);

            this.Loaded += MusicPlayer_Loaded;
            //this.MouseDoubleClick += MusicPlayer_MouseDoubleClick;
            //this.MouseDoubleClick += new MouseButtonEventHandler(MusicPlayer_MouseDoubleClick);
        }

        private void MusicPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadLibrary();
            SetPlaceholderImage();
        }

        //override

        private void MusicPlayer_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            MessageBox.Show("Music Player Window Double Clicked!");
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            // Update slider ONLY if music is loaded AND user is NOT holding the handle
            if (mediaPlayer.Source != null && mediaPlayer.NaturalDuration.HasTimeSpan && !isDragging)
            {
                sliderProgress.Maximum = mediaPlayer.NaturalDuration.TimeSpan.TotalSeconds;
                sliderProgress.Value = mediaPlayer.Position.TotalSeconds;
            }
        }

        //handler for play button
        private void BtnPlay_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn)
                btn.Background = Brushes.LightGreen;

            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                if (currentTrack == null || currentTrack.FilePath != track.FilePath)
                {
                    PlaySong(track);
                    return;
                }
            }

            mediaPlayer.Play();
            timer.Start();
            txtStatus.Text = "Playing";
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            txtStatus.Text = "Paused";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            timer.Stop();
            sliderProgress.Value = 0;
            txtStatus.Text = "Stopped";
            btn_play.Background = Brushes.AliceBlue;
        }
        private void SliderVolume_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            mediaPlayer.Volume = sliderVolume.Value;
        }
        private void Slider_DragStarted(object sender, MouseButtonEventArgs e)
        {
            isDragging = true; // Stop timer updates
        }

        private void Slider_DragCompleted(object sender, MouseButtonEventArgs e)
        {
            isDragging = false; // Resume timer updates
            mediaPlayer.Position = TimeSpan.FromSeconds(sliderProgress.Value);
        }

        //File Dialog to add music files from system
        private void BtnAdd_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Multiselect = true;
            ofd.Filter = "MP3 Files|*.mp3";

            //User confirmed
            if (ofd.ShowDialog() == true)
            {
                //iterate over all files selected as string
                foreach (string file in ofd.FileNames)
                {
                    //Create new MusicTrack object for each file
                    MusicTrack track = new MusicTrack
                    {
                        //Only file name
                        Title = System.IO.Path.GetFileNameWithoutExtension(file),
                        //full path
                        FilePath = file
                    };
                    //Add to library
                    library.Add(track);
                }
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void BtnRemove_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                library.Remove(track);
                UpdateLibraryUI();
                SaveLibrary();
            }
        }

        private void LstLibrary_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                PlaySong(track);
            }
        }

        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                UpdateSelectionInfo(track);
            }
        }

        // Helper method to refresh the box
        private void UpdateLibraryUI()
        {
            //take all librart list as Source to ListBox, display only Tostring()
            lstLibrary.ItemsSource = null;
            lstLibrary.ItemsSource = library;
        }


        private void SaveLibrary()
        {
            var options = new JsonSerializerOptions { WriteIndented = true };
            string json = JsonSerializer.Serialize(library);
            File.WriteAllText(FILE_NAME, json);
        }

        private void LoadLibrary()
        {
            if (File.Exists(FILE_NAME))
            {
                //read File
                string json = File.ReadAllText(FILE_NAME);
                //Create list from json or empty list if null
                 library = JsonSerializer.Deserialize<List<MusicTrack>>(json) ?? new List<MusicTrack>();
                //update UI
                UpdateLibraryUI();
            }
        }

        private void BtnSettings_Click(object sender, RoutedEventArgs e)
        {
            Settings settingsWin = new Settings();

            // Listen for the results
            settingsWin.OnScanCompleted += SettingsWin_OnScanCompleted;

            settingsWin.ShowDialog();

        }

        private void SettingsWin_OnScanCompleted(List<MusicTrack> newTracks)
        {
            foreach (var track in newTracks)
            {
                // Prevent duplicates based on FilePath
                if (!library.Any(x => x.FilePath == track.FilePath))
                {
                    library.Add(track);
                }
            }

            UpdateLibraryUI();
            SaveLibrary();
        }

        private void PlaySong(MusicTrack track)
        {
            if (!File.Exists(track.FilePath))
                return;

            currentTrack = track;

            // cancel previous request
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            string songName = GetSearchTerm(track.FilePath);

            // play song locally
            PlayLocalFile(track.FilePath);

            // clear ui
            ClearSongInfo();
            UpdateSelectionInfo(track);
            txtCurrentSong.Text = track.Title;
            txtStatus.Text = "Playing";

            // async API call
            _ = LoadSongInfoAsync(songName, track.FilePath, _cts.Token);
        }

        private void PlayLocalFile(string filePath)
        {
            mediaPlayer.Open(new Uri(filePath));
            mediaPlayer.Play();
            timer.Start();
        }

        private static string GetSearchTerm(string filePath)
        {
            string fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
            return fileName.Replace('-', ' ').Trim();
        }

        private async Task LoadSongInfoAsync(
            string songName,
            string filePath,
            CancellationToken token)
        {
            try
            {
                ItunesTrackInfo? info =
                    await _itunesService.SearchOneAsync(songName, token);

                if (info == null)
                {
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "No information found.";
                    });
                    return;
                }

                // return to UI thread 
                Dispatcher.Invoke(() =>
                {
                    TrackNameText.Text = info.TrackName ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
                    ArtistNameText.Text = info.ArtistName ?? string.Empty;
                    AlbumNameText.Text = info.AlbumName ?? string.Empty;
                    FilePathText.Text = filePath;
                    StatusText.Text = "Info loaded.";

                    if (!string.IsNullOrWhiteSpace(info.ArtworkUrl))
                    {
                        AlbumImage.Source =
                            new BitmapImage(new Uri(info.ArtworkUrl));
                    }
                    else
                    {
                        SetPlaceholderImage();
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // switch song, ignore
            }
            catch (Exception)
            {
                Dispatcher.Invoke(() =>
                {
                    TrackNameText.Text = System.IO.Path.GetFileNameWithoutExtension(filePath);
                    ArtistNameText.Text = string.Empty;
                    AlbumNameText.Text = string.Empty;
                    FilePathText.Text = filePath;
                    StatusText.Text = "Error loading song info.";
                    SetPlaceholderImage();
                });
            }
        }

        private void DisplayLocalInfo(MusicTrack track)
        {
            TrackNameText.Text = track.Title;
            FilePathText.Text = track.FilePath;
        }

        private void ClearSongInfo()
        {
            TrackNameText.Text = "";
            ArtistNameText.Text = "";
            AlbumNameText.Text = "";
            FilePathText.Text = "";
            SetPlaceholderImage();
        }

        private void UpdateSelectionInfo(MusicTrack track)
        {
            SelectedFileText.Text = track.Title;
            SelectedPathText.Text = track.FilePath;
        }
        
        private void SetPlaceholderImage()
        {
            AlbumImage.Source = new BitmapImage(new Uri(PlaceholderImageUri));
        }
    }
}
