using System;
using System.Threading.Tasks;
using Microsoft.Maui.Storage;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class LoginPage : ContentPage
    {
        public LoginPage()
        {
            // Initialisation de la page de connexion
            InitializeComponent();
        }

        // Fonction pour la connexion utilisateur
        private async void OnLoginClicked(object sender, EventArgs e)
        {
            var email = EmailEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Erreur", "Veuillez entrer votre email et votre mot de passe", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            try
            {
                var user = await DbService.ValidateCredentialsAsync(email, password);
                if (user is null)
            {
                    await DisplayAlert("Erreur", "Identifiants invalides", "OK");
                    return;
                }

                Preferences.Default.Set("userEmail", user.Email);
                Application.Current!.Windows[0].Page = new AppShell();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur de connexion: {ex.Message}", "OK");
            }
        }

        // Fonction pour la navigation vers la page d'inscription
        private async void OnSignUpTapped(object sender, EventArgs e)
        {
            // Navigation vers la page d'inscription
            await Navigation.PushAsync(new RegisterPage());
        }

        // Fonction pour vérifier si l'email est valide
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }
    }
}
