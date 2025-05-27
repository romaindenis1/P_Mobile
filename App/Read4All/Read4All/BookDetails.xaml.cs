using Read4All.Models;
using System.Text;
using System.Text.Json;
using System.Diagnostics;
using System.Text.Json.Serialization;
using VersOne.Epub;
using System.IO;

namespace Read4All;

public class ApiResponse<T>
{
	[JsonPropertyName("message")]
	public string Message { get; set; }

	[JsonPropertyName("data")]
	public T Data { get; set; }
}

[QueryProperty(nameof(BookId), "bookId")]
public partial class BookDetails : ContentPage
{
	private readonly HttpClient httpClient;
	public Book Book { get; private set; }
	private readonly Action _onTagChanged;
	private string bookId;

	public string BookId
	{
		get => bookId;
		set
		{
			Debug.WriteLine("asd BookId setter called with value: " + value);
			bookId = value;
			if (int.TryParse(value, out var id))
			{
				Debug.WriteLine($"asd Successfully parsed BookId: {id}");
				LoadBookDetails(id);
			}
			else
			{
				Debug.WriteLine("asd Failed to parse BookId to integer");
			}
		}
	}

	public BookDetails()
	{
		Debug.WriteLine("asd BookDetails constructor called");
		InitializeComponent();
		var handler = new HttpClientHandler
		{
			ServerCertificateCustomValidationCallback = (sender, cert, chain, sslPolicyErrors) => true
		};
		
		httpClient = new HttpClient(handler)
		{
			Timeout = TimeSpan.FromSeconds(30)
		};
		Debug.WriteLine("asd HttpClient initialized with 30s timeout");
	}

