using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Projet_Budget_M1.Commands;

namespace Projet_Budget_M1.ViewModels
{
    public class DashboardViewModel : INotifyPropertyChanged
    {
        private decimal _totalBalance = 2450.50m;
        private decimal _totalIncome = 3500.00m;
        private decimal _totalExpenses = -1049.50m;
        private decimal _budgetSpent = 1049.50m;
        private decimal _budgetLimit = 3000.00m;

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
            set => SetProperty(ref _budgetLimit, value);
        }

        public double BudgetProgress => (double)(BudgetSpent / BudgetLimit);

        public string BudgetSpentText => $"{BudgetSpent:C} / {BudgetLimit:C}";

        public ICommand ViewBudgetDetailsCommand { get; }
        public ICommand ViewAllTransactionsCommand { get; }
        public ICommand AddTransactionCommand { get; }

        public DashboardViewModel()
        {
            ViewBudgetDetailsCommand = new RelayCommand(async () => await ViewBudgetDetails());
            ViewAllTransactionsCommand = new RelayCommand(async () => await ViewAllTransactions());
            AddTransactionCommand = new RelayCommand(async () => await AddTransaction());
        }

        private async Task ViewBudgetDetails()
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Budget", "Afficher les détails du budget", "OK");
        }

        private async Task ViewAllTransactions()
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Transactions", "Afficher toutes les transactions", "OK");
        }

        private async Task AddTransaction()
        {
            await Application.Current!.Windows[0].Page!.DisplayAlert("Nouvelle Transaction", "Ajouter une nouvelle transaction", "OK");
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
