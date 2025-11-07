namespace Projet_Budget_M1.Models;

public class Categorie
{
    public int IdCategorie { get; set; }
    public string NomCategorie { get; set; } = string.Empty;
    public int IdUtilisateur { get; set; }
    
    // Navigation property
    public Utilisateur? Utilisateur { get; set; }
}

