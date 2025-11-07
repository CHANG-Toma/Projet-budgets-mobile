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
            await _viewModel.LoadAsync();
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
