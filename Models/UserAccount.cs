using System;

namespace Projet_Budget_M1.Models;

public class Utilisateur
{
    public int IdUtilisateur { get; set; }
    public string Nom { get; set; } = string.Empty;
    public string Prenom { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string MotDePasse { get; set; } = string.Empty;
    public DateTime DateInscription { get; set; }
}

// Alias pour compatibilité avec le code existant
public class UserAccount : Utilisateur
{
    public uint Id 
    { 
        get => (uint)IdUtilisateur; 
        set => IdUtilisateur = (int)value; 
    }
    
    public string? FullName 
    { 
        get => $"{Prenom} {Nom}".Trim(); 
        set 
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                var parts = value.Split(' ', 2);
                Prenom = parts[0];
                Nom = parts.Length > 1 ? parts[1] : string.Empty;
            }
        }
    }
    
    public string PasswordHash 
    { 
        get => MotDePasse; 
        set => MotDePasse = value; 
    }
    
    public DateTime CreatedAt 
    { 
        get => DateInscription; 
        set => DateInscription = value; 
    }
    
    public DateTime UpdatedAt { get; set; }
}

