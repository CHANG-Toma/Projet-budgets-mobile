using System;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class ProfilePage : ContentPage
    {
        private string _userEmail = string.Empty;

        public ProfilePage()
        {
            InitializeComponent();
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();
            _userEmail = Preferences.Default.Get("userEmail", string.Empty);
            if (!string.IsNullOrWhiteSpace(_userEmail))
            {
                var user = await DbService.GetUserByEmailAsync(_userEmail);
                if (user != null)
                {
                    FullNameEntry.Text = user.FullName ?? string.Empty;
                    EmailLabel.Text = user.Email;
                    CreatedAtLabel.Text = user.CreatedAt.ToString("dd/MM/yyyy à HH:mm");
                }
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(_userEmail))
            {
                await DisplayAlert("Erreur", "Utilisateur non connecté", "OK");
                return;
            }

            var newName = FullNameEntry.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrWhiteSpace(newName))
            {
                await DisplayAlert("Erreur", "Le nom ne peut pas être vide", "OK");
                return;
            }

            try
            {
                var ok = await DbService.UpdateUserFullNameAsync(_userEmail, newName);
                if (ok)
                {
                    await DisplayAlert("Succès", "Nom mis à jour", "OK");
                }
                else
                {
                    await DisplayAlert("Erreur", "Impossible de mettre à jour le nom", "OK");
                }
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur: {ex.Message}", "OK");
            }
        }

        private async void OnLogoutClicked(object sender, EventArgs e)
        {
            var confirm = await DisplayAlert("Déconnexion", "Voulez-vous vraiment vous déconnecter ?", "Oui", "Non");
            if (confirm)
            {
                Preferences.Default.Remove("userEmail");
                Application.Current!.Windows[0].Page = new NavigationPage(new LoginPage());
            }
        }
    }
}

