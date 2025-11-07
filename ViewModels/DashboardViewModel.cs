using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Collections.ObjectModel;
using System.Linq;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Commands;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private decimal _totalBalance = 0m;
        private decimal _totalIncome = 0m;
        private decimal _totalExpenses = 0m;
        private decimal _budgetSpent = 0m;
        private decimal _budgetLimit = 0m;
        private string _greetingText = "Bonjour";
        private bool _isLoading = false;
        private string _currentUserEmail = string.Empty;

        public ObservableCollection<Transaction> RecentTransactions { get; } = new();

        public decimal TotalBalance
        {
            get => _totalBalance;
            set => SetProperty(ref _totalBalance, value);
        }

        public decimal TotalIncome
        {
            get => _totalIncome;
            set => SetProperty(ref _totalIncome, value);
        }

        public decimal TotalExpenses
        {
            get => _totalExpenses;
            set => SetProperty(ref _totalExpenses, value);
        }

        public decimal BudgetSpent
        {
            get => _budgetSpent;
            set => SetProperty(ref _budgetSpent, value);
        }

        public decimal BudgetLimit
        {
            get => _budgetLimit;
            set
            {
                if (SetProperty(ref _budgetLimit, value))
                {
                    OnPropertyChanged(nameof(BudgetProgress));
                    OnPropertyChanged(nameof(BudgetSpentText));
                }
            }
        }

        public string GreetingText
        {
            get => _greetingText;
            set => SetProperty(ref _greetingText, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public double BudgetProgress => BudgetLimit > 0 ? (double)(BudgetSpent / BudgetLimit) : 0;

        public string BudgetSpentText => $"{BudgetSpent:F2} € / {(BudgetLimit > 0 ? BudgetLimit.ToString("F2") : "0.00")} €";

        public string TotalBalanceText => $"{TotalBalance:F2} €";

        public string TotalExpensesText => $"{Math.Abs(TotalExpenses):F2} €";

        public ICommand ViewBudgetDetailsCommand { get; }
        public ICommand ViewAllTransactionsCommand { get; }
        public ICommand AddTransactionCommand { get; }
        public ICommand LoadDataCommand { get; }

        public DashboardViewModel()
        {
            _currentUserEmail = Preferences.Default.Get("userEmail", string.Empty);
            ViewBudgetDetailsCommand = new RelayCommand(async () => await ViewBudgetDetails());
            ViewAllTransactionsCommand = new RelayCommand(async () => await ViewAllTransactions());
            AddTransactionCommand = new RelayCommand(async () => await AddTransaction());
            LoadDataCommand = new RelayCommand(async () => await LoadDataAsync());
        }

        public async Task LoadDataAsync()
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
                // Charger les informations utilisateur
                var user = await DbService.GetUserByEmailAsync(_currentUserEmail);
                if (user != null)
                {
                    var display = user.FullName;
                    if (string.IsNullOrWhiteSpace(display))
                    {
                        display = _currentUserEmail.Contains('@') ? _currentUserEmail.Split('@')[0] : _currentUserEmail;
                    }
                    GreetingText = $"Bonjour {display}";
                }

                // Charger les transactions
                var transactions = await DbService.GetTransactionsAsync(_currentUserEmail);

                // Calculer les totaux
                decimal totalBalance = 0m;
                decimal totalIncome = 0m;
                decimal totalExpenses = 0m;
                decimal budgetSpent = 0m;

                var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
                var nextMonth = currentMonth.AddMonths(1);

                foreach (var transaction in transactions)
                {
                    var amount = (decimal)transaction.Amount;
                    totalBalance += amount;

                    if (amount > 0)
                    {
                        totalIncome += amount;
                    }
                    else
                    {
                        totalExpenses += amount; // Négatif
                        // Calculer les dépenses du mois en cours
                        if (transaction.Date >= currentMonth && transaction.Date < nextMonth)
                        {
                            budgetSpent += Math.Abs(amount);
                        }
                    }
                }

                TotalBalance = totalBalance;
                TotalIncome = totalIncome;
                TotalExpenses = totalExpenses;
                BudgetSpent = budgetSpent;

                // Charger le budget mensuel
                var budgetLimit = await DbService.GetCurrentMonthBudgetLimitAsync(_currentUserEmail);
                BudgetLimit = budgetLimit ?? 0m;

                // Charger les transactions récentes (5 dernières)
                RecentTransactions.Clear();
                foreach (var transaction in transactions.Take(5))
                {
                    RecentTransactions.Add(transaction);
                }

                OnPropertyChanged(nameof(TotalBalanceText));
                OnPropertyChanged(nameof(TotalExpensesText));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du chargement des données: {ex.Message}");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ViewBudgetDetails()
        {
            await Application.Current!.MainPage!.DisplayAlert("Budget", "Afficher les détails du budget", "OK");
        }

        private async Task ViewAllTransactions()
        {
            await Shell.Current.GoToAsync("//TransactionsPage");
        }

        private async Task AddTransaction()
        {
            // Cette fonctionnalité est gérée directement dans DashboardPage
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
