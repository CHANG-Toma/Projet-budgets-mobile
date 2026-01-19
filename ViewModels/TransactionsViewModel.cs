using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Microsoft.Maui.ApplicationModel.DataTransfer;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Commands;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.ViewModels
{
    public class TransactionsViewModel : INotifyPropertyChanged
    {
        private string _currentUserEmail = string.Empty;
        private string _searchText = string.Empty;
        private bool _isLoading = false;
        private bool _isOverlayVisible = false;
        private string _formTitle = "Nouvelle transaction";
        private Transaction? _currentTransaction = null;

        // Propriétés du formulaire
        private string _title = string.Empty;
        private string _amount = "0.00";
        private DateTime _date = DateTime.Now;
        private string _selectedCategory = "Alimentation";
        private string _categoryIcon = "🍽️";
        private BudgetMensuel? _selectedBudget = null;
        private ObservableCollection<BudgetMensuel> _availableBudgets = new();
        private bool _createNewBudget = false;
        private string _newBudgetLimit = string.Empty;

        // Propriétés des filtres
        private bool _isFilterPanelVisible = false;
        private string _filterCategory = "Toutes";
        private DateTime? _filterDateStart = null;
        private DateTime? _filterDateEnd = null;
        private List<Transaction> _allTransactions = new();
        private bool _sortAscending = false; // false = décroissant (plus récent en premier)

        public ObservableCollection<TransactionGroup> TransactionGroups { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    ApplyFilters();
                }
            }
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public bool IsOverlayVisible
        {
            get => _isOverlayVisible;
            set => SetProperty(ref _isOverlayVisible, value);
        }

        public string FormTitle
        {
            get => _formTitle;
            set => SetProperty(ref _formTitle, value);
        }

        public string Title
        {
            get => _title;
            set => SetProperty(ref _title, value);
        }

        public string Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public DateTime Date
        {
            get => _date;
            set => SetProperty(ref _date, value);
        }

        public string SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    CategoryIcon = GetCategoryIcon(value);
                }
            }
        }

        public string CategoryIcon
        {
            get => _categoryIcon;
            set => SetProperty(ref _categoryIcon, value);
        }

        public ObservableCollection<BudgetMensuel> AvailableBudgets
        {
            get => _availableBudgets;
            set => SetProperty(ref _availableBudgets, value);
        }

        public BudgetMensuel? SelectedBudget
        {
            get => _selectedBudget;
            set => SetProperty(ref _selectedBudget, value);
        }

        public bool CreateNewBudget
        {
            get => _createNewBudget;
            set => SetProperty(ref _createNewBudget, value);
        }

        public string NewBudgetLimit
        {
            get => _newBudgetLimit;
            set => SetProperty(ref _newBudgetLimit, value);
        }

        public string[] Categories { get; } = { "Alimentation", "Transport", "Logement", "Santé", "Loisirs", "Autres" };
        public string[] FilterCategories { get; } = { "Toutes", "Alimentation", "Transport", "Logement", "Santé", "Loisirs", "Autres" };

        // Propriétés des filtres
        public bool IsFilterPanelVisible
        {
            get => _isFilterPanelVisible;
            set => SetProperty(ref _isFilterPanelVisible, value);
        }

        public string FilterCategory
        {
            get => _filterCategory;
            set
            {
                if (SetProperty(ref _filterCategory, value))
                {
                    ApplyFilters();
                }
            }
        }

        public DateTime? FilterDateStart
        {
            get => _filterDateStart;
            set
            {
                if (SetProperty(ref _filterDateStart, value))
                {
                    OnPropertyChanged(nameof(FilterDateStartValue));
                    ApplyFilters();
                }
            }
        }

        public DateTime FilterDateStartValue
        {
            get => _filterDateStart ?? DateTime.Today;
            set
            {
                // Toujours mettre à jour la valeur, même si c'est la date par défaut
                // L'utilisateur peut utiliser le bouton "Réinitialiser" pour effacer
                FilterDateStart = value;
            }
        }

        public DateTime? FilterDateEnd
        {
            get => _filterDateEnd;
            set
            {
                if (SetProperty(ref _filterDateEnd, value))
                {
                    OnPropertyChanged(nameof(FilterDateEndValue));
                    ApplyFilters();
                }
            }
        }

        public DateTime FilterDateEndValue
        {
            get => _filterDateEnd ?? DateTime.Today;
            set
            {
                // Toujours mettre à jour la valeur, même si c'est la date par défaut
                // L'utilisateur peut utiliser le bouton "Réinitialiser" pour effacer
                FilterDateEnd = value;
            }
        }

        // Commands
        public ICommand LoadDataCommand { get; }
        public ICommand AddTransactionCommand { get; }
        public ICommand EditTransactionCommand { get; }
        public ICommand DeleteTransactionCommand { get; }
        public ICommand SaveTransactionCommand { get; }
        public ICommand CloseOverlayCommand { get; }
        public ICommand NavigateHomeCommand { get; }
        public ICommand NavigateStatisticsCommand { get; }
        public ICommand NavigateBudgetCommand { get; }
        public ICommand ToggleFilterPanelCommand { get; }
        public ICommand ResetFiltersCommand { get; }
        public ICommand ToggleSortCommand { get; }
        public ICommand ExportPdfCommand { get; }

        public bool SortAscending
        {
            get => _sortAscending;
            set
            {
                if (SetProperty(ref _sortAscending, value))
                {
                    OnPropertyChanged(nameof(SortButtonText));
                    ApplyFilters();
                }
            }
        }

        public string SortButtonText => SortAscending ? "📅 ↑" : "📅 ↓";

        public TransactionsViewModel()
        {
            _currentUserEmail = Preferences.Default.Get("userEmail", string.Empty);
            
            LoadDataCommand = new RelayCommand(async () => await LoadAsync());
            AddTransactionCommand = new RelayCommand(OpenAddForm);
            EditTransactionCommand = new RelayCommand<Transaction>(EditTransaction);
            DeleteTransactionCommand = new RelayCommand<Transaction>(async (tx) => await DeleteTransaction(tx));
            SaveTransactionCommand = new RelayCommand(async () => await SaveTransaction());
            CloseOverlayCommand = new RelayCommand(CloseOverlay);
            NavigateHomeCommand = new RelayCommand(async () => await Shell.Current.GoToAsync("//DashboardPage"));
            NavigateStatisticsCommand = new RelayCommand(async () => await Shell.Current.GoToAsync("//StatisticsPage"));
            NavigateBudgetCommand = new RelayCommand(async () => await Shell.Current.GoToAsync("//BudgetPage"));
            ToggleFilterPanelCommand = new RelayCommand(() => IsFilterPanelVisible = !IsFilterPanelVisible);
            ResetFiltersCommand = new RelayCommand(ResetFilters);
            ToggleSortCommand = new RelayCommand(() => SortAscending = !SortAscending);
            ExportPdfCommand = new RelayCommand(async () => await ExportToPdf());
        }

        public async Task LoadAsync(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                _currentUserEmail = Preferences.Default.Get("userEmail", string.Empty);
                if (string.IsNullOrWhiteSpace(_currentUserEmail))
                {
                    _allTransactions = new List<Transaction>();
                    TransactionGroups.Clear();
                    return;
                }
            }

            IsLoading = true;
            try
            {
                var data = await DbService.GetTransactionsAsync(_currentUserEmail, search);
                _allTransactions = data ?? new List<Transaction>();
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _allTransactions = new List<Transaction>();
                TransactionGroups.Clear();
                
                System.Diagnostics.Debug.WriteLine($"Erreur dans LoadAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                try
                {
                    if (Application.Current?.MainPage != null)
                    {
                        await Application.Current.MainPage.DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
                    }
                }
                catch
                {
                    // Si on ne peut pas afficher l'alerte, on ignore
                }
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            try
            {
                if (_allTransactions == null)
                {
                    _allTransactions = new List<Transaction>();
                }

                var filtered = _allTransactions.AsEnumerable();

                // Filtre par texte de recherche
                if (!string.IsNullOrWhiteSpace(SearchText))
                {
                    filtered = filtered.Where(t => 
                        t != null && !string.IsNullOrEmpty(t.Title) &&
                        t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
                }

                // Filtre par catégorie
                if (!string.IsNullOrWhiteSpace(FilterCategory) && FilterCategory != "Toutes")
                {
                    filtered = filtered.Where(t => t != null && t.Category == FilterCategory);
                }

                // Filtre par date de début
                if (FilterDateStart.HasValue)
                {
                    filtered = filtered.Where(t => t != null && t.Date >= FilterDateStart.Value.Date);
                }

                // Filtre par date de fin
                if (FilterDateEnd.HasValue)
                {
                    filtered = filtered.Where(t => t != null && t.Date <= FilterDateEnd.Value.Date.AddDays(1).AddTicks(-1));
                }

                // Trier les transactions
                var sorted = SortAscending
                    ? filtered.Where(t => t != null).OrderBy(t => t.Date).ThenBy(t => t.Id)
                    : filtered.Where(t => t != null).OrderByDescending(t => t.Date).ThenByDescending(t => t.Id);

                // Grouper par date
                GroupTransactions(sorted.ToList());
            }
            catch (Exception ex)
            {
                // En cas d'erreur, on vide les groupes et on log l'erreur
                TransactionGroups.Clear();
                System.Diagnostics.Debug.WriteLine($"Erreur dans ApplyFilters: {ex.Message}");
            }
        }

        private void GroupTransactions(List<Transaction> transactions)
        {
            try
            {
                TransactionGroups.Clear();

                if (transactions == null || transactions.Count == 0)
                    return;

                var today = DateTime.Today;
                var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
                var thisMonthStart = new DateTime(today.Year, today.Month, 1);
                var lastMonthStart = thisMonthStart.AddMonths(-1);

                // Filtrer les transactions null avant de grouper
                var validTransactions = transactions.Where(t => t != null).ToList();
                if (validTransactions.Count == 0)
                    return;

                var grouped = validTransactions.GroupBy(t =>
                {
                    try
                    {
                        var date = t.Date.Date;
                        
                        if (date == today)
                            return "Aujourd'hui";
                        else if (date >= thisWeekStart && date < today)
                            return "Cette semaine";
                        else if (date >= thisMonthStart && date < thisWeekStart)
                            return "Ce mois";
                        else if (date >= lastMonthStart && date < thisMonthStart)
                            return "Mois dernier";
                        else if (date.Year == today.Year)
                            return date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("fr-FR"));
                        else
                            return date.ToString("yyyy");
                    }
                    catch
                    {
                        return "Autres";
                    }
                });

                // Trier les groupes selon l'ordre de tri
                var orderedGroups = SortAscending
                    ? grouped.OrderBy(g =>
                    {
                        try
                        {
                            var groupName = g.Key;
                            if (groupName == "Aujourd'hui") return 1;
                            if (groupName == "Cette semaine") return 2;
                            if (groupName == "Ce mois") return 3;
                            if (groupName == "Mois dernier") return 4;
                            var firstTransaction = g.FirstOrDefault();
                            if (firstTransaction == null) return int.MaxValue;
                            var firstDate = firstTransaction.Date;
                            return (int)(firstDate - DateTime.MinValue).TotalDays;
                        }
                        catch
                        {
                            return int.MaxValue;
                        }
                    })
                    : grouped.OrderByDescending(g =>
                    {
                        try
                        {
                            var groupName = g.Key;
                            if (groupName == "Aujourd'hui") return int.MaxValue;
                            if (groupName == "Cette semaine") return int.MaxValue - 1;
                            if (groupName == "Ce mois") return int.MaxValue - 2;
                            if (groupName == "Mois dernier") return int.MaxValue - 3;
                            var firstTransaction = g.FirstOrDefault();
                            if (firstTransaction == null) return 0;
                            var firstDate = firstTransaction.Date;
                            return (int)(firstDate - DateTime.MinValue).TotalDays;
                        }
                        catch
                        {
                            return 0;
                        }
                    });

                foreach (var group in orderedGroups)
                {
                    try
                    {
                        var firstTransaction = group.FirstOrDefault();
                        if (firstTransaction == null) continue;

                        var transactionGroup = new TransactionGroup(group.Key, firstTransaction.Date);
                        foreach (var transaction in group.Where(t => t != null))
                        {
                            transactionGroup.Add(transaction);
                        }
                        TransactionGroups.Add(transactionGroup);
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout d'un groupe: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                TransactionGroups.Clear();
                System.Diagnostics.Debug.WriteLine($"Erreur dans GroupTransactions: {ex.Message}");
            }
        }

        private void ResetFilters()
        {
            FilterCategory = "Toutes";
            FilterDateStart = null;
            FilterDateEnd = null;
            ApplyFilters();
        }

        private async Task ExportToPdf()
        {
            if (TransactionGroups.Count == 0)
            {
                await Application.Current!.MainPage!.DisplayAlert("Information", "Aucune transaction à exporter", "OK");
                return;
            }

            // Demander le type d'export
            var exportType = await Application.Current!.MainPage!.DisplayActionSheet(
                "Exporter en PDF",
                "Annuler",
                null,
                "Toutes les transactions",
                "Par mois",
                "Par catégorie");

            if (exportType == "Annuler" || string.IsNullOrEmpty(exportType))
                return;

            try
            {
                IsLoading = true;

                // Récupérer toutes les transactions filtrées
                var allFilteredTransactions = new List<Transaction>();
                foreach (var group in TransactionGroups)
                {
                    allFilteredTransactions.AddRange(group);
                }

                string pdfPath;
                string exportTypeParam = exportType switch
                {
                    "Par mois" => "month",
                    "Par catégorie" => "category",
                    _ => "all"
                };

                string? filterValue = exportType switch
                {
                    "Par mois" => FilterDateStart.HasValue 
                        ? FilterDateStart.Value.ToString("MMMM yyyy") 
                        : DateTime.Now.ToString("MMMM yyyy"),
                    "Par catégorie" => FilterCategory != "Toutes" ? FilterCategory : null,
                    _ => null
                };

                pdfPath = await PdfService.GenerateTransactionsPdfAsync(
                    allFilteredTransactions, 
                    exportTypeParam, 
                    filterValue);

                // Partager/sauvegarder le fichier
                await ShareFile(pdfPath);
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", 
                    $"Erreur lors de la génération du PDF: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ShareFile(string filePath)
        {
            try
            {
                await Share.Default.RequestAsync(new ShareFileRequest
                {
                    Title = "Exporter les transactions",
                    File = new ShareFile(filePath)
                });
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Information", 
                    $"PDF généré avec succès!\nEmplacement: {filePath}\n\nErreur lors du partage: {ex.Message}", "OK");
            }
        }

        private async void OpenAddForm()
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                _ = Application.Current!.MainPage!.DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            _currentTransaction = null;
            FormTitle = "Nouvelle transaction";
            ResetForm();
            
            // Charger les budgets disponibles
            try
            {
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                AvailableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    AvailableBudgets.Add(budget);
                }
                
                // Sélectionner le budget du mois en cours par défaut
                var currentMonth = new DateTime(Date.Year, Date.Month, 1);
                SelectedBudget = AvailableBudgets.FirstOrDefault(b => b.Mois == currentMonth);
                
                // Si aucun budget n'existe pour ce mois, proposer de créer un nouveau budget
                if (SelectedBudget == null)
                {
                    CreateNewBudget = true;
                    SelectedBudget = null; // S'assurer qu'aucun budget n'est sélectionné
                }
                else
                {
                    CreateNewBudget = false; // Un budget existe, ne pas créer de nouveau
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des budgets: {ex.Message}");
                // En cas d'erreur, proposer de créer un nouveau budget
                CreateNewBudget = true;
                SelectedBudget = null;
            }
            
            IsOverlayVisible = true;
        }

        private async void EditTransaction(Transaction? transaction)
        {
            if (transaction == null) return;

            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                _ = Application.Current!.MainPage!.DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            _currentTransaction = transaction;
            FormTitle = "Modifier transaction";
            LoadTransactionIntoForm(transaction);
            
            // Charger les budgets disponibles
            try
            {
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                AvailableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    AvailableBudgets.Add(budget);
                }
                
                // Sélectionner le budget du mois de la transaction
                var transactionMonth = new DateTime(transaction.Date.Year, transaction.Date.Month, 1);
                SelectedBudget = AvailableBudgets.FirstOrDefault(b => b.Mois == transactionMonth);
                CreateNewBudget = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des budgets: {ex.Message}");
            }
            
            IsOverlayVisible = true;
        }

        private void LoadTransactionIntoForm(Transaction tx)
        {
            Title = tx.Title ?? string.Empty;
            Amount = tx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Date = tx.Date;
            
            var categoryIndex = Array.IndexOf(Categories, tx.Category);
            if (categoryIndex >= 0)
            {
                SelectedCategory = Categories[categoryIndex];
            }
            else
            {
                SelectedCategory = Categories[0];
            }
        }

        private async Task DeleteTransaction(Transaction? transaction)
        {
            if (transaction == null) return;

            var confirm = await Application.Current!.MainPage!.DisplayAlert(
                "Confirmer", 
                $"Supprimer '{transaction.Title}' ?", 
                "Supprimer", 
                "Annuler");

            if (!confirm) return;

            try
            {
                await DbService.DeleteTransactionAsync(transaction.Id, _currentUserEmail);
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", $"Erreur lors de la suppression: {ex.Message}", "OK");
            }
        }

        private async Task SaveTransaction()
        {
            // Validation
            if (string.IsNullOrWhiteSpace(Title))
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", "Le titre est obligatoire", "OK");
                return;
            }

            if (string.IsNullOrWhiteSpace(Amount) || 
                !double.TryParse(Amount.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double amount))
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", "Le montant doit être un nombre valide", "OK");
                return;
            }

            try
            {
                // Gérer le budget - s'assurer qu'un budget est toujours disponible
                int? budgetId = null;
                
                if (CreateNewBudget)
                {
                    // Créer un nouveau budget
                    decimal? limite = null;
                    if (!string.IsNullOrWhiteSpace(NewBudgetLimit) && 
                        decimal.TryParse(NewBudgetLimit.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out decimal limitValue))
                    {
                        limite = limitValue;
                    }
                    
                    budgetId = await DbService.CreateBudgetAsync(_currentUserEmail, Date, limite);
                }
                else if (SelectedBudget != null)
                {
                    // Utiliser le budget sélectionné
                    budgetId = SelectedBudget.IdBudget;
                }
                // Si budgetId est null, AddOrUpdateTransactionAsync créera automatiquement un budget

                Transaction transaction;
                if (_currentTransaction != null && _currentTransaction.Id > 0)
                {
                    transaction = _currentTransaction;
                }
                else
                {
                    transaction = new Transaction
                    {
                        Id = 0,
                        OwnerEmail = _currentUserEmail,
                        Date = DateTime.Today
                    };
                }

                transaction.Title = Title.Trim();
                transaction.Amount = amount;
                transaction.Date = Date;
                transaction.Category = SelectedCategory;
                transaction.OwnerEmail = _currentUserEmail;

                var id = await DbService.AddOrUpdateTransactionAsync(transaction, budgetId);
                transaction.Id = id;

                await Application.Current!.MainPage!.DisplayAlert("Succès", "Transaction enregistrée", "OK");

                CloseOverlay();
                await LoadAsync();
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", $"Échec de l'enregistrement: {ex.Message}", "OK");
            }
        }

        private void CloseOverlay()
        {
            IsOverlayVisible = false;
            _currentTransaction = null;
            ResetForm();
        }

        private void ResetForm()
        {
            Title = string.Empty;
            Amount = "0.00";
            Date = DateTime.Now;
            SelectedCategory = Categories[0];
            CategoryIcon = GetCategoryIcon(Categories[0]);
            SelectedBudget = null;
            CreateNewBudget = false;
            NewBudgetLimit = string.Empty;
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}

