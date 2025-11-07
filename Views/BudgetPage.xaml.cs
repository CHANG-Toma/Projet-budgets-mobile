using Microsoft.Maui.Controls;
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
            _viewModel.AddCategoryCommand.Execute(result.Trim());
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
