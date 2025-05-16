using Read4All.Models;
using System.Text;
using System.Text.Json;

namespace Read4All;

public partial class BookDetails : ContentPage
{
	private readonly HttpClient httpClient;
	public Book Book { get; private set; }
	private readonly Action _onTagChanged;

	public BookDetails(Book book, Action onTagChanged = null)
	{
		InitializeComponent();
		Book = book;
		_onTagChanged = onTagChanged;
		BindingContext = this;

		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
		};
		
		httpClient = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(30)
		};
	}
    private async void GoToHome(object sender, EventArgs e)
    {
        await Navigation.PopAsync();
    }
    private async void GoToRead(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LivrePage());
    }

    private async void OnChangeTagClicked(object sender, EventArgs e)
    {
        string result = await DisplayPromptAsync(
            "Change Tag",
            "Enter a new tag:",
            initialValue: Book.Categorie?.Libelle,
            maxLength: 50,
            keyboard: Keyboard.Text);

        if (!string.IsNullOrWhiteSpace(result))
        {
            // Ensure we have a Categorie object
            if (Book.Categorie == null)
            {
                Book.Categorie = new Categorie { Id = 0 };
            }

            // Update the category
            Book.Categorie.Libelle = result.Trim();
            
            // Notify the main page that tags have changed
            _onTagChanged?.Invoke();
            
            // Force UI update
            OnPropertyChanged(nameof(Book));
        }
    }

    private async Task<bool> UpdateBookTagAsync(string newTag)
    {
        try
        {
            var updateData = new
            {
                categorie = new { libelle = newTag }
            };

            var json = JsonSerializer.Serialize(updateData);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            var uri = new Uri($"http://10.0.2.2:3000/livres/{Book.Id}");
            var response = await httpClient.PutAsync(uri, content);

            if (response.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                await MainThread.InvokeOnMainThreadAsync(() =>
                    DisplayAlert("Error", "Failed to update the book's tag. Please try again.", "OK"));
                return false;
            }
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
                DisplayAlert("Error", "Could not connect to the server. Please check your connection and try again.", "OK"));
            return false;
        }
    }
}