	private async Task LoadBookDetails(int bookId)
	{
		Debug.WriteLine($"asd LoadBookDetails started for bookId: {bookId}");
		try
		{
			var uri = new Uri($"http://10.0.2.2:3000/livres/{bookId}");
			Debug.WriteLine($"asd Making request to: {uri}");

			var response = await httpClient.GetAsync(uri);
			Debug.WriteLine($"asd Response status code: {response.StatusCode}");

			if (response.IsSuccessStatusCode)
			{
				var content = await response.Content.ReadAsStringAsync();
				Debug.WriteLine($"asd Received content length: {content?.Length ?? 0} characters");
				Debug.WriteLine("asd First 1000 characters of response: " + content.Substring(0, Math.Min(1000, content.Length)));
				
				if (string.IsNullOrEmpty(content))
				{
					Debug.WriteLine("asd Received empty content from server");
					await DisplayAlert("Error", "Received empty response from server", "OK");
					return;
				}

				var options = new JsonSerializerOptions
				{
					PropertyNameCaseInsensitive = true
				};

				var apiResponse = JsonSerializer.Deserialize<ApiResponse<Book>>(content, options);
				Debug.WriteLine($"asd Deserialized API response - Message: {apiResponse?.Message}");
				
				if (apiResponse?.Data == null)
				{
					Debug.WriteLine("asd API response data is null");
					await DisplayAlert("Error", "No book data received", "OK");
					return;
				}

				Book = apiResponse.Data;
				Debug.WriteLine($"asd Deserialized book - Title: {Book?.Title}, Has Livre: {Book?.Livre != null}");
				
				if (Book?.Livre == null)
				{
					Debug.WriteLine("asd Book.Livre is null");
					Debug.WriteLine("asd Book properties available:");
					var bookJson = JsonSerializer.Serialize(Book, new JsonSerializerOptions { WriteIndented = true });
					Debug.WriteLine(bookJson);
				}
				else
				{
					Debug.WriteLine($"asd Book content byte length: {Book.Livre.Length}");
				}

				BindingContext = this;
				Debug.WriteLine("asd Set BindingContext");

				// Handle EPUB content if available
				if (Book?.Livre != null)
				{
					try
					{
						Debug.WriteLine($"asd Book content byte length: {Book.Livre.Length}");
						
						// Create a temporary file to store the EPUB
						string tempFile = Path.Combine(FileSystem.CacheDirectory, $"temp_{Book.Id}.epub");
						Debug.WriteLine($"asd Creating temporary file: {tempFile}");
						
						await File.WriteAllBytesAsync(tempFile, Book.Livre);
						Debug.WriteLine("asd Wrote EPUB data to temporary file");

						// lecture du fichier EPUB
						var epubBook = await EpubReader.ReadBookAsync(tempFile);
						Debug.WriteLine($"asd Loaded EPUB book - Title: {epubBook.Title}");

						// preparation pour stocker tout le contenu du livre
						var textBuilder = new StringBuilder();
						const int maxContentLength = 500000; // limite de 500KB pour le texte final
						bool contentTruncated = false;

						// recuperation de tous les fichiers HTML du livre
						var htmlFiles = epubBook.Content.Html.Local;
						Debug.WriteLine($"asd Found {htmlFiles.Count} HTML files");

						foreach (var htmlFile in htmlFiles.ToList())
						{
							try
							{
								// verification si on a deja atteint la limite
								if (textBuilder.Length >= maxContentLength)
								{
									contentTruncated = true;
									Debug.WriteLine("asd Content length limit reached, stopping processing");
									break;
								}

								Debug.WriteLine($"asd Processing HTML file: {htmlFile.FilePath}");
								
								string htmlContent = htmlFile.Content;
								
								if (!string.IsNullOrEmpty(htmlContent))
								{
									// filtrage basique du contenu
									if (htmlContent.Contains("<body"))
									{
										// extraction du contenu entre <body> et </body>
										int bodyStart = htmlContent.IndexOf("<body");
										bodyStart = htmlContent.IndexOf(">", bodyStart) + 1;
										int bodyEnd = htmlContent.LastIndexOf("</body>");
										
										if (bodyStart > 0 && bodyEnd > bodyStart)
										{
											htmlContent = htmlContent.Substring(bodyStart, bodyEnd - bodyStart);
										}
									}

									// nettoyage du HTML
									htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<script.*?</script>", " ", System.Text.RegularExpressions.RegexOptions.Singleline);
									htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<style.*?</style>", " ", System.Text.RegularExpressions.RegexOptions.Singleline);
									htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, "<[^>]+>", " ");
									htmlContent = System.Text.RegularExpressions.Regex.Replace(htmlContent, @"\s{2,}", " ");
									htmlContent = htmlContent.Trim();

									// verification de la taille restante
									int remainingSpace = maxContentLength - textBuilder.Length;
									if (remainingSpace > 0)
									{
										// ajout du contenu, tronque si necessaire
										string contentToAdd = htmlContent.Length > remainingSpace 
											? htmlContent.Substring(0, remainingSpace) 
											: htmlContent;
										
										textBuilder.AppendLine(contentToAdd);
										Debug.WriteLine($"asd Added {contentToAdd.Length} characters from {htmlFile.FilePath}");
									}
									else
									{
										contentTruncated = true;
										break;
									}
								}
							}
							catch (Exception ex)
							{
								Debug.WriteLine($"asd Error processing file {htmlFile.FilePath}: {ex.Message}");
								Debug.WriteLine($"asd Error stack trace: {ex.StackTrace}");
								continue;
							}
						}

						// suppression du fichier temporaire
						File.Delete(tempFile);
						Debug.WriteLine("asd Deleted temporary file");

						var finalText = textBuilder.ToString().Trim();
						Debug.WriteLine($"asd Final text length: {finalText.Length} characters");
						Debug.WriteLine($"asd Content was truncated: {contentTruncated}");
						Debug.WriteLine($"asd First 100 chars of content: {finalText.Substring(0, Math.Min(100, finalText.Length))}");

						MainThread.BeginInvokeOnMainThread(() =>
						{
							Debug.WriteLine("asd Setting BookContent.Text on main thread");
							BookContent.Text = finalText;
							if (contentTruncated)
							{
								BookContent.Text += "\n\n[Contenu tronqué pour des raisons de performance...]";
							}
							Debug.WriteLine("asd BookContent.Text has been set");
						});
					}
					catch (Exception ex)
					{
						Debug.WriteLine($"asd Error processing EPUB content: {ex.Message}");
						Debug.WriteLine($"asd Error stack trace: {ex.StackTrace}");
						await DisplayAlert("Error", "Failed to load book content.", "OK");
					}
				}
				else
				{
					Debug.WriteLine("asd Book.Livre is null");
				}
			}
			else
			{
				Debug.WriteLine($"asd Request failed with status: {response.StatusCode}");
				var errorContent = await response.Content.ReadAsStringAsync();
				Debug.WriteLine($"asd Error response content: {errorContent}");
				await DisplayAlert("Error", "Could not load book details. Please try again.", "OK");
				await Navigation.PopAsync();
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"asd Exception in LoadBookDetails: {ex.Message}");
			Debug.WriteLine($"asd Exception type: {ex.GetType().Name}");
			Debug.WriteLine($"asd Stack trace: {ex.StackTrace}");
			if (ex.InnerException != null)
			{
				Debug.WriteLine($"asd Inner exception: {ex.InnerException.Message}");
				Debug.WriteLine($"asd Inner exception type: {ex.InnerException.GetType().Name}");
			}
			await DisplayAlert("Error", "Could not connect to the server. Please check your connection and try again.", "OK");
			await Navigation.PopAsync();
		}
	}

	private async void GoToHome(object sender, EventArgs e)
	{
		Debug.WriteLine("asd GoToHome called");
		await Navigation.PopAsync();
	}

	private async void OnChangeTagClicked(object sender, EventArgs e)
	{
		Debug.WriteLine("asd OnChangeTagClicked called");
		string result = await DisplayPromptAsync(
			"Change Tag",
			"Enter a new tag:",
			initialValue: Book.Categorie?.Libelle,
			maxLength: 50,
			keyboard: Keyboard.Text);

		if (!string.IsNullOrWhiteSpace(result))
		{
			Debug.WriteLine($"asd New tag entered: {result}");
			if (Book.Categorie == null)
			{
				Book.Categorie = new Categorie { Id = 0 };
				Debug.WriteLine("asd Created new Categorie object");
			}

			Book.Categorie.Libelle = result.Trim();
			_onTagChanged?.Invoke();
			OnPropertyChanged(nameof(Book));
			Debug.WriteLine("asd Tag update completed");
		}
		else
		{
			Debug.WriteLine("asd Tag change cancelled or empty input");
		}
	}

	private async Task<bool> UpdateBookTagAsync(string newTag)
	{
		Debug.WriteLine($"asd UpdateBookTagAsync called with tag: {newTag}");
		try
		{
			var updateData = new
			{
				categorie = new { libelle = newTag }
			};

			var json = JsonSerializer.Serialize(updateData);
			Debug.WriteLine($"asd Serialized update data: {json}");
			var content = new StringContent(json, Encoding.UTF8, "application/json");

			var uri = new Uri($"http://10.0.2.2:3000/livres/{Book.Id}");
			Debug.WriteLine($"asd Sending PUT request to: {uri}");
			var response = await httpClient.PutAsync(uri, content);
			Debug.WriteLine($"asd PUT response status: {response.StatusCode}");

			if (response.IsSuccessStatusCode)
			{
				Debug.WriteLine("asd Tag update successful");
				return true;
			}
			else
			{
				var errorContent = await response.Content.ReadAsStringAsync();
				Debug.WriteLine($"asd Tag update failed. Error content: {errorContent}");
				await MainThread.InvokeOnMainThreadAsync(() =>
					DisplayAlert("Error", "Failed to update the book's tag. Please try again.", "OK"));
				return false;
			}
		}
		catch (Exception ex)
		{
			Debug.WriteLine($"asd Exception in UpdateBookTagAsync: {ex.Message}");
			Debug.WriteLine($"asd Stack trace: {ex.StackTrace}");
			await MainThread.InvokeOnMainThreadAsync(() =>
				DisplayAlert("Error", "Could not connect to the server. Please check your connection and try again.", "OK"));
			return false;
		}
	}
}

public class BufferObject
{
	[JsonPropertyName("type")]
	public string Type { get; set; }

	[JsonPropertyName("data")]
	public byte[] Data { get; set; }
}
