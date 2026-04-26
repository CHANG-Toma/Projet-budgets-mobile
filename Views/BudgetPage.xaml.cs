using Microsoft.Maui.Controls;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Services;
using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1.Views;

public partial class BudgetPage : ContentPage
{
    private readonly BudgetViewModel _viewModel;

    public BudgetPage()
    {
        InitializeComponent();
        _viewModel = new BudgetViewModel();
        BindingContext = _viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.InitializeAsync();
        await RefreshRecordedBudgetDisplayAsync();
    }

    private async void OnAddCategoryClicked(object sender, EventArgs e)
    {
        var result = await DisplayPromptAsync(
            "Nouvelle catégorie",
            "Entrez le nom de la catégorie:",
            "Créer",
            "Annuler",
            placeholder: "Ex: Logement, Transport..."
        );

        if (!string.IsNullOrWhiteSpace(result))
        {
            await _viewModel.AddCategoryAsync(result.Trim());
        }
    }

    private async void OnSetMonthlyBudgetClicked(object sender, EventArgs e)
    {
        try
        {
            var placeholder = _viewModel.TotalBudget > 0
                ? _viewModel.TotalBudget.ToString("F2")
                : "0";

            var result = await DisplayPromptAsync(
                "Budget mensuel",
                "Entrez le budget total du mois:",
                "Enregistrer",
                "Annuler",
                placeholder: placeholder,
                keyboard: Keyboard.Numeric
            );

            if (string.IsNullOrWhiteSpace(result))
            {
                return;
            }

            var normalized = result.Trim().Replace(" ", "").Replace(",", ".");
            if (!double.TryParse(
                    normalized,
                    System.Globalization.NumberStyles.Float,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out var newLimit))
            {
                await DisplayAlert("Saisie invalide", "Entrez un montant valide (ex: 250 ou 250,50).", "OK");
                return;
            }

            if (newLimit < 0)
            {
                await DisplayAlert("Saisie invalide", "Le budget ne peut pas être négatif.", "OK");
                return;
            }

            await _viewModel.UpdateMonthlyBudgetAsync(newLimit);
            await RefreshRecordedBudgetDisplayAsync();
            await DisplayAlert("Succès", "Budget mensuel enregistré.", "OK");
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur OnSetMonthlyBudgetClicked: {ex.Message}");
            System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
            await DisplayAlert("Erreur", $"Impossible de modifier le budget: {ex.Message}", "OK");
        }
    }

    private async Task RefreshRecordedBudgetDisplayAsync()
    {
        try
        {
            var email = Preferences.Default.Get("userEmail", string.Empty);
            if (string.IsNullOrWhiteSpace(email))
            {
                return;
            }

            // Meme logique que l'accueil pour le mois courant.
            var budgetLimit = await DbService.GetCurrentMonthBudgetLimitAsync(email) ?? 0m;
            var transactions = await DbService.GetTransactionsAsync(email);
            var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var nextMonth = currentMonth.AddMonths(1);

            decimal spent = 0m;
            foreach (var transaction in transactions)
            {
                if (transaction.Date >= currentMonth && transaction.Date < nextMonth && transaction.Amount < 0)
                {
                    spent += (decimal)Math.Abs(transaction.Amount);
                }
            }

            // Force l'affichage des valeurs enregistrees.
            BudgetTotalValueLabel.Text = $"{budgetLimit:F2} €";
            BudgetSpentValueLabel.Text = $"{spent:F2} €";

            // Maintient aussi le ViewModel synchronise.
            _viewModel.TotalBudget = (double)budgetLimit;
            _viewModel.TotalSpent = (double)spent;
            _viewModel.Remaining = _viewModel.TotalBudget - _viewModel.TotalSpent;
            _viewModel.ProgressPercentage = _viewModel.TotalBudget > 0
                ? (_viewModel.TotalSpent / _viewModel.TotalBudget) * 100
                : 0;
            _viewModel.ProgressBarWidth = _viewModel.TotalBudget > 0
                ? Math.Min((_viewModel.TotalSpent / _viewModel.TotalBudget) * 300, 300)
                : 0;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"RefreshRecordedBudgetDisplayAsync error: {ex.Message}");
        }
    }

    // Navigation Menu
    private async void OnHomeTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//DashboardPage");
    }

    private async void OnTransactionsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//TransactionsPage");
    }

    private async void OnStatisticsTapped(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("//StatisticsPage");
    }

    private void OnBudgetTapped(object sender, EventArgs e)
    {
        // Déjà sur la page Budget
    }
}
