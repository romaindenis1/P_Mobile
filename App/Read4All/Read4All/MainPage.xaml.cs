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
        private readonly MainViewModel _viewModel;

        public MainPage()
        {
            InitializeComponent();
            _viewModel = new MainViewModel();
            BindingContext = _viewModel;

            BooksCollection.ItemsSource = _viewModel.FilteredBooks;
            RefreshView.Command = _viewModel.RefreshCommand;
            BooksCollection.SelectionChanged += OnBookSelected;
        }

        private async void OnBookSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Book selectedBook)
            {
                await Shell.Current.GoToAsync($"bookdetails?bookId={selectedBook.Id}");
                BooksCollection.SelectedItem = null;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadBooksAsync();
        }
    }
}
