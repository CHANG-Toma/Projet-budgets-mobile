using System;
using System.Threading.Tasks;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class RegisterPage : ContentPage
    {
        public RegisterPage()
        {
            InitializeComponent();
        }

        private async void OnRegisterClicked(object sender, EventArgs e)
        {
            var fullName = FullNameEntry.Text?.Trim() ?? string.Empty;
            var email = EmailEntry.Text?.Trim() ?? string.Empty;
            var password = PasswordEntry.Text ?? string.Empty;

            if (string.IsNullOrWhiteSpace(fullName) || string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                await DisplayAlert("Erreur", "Veuillez remplir tous les champs", "OK");
                return;
            }

            if (!IsValidEmail(email))
            {
                await DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            if (password.Length < 6)
            {
                await DisplayAlert("Erreur", "Le mot de passe doit contenir au moins 6 caractères", "OK");
                return;
            }

            try
            {
                var result = await DbService.RegisterUserAsync(fullName, email, password);

                if (result.EmailExists)
                {
                    await DisplayAlert("Erreur", result.ErrorMessage ?? "Cette adresse email est déjà utilisée", "OK");
                    return;
                }

                if (!result.IsSuccess)
                {
                    await DisplayAlert("Erreur", result.ErrorMessage ?? "Une erreur s'est produite lors de l'inscription", "OK");
                    return;
                }

                await DisplayAlert("Succès", $"Inscription réussie pour {fullName} ({email})", "OK");
<<<<<<< HEAD
                await Navigation.PushAsync(new LoginPage());
=======

                // Naviguer vers la page d'accueil après inscription réussie
                Application.Current!.Windows[0].Page = new AppShell();
>>>>>>> 43361390fb5f57f4fc27263a5a4514720fda8168
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Erreur d'inscription: {ex.Message}", "OK");
            }
        }

        private async void OnLoginTapped(object sender, EventArgs e)
        {
            await Navigation.PopAsync();
        }

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
