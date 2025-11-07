using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly ObservableCollection<Transaction> _items = new();
        private string _currentUserEmail = string.Empty;
        private Transaction? _currentTransaction = null; // Transaction en cours d'édition

        public TransactionsPage()
        {
            InitializeComponent();
            TransactionsList.BindingContext = _items;
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
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
            await LoadAsync();
        }

        private async Task LoadAsync(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail)) return;
            var data = await DbService.GetTransactionsAsync(_currentUserEmail, search);
            _items.Clear();
            foreach (var t in data) _items.Add(t);
        }

        private void OnAddClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            // Réinitialiser pour une nouvelle transaction
            _currentTransaction = null;
            FormTitleLabel.Text = "Nouvelle transaction";
            ResetTransactionForm();
            
            // Afficher l'overlay
            TransactionOverlay.IsVisible = true;
        }

        private void OnEditClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            if ((sender as Button)?.CommandParameter is not Transaction tx) return;

            // Charger la transaction pour édition
            _currentTransaction = tx;
            FormTitleLabel.Text = "Modifier transaction";
            LoadTransactionIntoForm(tx);
            
            // Afficher l'overlay
            TransactionOverlay.IsVisible = true;
        }

        private void LoadTransactionIntoForm(Transaction tx)
        {
            TitleEntry.Text = tx.Title ?? string.Empty;
            AmountEntry.Text = tx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            DatePicker.Date = tx.Date;
            
            // Définir la catégorie
            var categories = new[] { "Alimentation", "Transport", "Logement", "Santé", "Loisirs", "Autres" };
            var categoryIndex = Array.IndexOf(categories, tx.Category);
            if (categoryIndex >= 0)
            {
                CategoryPicker.SelectedIndex = categoryIndex;
                CategoryIcon.Text = GetCategoryIcon(tx.Category);
            }
            else
            {
                CategoryPicker.SelectedIndex = 0;
                CategoryIcon.Text = GetCategoryIcon("Alimentation");
            }

        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is not Transaction tx) return;
            var confirm = await DisplayAlert("Confirmer", $"Supprimer '{tx.Title}' ?", "Supprimer", "Annuler");
            if (!confirm) return;
            await DbService.DeleteTransactionAsync(tx.Id, _currentUserEmail);
            await LoadAsync(SearchEntry.Text);
        }

        private async void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            await LoadAsync(e.NewTextValue);
        }

        // Gestion du formulaire de transaction
        private void OnCloseTransactionFormClicked(object sender, EventArgs e)
        {
            TransactionOverlay.IsVisible = false;
            _currentTransaction = null;
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            // Fermer l'overlay si on clique sur le fond (mais pas sur le formulaire)
            TransactionOverlay.IsVisible = false;
            _currentTransaction = null;
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
                // Créer ou mettre à jour la transaction
                Transaction transaction;
                if (_currentTransaction != null && _currentTransaction.Id > 0)
                {
                    // Mise à jour d'une transaction existante
                    transaction = _currentTransaction;
                }
                else
                {
                    // Création d'une nouvelle transaction
                    transaction = new Transaction
                    {
                        Id = 0, // S'assurer que l'ID est 0 pour une nouvelle transaction
                        OwnerEmail = _currentUserEmail,
                        Date = DateTime.Today
                    };
                }

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
                _currentTransaction = null;
                
                // Réinitialiser le formulaire
                ResetTransactionForm();
                
                // Recharger la liste
                await LoadAsync(SearchEntry.Text);
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
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private async void OnTransactionsTapped(object sender, EventArgs e)
        {
            await LoadAsync(SearchEntry.Text);
        }

        private async void OnStatisticsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//StatisticsPage");
        }

        private async void OnBudgetTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }
    }
}
