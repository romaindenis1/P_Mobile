namespace Read4All
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("bookdetails", typeof(BookDetails));
        }
    }
}
