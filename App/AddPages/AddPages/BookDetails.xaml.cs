namespace AddPages;

public partial class BookDetails : ContentPage
{
	public BookDetails()
	{
		InitializeComponent();
	}
    private async void GoToHome(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new MainPage());
    }
    private async void GoToRead(object sender, EventArgs e)
    {
        await Navigation.PushAsync(new LivrePage());
    }
}