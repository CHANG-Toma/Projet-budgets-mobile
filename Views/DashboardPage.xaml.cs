using System;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;
using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1.Views
{
    public partial class DashboardPage : ContentPage
    {
        private readonly ObservableCollection<Transaction> _recentTransactions = new();
        private string _currentUserEmail = string.Empty;
        private DashboardViewModel _viewModel;
        private ObservableCollection<BudgetMensuel> _availableBudgets = new();
        private ObservableCollection<Categorie> _availableCategories = new();
        private ObservableCollection<CategorieWrapper> _availableCategoriesForBudget = new();
        private bool _createNewCategory = false;

        public DashboardPage()
        {
            InitializeComponent();
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
            _viewModel = new DashboardViewModel();
            BindingContext = _viewModel;
            InitializeTransactionForm();
        }

        private async void InitializeTransactionForm()
        {
            // Initialiser la date à aujourd'hui
            DatePicker.Date = DateTime.Now;
            
            // Charger les catégories
            await LoadCategoriesAsync();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await _viewModel.LoadDataAsync();
        }

        private async void OnViewBudgetDetailsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }

        private async void OnViewAllTransactionsTapped(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private async void OnAddTransactionClicked(object sender, EventArgs e)
        {
            // Réinitialiser le formulaire
            ResetTransactionForm();
            
            // Charger les budgets et catégories disponibles
            await LoadBudgetsAsync();
            await LoadCategoriesAsync();
            
            // Afficher l'overlay
            TransactionOverlay.IsVisible = true;
        }

        private async Task LoadBudgetsAsync()
        {
            try
            {
                // Charger tous les budgets disponibles
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                _availableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    _availableBudgets.Add(budget);
                }
                
                BudgetPicker.ItemsSource = _availableBudgets;
                
                // Sélectionner par défaut le budget du mois de la transaction s'il existe
                var transactionMonth = new DateTime(DatePicker.Date.Year, DatePicker.Date.Month, 1);
                var budgetForMonth = _availableBudgets.FirstOrDefault(b => b.Mois == transactionMonth);
                
                if (budgetForMonth != null)
                {
                    BudgetPicker.SelectedItem = budgetForMonth;
                }
                
                // Gérer le changement de la checkbox budget
                CreateNewBudgetCheckBox.CheckedChanged -= OnCreateNewBudgetCheckedChanged; // Retirer d'abord pour éviter les doublons
                CreateNewBudgetCheckBox.CheckedChanged += OnCreateNewBudgetCheckedChanged;
                
                // Gérer le changement de sélection du budget
                BudgetPicker.SelectedIndexChanged -= OnBudgetSelected; // Retirer d'abord pour éviter les doublons
                BudgetPicker.SelectedIndexChanged += OnBudgetSelected;
                
                // Gérer le changement de la checkbox catégorie
                CreateNewCategoryCheckBox.CheckedChanged -= OnCreateNewCategoryCheckedChanged; // Retirer d'abord pour éviter les doublons
                CreateNewCategoryCheckBox.CheckedChanged += OnCreateNewCategoryCheckedChanged;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des budgets: {ex.Message}");
            }
        }

        private void OnCreateNewBudgetCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                // Créer un nouveau budget - désélectionner le budget existant
                BudgetPicker.SelectedItem = null;
                NewBudgetLimitFrame.IsVisible = true;
                BudgetCategoriesFrame.IsVisible = true;
                BudgetCategoriesCollectionView.ItemsSource = _availableCategoriesForBudget;
            }
            else
            {
                // Utiliser un budget existant
                NewBudgetLimitFrame.IsVisible = false;
                BudgetCategoriesFrame.IsVisible = false;
                NewBudgetLimitEntry.Text = string.Empty;
            }
        }

        private void OnBudgetSelected(object? sender, EventArgs e)
        {
            // Si un budget est sélectionné, décocher "créer un nouveau budget"
            if (BudgetPicker.SelectedItem != null)
            {
                CreateNewBudgetCheckBox.IsChecked = false;
            }
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

        private async void OnDateSelected(object sender, DateChangedEventArgs e)
        {
            // Vérifier le budget pour la nouvelle date sélectionnée
            await LoadBudgetsAsync();
        }

        private async Task LoadCategoriesAsync()
        {
            try
            {
                var categories = await DbService.GetCategoriesAsync(_currentUserEmail);
                _availableCategories.Clear();
                foreach (var category in categories)
                {
                    _availableCategories.Add(category);
                }
                
                // Charger aussi pour la sélection de budget
                _availableCategoriesForBudget.Clear();
                foreach (var category in categories)
                {
                    _availableCategoriesForBudget.Add(new CategorieWrapper { Categorie = category, IsSelected = false });
                }
                
                CategoryPicker.ItemsSource = _availableCategories;
                
                // Sélectionner la première catégorie par défaut
                if (_availableCategories.Count > 0)
                {
                    CategoryPicker.SelectedItem = _availableCategories[0];
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories: {ex.Message}");
            }
        }

        private void OnCategorySelected(object sender, EventArgs e)
        {
            // Mettre à jour l'icône selon la catégorie sélectionnée
            if (CategoryPicker.SelectedItem is Categorie selectedCategory)
            {
                CategoryIcon.Text = GetCategoryIcon(selectedCategory.NomCategorie);
            }
        }
        
        private void OnCreateNewCategoryCheckedChanged(object? sender, CheckedChangedEventArgs e)
        {
            if (e.Value)
            {
                // Créer une nouvelle catégorie - désélectionner la catégorie existante
                CategoryPicker.SelectedItem = null;
                NewCategoryNameFrame.IsVisible = true;
            }
            else
            {
                // Utiliser une catégorie existante
                NewCategoryNameFrame.IsVisible = false;
                NewCategoryNameEntry.Text = string.Empty;
            }
        }

        private string GetCategoryIcon(string category)
        {
            if (string.IsNullOrWhiteSpace(category))
                return "📋";
                
            return category.ToLower() switch
            {
                "alimentation" or "food" or "nourriture" => "🍽️",
                "transport" or "véhicule" => "🚗",
                "logement" or "habitation" or "maison" => "🏠",
                "santé" or "health" or "médical" => "🏥",
                "loisirs" or "divertissement" or "entertainment" => "🎮",
                "autres" or "other" => "📦",
                "salaire" or "revenu" or "income" => "💰",
                "courses" or "shopping" => "🛒",
                "restaurant" or "repas" => "🍴",
                "cinéma" or "cinema" => "🎬",
                "sport" => "⚽",
                "voyage" or "travel" => "✈️",
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
                // Gérer la catégorie AVANT le budget pour avoir son ID
                string categoryName = "Autres";
                int? newCategoryId = null;
                
                if (CreateNewCategoryCheckBox.IsChecked && !string.IsNullOrWhiteSpace(NewCategoryNameEntry.Text))
                {
                    // Créer une nouvelle catégorie et récupérer son ID
                    newCategoryId = await DbService.CreateCategoryAsync(_currentUserEmail, NewCategoryNameEntry.Text.Trim());
                    categoryName = NewCategoryNameEntry.Text.Trim();
                    // Recharger les catégories pour avoir la nouvelle
                    await LoadCategoriesAsync();
                }
                else if (CategoryPicker.SelectedItem is Categorie selectedCategory)
                {
                    // Utiliser la catégorie sélectionnée
                    categoryName = selectedCategory.NomCategorie;
                }
                
                // Gérer le budget selon le choix de l'utilisateur
                int? budgetId = null;
                List<int>? categoryIdsForBudget = null;
                
                if (CreateNewBudgetCheckBox.IsChecked)
                {
                    // Créer un nouveau budget pour le mois de la transaction
                    decimal? limite = null;
                    if (!string.IsNullOrWhiteSpace(NewBudgetLimitEntry.Text))
                    {
                        // Nettoyer la chaîne (enlever les espaces, remplacer virgule par point)
                        var cleanedLimit = NewBudgetLimitEntry.Text.Trim().Replace(",", ".").Replace(" ", "");
                        System.Diagnostics.Debug.WriteLine($"Tentative de parsing de la limite: '{NewBudgetLimitEntry.Text}' -> '{cleanedLimit}'");
                        
                        if (decimal.TryParse(cleanedLimit, System.Globalization.NumberStyles.Any, System.Globalization.CultureInfo.InvariantCulture, out decimal limitValue))
                        {
                            limite = limitValue;
                            System.Diagnostics.Debug.WriteLine($"Limite parsée avec succès: {limite.Value}");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Échec du parsing de la limite: '{cleanedLimit}'");
                        }
                    }
                    
                    // Préparer les catégories à associer au budget
                    categoryIdsForBudget = new List<int>();
                    
                    // Ajouter les catégories sélectionnées pour le budget
                    foreach (var wrapper in _availableCategoriesForBudget.Where(w => w.IsSelected))
                    {
                        if (!categoryIdsForBudget.Contains(wrapper.Categorie.IdCategorie))
                        {
                            categoryIdsForBudget.Add(wrapper.Categorie.IdCategorie);
                        }
                    }
                    
                    // Ajouter aussi la catégorie de la transaction si elle n'est pas déjà dans la liste
                    if (newCategoryId.HasValue)
                    {
                        // Si on vient de créer une catégorie, utiliser directement son ID
                        if (!categoryIdsForBudget.Contains(newCategoryId.Value))
                        {
                            categoryIdsForBudget.Add(newCategoryId.Value);
                        }
                    }
                    else if (CategoryPicker.SelectedItem is Categorie selectedCat && !categoryIdsForBudget.Contains(selectedCat.IdCategorie))
                    {
                        categoryIdsForBudget.Add(selectedCat.IdCategorie);
                    }
                    
                    budgetId = await DbService.CreateBudgetAsync(_currentUserEmail, DatePicker.Date, limite, categoryIdsForBudget);
                }
                else if (BudgetPicker.SelectedItem is BudgetMensuel selectedBudget)
                {
                    // Utiliser le budget sélectionné par l'utilisateur
                    budgetId = selectedBudget.IdBudget;
                }
                // Si budgetId est null, AddOrUpdateTransactionAsync créera automatiquement un budget sans limite

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
                transaction.Category = categoryName;
                transaction.OwnerEmail = _currentUserEmail;

                // Sauvegarder via DbService avec le budget
                var id = await DbService.AddOrUpdateTransactionAsync(transaction, budgetId);
                transaction.Id = id;

                // Fermer l'overlay avant de recharger pour éviter les problèmes de thread
                TransactionOverlay.IsVisible = false;
                
                // Réinitialiser le formulaire
                ResetTransactionForm();

                // Afficher le message de succès
                await DisplayAlert("Succès", "Transaction enregistrée", "OK");
                
                // Recharger les données du dashboard de manière sécurisée
                try
                {
                    await _viewModel.LoadDataAsync();
                }
                catch (Exception loadEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors du rechargement: {loadEx.Message}");
                    // Ne pas afficher d'erreur à l'utilisateur car la transaction est déjà sauvegardée
                }
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
            
            // Réinitialiser la catégorie
            if (_availableCategories.Count > 0)
            {
                CategoryPicker.SelectedItem = _availableCategories[0];
            }
            else
            {
                CategoryPicker.SelectedItem = null;
            }
            CategoryIcon.Text = "📋";
            
            // Réinitialiser les champs de catégorie
            CreateNewCategoryCheckBox.IsChecked = false;
            NewCategoryNameEntry.Text = string.Empty;
            NewCategoryNameFrame.IsVisible = false;
            
            // Réinitialiser les champs de budget
            BudgetPicker.SelectedItem = null;
            CreateNewBudgetCheckBox.IsChecked = false;
            NewBudgetLimitEntry.Text = string.Empty;
            NewBudgetLimitFrame.IsVisible = false;
            BudgetCategoriesFrame.IsVisible = false;
            
            // Réinitialiser les sélections de catégories pour le budget
            foreach (var wrapper in _availableCategoriesForBudget)
            {
                wrapper.IsSelected = false;
            }
            
            // Retirer les événements pour éviter les fuites mémoire
            CreateNewBudgetCheckBox.CheckedChanged -= OnCreateNewBudgetCheckedChanged;
            BudgetPicker.SelectedIndexChanged -= OnBudgetSelected;
            CreateNewCategoryCheckBox.CheckedChanged -= OnCreateNewCategoryCheckedChanged;
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
