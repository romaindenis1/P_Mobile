using Read4All.Models;
using System.Text;
using System.Text.Json;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Read4All;

//cet attribut permet de recuperer le bookId depuis l'url
[QueryProperty(nameof(BookId), "bookId")]
public partial class BookDetails : ContentPage, INotifyPropertyChanged
{
	private readonly HttpClient httpClient;
	private Book book;
	private readonly Action _onTagChanged;
	private string bookId;

	public Book Book
	{
		get => book;
		private set
		{
			book = value;
			OnPropertyChanged();
		}
	}

	//cette propriete est appelee automatiquement quand on navigue vers cette page
	public string BookId
	{
		get => bookId;
		set
		{
			bookId = value;
			if (int.TryParse(value, out var id))
			{
				LoadBookDetails(id);
			}
		}
	}

	public BookDetails()
	{
		InitializeComponent();
		//on desactive la verification du certificat ssl pour le dev
		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
		};
		
		httpClient = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(30)
		};

		BindingContext = this;
	}

	private async Task LoadBookDetails(int bookId)
	{
		try
		{
			var uri = new Uri($"http://10.0.2.2:3000/livres/{bookId}");
			var response = await httpClient.GetAsync(uri);

			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};
				Book = JsonSerializer.Deserialize<Book>(content, options);
			}
			else
			{
				await DisplayAlert("Error", "Could not load book details. Please try again.", "OK");
				await Navigation.PopAsync();
			}
		}
		catch (Exception ex)
		{
			await DisplayAlert("Error", "Could not connect to the server. Please check your connection and try again.", "OK");
			await Navigation.PopAsync();
		}
	}

	private async void GoToHome(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void GoToRead(object sender, EventArgs e)
	{
		if (Book != null)
		{
			await Navigation.PushAsync(new LivrePage { BookId = Book.Id.ToString() });
		}
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
			//on cree une categorie si elle n'existe pas
			if (Book.Categorie == null)
			{
				Book.Categorie = new Categorie { Id = 0 };
			}

			//on met a jour la categorie
			Book.Categorie.Libelle = result.Trim();
			
			//on notifie la page principale que les tags ont change
			_onTagChanged?.Invoke();
			
			//on force la mise a jour de l'ui
			OnPropertyChanged(nameof(Book));
		}
	}

	private async Task<bool> UpdateBookTagAsync(string newTag)
	{
		try
		{
			//on prepare les donnees pour l'api
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

	public new event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
