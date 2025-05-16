using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using Read4All.Models;
using System.Collections.ObjectModel;
using Read4All.Pages;
using System.Windows.Input;

namespace Read4All
{
    public partial class MainPage : ContentPage
    {
        private readonly HttpClient httpClient;
        private readonly ObservableCollection<Book> books = new();
        private readonly ObservableCollection<Book> filteredBooks = new();
        private readonly HashSet<string> availableTags = new();
        public ICommand OpenBookDetailsCommand { get; private set; }

        public MainPage()
        {
            InitializeComponent();
            
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            
            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            BooksCollection.ItemsSource = filteredBooks;

            RefreshView.Command = new Command(async () => await LoadBooksAsync());

            OpenBookDetailsCommand = new Command<Book>(async (book) => await OpenBookDetails(book));

            BindingContext = this;

            LoadBooksAsync();
        }

        private async Task OpenBookDetails(Book book)
        {
            if (book != null)
            {
                await Navigation.PushAsync(new BookDetails(book, OnTagChanged));
            }
        }

        private async void OnBookSelected(object sender, SelectionChangedEventArgs e)
        {
            if (e.CurrentSelection.FirstOrDefault() is Book selectedBook)
            {
                ((CollectionView)sender).SelectedItem = null;

                await OpenBookDetails(selectedBook);
            }
        }

        private void OnTagChanged()
        {
            UpdateAvailableTags();
            
            ApplyFilters();

            BooksCollection.ItemsSource = null;
            BooksCollection.ItemsSource = filteredBooks;
        }

        private void UpdateAvailableTags()
        {
            availableTags.Clear();
            foreach (var book in books)
            {
                if (!string.IsNullOrEmpty(book.Categorie?.Libelle))
                {
                    availableTags.Add(book.Categorie.Libelle);
                }
            }

            MainThread.BeginInvokeOnMainThread(() =>
            {
                var currentSelection = TagFilter.SelectedItem;
                TagFilter.ItemsSource = availableTags.OrderBy(t => t).ToList();
                if (currentSelection != null && availableTags.Contains((string)currentSelection))
                {
                    TagFilter.SelectedItem = currentSelection;
                }
            });
        }

        private void OnTagFilterChanged(object sender, EventArgs e)
        {
            ApplyFilters();
        }

        private void OnClearFilterClicked(object sender, EventArgs e)
        {
            TagFilter.SelectedItem = null;
            ApplyFilters();
        }

        private void ApplyFilters()
        {
            var selectedTag = TagFilter.SelectedItem as string;

            var filtered = books.Where(b => 
                selectedTag == null || 
                b.Categorie?.Libelle == selectedTag);


            var sorted = filtered.OrderByDescending(b => b.AnneeEdition);

            filteredBooks.Clear();
            foreach (var book in sorted)
            {
                filteredBooks.Add(book);
            }
        }

        private async Task LoadBooksAsync()
        {
            if (IsBusy)
                return;

            try
            {
                IsBusy = true;

                var uri = new Uri("http://10.0.2.2:3000/livres");
                var response = await httpClient.GetAsync(uri);
                
                if (response.IsSuccessStatusCode)
                {
                    var content = await response.Content.ReadAsStringAsync();
                    var booksResponse = JsonSerializer.Deserialize<BooksResponse>(content);

                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        books.Clear();
                        foreach (var book in booksResponse.Data)
                        {
                            books.Add(book);
                        }
                        UpdateAvailableTags();
                        ApplyFilters();
                    });
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                        DisplayAlert("Error", $"Server returned {response.StatusCode}", "OK"));
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error: {ex.Message}");
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Error", "Could not load books. Please try again later.", "OK"));
            }
            finally
            {
                IsBusy = false;
                RefreshView.IsRefreshing = false;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadBooksAsync();
        }
    }
}
