namespace Projet_Budget_M1.Models;

public class Attribuer
{
    public int IdCategorie { get; set; }
    public int IdBudget { get; set; }
    
    // Navigation properties
    public Categorie? Categorie { get; set; }
    public BudgetMensuel? BudgetMensuel { get; set; }
}

