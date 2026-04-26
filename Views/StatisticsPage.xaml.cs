using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views;

public partial class StatisticsPage : ContentPage, INotifyPropertyChanged
{
    private string _currentUserEmail = string.Empty;
    private bool _isLoading;
    private string _currentMonthLabel = DateTime.Now.ToString("MMMM yyyy", CultureInfo.CurrentCulture);
    private double _currentMonthExpenses;
    private double _currentMonthIncome;
    private int _currentMonthTransactionsCount;
    private string _topCategoryText = "Aucune";
    private double _salaryCategoryAmount;
    private string _topCategoriesSummaryText = "Aucune donnée disponible";
    private string _monthlyTrendSummaryText = "Aucune donnée disponible";

    public ObservableCollection<CategoryStatItem> TopCategories { get; } = new();
    public ObservableCollection<MonthlyTrendItem> MonthlyTrend { get; } = new();

    public bool IsLoading
    {
        get => _isLoading;
        set => SetProperty(ref _isLoading, value);
    }

    public string CurrentMonthLabel
    {
        get => _currentMonthLabel;
        set => SetProperty(ref _currentMonthLabel, value);
    }

    public double CurrentMonthExpenses
    {
        get => _currentMonthExpenses;
        set
        {
            if (SetProperty(ref _currentMonthExpenses, value))
            {
                OnPropertyChanged(nameof(CurrentMonthExpensesText));
            }
        }
    }

    public double CurrentMonthIncome
    {
        get => _currentMonthIncome;
        set
        {
            if (SetProperty(ref _currentMonthIncome, value))
            {
                OnPropertyChanged(nameof(CurrentMonthIncomeText));
            }
        }
    }

    public int CurrentMonthTransactionsCount
    {
        get => _currentMonthTransactionsCount;
        set
        {
            if (SetProperty(ref _currentMonthTransactionsCount, value))
            {
                OnPropertyChanged(nameof(CurrentMonthTransactionsCountText));
            }
        }
    }

    public string TopCategoryText
    {
        get => _topCategoryText;
        set => SetProperty(ref _topCategoryText, value);
    }

    public double SalaryCategoryAmount
    {
        get => _salaryCategoryAmount;
        set
        {
            if (SetProperty(ref _salaryCategoryAmount, value))
            {
                OnPropertyChanged(nameof(SalaryCategoryAmountText));
            }
        }
    }

    public string CurrentMonthExpensesText => $"{CurrentMonthExpenses:F2} EUR";
    public string CurrentMonthIncomeText => $"{CurrentMonthIncome:F2} EUR";
    public string CurrentMonthTransactionsCountText => CurrentMonthTransactionsCount.ToString(CultureInfo.InvariantCulture);
    public string SalaryCategoryAmountText => $"{SalaryCategoryAmount:F2} EUR";
    public string TopCategoriesSummaryText
    {
        get => _topCategoriesSummaryText;
        set => SetProperty(ref _topCategoriesSummaryText, value);
    }
    public string MonthlyTrendSummaryText
    {
        get => _monthlyTrendSummaryText;
        set => SetProperty(ref _monthlyTrendSummaryText, value);
    }

    public StatisticsPage()
    {
        InitializeComponent();
        BindingContext = this;
        _currentUserEmail = Preferences.Default.Get("userEmail", "");
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        _currentUserEmail = Preferences.Default.Get("userEmail", "");
        await LoadStatisticsAsync();
    }

