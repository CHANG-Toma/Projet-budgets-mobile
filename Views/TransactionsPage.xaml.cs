using System;
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
            
            // Attendre que la page soit complètement chargée
            await Task.Delay(50);
            
            try
            {
                if (_viewModel == null)
                {
                    System.Diagnostics.Debug.WriteLine("ViewModel est null dans OnAppearing");
                    return;
                }
                
                // Vérifier que le BindingContext est bien défini
                if (BindingContext == null)
                {
                    System.Diagnostics.Debug.WriteLine("BindingContext est null, réinitialisation...");
                    BindingContext = _viewModel;
                }
                
                await _viewModel.LoadAsync();
            }
            catch (Exception ex)
            {
                // Log l'erreur pour le débogage
                System.Diagnostics.Debug.WriteLine($"Erreur dans OnAppearing: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    System.Diagnostics.Debug.WriteLine($"Inner exception: {ex.InnerException.Message}");
                    System.Diagnostics.Debug.WriteLine($"Inner stack trace: {ex.InnerException.StackTrace}");
                }
                
                // Afficher une alerte à l'utilisateur
                try
                {
                    await DisplayAlert("Erreur", $"Erreur lors du chargement des transactions: {ex.Message}", "OK");
                }
                catch
                {
                    // Ignorer si on ne peut pas afficher l'alerte
                }
            }
        }

        private void OnOverlayTapped(object sender, EventArgs e)
        {
            // Fermer l'overlay seulement si on clique sur le fond (pas sur le formulaire)
            // Le TapGestureRecognizer sur le Frame du formulaire empêchera cette méthode
            // de se déclencher quand on clique sur le formulaire ou ses enfants
            _viewModel.CloseOverlayCommand.Execute(null);
        }

        private void OnFormFrameTapped(object sender, EventArgs e)
        {
            // Cette méthode empêche le TapGestureRecognizer du Grid parent (overlay)
            // de se déclencher quand on clique sur le formulaire ou ses enfants
            // En MAUI, avoir un TapGestureRecognizer sur un enfant empêche le parent
            // de recevoir l'événement, donc cette méthode "consomme" l'événement
        }
    }
}
