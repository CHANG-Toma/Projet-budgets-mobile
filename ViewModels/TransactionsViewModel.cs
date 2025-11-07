using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
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
        }

        public async Task LoadAsync(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                _currentUserEmail = Preferences.Default.Get("userEmail", string.Empty);
                if (string.IsNullOrWhiteSpace(_currentUserEmail))
                    return;
            }

            IsLoading = true;
            try
            {
                var data = await DbService.GetTransactionsAsync(_currentUserEmail, search);
                _allTransactions = data;
                ApplyFilters();
            }
            catch (Exception ex)
            {
                await Application.Current!.MainPage!.DisplayAlert("Erreur", $"Erreur lors du chargement: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private void ApplyFilters()
        {
            var filtered = _allTransactions.AsEnumerable();

            // Filtre par texte de recherche
            if (!string.IsNullOrWhiteSpace(SearchText))
            {
                filtered = filtered.Where(t => 
                    t.Title.Contains(SearchText, StringComparison.OrdinalIgnoreCase));
            }

            // Filtre par catégorie
            if (!string.IsNullOrWhiteSpace(FilterCategory) && FilterCategory != "Toutes")
            {
                filtered = filtered.Where(t => t.Category == FilterCategory);
            }

            // Filtre par date de début
            if (FilterDateStart.HasValue)
            {
                filtered = filtered.Where(t => t.Date >= FilterDateStart.Value.Date);
            }

            // Filtre par date de fin
            if (FilterDateEnd.HasValue)
            {
                filtered = filtered.Where(t => t.Date <= FilterDateEnd.Value.Date.AddDays(1).AddTicks(-1));
            }

            // Trier les transactions
            var sorted = SortAscending
                ? filtered.OrderBy(t => t.Date).ThenBy(t => t.Id)
                : filtered.OrderByDescending(t => t.Date).ThenByDescending(t => t.Id);

            // Grouper par date
            GroupTransactions(sorted.ToList());
        }

        private void GroupTransactions(List<Transaction> transactions)
        {
            TransactionGroups.Clear();

            if (transactions.Count == 0)
                return;

            var today = DateTime.Today;
            var thisWeekStart = today.AddDays(-(int)today.DayOfWeek);
            var thisMonthStart = new DateTime(today.Year, today.Month, 1);
            var lastMonthStart = thisMonthStart.AddMonths(-1);

            var grouped = transactions.GroupBy(t =>
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
            });

            // Trier les groupes selon l'ordre de tri
            var orderedGroups = SortAscending
                ? grouped.OrderBy(g =>
                {
                    var groupName = g.Key;
                    if (groupName == "Aujourd'hui") return 1;
                    if (groupName == "Cette semaine") return 2;
                    if (groupName == "Ce mois") return 3;
                    if (groupName == "Mois dernier") return 4;
                    var firstDate = g.First().Date;
                    return (int)(firstDate - DateTime.MinValue).TotalDays;
                })
                : grouped.OrderByDescending(g =>
                {
                    var groupName = g.Key;
                    if (groupName == "Aujourd'hui") return int.MaxValue;
                    if (groupName == "Cette semaine") return int.MaxValue - 1;
                    if (groupName == "Ce mois") return int.MaxValue - 2;
                    if (groupName == "Mois dernier") return int.MaxValue - 3;
                    var firstDate = g.First().Date;
                    return (int)(firstDate - DateTime.MinValue).TotalDays;
                });

            foreach (var group in orderedGroups)
            {
                var transactionGroup = new TransactionGroup(group.Key, group.First().Date);
                foreach (var transaction in group)
                {
                    transactionGroup.Add(transaction);
                }
                TransactionGroups.Add(transactionGroup);
            }
        }

        private void ResetFilters()
        {
            FilterCategory = "Toutes";
            FilterDateStart = null;
            FilterDateEnd = null;
            ApplyFilters();
        }

        private void OpenAddForm()
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail))
            {
                _ = Application.Current!.MainPage!.DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                return;
            }

            _currentTransaction = null;
            FormTitle = "Nouvelle transaction";
            ResetForm();
            IsOverlayVisible = true;
        }

        private void EditTransaction(Transaction? transaction)
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

                var id = await DbService.AddOrUpdateTransactionAsync(transaction);
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

