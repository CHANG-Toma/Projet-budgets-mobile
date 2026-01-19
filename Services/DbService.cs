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

    public static async Task<int> AddOrUpdateTransactionAsync(Transaction tx, int? budgetId = null)
    {
        await using var connection = await OpenConnectionAsync();
        
        // Récupérer l'ID utilisateur à partir de l'email
        var user = await GetUserByEmailAsync(tx.OwnerEmail);
        if (user == null) throw new Exception("Utilisateur introuvable");
        
        // Valeurs par défaut pour id_moyen (1 = Espèces par défaut)
        int idMoyen = 1;
        
        // Créer ou récupérer le moyen de paiement par défaut
        await using var cmdMoyen = connection.CreateCommand();
        cmdMoyen.CommandText = "INSERT IGNORE INTO moyenpaiement (id_moyen, libelle, id_utilisateur) VALUES (1, 'Espèces', @id_utilisateur)";
        cmdMoyen.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        await cmdMoyen.ExecuteNonQueryAsync();
        
        // Gérer le budget
        int idBudget;
        if (budgetId.HasValue)
        {
            // Utiliser le budget fourni
            idBudget = budgetId.Value;
        }
        else
        {
            // Calculer le mois de la transaction (premier jour du mois)
            var moisBudget = new DateTime(tx.Date.Year, tx.Date.Month, 1);
            
            // Vérifier si un budget existe déjà pour ce mois et cet utilisateur
            await using var cmdCheckBudget = connection.CreateCommand();
            cmdCheckBudget.CommandText = "SELECT id_budget FROM budgetmensuel WHERE mois = @mois AND id_utilisateur = @id_utilisateur LIMIT 1";
            cmdCheckBudget.Parameters.AddWithValue("@mois", moisBudget);
            cmdCheckBudget.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            
            var existingBudgetId = await cmdCheckBudget.ExecuteScalarAsync();
            
            if (existingBudgetId != null && existingBudgetId != DBNull.Value)
            {
                // Budget existe déjà, utiliser son ID
                idBudget = Convert.ToInt32(existingBudgetId);
            }
            else
            {
                // Créer un nouveau budget mensuel (laisser MySQL générer l'ID automatiquement)
                await using var cmdBudget = connection.CreateCommand();
                cmdBudget.CommandText = "INSERT INTO budgetmensuel (mois, limite, id_utilisateur) VALUES (@mois, NULL, @id_utilisateur); SELECT LAST_INSERT_ID();";
                cmdBudget.Parameters.AddWithValue("@mois", moisBudget);
                cmdBudget.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
                
                var newBudgetId = await cmdBudget.ExecuteScalarAsync();
                if (newBudgetId == null || newBudgetId == DBNull.Value)
                {
                    throw new Exception("Erreur lors de la création du budget mensuel");
                }
                idBudget = Convert.ToInt32(newBudgetId);
            }
        }
        
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
                                SET date_depense=@date_depense, montant=@montant, description=@description, id_budget=@id_budget
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
        catch (MySqlException ex) when (ex.Number == 1452)
        {
            // Erreur de clé étrangère
            throw new Exception($"Erreur de contrainte de clé étrangère: {ex.Message}");
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
        
        try
        {
            if (string.IsNullOrWhiteSpace(ownerEmail))
            {
                return list;
            }

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
                try
                {
                    // Vérifier que toutes les colonnes nécessaires sont présentes et non null
                    if (reader.IsDBNull(0) || reader.IsDBNull(2) || reader.IsDBNull(3) || reader.IsDBNull(4))
                    {
                        System.Diagnostics.Debug.WriteLine("Transaction ignorée: données manquantes");
                        continue;
                    }

                    var id = reader.GetInt32(0);
                    var description = reader.IsDBNull(1) ? "" : reader.GetString(1);
                    var amount = reader.GetDouble(2);
                    var date = reader.GetDateTime(3);
                    var idUtilisateur = reader.GetInt32(4);
                    
                    // Parser la description
                    string title;
                    string category;
                    
                    if (string.IsNullOrWhiteSpace(description))
                    {
                        title = "Transaction sans titre";
                        category = "Autres";
                    }
                    else
                    {
                        var parts = description.Split(new[] { " - " }, 2, StringSplitOptions.None);
                        title = parts.Length > 0 && !string.IsNullOrWhiteSpace(parts[0]) ? parts[0] : "Transaction sans titre";
                        category = parts.Length > 1 && !string.IsNullOrWhiteSpace(parts[1]) ? parts[1] : "Autres";
                    }
                    
                    // Vérifier que la date est valide
                    if (date == default(DateTime))
                    {
                        date = DateTime.Now;
                    }
                    
                    list.Add(new Transaction
                    {
                        Id = id,
                        Title = title ?? "Transaction sans titre",
                        Amount = amount,
                        Date = date,
                        Category = category ?? "Autres",
                        OwnerEmail = ownerEmail ?? string.Empty,
                        IdUtilisateur = idUtilisateur
                    });
                }
                catch (Exception ex)
                {
                    // Ignorer les lignes problématiques et continuer
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture d'une transaction: {ex.Message}");
                    System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                }
            }
        }
        catch (Exception ex)
        {
            // Log l'erreur et retourner une liste vide plutôt que de crasher
            System.Diagnostics.Debug.WriteLine($"Erreur dans GetTransactionsAsync: {ex.Message}");
            return list;
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

    public static async Task<List<BudgetMensuel>> GetBudgetsAsync(string ownerEmail)
    {
        var list = new List<BudgetMensuel>();
        
        try
        {
            if (string.IsNullOrWhiteSpace(ownerEmail))
                return list;

            await using var connection = await OpenConnectionAsync();
            
            var user = await GetUserByEmailAsync(ownerEmail);
            if (user == null) return list;
            
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT id_budget, mois, limite, id_utilisateur 
                                FROM budgetmensuel 
                                WHERE id_utilisateur=@id_utilisateur 
                                ORDER BY mois DESC";
            cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    list.Add(new BudgetMensuel
                    {
                        IdBudget = reader.GetInt32(0),
                        Mois = reader.GetDateTime(1),
                        Limite = reader.IsDBNull(2) ? null : Convert.ToDecimal(reader.GetValue(2)),
                        IdUtilisateur = reader.GetInt32(3)
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture d'un budget: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur dans GetBudgetsAsync: {ex.Message}");
        }
        
        return list;
    }

    public static async Task<int> CreateBudgetAsync(string ownerEmail, DateTime mois, decimal? limite = null)
    {
        await using var connection = await OpenConnectionAsync();
        
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) throw new Exception("Utilisateur introuvable");
        
        var moisBudget = new DateTime(mois.Year, mois.Month, 1);
        
        // Vérifier si un budget existe déjà pour ce mois
        await using var cmdCheck = connection.CreateCommand();
        cmdCheck.CommandText = "SELECT id_budget FROM budgetmensuel WHERE mois = @mois AND id_utilisateur = @id_utilisateur LIMIT 1";
        cmdCheck.Parameters.AddWithValue("@mois", moisBudget);
        cmdCheck.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var existingId = await cmdCheck.ExecuteScalarAsync();
        if (existingId != null && existingId != DBNull.Value)
        {
            return Convert.ToInt32(existingId);
        }
        
        // Créer un nouveau budget
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO budgetmensuel (mois, limite, id_utilisateur) VALUES (@mois, @limite, @id_utilisateur); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@mois", moisBudget);
        cmd.Parameters.AddWithValue("@limite", limite.HasValue ? (object)limite.Value : DBNull.Value);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
        {
            throw new Exception("Erreur lors de la création du budget");
        }
        
        return Convert.ToInt32(result);
    }
}

public readonly record struct RegistrationResult(bool IsSuccess, string? ErrorMessage, bool EmailExists)
{
    public static RegistrationResult Success() => new(true, null, false);
    public static RegistrationResult EmailAlreadyExists() => new(false, "Cette adresse email est déjà utilisée.", true);
    public static RegistrationResult Failed(string? errorMessage) => new(false, errorMessage, false);
}