    private async Task LoadStatisticsAsync()
    {
        if (string.IsNullOrWhiteSpace(_currentUserEmail))
        {
            return;
        }

        IsLoading = true;
        try
        {
            var now = DateTime.Now;
            CurrentMonthLabel = now.ToString("MMMM yyyy", CultureInfo.CurrentCulture);

            List<Transaction> transactions;
            try
            {
                transactions = await DbService.GetTransactionsAsync(_currentUserEmail);
            }
            catch
            {
                transactions = new List<Transaction>();
            }

            var monthTransactions = transactions
                .Where(t => t.Date.Year == now.Year && t.Date.Month == now.Month)
                .ToList();
            var monthExpenseTransactions = monthTransactions
                .Where(t => !IsIncomeTransaction(t))
                .ToList();
            var monthIncomeTransactions = monthTransactions
                .Where(IsIncomeTransaction)
                .ToList();

            CurrentMonthTransactionsCount = monthTransactions.Count;
            CurrentMonthExpenses = monthExpenseTransactions.Sum(t => Math.Abs(t.Amount));
            // Revenus = table revenu + transactions detectees comme salaire/revenu.
            double revenusFromTable = 0;
            try
            {
                revenusFromTable = await DbService.GetTotalRevenusAsync(_currentUserEmail, now);
            }
            catch
            {
                revenusFromTable = 0;
            }
            var revenusFromTransactions = monthIncomeTransactions.Sum(t => Math.Abs(t.Amount));
            CurrentMonthIncome = revenusFromTable + revenusFromTransactions;
            SalaryCategoryAmount = monthTransactions
                .Where(IsIncomeTransaction)
                .Sum(t => Math.Abs(t.Amount));

            // Top categories: utiliser toutes les transactions du mois
            // pour eviter un ecran vide si les donnees sont principalement "salaire/revenu".
            var grouped = monthTransactions
                .GroupBy(t => string.IsNullOrWhiteSpace(t.Category) ? "Autres" : t.Category.Trim())
                .Select(g => new
                {
                    Name = g.Key,
                    Amount = g.Sum(x => Math.Abs(x.Amount))
                })
                .OrderByDescending(x => x.Amount)
                .ToList();

            TopCategoryText = grouped.FirstOrDefault() is { } top
                ? $"{top.Name} ({top.Amount:F2} EUR)"
                : "Aucune";

            TopCategoriesSummaryText = grouped.Count == 0
                ? "Aucune catégorie pour le mois en cours."
                : string.Join("\n", grouped.Take(5).Select((x, i) => $"{i + 1}. {x.Name}: {x.Amount:F2} EUR"));

            var months = Enumerable.Range(0, 6)
                .Select(i => new DateTime(now.Year, now.Month, 1).AddMonths(-5 + i))
                .ToList();

            var perMonth = months.Select(monthStart =>
            {
                var amount = transactions
                    .Where(t => t.Date.Year == monthStart.Year && t.Date.Month == monthStart.Month)
                    .Sum(t => Math.Abs(t.Amount));
                return new
                {
                    Month = monthStart,
                    Amount = amount
                };
            }).ToList();

            MonthlyTrend.Clear();
            var maxMonthly = perMonth.Count > 0 ? perMonth.Max(x => x.Amount) : 0d;
            const double maxMonthBarWidth = 220d;
            foreach (var item in perMonth)
            {
                var progress = maxMonthly > 0 ? Math.Min(item.Amount / maxMonthly, 1d) : 0d;
                MonthlyTrend.Add(new MonthlyTrendItem
                {
                    MonthShort = item.Month.ToString("MMM", CultureInfo.CurrentCulture),
                    Amount = item.Amount,
                    Progress = progress,
                    BarWidth = progress * maxMonthBarWidth
                });
            }

            MonthlyTrendSummaryText = perMonth.Count == 0
                ? "Aucune donnée sur les 6 derniers mois."
                : string.Join("\n", perMonth.Select(x => $"{x.Month:MMM yyyy}: {x.Amount:F2} EUR"));
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur LoadStatisticsAsync: {ex.Message}");
            // Fallback visuel pour eviter un ecran vide en cas d'erreur partielle.
            TopCategoriesSummaryText = "Impossible de calculer les categories (voir logs).";
            MonthlyTrendSummaryText = "Impossible de calculer l'evolution (voir logs).";
        }
        finally
        {
            IsLoading = false;
        }
    }

    // Navigation vers les autres pages
    private async void OnHomeTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnTransactionsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TransactionsPage");
    }

    private void OnStatisticsTapped(object sender, EventArgs e)
    {
        // Page active
    }

    private async void OnBudgetTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//BudgetPage");
    }

    public new event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(backingStore, value))
        {
            return false;
        }

        backingStore = value;
        OnPropertyChanged(propertyName);
        return true;
    }

    private static string GetCategoryIcon(string category)
    {
        var c = (category ?? string.Empty).ToLowerInvariant();
        if (c.Contains("logement") || c.Contains("loyer")) return "🏠";
        if (c.Contains("alimentation") || c.Contains("courses") || c.Contains("resto")) return "🍽️";
        if (c.Contains("transport") || c.Contains("essence") || c.Contains("navigo")) return "🚗";
        if (c.Contains("facture") || c.Contains("edf") || c.Contains("eau")) return "💡";
        if (c.Contains("sante")) return "⚕️";
        if (c.Contains("loisir")) return "🎮";
        return "📦";
    }

    private static bool IsIncomeTransaction(Transaction t)
    {
        var title = ((string?)t.Title ?? string.Empty).Trim();
        var category = ((string?)t.Category ?? string.Empty).Trim();
        var normalizedTitle = title.ToLowerInvariant();
        var normalizedCategory = category.ToLowerInvariant();

        // Cas demande par l'utilisateur: description de type "Salaire - Autres".
        // On reconstruit un equivalent de description a partir des champs parsés.
        var normalizedDescriptionLike = $"{normalizedTitle} - {normalizedCategory}";

        return normalizedTitle.Contains("salaire")
               || normalizedTitle.Contains("revenu")
               || normalizedCategory.Contains("salaire")
               || normalizedCategory.Contains("revenu")
               || normalizedDescriptionLike.Contains("salaire -");
    }
}

public class CategoryStatItem
{
    public string Name { get; set; } = string.Empty;
    public string Icon { get; set; } = "📦";
    public double Amount { get; set; }
    public string AmountText => $"{Amount:F2} EUR";
    public double Progress { get; set; }
    public string ProgressText => $"{Progress * 100:F0}%";
    public double SharePercentage { get; set; }
    public string ShareText => $"{SharePercentage:F0}% des depenses";
    public double BarWidth { get; set; }
}

public class MonthlyTrendItem
{
    public string MonthShort { get; set; } = string.Empty;
    public double Amount { get; set; }
    public string AmountText => $"{Amount:F2} EUR";
    public double Progress { get; set; }
    public double BarWidth { get; set; }
}
