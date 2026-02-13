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
using Telhai.DotNet.HadarKeller.PlayerProject.Models;
using Telhai.DotNet.HadarKeller.PlayerProject.Services;
using Telhai.DotNet.HadarKeller.PlayerProject.ViewModels;

namespace Telhai.DotNet.HadarKeller.PlayerProject
{
    /// <summary>
    /// Interaction logic for MusicPlayer.xaml
    /// </summary>
    public partial class MusicPlayer : Window
    {
        private const string PlaceholderImageUri = "pack://application:,,,/Assets/placeholder.jpg";
        private MediaPlayer mediaPlayer = new MediaPlayer();
        private DispatcherTimer timer = new DispatcherTimer();
        private DispatcherTimer imageRotationTimer = new DispatcherTimer();
        private List<MusicTrack> library = new List<MusicTrack>();
        private bool isDragging = false;
        private const string FILE_NAME = "library.json";
        private readonly ItunesService _itunesService = new ItunesService();
        private readonly SongMetadataStore _metadataStore = new SongMetadataStore();
        private CancellationTokenSource? _cts;
        private MusicTrack? currentTrack;
        private List<string> currentImageList = new List<string>();
        private int currentImageIndex = 0;

        public MusicPlayer()
        {
            InitializeComponent();
            timer.Interval = TimeSpan.FromMilliseconds(500);
            timer.Tick += new EventHandler(Timer_Tick);

            // Image rotation timer - change image every 3 seconds
            imageRotationTimer.Interval = TimeSpan.FromSeconds(3);
            imageRotationTimer.Tick += ImageRotationTimer_Tick;

            this.Loaded += MusicPlayer_Loaded;
        }

        private void MusicPlayer_Loaded(object sender, RoutedEventArgs e)
        {
            this.LoadLibrary();
            SetPlaceholderImage();
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

        /// <summary>
        /// Rotates through custom images during playback
        /// </summary>
        private void ImageRotationTimer_Tick(object? sender, EventArgs e)
        {
            if (currentImageList.Count == 0)
                return;

            currentImageIndex = (currentImageIndex + 1) % currentImageList.Count;
            DisplayImageFromList(currentImageIndex);
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
            
            // Start image rotation if custom images exist
            if (currentImageList.Count > 0)
                imageRotationTimer.Start();
            
            txtStatus.Text = "Playing";
        }

        private void BtnPause_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Pause();
            imageRotationTimer.Stop();
            txtStatus.Text = "Paused";
        }

        private void BtnStop_Click(object sender, RoutedEventArgs e)
        {
            mediaPlayer.Stop();
            timer.Stop();
            imageRotationTimer.Stop();
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

        /// <summary>
        /// Displays song metadata when a track is selected (single click)
        /// Loads from cache if available, without calling API
        /// </summary>
        private void LstLibrary_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                UpdateSelectionInfo(track);
                
                // Display cached metadata if available (no API call)
                SongMetadata? metadata = _metadataStore.GetByFilePath(track.FilePath);
                if (metadata != null)
                {
                    DisplayMetadata(metadata, track.FilePath, fromCache: true);
                }
            }
        }

