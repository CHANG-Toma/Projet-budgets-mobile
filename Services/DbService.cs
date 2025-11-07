using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using MySqlConnector;
using Projet_Budget_M1.Models;

namespace Projet_Budget_M1.Services;

public static class DbService
{
    // Connexion locale XAMPP (utilisateur root sans mot de passe).
    private const string ConnectionString = "Server=localhost;Port=3306;Database=projet_budgets;User Id=root;Password=;SslMode=None;AllowPublicKeyRetrieval=True;";

    private static async Task<MySqlConnection> OpenConnectionAsync()
    {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }

    public static async Task<UserAccount?> ValidateCredentialsAsync(string email, string password)
    {
        var user = await GetUserByEmailAsync(email);
        if (user is null)
        {
            return null;
        }

        return BCrypt.Net.BCrypt.Verify(password, user.PasswordHash) ? user : null;
    }

    public static async Task<RegistrationResult> RegisterUserAsync(string fullName, string email, string password)
    {
        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
        {
            return RegistrationResult.Failed("L'email et le mot de passe sont requis.");
        }

        var existing = await GetUserByEmailAsync(email);
        if (existing is not null)
        {
            return RegistrationResult.EmailAlreadyExists();
        }

        var passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

        // Séparer le nom complet en prénom et nom
        var prenom = string.Empty;
        var nom = string.Empty;
        if (!string.IsNullOrWhiteSpace(fullName))
        {
            var parts = fullName.Trim().Split(' ', 2);
            prenom = parts[0];
            nom = parts.Length > 1 ? parts[1] : string.Empty;
        }
        else
        {
            // Si pas de nom complet, utiliser l'email comme prénom
            prenom = email.Split('@')[0];
        }

        try
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO utilisateur (nom, prénom, email, mot_de_passe, date_inscription) VALUES (@nom, @prenom, @email, @mot_de_passe, @date_inscription)";
            command.Parameters.AddWithValue("@nom", nom);
            command.Parameters.AddWithValue("@prenom", prenom);
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@mot_de_passe", passwordHash);
            command.Parameters.AddWithValue("@date_inscription", DateTime.Now);

            var rows = await command.ExecuteNonQueryAsync();
            return rows == 1 ? RegistrationResult.Success() : RegistrationResult.Failed("Aucune ligne affectée lors de l'inscription.");
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            return RegistrationResult.EmailAlreadyExists();
        }
        catch (Exception ex)
        {
            return RegistrationResult.Failed($"Erreur lors de l'inscription: {ex.Message}");
        }
    }

    public static async Task<UserAccount?> GetUserByEmailAsync(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return null;
        }

        try
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "SELECT id_utilisateur, nom, prénom, email, mot_de_passe, date_inscription FROM utilisateur WHERE email = @email LIMIT 1";
            command.Parameters.AddWithValue("@email", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserAccount
                {
                    IdUtilisateur = reader.GetInt32(0),
                    Nom = reader.IsDBNull(1) ? string.Empty : reader.GetString(1),
                    Prenom = reader.IsDBNull(2) ? string.Empty : reader.GetString(2),
                    Email = reader.GetString(3),
                    MotDePasse = reader.GetString(4),
                    DateInscription = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5),
                    UpdatedAt = DateTime.Now // Pas de colonne updated_at dans le schéma, on utilise la date actuelle
                };
            }
        }
        catch
        {
            // Ignored – caller will interpret null as "user introuvable ou erreur".
        }

        return null;
    }

    public static async Task<(bool IsHealthy, string? ErrorMessage)> TestConnectionAsync()
    {
        try
        {
            await using var connection = await OpenConnectionAsync();
            await connection.PingAsync();
            return (true, null);
        }
        catch (Exception ex)
        {
            return (false, ex.Message);
        }
    }

    public static async Task<bool> UpdateUserFullNameAsync(string email, string newFullName)
    {
        if (string.IsNullOrWhiteSpace(email)) return false;
        
        // Séparer le nom complet en prénom et nom
        var parts = newFullName.Trim().Split(' ', 2);
        var prenom = parts[0];
        var nom = parts.Length > 1 ? parts[1] : string.Empty;
        
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE utilisateur SET prénom=@prenom, nom=@nom WHERE email=@email";
        cmd.Parameters.AddWithValue("@prenom", prenom);
        cmd.Parameters.AddWithValue("@nom", nom);
        cmd.Parameters.AddWithValue("@email", email);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public static async Task<int> AddOrUpdateTransactionAsync(Transaction tx)
    {
        await using var connection = await OpenConnectionAsync();
        
        // Récupérer l'ID utilisateur à partir de l'email
        var user = await GetUserByEmailAsync(tx.OwnerEmail);
        if (user == null) throw new Exception("Utilisateur introuvable");
        
        // Valeurs par défaut pour id_moyen (1 = Espèces par défaut) et id_budget (générique)
        int idMoyen = 1;
        string idBudget = $"BUDGET_{DateTime.Now:yyyyMM}"; // Budget mensuel auto
        
        // Créer ou récupérer le moyen de paiement par défaut
        await using var cmdMoyen = connection.CreateCommand();
        cmdMoyen.CommandText = "INSERT IGNORE INTO moyenpaiement (id_moyen, libelle, id_utilisateur) VALUES (1, 'Espèces', @id_utilisateur)";
        cmdMoyen.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        await cmdMoyen.ExecuteNonQueryAsync();
        
        // Créer ou récupérer le budget mensuel
        await using var cmdBudget = connection.CreateCommand();
        cmdBudget.CommandText = "INSERT IGNORE INTO budgetmensuel (id_budget, mois, limite, id_utilisateur) VALUES (@id_budget, @mois, NULL, @id_utilisateur)";
        cmdBudget.Parameters.AddWithValue("@id_budget", idBudget);
        cmdBudget.Parameters.AddWithValue("@mois", new DateTime(tx.Date.Year, tx.Date.Month, 1));
        cmdBudget.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        await cmdBudget.ExecuteNonQueryAsync();
        
        await using var cmd = connection.CreateCommand();
        if (tx.Id == 0)
        {
            // Pour une nouvelle transaction, ne pas spécifier id_depense pour laisser MySQL l'auto-incrémenter
            cmd.CommandText = @"INSERT INTO depense (date_depense, montant, description, id_moyen, id_budget, id_utilisateur) 
                                VALUES (@date_depense, @montant, @description, @id_moyen, @id_budget, @id_utilisateur); 
                                SELECT LAST_INSERT_ID();";
        }
        else
        {
            cmd.CommandText = @"UPDATE depense 
                                SET date_depense=@date_depense, montant=@montant, description=@description 
                                WHERE id_depense=@id AND id_utilisateur=@id_utilisateur; 
                                SELECT @id;";
            cmd.Parameters.AddWithValue("@id", tx.Id);
        }
        
        cmd.Parameters.AddWithValue("@date_depense", tx.Date);
        cmd.Parameters.AddWithValue("@montant", tx.Amount);
        cmd.Parameters.AddWithValue("@description", $"{tx.Title} - {tx.Category}");
        cmd.Parameters.AddWithValue("@id_moyen", idMoyen);
        cmd.Parameters.AddWithValue("@id_budget", idBudget);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);

        try
        {
            var result = await cmd.ExecuteScalarAsync();
            var id = Convert.ToInt32(result);
            
            // Si LAST_INSERT_ID() retourne 0 et qu'on fait un INSERT, cela signifie que AUTO_INCREMENT n'est pas activé
            if (id == 0 && tx.Id == 0)
            {
                throw new Exception("La table 'depense' doit avoir AUTO_INCREMENT activé sur 'id_depense'. Exécutez: ALTER TABLE depense MODIFY id_depense int(11) NOT NULL AUTO_INCREMENT;");
            }
            
            return id;
        }
        catch (MySqlException ex) when (ex.Number == 1062)
        {
            throw new Exception("Erreur: Une transaction avec cet ID existe déjà. La table 'depense' doit avoir AUTO_INCREMENT activé. Exécutez: ALTER TABLE depense MODIFY id_depense int(11) NOT NULL AUTO_INCREMENT;");
        }
    }

    public static async Task<bool> DeleteTransactionAsync(int id, string ownerEmail)
    {
        await using var connection = await OpenConnectionAsync();
        
        // Récupérer l'ID utilisateur à partir de l'email
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) return false;
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM depense WHERE id_depense=@id AND id_utilisateur=@id_utilisateur";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public static async Task<List<Transaction>> GetTransactionsAsync(string ownerEmail, string? search = null)
    {
        var list = new List<Transaction>();
        await using var connection = await OpenConnectionAsync();
        
        // Récupérer l'ID utilisateur à partir de l'email
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) return list;
        
        await using var cmd = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText = @"SELECT d.id_depense, d.description, d.montant, d.date_depense, d.id_utilisateur
                                FROM depense d
                                WHERE d.id_utilisateur=@id_utilisateur 
                                ORDER BY d.date_depense DESC, d.id_depense DESC";
        }
        else
        {
            cmd.CommandText = @"SELECT d.id_depense, d.description, d.montant, d.date_depense, d.id_utilisateur
                                FROM depense d
                                WHERE d.id_utilisateur=@id_utilisateur 
                                AND d.description LIKE @q 
                                ORDER BY d.date_depense DESC, d.id_depense DESC";
            cmd.Parameters.AddWithValue("@q", $"%{search}%");
        }
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var description = reader.IsDBNull(1) ? "" : reader.GetString(1);
            var parts = description.Split(" - ", 2);
            var title = parts.Length > 0 ? parts[0] : description;
            var category = parts.Length > 1 ? parts[1] : "Général";
            
            list.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Title = title,
                Amount = reader.GetDouble(2),
                Date = reader.GetDateTime(3),
                Category = category,
                OwnerEmail = ownerEmail,
                IdUtilisateur = reader.GetInt32(4)
            });
        }
        return list;
    }

    public static async Task<decimal?> GetCurrentMonthBudgetLimitAsync(string ownerEmail)
    {
        await using var connection = await OpenConnectionAsync();
        
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) return null;
        
        var currentMonth = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT limite FROM budgetmensuel 
                            WHERE id_utilisateur=@id_utilisateur 
                            AND mois=@mois 
                            LIMIT 1";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@mois", currentMonth);
        
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            return null;
        
        return Convert.ToDecimal(result);
    }
}

public readonly record struct RegistrationResult(bool IsSuccess, string? ErrorMessage, bool EmailExists)
{
    public static RegistrationResult Success() => new(true, null, false);
    public static RegistrationResult EmailAlreadyExists() => new(false, "Cette adresse email est déjà utilisée.", true);
    public static RegistrationResult Failed(string? errorMessage) => new(false, errorMessage, false);
}

