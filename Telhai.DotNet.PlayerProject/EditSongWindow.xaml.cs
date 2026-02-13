using System.Windows;
using Telhai.DotNet.HadarKeller.PlayerProject.ViewModels;

namespace Telhai.DotNet.HadarKeller.PlayerProject
{
    public partial class EditSongWindow : Window
    {
        public EditSongWindow(SongEditViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
            viewModel.RequestClose += () => Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
