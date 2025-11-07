using System;
using System.Collections.ObjectModel;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly ObservableCollection<Transaction> _items = new();
        private string _currentUserEmail = string.Empty; // TODO: à remplacer par l'email de l'utilisateur connecté

        public TransactionsPage()
        {
            InitializeComponent();
            TransactionsList.BindingContext = _items;
            // TODO: définir l'email de l'utilisateur connecté
            _currentUserEmail = Preferences.Default.Get("userEmail", "");
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            await LoadAsync();
        }

        private async Task LoadAsync(string? search = null)
        {
            if (string.IsNullOrWhiteSpace(_currentUserEmail)) return;
            var data = await DbService.GetTransactionsAsync(_currentUserEmail, search);
            _items.Clear();
            foreach (var t in data) _items.Add(t);
        }

        private async void OnAddClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserEmail))
                {
                    await DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                    return;
                }
                var page = new TransactionEditPage(_currentUserEmail, null);
                page.Saved += async (_, __) => await LoadAsync(SearchEntry.Text);
                await Navigation.PushAsync(page);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible d'ouvrir la page: {ex.Message}", "OK");
            }
        }

        private async void OnEditClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_currentUserEmail))
                {
                    await DisplayAlert("Information", "Utilisateur non défini. Connectez-vous d'abord.", "OK");
                    return;
                }
                if ((sender as Button)?.CommandParameter is not Transaction tx) return;
                var page = new TransactionEditPage(_currentUserEmail, tx);
                page.Saved += async (_, __) => await LoadAsync(SearchEntry.Text);
                await Navigation.PushAsync(page);
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Impossible d'ouvrir la page: {ex.Message}", "OK");
            }
        }

        private async void OnDeleteClicked(object sender, EventArgs e)
        {
            if ((sender as Button)?.CommandParameter is not Transaction tx) return;
            var confirm = await DisplayAlert("Confirmer", $"Supprimer '{tx.Title}' ?", "Supprimer", "Annuler");
            if (!confirm) return;
            await DbService.DeleteTransactionAsync(tx.Id, _currentUserEmail);
            await LoadAsync(SearchEntry.Text);
        }

        private async void OnSearchChanged(object sender, TextChangedEventArgs e)
        {
            await LoadAsync(e.NewTextValue);
        }

        // Navigation vers les autres pages
        private async void OnHomeClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//DashboardPage");
        }

        private async void OnTransactionsClicked(object sender, EventArgs e)
        {
            await LoadAsync(SearchEntry.Text);
        }

        private async void OnStatisticsClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//StatisticsPage");
        }

        private async void OnBudgetClicked(object sender, EventArgs e)
        {
            await Shell.Current.GoToAsync("//BudgetPage");
        }
    }
}
