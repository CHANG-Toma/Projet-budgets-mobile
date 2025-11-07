using System;

namespace Projet_Budget_M1.Models;

// Alias pour compatibilité avec le code existant qui utilise Transaction
// Cette classe mappe vers la table depense
public class Transaction
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public double Amount { get; set; }
    public DateTime Date { get; set; }
    public string Category { get; set; } = string.Empty;
    public string OwnerEmail { get; set; } = string.Empty;
    
    // Propriété pour stocker l'ID utilisateur
    public int IdUtilisateur { get; set; }
}


