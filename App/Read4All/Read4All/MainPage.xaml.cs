using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Read4All.Models;
using System.Collections.ObjectModel;
using System.Windows.Input;
using Read4All.ViewModels;

namespace Read4All
{
    public partial class MainPage : ContentPage
    {
        private readonly MainViewModel viewModel;

        public MainPage()
        {
            InitializeComponent();
            viewModel = new MainViewModel();
            BindingContext = viewModel;

            BooksCollection.ItemsSource = viewModel.FilteredBooks;
            RefreshView.Command = viewModel.RefreshCommand;
            BooksCollection.SelectionChanged += OnBookSelected;
        }

        private async void OnBookSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Book selectedBook)
            {
                ((CollectionView)sender).SelectedItem = null;
                viewModel.OpenBookDetailsCommand.Execute(selectedBook);
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await viewModel.LoadBooksAsync();
        }
    }
}
