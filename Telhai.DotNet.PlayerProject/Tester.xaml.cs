using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Telhai.DotNet.HadarKeller.PlayerProject.Models;
using Telhai.DotNet.HadarKeller.PlayerProject.Services;

namespace Telhai.DotNet.HadarKeller.PlayerProject
{
    /// <summary>
    /// Interaction logic for Tester.xaml
    /// </summary>
    public partial class Tester : Window
    {
        private readonly ItunesService _itunesService = new ItunesService();
        private CancellationTokenSource? _cts;
        public Tester()
        {
            InitializeComponent();
            this.MouseDoubleClick += Tester_MouseDoubleClick;
        }

        private void Tester_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            PlaySong(@"C:\Users\Hadar\Music\Audio\SultansOfSwing.mp3");
        }

        private void PlaySong(string filePath)
        {
            if (!File.Exists(filePath))
                return;

            // cancel previous request
            _cts?.Cancel();
            _cts = new CancellationTokenSource();

            string songName = System.IO.Path.GetFileNameWithoutExtension(filePath);

            // play song locally
            PlayLocalFile(filePath);

            // clear ui
            ClearSongInfo();
            StatusText.Text = "Searching song info...";

            // async API call
            _ = LoadSongInfoAsync(songName, _cts.Token);
        }

        private void PlayLocalFile(string filePath)
        {
            MediaPlayer player = new MediaPlayer();
            player.Open(new Uri(filePath));
            player.Play();
        }

        private async Task LoadSongInfoAsync(
            string songName,
            CancellationToken token)
        {
            try
            {
                ItunesTrackInfo? info =
                    await _itunesService.SearchOneAsync(songName, token);

                if (info == null)
                {
                    StatusText.Text = "No information found.";
                    return;
                }

                // return to UI thread 
                Dispatcher.Invoke(() =>
                {
                    TrackNameText.Text = info.TrackName;
                    ArtistNameText.Text = info.ArtistName;
                    AlbumNameText.Text = info.AlbumName;
                    StatusText.Text = "Info loaded.";

                    if (!string.IsNullOrWhiteSpace(info.ArtworkUrl))
                    {
                        AlbumImage.Source =
                            new BitmapImage(new Uri(info.ArtworkUrl));
                    }
                });
            }
            catch (OperationCanceledException)
            {
                // switch song, ignore
            }
            catch (Exception ex)
            {
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = "Error loading song info.";
                });
            }
        }

        private void ClearSongInfo()
        {
            TrackNameText.Text = "";
            ArtistNameText.Text = "";
            AlbumNameText.Text = "";
            AlbumImage.Source = null;
        }
    }
}
