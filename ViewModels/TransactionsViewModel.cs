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
        private Categorie? _selectedCategory = null;
        private string _categoryIcon = "🍽️";
        private BudgetMensuel? _selectedBudget = null;
        private ObservableCollection<BudgetMensuel> _availableBudgets = new();
        private ObservableCollection<Categorie> _availableCategories = new();
        private ObservableCollection<CategorieWrapper> _availableCategoriesForBudget = new();
        private bool _createNewBudget = false;
        private string _newBudgetLimit = string.Empty;
        private bool _createNewCategory = false;
        private string _newCategoryName = string.Empty;

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
            set
            {
                if (SetProperty(ref _date, value))
                {
                    // Vérifier le budget pour la nouvelle date
                    CheckBudgetForDate();
                }
            }
        }

        private async void CheckBudgetForDate()
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail) || !IsOverlayVisible)
                return;

            try
            {
                // Recharger les budgets et sélectionner celui du mois si disponible
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                AvailableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    AvailableBudgets.Add(budget);
                }
                
                var transactionMonth = new DateTime(Date.Year, Date.Month, 1);
                var budgetForMonth = AvailableBudgets.FirstOrDefault(b => b.Mois == transactionMonth);
                
                // Si un budget existe pour ce mois et qu'aucun n'est sélectionné, le sélectionner
                if (budgetForMonth != null && SelectedBudget == null && !CreateNewBudget)
                {
                    SelectedBudget = budgetForMonth;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la vérification du budget: {ex.Message}");
            }
        }

        public Categorie? SelectedCategory
        {
            get => _selectedCategory;
            set
            {
                if (SetProperty(ref _selectedCategory, value))
                {
                    if (value != null)
                    {
                        CategoryIcon = GetCategoryIcon(value.NomCategorie);
                    }
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

        public ObservableCollection<Categorie> AvailableCategories
        {
            get => _availableCategories;
            set => SetProperty(ref _availableCategories, value);
        }

        public ObservableCollection<CategorieWrapper> AvailableCategoriesForBudget
        {
            get => _availableCategoriesForBudget;
            set => SetProperty(ref _availableCategoriesForBudget, value);
        }

        public BudgetMensuel? SelectedBudget
        {
            get => _selectedBudget;
            set
            {
                if (SetProperty(ref _selectedBudget, value))
                {
                    // Si on sélectionne un budget, décocher "créer un nouveau budget"
                    if (value != null)
                    {
                        CreateNewBudget = false;
                        // Charger les catégories associées à ce budget
                        LoadCategoriesForBudget(value.IdBudget);
                    }
                }
            }
        }

        public bool CreateNewBudget
        {
            get => _createNewBudget;
            set
            {
                if (SetProperty(ref _createNewBudget, value))
                {
                    // Si on coche "créer un nouveau budget", désélectionner le budget existant
                    if (value)
                    {
                        SelectedBudget = null;
                        // Charger toutes les catégories disponibles (fire and forget)
                        _ = LoadAllCategories();
                    }
                }
            }
        }

        public string NewBudgetLimit
        {
            get => _newBudgetLimit;
            set => SetProperty(ref _newBudgetLimit, value);
        }

        public bool CreateNewCategory
        {
            get => _createNewCategory;
            set
            {
                if (SetProperty(ref _createNewCategory, value))
                {
                    if (value)
                    {
                        SelectedCategory = null;
                    }
                }
            }
        }

        public string NewCategoryName
        {
            get => _newCategoryName;
            set => SetProperty(ref _newCategoryName, value);
        }

        public string[] FilterCategories { get; private set; } = Array.Empty<string>();

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
            
            // Initialiser FilterCategories avec au moins "Toutes"
            FilterCategories = new[] { "Toutes" };
            FilterCategory = "Toutes";
            
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
                // S'assurer que TransactionGroups est initialisé
                if (TransactionGroups == null)
                {
                    System.Diagnostics.Debug.WriteLine("TransactionGroups est null, initialisation...");
                    return;
                }
                
                // Charger les catégories au démarrage (avec gestion d'erreur)
                try
                {
                    await LoadAllCategories();
                }
                catch (Exception catEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories dans LoadAsync: {catEx.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace catégories: {catEx.StackTrace}");
                    // Continuer même si le chargement des catégories échoue
                }
                
                // Charger les transactions
                var data = await DbService.GetTransactionsAsync(_currentUserEmail, search);
                _allTransactions = data ?? new List<Transaction>();
                
                // S'assurer que _allTransactions n'est pas null
                if (_allTransactions == null)
                {
                    _allTransactions = new List<Transaction>();
                }
                
                // Appliquer les filtres
                ApplyFilters();
            }
            catch (Exception ex)
            {
                _allTransactions = new List<Transaction>();
                if (TransactionGroups != null)
                {
                    TransactionGroups.Clear();
                }
                
                System.Diagnostics.Debug.WriteLine($"Erreur dans LoadAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException?.Message}");
                
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
                if (TransactionGroups == null)
                {
                    System.Diagnostics.Debug.WriteLine("TransactionGroups est null dans GroupTransactions");
                    return;
                }
                
                TransactionGroups.Clear();

                if (transactions == null || transactions.Count == 0)
                {
                    System.Diagnostics.Debug.WriteLine("Aucune transaction à grouper");
                    return;
                }

                var today = DateTime.Today;
                // Calculer le début de la semaine (lundi)
                var dayOfWeek = (int)today.DayOfWeek;
                // En C#, DayOfWeek.Sunday = 0, donc on doit ajuster
                var daysFromMonday = dayOfWeek == 0 ? 6 : dayOfWeek - 1;
                var thisWeekStart = today.AddDays(-daysFromMonday);
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
                            var days = (firstDate - DateTime.MinValue).TotalDays;
                            // Limiter à la plage d'un int pour éviter les dépassements
                            if (days > int.MaxValue) return int.MaxValue;
                            if (days < int.MinValue) return int.MinValue;
                            return (int)days;
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
                            var days = (firstDate - DateTime.MinValue).TotalDays;
                            // Limiter à la plage d'un int pour éviter les dépassements
                            if (days > int.MaxValue) return int.MaxValue;
                            if (days < int.MinValue) return int.MinValue;
                            return (int)days;
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
                        if (group == null) continue;
                        
                        var firstTransaction = group.FirstOrDefault();
                        if (firstTransaction == null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Groupe '{group.Key}' n'a pas de première transaction");
                            continue;
                        }

                        var transactionGroup = new TransactionGroup(group.Key, firstTransaction.Date);
                        
                        // Compter les transactions valides
                        var validTransactionsInGroup = group.Where(t => t != null).ToList();
                        System.Diagnostics.Debug.WriteLine($"Ajout du groupe '{group.Key}' avec {validTransactionsInGroup.Count} transactions");
                        
                        foreach (var transaction in validTransactionsInGroup)
                        {
                            try
                            {
                                if (transaction != null)
                                {
                                    transactionGroup.Add(transaction);
                                }
                            }
                            catch (Exception txEx)
                            {
                                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout d'une transaction au groupe: {txEx.Message}");
                            }
                        }
                        
                        // Ne ajouter le groupe que s'il contient au moins une transaction
                        if (transactionGroup.Count > 0)
                        {
                            TransactionGroups.Add(transactionGroup);
                            System.Diagnostics.Debug.WriteLine($"Groupe '{group.Key}' ajouté avec succès, {transactionGroup.Count} transactions");
                        }
                        else
                        {
                            System.Diagnostics.Debug.WriteLine($"Groupe '{group.Key}' ignoré car vide");
                        }
                    }
                    catch (Exception ex)
                    {
                        System.Diagnostics.Debug.WriteLine($"Erreur lors de l'ajout d'un groupe: {ex.Message}");
                        System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                        if (ex.InnerException != null)
                        {
                            System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                        }
                    }
                }
                
                System.Diagnostics.Debug.WriteLine($"Groupement terminé: {TransactionGroups.Count} groupes créés");
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
            
            // Charger tous les budgets disponibles
            try
            {
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                AvailableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    AvailableBudgets.Add(budget);
                }
                
                // Sélectionner par défaut le budget du mois de la transaction s'il existe
                var transactionMonth = new DateTime(Date.Year, Date.Month, 1);
                SelectedBudget = AvailableBudgets.FirstOrDefault(b => b.Mois == transactionMonth);
                
                // Si aucun budget n'existe pour ce mois, ne pas pré-cocher "créer un nouveau budget"
                // L'utilisateur choisira s'il veut en créer un
                CreateNewBudget = false;
                
                // Charger toutes les catégories disponibles
                await LoadAllCategories();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des budgets: {ex.Message}");
                CreateNewBudget = false;
            }
            
            IsOverlayVisible = true;
        }

        private async Task LoadAllCategories()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserEmail))
                {
                    _currentUserEmail = Preferences.Default.Get("userEmail", string.Empty);
                    if (string.IsNullOrWhiteSpace(_currentUserEmail))
                    {
                        // Pas d'utilisateur connecté, initialiser avec des valeurs par défaut
                        FilterCategories = new[] { "Toutes" };
                        OnPropertyChanged(nameof(FilterCategories));
                        return;
                    }
                }
                
                var categories = await DbService.GetCategoriesAsync(_currentUserEmail);
                
                // S'assurer que les collections sont initialisées
                if (AvailableCategories == null)
                {
                    AvailableCategories = new ObservableCollection<Categorie>();
                }
                if (AvailableCategoriesForBudget == null)
                {
                    AvailableCategoriesForBudget = new ObservableCollection<CategorieWrapper>();
                }
                
                AvailableCategories.Clear();
                foreach (var category in categories ?? new List<Categorie>())
                {
                    AvailableCategories.Add(category);
                }
                
                // Charger aussi pour la sélection de budget
                AvailableCategoriesForBudget.Clear();
                foreach (var category in categories ?? new List<Categorie>())
                {
                    AvailableCategoriesForBudget.Add(new CategorieWrapper { Categorie = category, IsSelected = false });
                }
                
                // Mettre à jour FilterCategories pour inclure "Toutes" + toutes les catégories
                var filterList = new List<string> { "Toutes" };
                if (categories != null && categories.Count > 0)
                {
                    filterList.AddRange(categories.Select(c => c.NomCategorie));
                }
                FilterCategories = filterList.ToArray();
                OnPropertyChanged(nameof(FilterCategories));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                
                // En cas d'erreur, initialiser avec au moins "Toutes"
                FilterCategories = new[] { "Toutes" };
                OnPropertyChanged(nameof(FilterCategories));
            }
        }

        private async void LoadCategoriesForBudget(int budgetId)
        {
            try
            {
                var categories = await DbService.GetCategoriesForBudgetAsync(budgetId);
                AvailableCategories.Clear();
                foreach (var category in categories)
                {
                    AvailableCategories.Add(category);
                }
                
                // Mettre à jour aussi AvailableCategoriesForBudget
                AvailableCategoriesForBudget.Clear();
                var allCategories = await DbService.GetCategoriesAsync(_currentUserEmail);
                foreach (var category in allCategories)
                {
                    bool isSelected = categories.Any(c => c.IdCategorie == category.IdCategorie);
                    AvailableCategoriesForBudget.Add(new CategorieWrapper { Categorie = category, IsSelected = isSelected });
                }
                
                // Si aucune catégorie n'est associée au budget, charger toutes les catégories
                if (AvailableCategories.Count == 0)
                {
                    await LoadAllCategories();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories du budget: {ex.Message}");
                await LoadAllCategories();
            }
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
            
            // Charger les catégories AVANT de charger la transaction dans le formulaire
            try
            {
                await LoadAllCategories();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des catégories: {ex.Message}");
            }
            
            LoadTransactionIntoForm(transaction);
            
            // Charger tous les budgets disponibles
            try
            {
                var budgets = await DbService.GetBudgetsAsync(_currentUserEmail);
                AvailableBudgets.Clear();
                foreach (var budget in budgets)
                {
                    AvailableBudgets.Add(budget);
                }
                
                // Sélectionner le budget du mois de la transaction s'il existe
                var transactionMonth = new DateTime(transaction.Date.Year, transaction.Date.Month, 1);
                SelectedBudget = AvailableBudgets.FirstOrDefault(b => b.Mois == transactionMonth);
                CreateNewBudget = false;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des budgets: {ex.Message}");
                CreateNewBudget = false;
            }
            
            IsOverlayVisible = true;
        }

        private void LoadTransactionIntoForm(Transaction tx)
        {
            Title = tx.Title ?? string.Empty;
            Amount = tx.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
            Date = tx.Date;
            
            // Trouver la catégorie correspondante (les catégories doivent être déjà chargées)
            SelectedCategory = AvailableCategories?.FirstOrDefault(c => c.NomCategorie == tx.Category);
            if (SelectedCategory == null && AvailableCategories != null && AvailableCategories.Count > 0)
            {
                SelectedCategory = AvailableCategories[0];
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
                // Gérer la catégorie AVANT le budget pour avoir son ID
                string categoryName = "Autres";
                int? newCategoryId = null;
                
                if (CreateNewCategory && !string.IsNullOrWhiteSpace(NewCategoryName))
                {
                    // Créer une nouvelle catégorie et récupérer son ID
                    newCategoryId = await DbService.CreateCategoryAsync(_currentUserEmail, NewCategoryName.Trim());
                    categoryName = NewCategoryName.Trim();
                    // Recharger les catégories pour avoir la nouvelle
                    await LoadAllCategories();
                }
                else if (SelectedCategory != null)
                {
                    // Utiliser la catégorie sélectionnée
                    categoryName = SelectedCategory.NomCategorie;
                }
                
                // Gérer le budget selon le choix de l'utilisateur
                int? budgetId = null;
                List<int>? categoryIdsForBudget = null;
                
                if (CreateNewBudget)
                {
                    // Créer un nouveau budget pour le mois de la transaction
                    decimal? limite = null;
                    if (!string.IsNullOrWhiteSpace(NewBudgetLimit))
                    {
                        // Nettoyer la chaîne (enlever les espaces, remplacer virgule par point)
                        var cleanedLimit = NewBudgetLimit.Trim().Replace(",", ".").Replace(" ", "");
                        System.Diagnostics.Debug.WriteLine($"Tentative de parsing de la limite: '{NewBudgetLimit}' -> '{cleanedLimit}'");
                        
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
                    foreach (var wrapper in AvailableCategoriesForBudget.Where(w => w.IsSelected))
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
                    else if (SelectedCategory != null && !categoryIdsForBudget.Contains(SelectedCategory.IdCategorie))
                    {
                        categoryIdsForBudget.Add(SelectedCategory.IdCategorie);
                    }
                    
                    budgetId = await DbService.CreateBudgetAsync(_currentUserEmail, Date, limite, categoryIdsForBudget);
                }
                else if (SelectedBudget != null)
                {
                    // Utiliser le budget sélectionné par l'utilisateur
                    budgetId = SelectedBudget.IdBudget;
                }
                // Si budgetId est null, AddOrUpdateTransactionAsync créera automatiquement un budget sans limite

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
                transaction.Category = categoryName;
                transaction.OwnerEmail = _currentUserEmail;

                var id = await DbService.AddOrUpdateTransactionAsync(transaction, budgetId);
                transaction.Id = id;

                // Fermer l'overlay avant de recharger pour éviter les problèmes de thread
                CloseOverlay();
                
                // Afficher le message de succès
                await Application.Current!.MainPage!.DisplayAlert("Succès", "Transaction enregistrée", "OK");

                // Recharger les données de manière sécurisée
                try
                {
                    await LoadAsync();
                }
                catch (Exception loadEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors du rechargement: {loadEx.Message}");
                    // Ne pas afficher d'erreur à l'utilisateur car la transaction est déjà sauvegardée
                }
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
            SelectedCategory = AvailableCategories?.FirstOrDefault();
            CategoryIcon = SelectedCategory != null ? GetCategoryIcon(SelectedCategory.NomCategorie) : "📋";
            NewBudgetLimit = string.Empty;
            CreateNewBudget = false;
            CreateNewCategory = false;
            NewCategoryName = string.Empty;
            // Réinitialiser les sélections de catégories pour le budget
            if (AvailableCategoriesForBudget != null)
            {
                foreach (var wrapper in AvailableCategoriesForBudget)
                {
                    wrapper.IsSelected = false;
                }
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

