namespace Projet_Budget_M1.Models;

/// <summary>
/// Représente un budget pour une catégorie spécifique (table attribuer)
/// </summary>
public class BudgetCategorie
{
    public int IdCategorie { get; set; }
    public int IdBudget { get; set; }
    public double LimiteCategorie { get; set; }
    
    // Propriétés additionnelles pour l'affichage
    public string NomCategorie { get; set; } = string.Empty;
    public double Depense { get; set; }
    public double Restant => LimiteCategorie - Depense;
    public double Pourcentage => LimiteCategorie > 0 ? (Depense / LimiteCategorie) * 100 : 0;
    
    // Pour l'icône et la couleur
    public string Icone { get; set; } = "📊";
    public string Couleur { get; set; } = "#3498DB";
}

