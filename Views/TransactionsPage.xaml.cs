using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly ObservableCollection<Transaction> _items = new();
        private readonly ObservableCollection<string> _categories = new();
        private string _currentUserEmail = string.Empty;
        private Transaction? _currentTransaction = null; // Transaction en cours d'édition

        public TransactionsPage()
        {
            InitializeComponent();
            TransactionsList.BindingContext = _items;
            CategoryPicker.ItemsSource = _categories;
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
            InitializeTransactionForm();
        }

        private async void InitializeTransactionForm()
        {
            // Initialiser la date à aujourd'hui
            DatePicker.Date = DateTime.Now;
            
            // Charger les catégories depuis la base de données
            await LoadCategoriesAsync();
            
            // Définir la catégorie par défaut
            if (_categories.Count > 0)
            {
                CategoryPicker.SelectedIndex = 0;
                CategoryIcon.Text = GetCategoryIcon(_categories[0]);
            }
        }

        private async Task LoadCategoriesAsync()
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail)) return;
            
            try
            {
                _categories.Clear();
                
                // Catégories par défaut (toujours affichées)
                var defaultCategories = new[] { "Logement", "Alimentation", "Transport", "Santé", "Loisirs", "Factures", "Salaire" };
                foreach (var cat in defaultCategories)
                {
                    if (!_categories.Contains(cat))
                        _categories.Add(cat);
                }
                
                // Charger les catégories de l'utilisateur (celles créées en BD)
                var categories = await DbService.GetCategoriesAsync(_currentUserEmail);
                foreach (var cat in categories.OrderBy(c => c.NomCategorie))
                {
                    if (!_categories.Contains(cat.NomCategorie))
                        _categories.Add(cat.NomCategorie);
                }
                
                // Récupérer aussi les catégories des transactions existantes
                var transactions = await DbService.GetTransactionsAsync(_currentUserEmail);
                foreach (var tx in transactions)
                {
                    if (!string.IsNullOrWhiteSpace(tx.Category) && !_categories.Contains(tx.Category))
                        _categories.Add(tx.Category);
                }
                
                // Trier la liste
                var sorted = _categories.OrderBy(c => c).ToList();
                _categories.Clear();
                foreach (var cat in sorted)
                {
                    _categories.Add(cat);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories: {ex.Message}");
                // En cas d'erreur, utiliser les catégories par défaut
                _categories.Clear();
                _categories.Add("Alimentation");
                _categories.Add("Transport");
                _categories.Add("Logement");
                _categories.Add("Santé");
                _categories.Add("Loisirs");
                _categories.Add("Factures");
                _categories.Add("Salaire");
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadCategoriesAsync();
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
            
            // Définir la catégorie depuis la liste chargée
            var categoryIndex = _categories.IndexOf(tx.Category);
            if (categoryIndex >= 0)
            {
                CategoryPicker.SelectedIndex = categoryIndex;
                CategoryIcon.Text = GetCategoryIcon(tx.Category);
            }
            else
            {
                // Si la catégorie n'existe pas dans la liste, l'ajouter temporairement
                if (!_categories.Contains(tx.Category))
                {
                    _categories.Add(tx.Category);
                }
                CategoryPicker.SelectedIndex = _categories.IndexOf(tx.Category);
                CategoryIcon.Text = GetCategoryIcon(tx.Category);
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
            return category.ToLower() switch
            {
                var c when c.Contains("logement") || c.Contains("loyer") => "🏠",
                var c when c.Contains("alimentation") || c.Contains("courses") || c.Contains("nourriture") => "🍽️",
                var c when c.Contains("transport") || c.Contains("voiture") || c.Contains("essence") => "🚗",
                var c when c.Contains("loisir") || c.Contains("divertissement") => "🎮",
                var c when c.Contains("facture") || c.Contains("électricité") || c.Contains("eau") => "💡",
                var c when c.Contains("santé") || c.Contains("sante") || c.Contains("médecin") => "⚕️",
                var c when c.Contains("salaire") || c.Contains("revenu") => "💰",
                _ => "📦"
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

            var categoryName = CategoryPicker.SelectedItem?.ToString() ?? "Autres";
            var transactionDate = DatePicker.Date;
            var existingId = _currentTransaction?.Id > 0 ? _currentTransaction.Id : (int?)null;

            // Vérifier si la dépense dépasse le budget (seulement pour les dépenses négatives)
            if (amount < 0)
            {
                var budgetCheck = await DbService.CheckBudgetExceedanceAsync(
                    _currentUserEmail, categoryName, amount, transactionDate, existingId);

                if (budgetCheck.WillExceed && budgetCheck.BudgetLimit > 0)
                {
                    var depassement = budgetCheck.NewTotal - budgetCheck.BudgetLimit;
                    var message = $"⚠️ Attention : Budget dépassé !\n\n" +
                                 $"Catégorie : {budgetCheck.CategoryName}\n" +
                                 $"Budget alloué : {budgetCheck.BudgetLimit:F2} €\n" +
                                 $"Dépense actuelle : {budgetCheck.CurrentSpent:F2} €\n" +
                                 $"Nouveau total : {budgetCheck.NewTotal:F2} €\n" +
                                 $"Dépassement : {depassement:F2} €\n\n" +
                                 $"Le budget passera en négatif. Voulez-vous continuer ?";
                    
                    var continueAnyway = await DisplayAlert(
                        "Budget dépassé",
                        message,
                        "Continuer quand même",
                        "Annuler"
                    );

                    if (!continueAnyway)
                    {
                        return; // L'utilisateur a annulé
                    }
                }
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
                transaction.Date = transactionDate;
                transaction.Category = categoryName;
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
            if (_categories.Count > 0)
            {
                CategoryPicker.SelectedIndex = 0;
                CategoryIcon.Text = GetCategoryIcon(_categories[0]);
            }
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
