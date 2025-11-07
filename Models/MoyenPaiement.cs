namespace Projet_Budget_M1.Models;

public class MoyenPaiement
{
    public int IdMoyen { get; set; }
    public string Libelle { get; set; } = string.Empty;
    public int IdUtilisateur { get; set; }
    
    // Navigation property
    public Utilisateur? Utilisateur { get; set; }
}

