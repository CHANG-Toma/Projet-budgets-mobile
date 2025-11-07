using System;

namespace Projet_Budget_M1.Models;

public class Depense
{
    public int IdDepense { get; set; }
    public DateTime DateDepense { get; set; }
    public double Montant { get; set; } // Changé de int à double pour supporter les décimales
    public string Description { get; set; } = string.Empty;
    public int IdMoyen { get; set; }
    public string IdBudget { get; set; } = string.Empty;
    public int IdUtilisateur { get; set; }
    
    // Navigation properties
    public MoyenPaiement? MoyenPaiement { get; set; }
    public BudgetMensuel? BudgetMensuel { get; set; }
    public Utilisateur? Utilisateur { get; set; }
}

