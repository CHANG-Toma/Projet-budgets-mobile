using System;

namespace Projet_Budget_M1.Models;

public class BudgetMensuel
{
    public string IdBudget { get; set; } = string.Empty;
    public DateTime Mois { get; set; }
    public int Limite { get; set; }
    public int IdUtilisateur { get; set; }
    
    // Navigation property
    public Utilisateur? Utilisateur { get; set; }
}

