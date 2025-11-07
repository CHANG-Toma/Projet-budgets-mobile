using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
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

        public ObservableCollection<Transaction> Transactions { get; } = new();

        public string SearchText
        {
            get => _searchText;
            set
            {
                if (SetProperty(ref _searchText, value))
                {
                    _ = LoadAsync(value);
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
                Transactions.Clear();
                foreach (var t in data)
                {
                    Transactions.Add(t);
                }
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
                await LoadAsync(SearchText);
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
                await LoadAsync(SearchText);
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

