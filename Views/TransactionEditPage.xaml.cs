using System;
using Projet_Budget_M1.Models;
using Projet_Budget_M1.Services;

namespace Projet_Budget_M1.Views
{
    public partial class TransactionEditPage : ContentPage
    {
        private readonly string _ownerEmail;
        private Transaction _model;

        public event EventHandler? Saved;

        public TransactionEditPage(string ownerEmail, Transaction? tx)
        {
            try
            {
                InitializeComponent();
                _ownerEmail = ownerEmail;
                _model = tx != null ? new Transaction
                {
                    Id = tx.Id,
                    Title = tx.Title,
                    Amount = tx.Amount,
                    Date = tx.Date,
                    Category = tx.Category,
                    OwnerEmail = tx.OwnerEmail
                } : new Transaction { Date = DateTime.Today, OwnerEmail = ownerEmail };

                TitleEntry.Text = _model.Title ?? string.Empty;
                AmountEntry.Text = _model.Id == 0 ? string.Empty : _model.Amount.ToString(System.Globalization.CultureInfo.InvariantCulture);
                DatePicker.Date = _model.Date;
                CategoryEntry.Text = _model.Category ?? string.Empty;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur dans TransactionEditPage constructor: {ex}");
                throw;
            }
        }

        private async void OnSaveClicked(object sender, EventArgs e)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(TitleEntry.Text) || string.IsNullOrWhiteSpace(AmountEntry.Text))
                {
                    await DisplayAlert("Erreur", "Titre et montant sont requis", "OK");
                    return;
                }

                if (!double.TryParse(AmountEntry.Text!.Replace(",", "."), System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out var amount))
                {
                    await DisplayAlert("Erreur", "Montant invalide", "OK");
                    return;
                }

                _model.Title = TitleEntry.Text!.Trim();
                _model.Amount = amount;
                _model.Date = DatePicker.Date;
                _model.Category = CategoryEntry.Text?.Trim() ?? string.Empty;
                _model.OwnerEmail = _ownerEmail;

                var id = await DbService.AddOrUpdateTransactionAsync(_model);
                _model.Id = id;
                await DisplayAlert("Succès", "Transaction enregistrée", "OK");
                Saved?.Invoke(this, EventArgs.Empty);
                await Navigation.PopAsync();
            }
            catch (Exception ex)
            {
                await DisplayAlert("Erreur", $"Échec de l'enregistrement: {ex.Message}", "OK");
            }
        }
    }
}


