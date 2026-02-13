using Microsoft.Win32;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Telhai.DotNet.HadarKeller.PlayerProject.Models;
using Telhai.DotNet.HadarKeller.PlayerProject.Services;

namespace Telhai.DotNet.HadarKeller.PlayerProject.ViewModels
{
    public class SongEditViewModel : INotifyPropertyChanged
    {
        private const string PlaceholderImageUri = "pack://application:,,,/Assets/placeholder.jpg";
        private readonly SongMetadataStore _store;
        private readonly SongMetadata _metadata;
        private string _title;
        private string? _selectedImagePath;
        private ImageSource? _displayImage;

        // Initialize the view model with the track, existing metadata (if any), and the metadata store for saving changes
        public SongEditViewModel(MusicTrack track, SongMetadata? metadata, SongMetadataStore store)
        {
            _store = store;
            _metadata = metadata ?? new SongMetadata { FilePath = track.FilePath };

            FilePath = track.FilePath;
            ArtistName = _metadata.ArtistName;
            AlbumName = _metadata.AlbumName;
            ArtworkUrl = _metadata.ArtworkUrl;

            _title = _metadata.CustomTitle ?? _metadata.TrackName ?? track.Title;

            ImagePaths = new ObservableCollection<string>(_metadata.ImagePaths ?? new());
            if (ImagePaths.Count > 0)
            {
                SelectedImagePath = ImagePaths[0];
            }

            AddImageCommand = new RelayCommand(AddImage);
            RemoveImageCommand = new RelayCommand(RemoveImage, () => !string.IsNullOrWhiteSpace(SelectedImagePath));
            SaveCommand = new RelayCommand(Save);

            UpdateDisplayImage();
        }

        public event PropertyChangedEventHandler? PropertyChanged;
        public event Action? RequestClose;

        public string FilePath { get; }
        public string? ArtistName { get; }
        public string? AlbumName { get; }
        public string? ArtworkUrl { get; }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public ObservableCollection<string> ImagePaths { get; }

        // The currently selected image path, which can be from the list of image paths or null if none is selected
        public string? SelectedImagePath
        {
            get => _selectedImagePath;
            set
            {
                if (_selectedImagePath != value)
                {
                    _selectedImagePath = value;
                    OnPropertyChanged();
                    ((RelayCommand)RemoveImageCommand).RaiseCanExecuteChanged();
                    UpdateDisplayImage();
                }
            }
        }

        // The image to display in the edit window, which can be from the selected image path, artwork URL, or a placeholder
        public ImageSource? DisplayImage
        {
            get => _displayImage;
            private set
            {
                if (_displayImage != value)
                {
                    _displayImage = value;
                    OnPropertyChanged();
                }
            }
        }

        public RelayCommand AddImageCommand { get; }
        public RelayCommand RemoveImageCommand { get; }
        public RelayCommand SaveCommand { get; }

        // Open a file dialog to select an image and add it to the list of image paths
        private void AddImage()
        {
            OpenFileDialog dialog = new OpenFileDialog
            {
                Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp"
            };

            if (dialog.ShowDialog() == true)
            {
                ImagePaths.Add(dialog.FileName);
                SelectedImagePath = dialog.FileName;
            }
        }

        // Remove the selected image from the list and update the display
        private void RemoveImage()
        {
            if (SelectedImagePath == null)
                return;

            int index = ImagePaths.IndexOf(SelectedImagePath);
            if (index >= 0)
            {
                ImagePaths.RemoveAt(index);
                SelectedImagePath = ImagePaths.FirstOrDefault();
            }
        }

        // Save the metadata to the store and close the edit window
        private void Save()
        {
            _metadata.FilePath = FilePath;
            _metadata.CustomTitle = Title;
            _metadata.ImagePaths = ImagePaths.ToList();

            _store.Upsert(_metadata);
            RequestClose?.Invoke();
        }

        // Update the displayed image based on the selected image path, artwork URL, or placeholder
        private void UpdateDisplayImage()
        {
            if (!string.IsNullOrWhiteSpace(SelectedImagePath) && File.Exists(SelectedImagePath))
            {
                DisplayImage = new BitmapImage(new Uri(SelectedImagePath, UriKind.Absolute));
                return;
            }

            if (!string.IsNullOrWhiteSpace(ArtworkUrl))
            {
                DisplayImage = new BitmapImage(new Uri(ArtworkUrl, UriKind.Absolute));
                return;
            }

            DisplayImage = new BitmapImage(new Uri(PlaceholderImageUri, UriKind.Absolute));
        }

        // Helper method to raise PropertyChanged event
        private void OnPropertyChanged([CallerMemberName] string? name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}
