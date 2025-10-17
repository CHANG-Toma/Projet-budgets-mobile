namespace Projet_Budget_M1.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        private async void OnViewBudgetDetailsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Budget", "Afficher les détails du budget", "OK");
        }

        private async void OnViewAllTransactionsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Transactions", "Afficher toutes les transactions", "OK");
        }

        private async void OnAddTransactionClicked(object sender, EventArgs e)
        {
            await DisplayAlert("Nouvelle Transaction", "Ajouter une nouvelle transaction", "OK");
        }

        // Navigation vers les autres pages
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            // On est déjà sur la page d'accueil
            await DisplayAlert("Navigation", "Vous êtes déjà sur la page d'accueil", "OK");
        }

        private async void OnTransactionsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
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
