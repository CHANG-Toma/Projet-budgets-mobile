namespace Projet_Budget_M1.Views
{
    public partial class TransactionsPage : ContentPage
    {
        public TransactionsPage()
        {
            InitializeComponent();
        }

        // Navigation vers les autres pages
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private async void OnTransactionsClicked(object sender, EventArgs e)
        {
            // On est déjà sur la page transactions
            await DisplayAlert("Navigation", "Vous êtes déjà sur la page transactions", "OK");
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//StatisticsPage");
        }

        private async void OnBudgetClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }
    }
}
