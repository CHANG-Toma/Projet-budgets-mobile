using System;

namespace Projet_Budget_M1.Models;

public class Depense
{
    public int IdDepense { get; set; }
    public DateTime DateDepense { get; set; }
    public int Montant { get; set; }
    public string Description { get; set; } = string.Empty;
    public int IdMoyen { get; set; }
    public string IdBudget { get; set; } = string.Empty;
    public int IdUtilisateur { get; set; }
    
    // Navigation properties
    public MoyenPaiement? MoyenPaiement { get; set; }
    public BudgetMensuel? BudgetMensuel { get; set; }
    public Utilisateur? Utilisateur { get; set; }
}