        /// <summary>
        /// Opens the Edit Song window for the selected track
        /// </summary>
        private void BtnEdit_Click(object sender, RoutedEventArgs e)
        {
            if (lstLibrary.SelectedItem is MusicTrack track)
            {
                SongMetadata? metadata = _metadataStore.GetByFilePath(track.FilePath);
                
                SongEditViewModel viewModel = new SongEditViewModel(track, metadata, _metadataStore);
                EditSongWindow editWindow = new EditSongWindow(viewModel)
                {
                    Owner = this
                };
                
                editWindow.ShowDialog();
                
                // Refresh display if this is the currently playing track
                if (currentTrack?.FilePath == track.FilePath)
                {
                    SongMetadata? updatedMetadata = _metadataStore.GetByFilePath(track.FilePath);
                    if (updatedMetadata != null)
                    {
                        DisplayMetadata(updatedMetadata, track.FilePath, fromCache: true);
                        
                        // Update the current song title display
                        txtCurrentSong.Text = updatedMetadata.CustomTitle ?? updatedMetadata.TrackName ?? track.Title;
                    }
                }
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

        /// <summary>
        /// Plays a song and loads metadata from cache or API
        /// </summary>
        private void PlaySong(MusicTrack track)
        {
            if (!File.Exists(track.FilePath))
                return;

            currentTrack = track;

            // cancel previous request
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            // play song locally
            PlayLocalFile(track.FilePath);

            // clear ui
            ClearSongInfo();
            UpdateSelectionInfo(track);
            
            // Check if metadata already exists in cache
            SongMetadata? metadata = _metadataStore.GetByFilePath(track.FilePath);
            
            if (metadata != null)
            {
                // Display cached metadata (no API call needed)
                DisplayMetadata(metadata, track.FilePath, fromCache: true);
                txtCurrentSong.Text = metadata.CustomTitle ?? metadata.TrackName ?? track.Title;
                txtStatus.Text = "Playing (cached)";
            }
            else
            {
                // No cached metadata, fetch from API
                string songName = GetSearchTerm(track.FilePath);
                txtCurrentSong.Text = track.Title;
                txtStatus.Text = "Playing";
                
                // async API call and save to cache
                _ = LoadSongInfoAsync(songName, track.FilePath, _cts.Token);
            }
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

        /// <summary>
        /// Fetches song info from iTunes API and saves to metadata store
        /// </summary>
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

                // Save metadata to store
                SongMetadata metadata = new SongMetadata
                {
                    FilePath = filePath,
                    TrackName = info.TrackName,
                    ArtistName = info.ArtistName,
                    AlbumName = info.AlbumName,
                    ArtworkUrl = info.ArtworkUrl
                };
                _metadataStore.Upsert(metadata);

                // return to UI thread 
                Dispatcher.Invoke(() =>
                {
                    DisplayMetadata(metadata, filePath, fromCache: false);
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

        /// <summary>
        /// Displays metadata in the UI, handling custom images and rotation
        /// </summary>
        private void DisplayMetadata(SongMetadata metadata, string filePath, bool fromCache)
        {
            string displayTitle = metadata.CustomTitle ?? metadata.TrackName ?? System.IO.Path.GetFileNameWithoutExtension(filePath);
            
            TrackNameText.Text = displayTitle;
            ArtistNameText.Text = metadata.ArtistName ?? string.Empty;
            AlbumNameText.Text = metadata.AlbumName ?? string.Empty;
            FilePathText.Text = filePath;
            StatusText.Text = fromCache ? "Info loaded from cache." : "Info loaded from API.";

            // Prepare image list for rotation
            currentImageList.Clear();
            currentImageIndex = 0;
            
            // Priority: Custom images > API artwork > Placeholder
            if (metadata.ImagePaths != null && metadata.ImagePaths.Count > 0)
            {
                // Add valid custom images
                foreach (var imagePath in metadata.ImagePaths)
                {
                    if (File.Exists(imagePath))
                    {
                        currentImageList.Add(imagePath);
                    }
                }
            }
            
            // If no custom images, fall back to API artwork
            if (currentImageList.Count == 0 && !string.IsNullOrWhiteSpace(metadata.ArtworkUrl))
            {
                currentImageList.Add(metadata.ArtworkUrl);
            }
            
            // Display first image or placeholder
            if (currentImageList.Count > 0)
            {
                DisplayImageFromList(0);
                
                // Start rotation if playing and multiple images
                if (currentImageList.Count > 1 && mediaPlayer.Source != null)
                {
                    imageRotationTimer.Start();
                }
                else
                {
                    imageRotationTimer.Stop();
                }
            }
            else
            {
                SetPlaceholderImage();
                imageRotationTimer.Stop();
            }
        }

        /// <summary>
        /// Displays an image from the current image list by index
        /// </summary>
        private void DisplayImageFromList(int index)
        {
            if (index < 0 || index >= currentImageList.Count)
                return;

            string imagePath = currentImageList[index];
            
            try
            {
                // Check if it's a local file or URL
                if (File.Exists(imagePath))
                {
                    AlbumImage.Source = new BitmapImage(new Uri(imagePath, UriKind.Absolute));
                }
                else if (Uri.TryCreate(imagePath, UriKind.Absolute, out Uri? uri))
                {
                    AlbumImage.Source = new BitmapImage(uri);
                }
                else
                {
                    SetPlaceholderImage();
                }
            }
            catch
            {
                SetPlaceholderImage();
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
            currentImageList.Clear();
            imageRotationTimer.Stop();
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
