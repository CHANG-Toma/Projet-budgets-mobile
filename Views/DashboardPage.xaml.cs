using Microsoft.Maui.Storage;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class DashboardPage : ContentPage
    {
        public DashboardPage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            var email = Preferences.Default.Get("userEmail", string.Empty);
            if (!string.IsNullOrWhiteSpace(email))
            {
                var user = await DbService.GetUserByEmailAsync(email);
                var display = user?.FullName;
                if (string.IsNullOrWhiteSpace(display))
                {
                    display = email.Contains('@') ? email.Split('@')[0] : email;
                }
                GreetingLabel.Text = $"Bonjour {display}";
            }
            else
            {
                GreetingLabel.Text = "Bonjour";
            }
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

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }
    }
}
