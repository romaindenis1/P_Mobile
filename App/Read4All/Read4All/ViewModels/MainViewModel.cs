using System.Collections.ObjectModel;
using System.Windows.Input;
using Read4All.Models;
using System.Net.Http;
using System.Text.Json;
using System.Diagnostics;
using System.IO;

namespace Read4All.ViewModels
{
    //ViewModel principal qui gere la liste des livres et les filtres
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

        //les collections qu'on expose en lecture seule pour la vue
        public ObservableCollection<Book> Books => books;
        public ObservableCollection<Book> FilteredBooks => filteredBooks;
        public IList<string> AvailableTags => availableTags.OrderBy(t => t).ToList();
        public ICommand RefreshCommand { get; }
        public ICommand OpenBookDetailsCommand { get; }
        public ICommand ClearFilterCommand { get; }

        //indique si une operation est en cours (pour afficher un loader)
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                isBusy = value;
                OnPropertyChanged();
            }
        }

        //le tag selectionne pour filtrer les livres
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

            //on desactive la verification du certificat ssl pour le dev
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
            };
            
            httpClient = new HttpClient(handler)
            {
                Timeout = TimeSpan.FromSeconds(60)
            };

            //initialisation des commandes
            RefreshCommand = new Command(async () => await LoadBooksAsync());
            OpenBookDetailsCommand = new Command<Book>(async (book) => await OpenBookDetails(book));
            ClearFilterCommand = new Command(() => SelectedTag = null);

            LoadBooksAsync();
        }

        //ouvre la page de details d'un livre
        private async Task OpenBookDetails(Book book)
        {
            if (book != null)
            {
                await Shell.Current.GoToAsync($"bookdetails?bookId={book.Id}");
            }
        }

        //met a jour la liste des tags disponibles
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

        //filtre les livres selon le tag selectionne
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

        //charge la liste des livres depuis l'api avec gestion des erreurs et retry
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
                            //on lit le stream dans un bloc using pour etre sur qu'il est bien ferme
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

                            //on desactive la case sensitivity pour le json
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
                                //on ajoute l'url de base pour les images
                                if (!string.IsNullOrEmpty(book.ImageCouverturePath))
                                {
                                    book.ImageCouverturePath = $"{BaseUrl}{book.ImageCouverturePath}";
                                }
                                books.Add(book);
                            }
                            UpdateAvailableTags();
                            ApplyFilters();
                            return;
                        }
                        catch (Exception ex) when (ex is not HttpRequestException && ex is not JsonException)
                        {
                            //on convertit les erreurs non-http en HttpRequestException pour le retry
                            Debug.WriteLine($"asd Content reading error: {ex.Message}");
                            throw new HttpRequestException("Error reading response content", ex);
                        }
                    }
                    else
                    {
                        Debug.WriteLine($"asd Server Error: Status code {response.StatusCode}");
                        await Shell.Current.DisplayAlert("Connection Error", $"Unable to connect to the server. Status code: {response.StatusCode}", "OK");
                        return;
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
                        //on attend de plus en plus longtemps entre chaque retry
                        Debug.WriteLine($"asd Retrying... Attempt {retryCount + 1} of {MaxRetries}");
                        await Task.Delay(RetryDelayMs * retryCount);
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
                    return;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"asd Unexpected Error: {ex.Message}");
                    Debug.WriteLine($"asd Inner Exception: {ex.InnerException?.Message}");
                    Debug.WriteLine($"asd Stack Trace: {ex.StackTrace}");
                    await Shell.Current.DisplayAlert("Error", "An unexpected error occurred. Please try again later.", "OK");
                    return;
                }
                finally
                {
                    IsBusy = false;
                }
            }
        }
    }
} 