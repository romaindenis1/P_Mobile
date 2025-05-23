using System.Collections.ObjectModel;
using System.Windows.Input;
using Read4All.Models;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.IO;

namespace Read4All.ViewModels
{
    public class MainViewModel : BindableObject
    {
        private readonly HttpClient httpClient;
        private readonly ObservableCollection<Book> books;
        private readonly ObservableCollection<Book> filteredBooks;
        private readonly HashSet<string> availableTags;
        private bool isBusy;
        private string selectedTag;
        private const int MaxRetries = 3;
        private const int RetryDelayMs = 1000;
        private const string BaseUrl = "http://10.0.2.2:3000";

        public ObservableCollection<Book> Books => books;
        public ObservableCollection<Book> FilteredBooks => filteredBooks;
        public IList<string> AvailableTags => availableTags.OrderBy(t => t).ToList();
        public ICommand RefreshCommand { get; }
        public ICommand OpenBookDetailsCommand { get; }
        public ICommand ClearFilterCommand { get; }

        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }

        public string SelectedTag
        {
            get => selectedTag;
            set
            {
                selectedTag = value;
                OnPropertyChanged();
                ApplyFilters();
            }
        }

        public MainViewModel()
        {
            books = new ObservableCollection<Book>();
            filteredBooks = new ObservableCollection<Book>();
            availableTags = new HashSet<string>();

            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };

            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            RefreshCommand = new Command(async () => await LoadBooksAsync());
            OpenBookDetailsCommand = new Command<Book>(async (book) => await OpenBookDetails(book));
            ClearFilterCommand = new Command(() => SelectedTag = null);

            LoadBooksAsync();
        }

        private async Task OpenBookDetails(Book book)
        {
            if (book != null)
            {
                await Shell.Current.GoToAsync($"bookdetails?bookId={book.Id}");
            }
        }

        public void UpdateAvailableTags()
        {
            availableTags.Clear();
            foreach (var book in books)
            {
                if (!string.IsNullOrEmpty(book.Categorie?.Libelle))
                {
                    availableTags.Add(book.Categorie.Libelle);
                }
            }
            OnPropertyChanged(nameof(AvailableTags));
        }

        private void ApplyFilters()
        {
            var filtered = books.Where(b =>
                SelectedTag == null ||
                b.Categorie?.Libelle == SelectedTag);

            var sorted = filtered.OrderByDescending(b => b.AnneeEdition);

            filteredBooks.Clear();
            foreach (var book in sorted)
            {
                filteredBooks.Add(book);
            }
        }

        public async Task LoadBooksAsync()
        {
            if (IsBusy)
                return;

            int retryCount = 0;
            while (retryCount < MaxRetries)
            {
                try
                {
                    IsBusy = true;

                    var uri = new Uri($"{BaseUrl}/livres");
                    using var request = new HttpRequestMessage(HttpMethod.Get, uri);
                    request.Headers.Add("Connection", "keep-alive");
                    
                    var response = await httpClient.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
                    
                    if (response.IsSuccessStatusCode)
                    {
                        try
                        {
                            string content;
                            using (var stream = await response.Content.ReadAsStreamAsync())
                            {
                                using var reader = new StreamReader(stream);
                                content = await reader.ReadToEndAsync();
                            }
                            
                            if (string.IsNullOrEmpty(content))
                            {
                                Debug.WriteLine("asd Empty response received from server");
                                throw new HttpRequestException("Empty response received");
                            }

                            var options = new JsonSerializerOptions
                            {
                                PropertyNameCaseInsensitive = true
                            };

                            var booksResponse = JsonSerializer.Deserialize<BooksResponse>(content, options);
                            if (booksResponse?.Data == null)
                            {
                                Debug.WriteLine("asd Invalid response format");
                                throw new JsonException("Invalid response format");
                            }

                            books.Clear();
                            foreach (var book in booksResponse.Data)
                            {
                                // Convert relative image path to full URL
                                if (!string.IsNullOrEmpty(book.CoverImagePath))
                                {
                                    book.CoverImagePath = $"{BaseUrl}{book.CoverImagePath}";
                                }
                                books.Add(book);
                            }
                            UpdateAvailableTags();
                            ApplyFilters();
                            return; // Success, exit the retry loop
                        }
                        catch (Exception ex) when (ex is not HttpRequestException && ex is not JsonException)
                        {
                            Debug.WriteLine($"asd Content reading error: {ex.Message}");
                            throw new HttpRequestException("Error reading response content", ex);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"asd Server Error: Status code {response.StatusCode}");
                        await Shell.Current.DisplayAlert("Connection Error", $"Unable to connect to the server. Status code: {response.StatusCode}", "OK");
                        return; // Server error, don't retry
                    }
                }
                catch (HttpRequestException ex)
                {
                    Debug.WriteLine($"asd Network Error: {ex.Message}");
                    Debug.WriteLine($"asd Inner Exception: {ex.InnerException?.Message}");
                    Debug.WriteLine($"asd Stack Trace: {ex.StackTrace}");
                    
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        Debug.WriteLine($"asd Retrying... Attempt {retryCount + 1} of {MaxRetries}");
                        await Task.Delay(RetryDelayMs * retryCount); // Exponential backoff
                        continue;
                    }
                    
                    await Shell.Current.DisplayAlert("Network Error", "Unable to connect to the server. Please check your internet connection and try again.", "OK");
                }
                catch (TaskCanceledException ex)
                {
                    Debug.WriteLine($"asd Request Timeout: {ex.Message}");
                    retryCount++;
                    if (retryCount < MaxRetries)
                    {
                        Debug.WriteLine($"asd Retrying... Attempt {retryCount + 1} of {MaxRetries}");
                        await Task.Delay(RetryDelayMs * retryCount);
                        continue;
                    }
                    await Shell.Current.DisplayAlert("Connection Timeout", "The request took too long to complete. Please check your internet connection and try again.", "OK");
                }
                catch (JsonException ex)
                {
                    Debug.WriteLine($"asd Data Error: {ex.Message}");
                    await Shell.Current.DisplayAlert("Data Error", "Unable to process the server response. Please try again later.", "OK");
                    return; // Don't retry on data errors
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"asd Unexpected Error: {ex.Message}");
                    Debug.WriteLine($"asd Inner Exception: {ex.InnerException?.Message}");
                    Debug.WriteLine($"asd Stack Trace: {ex.StackTrace}");
                    await Shell.Current.DisplayAlert("Error", "An unexpected error occurred. Please try again later.", "OK");
                    return; // Don't retry on unexpected errors
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }
} 