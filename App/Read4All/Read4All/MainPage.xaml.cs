using Read4All.Database;
using System.Collections.ObjectModel;

namespace Read4All
{
    public partial class MainPage : ContentPage
    {
        private readonly TestConnection testconnection;
        public MainPage()
        {
            InitializeComponent();
            testconnection = new TestConnection();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await testconnection.TestConnectionAsync();
        }
    }
}
