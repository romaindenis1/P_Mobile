using Read4All.Models;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Read4All;

[QueryProperty(nameof(BookId), "bookId")]
public partial class LivrePage : ContentPage, INotifyPropertyChanged
{
	private readonly HttpClient httpClient;
	private string bookId;
	private string bookTitle;
	private string epubSource;
	private const string BaseUrl = "http://10.0.2.2:3000";

	public string BookId
	{
		get => bookId;
		set
		{
			bookId = value;
			if (int.TryParse(value, out var id))
			{
				LoadBookAsync(id);
			}
		}
	}

	public string BookTitle
	{
		get => bookTitle;
		set
		{
			bookTitle = value;
			OnPropertyChanged();
		}
	}

	public string EpubSource
	{
		get => epubSource;
		set
		{
			epubSource = value;
			OnPropertyChanged();
		}
	}

	public LivrePage()
	{
		InitializeComponent();
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

	private async Task LoadBookAsync(int bookId)
	{
		try
		{
			var uri = new Uri($"{BaseUrl}/livres/{bookId}");
			var response = await httpClient.GetAsync(uri);

			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				var options = new System.Text.Json.JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};
				var book = System.Text.Json.JsonSerializer.Deserialize<Book>(content, options);

				if (book != null)
				{
					BookTitle = book.Title;
					if (!string.IsNullOrEmpty(book.Livre))
					{
						// Construct the full URL for the EPUB file
						EpubSource = $"{BaseUrl}{book.Livre}";
					}
					else
					{
						await DisplayAlert("Error", "No EPUB file found for this book.", "OK");
						await Navigation.PopAsync();
					}
				}
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

	private async void OnBackClicked(object sender, EventArgs e)
	{
		await Navigation.PopAsync();
	}

	private async void OnSettingsClicked(object sender, EventArgs e)
	{
		// TODO: Implement reading settings (font size, theme, etc.)
		await DisplayAlert("Settings", "Reading settings will be implemented soon.", "OK");
	}

	public new event PropertyChangedEventHandler PropertyChanged;

	protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
	{
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}