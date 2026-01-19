using System;

namespace Projet_Budget_M1.Models;

public class BudgetMensuel
{
    public int IdBudget { get; set; }
    public DateTime Mois { get; set; }
    public decimal? Limite { get; set; }
    public int IdUtilisateur { get; set; }
    
    // Navigation property
    public Utilisateur? Utilisateur { get; set; }
    
    // Propriété pour l'affichage
    public string DisplayText => $"{Mois:MMMM yyyy}" + (Limite.HasValue ? $" - {Limite:F2} €" : "");
}

