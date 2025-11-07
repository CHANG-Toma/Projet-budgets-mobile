using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.Windows.Input;
using Projet_Budget_M1.Commands;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _email = string.Empty;
        private string _password = string.Empty;
        private bool _isLoading;

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public bool IsLoading
        {
            get => _isLoading;
            set => SetProperty(ref _isLoading, value);
        }

        public ICommand LoginCommand { get; }
        public ICommand SignUpCommand { get; }

        public LoginViewModel()
        {
            LoginCommand = new RelayCommand(async () => await ExecuteLoginAsync(), CanExecuteLogin);
            SignUpCommand = new RelayCommand(async () => await ExecuteSignUpAsync());
        }

        private async Task ExecuteLoginAsync()
        {
            if (string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Veuillez remplir tous les champs", "OK");
                return;
            }

            if (!IsValidEmail(Email))
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Veuillez entrer une adresse email valide", "OK");
                return;
            }

            IsLoading = true;

            try
            {
                var user = await DbService.ValidateCredentialsAsync(Email.Trim(), Password);

                if (user is null)
                {
                    await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", "Identifiants invalides", "OK");
                    return;
                }

                await Application.Current!.Windows[0].Page!.DisplayAlert("Succès", $"Connexion réussie pour {user.FullName ?? user.Email}", "OK");

                // TODO: naviguer vers la page principale ou charger les données utilisateur.
            }
            catch (Exception ex)
            {
                await Application.Current!.Windows[0].Page!.DisplayAlert("Erreur", $"Erreur de connexion: {ex.Message}", "OK");
            }
            finally
            {
                IsLoading = false;
            }
        }

        private async Task ExecuteSignUpAsync()
        {
            await Application.Current!.Windows[0].Page!.Navigation.PushAsync(new Views.RegisterPage());
        }

        private bool CanExecuteLogin()
        {
            return !IsLoading && !string.IsNullOrWhiteSpace(Email) && !string.IsNullOrWhiteSpace(Password);
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

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T backingStore, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(backingStore, value))
                return false;

            backingStore = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
