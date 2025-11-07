namespace Projet_Budget_M1.Views
{
    public partial class StatisticsPage : ContentPage
    {
        public StatisticsPage()
        {
            InitializeComponent();
        }

        // Navigation vers les autres pages
        private async void OnHomeTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private async void OnTransactionsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private async void OnStatisticsTapped(object sender, EventArgs e)
        {
            // On est déjà sur la page statistiques
            await DisplayAlert("Navigation", "Vous êtes déjà sur la page statistiques", "OK");
        }

        private async void OnBudgetTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }
    }
}
