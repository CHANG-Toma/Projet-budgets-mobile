using System;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;
using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1.Views
{
    public partial class DashboardPage : ContentPage
    {
        private string _currentUserEmail = string.Empty;
        private DashboardViewModel _viewModel;

        public DashboardPage()
        {
            InitializeComponent();
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
            _viewModel = new DashboardViewModel();
            BindingContext = _viewModel;
            InitializeTransactionForm();
        }

        private void InitializeTransactionForm()
        {
            // Initialiser la date à aujourd'hui
            DatePicker.Date = DateTime.Now;
            
            // Définir la catégorie par défaut
            CategoryPicker.SelectedIndex = 0; // Alimentation
            CategoryIcon.Text = GetCategoryIcon("Alimentation");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }

        private async void OnViewBudgetDetailsTapped(object sender, EventArgs e)
        {
            await DisplayAlert("Budget", "Afficher les détails du budget", "OK");
        }

        private async void OnViewAllTransactionsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private void OnAddTransactionClicked(object sender, EventArgs e)
        {
            // Réinitialiser le formulaire
            ResetTransactionForm();
            
            // Afficher l'overlay
            TransactionOverlay.IsVisible = true;
        }

        private void OnCloseTransactionFormClicked(object sender, EventArgs e)
        {
            TransactionOverlay.IsVisible = false;
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            // Fermer l'overlay si on clique sur le fond (mais pas sur le formulaire)
            TransactionOverlay.IsVisible = false;
        }

        private void OnFormFrameTapped(object sender, EventArgs e)
        {
            // Empêcher la propagation de l'événement pour que le formulaire ne se ferme pas
            // quand on clique dessus
        }

        private void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            // La date est automatiquement mise à jour dans le DatePicker
        }

        private void OnCategorySelected(object sender, EventArgs e)
        {
            // Mettre à jour l'icône selon la catégorie sélectionnée
            if (CategoryPicker.SelectedItem != null)
            {
                string category = CategoryPicker.SelectedItem.ToString();
                CategoryIcon.Text = GetCategoryIcon(category);
            }
        }

        private string GetCategoryIcon(string category)
        {
            return category switch
            {
                "Alimentation" => "🍽️",
                "Transport" => "🚗",
                "Logement" => "🏠",
                "Santé" => "🏥",
                "Loisirs" => "🎮",
                "Autres" => "📦",
                _ => "📋"
            };
        }

        private async void OnSaveTransactionClicked(object sender, EventArgs e)
        {
            // Validation
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                await DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(TitleEntry.Text))
            {
                await DisplayAlert("Erreur", "Le titre est obligatoire", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(AmountEntry.Text) || 
                !double.TryParse(AmountEntry.Text.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double amount))
            {
                await DisplayAlert("Erreur", "Le montant doit être un nombre valide", "OK");
                return;
            }

            try
            {
                // Créer une nouvelle transaction
                var transaction = new Transaction
                {
                    Id = 0, // Nouvelle transaction
                    OwnerEmail = _currentUserEmail,
                    Date = DatePicker.Date
                };

                transaction.Title = TitleEntry.Text.Trim();
                transaction.Amount = amount;
                transaction.Date = DatePicker.Date;
                transaction.Category = CategoryPicker.SelectedItem?.ToString() ?? "Autres";
                transaction.OwnerEmail = _currentUserEmail;

                // Sauvegarder via DbService
                var id = await DbService.AddOrUpdateTransactionAsync(transaction);
                transaction.Id = id;

                await DisplayAlert("Succès", "Transaction enregistrée", "OK");

                // Fermer l'overlay
                TransactionOverlay.IsVisible = false;
                
                // Réinitialiser le formulaire
                ResetTransactionForm();
                
                // Recharger les données du dashboard
                await _viewModel.LoadDataAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Échec de l'enregistrement: {ex.Message}", "OK");
            }
        }

        private void ResetTransactionForm()
        {
            TitleEntry.Text = string.Empty;
            AmountEntry.Text = "0.00";
            DatePicker.Date = DateTime.Now;
            CategoryPicker.SelectedIndex = 0;
            CategoryIcon.Text = GetCategoryIcon("Alimentation");
        }

        // Navigation vers les autres pages
        private async void OnHomeTapped(object sender, EventArgs e)
        {
            // On est déjà sur la page d'accueil
            await DisplayAlert("Navigation", "Vous êtes déjà sur la page d'accueil", "OK");
        }

        private async void OnTransactionsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private async void OnStatisticsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//StatisticsPage");
        }

        private async void OnBudgetTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }

        private async void OnProfileTapped(object sender, EventArgs e)
        {
            await Navigation.PushAsync(new ProfilePage());
        }
    }
}
