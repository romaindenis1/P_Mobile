using Read4All.Database;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Read4All
{
    public partial class MainPage : ContentPage
    {
        private TestConnection testconnection;

        public MainPage()
        {
            InitializeComponent();
            testconnection = new TestConnection();

        }

        private async Task InitializeAsync()
        {
            HttpClient httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60) 
            };
            string URL = "http://10.0.2.2:3000/livres";

            try
            {
                var response = await httpClient.GetAsync(URL);
                if (response.IsSuccessStatusCode)
                {
                    var jsonResponse = await response.Content.ReadAsStringAsync();
                    Debug.WriteLine($"asd Response: {jsonResponse}");
                }
                else
                {
                    Debug.WriteLine($"asd HTTP Error: {response.StatusCode}");
                }
            }
            catch (HttpRequestException ex)
            {
                Debug.WriteLine($"asd HttpRequestException: {ex.Message}");
                Debug.WriteLine($"asd StackTrace: {ex.StackTrace}");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"asd Exception: {ex.Message}");
                Debug.WriteLine($"asd StackTrace: {ex.StackTrace}");
            }


        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await InitializeAsync(); // Il est content quand je precise que ya pas de return donc voila
            await testconnection.TestConnectionAsync();
        }
    }
}
