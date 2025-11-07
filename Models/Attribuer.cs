namespace Projet_Budget_M1.Models;

public class Attribuer
{
    public int IdCategorie { get; set; }
    public string IdBudget { get; set; } = string.Empty;
    
    // Navigation properties
    public Categorie? Categorie { get; set; }
    public BudgetMensuel? BudgetMensuel { get; set; }
}

