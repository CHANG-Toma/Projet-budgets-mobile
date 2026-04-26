using System;
using System.Threading.Tasks;
using Microsoft.Maui.Controls;
using Projet_Budget_M1.ViewModels;

namespace Projet_Budget_M1.Views
{
    public partial class TransactionsPage : ContentPage
    {
        private readonly TransactionsViewModel _viewModel;

        public TransactionsPage()
        {
            InitializeComponent();
            _viewModel = new TransactionsViewModel();
            BindingContext = _viewModel;
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            // Petit délai pour laisser l'UI stabiliser les bindings du formulaire.
            await Task.Delay(50);

            try
            {
                if (BindingContext == null)
                {
                    BindingContext = _viewModel;
                }

                await _viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans OnAppearing: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }

                try
                {
                    await DisplayAlert("Erreur", $"Erreur lors du chargement des transactions: {ex.Message}", "OK");
                }
                catch
                {
                    // Ignorer si l'alerte ne peut pas etre affichee.
                }
            }
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            _viewModel.CloseOverlayCommand.Execute(null);
        }

        private void OnFormFrameTapped(object sender, EventArgs e)
        {
            // Consomme le tap pour eviter de fermer l'overlay.
        }
    }
}
