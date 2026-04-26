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
        
        // Déterminer le budget à utiliser : explicite si fourni, sinon budget du mois.
        int idBudget = budgetId.HasValue
            ? budgetId.Value
            : await GetOrCreateMonthlyBudgetIdAsync(tx.OwnerEmail, tx.Date);
        
        // S'assurer que la catégorie existe dans la table catégorie.
        await GetOrCreateCategoryAsync(tx.OwnerEmail, tx.Category);
        
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
        
        var now = DateTime.Now;
        
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT limite FROM budgetmensuel 
                            WHERE id_utilisateur=@id_utilisateur 
                            AND YEAR(mois)=@year
                            AND MONTH(mois)=@month
                            ORDER BY id_budget DESC
                            LIMIT 1";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@year", now.Year);
        cmd.Parameters.AddWithValue("@month", now.Month);
        
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
            return null;
        
        return Convert.ToDecimal(result);
    }

    public static async Task<decimal?> GetMonthlyBudgetLimitAsync(string ownerEmail, DateTime month)
    {
        await using var connection = await OpenConnectionAsync();

        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) return null;

        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT limite
                            FROM budgetmensuel
                            WHERE id_utilisateur = @id_utilisateur
                              AND YEAR(mois) = @year
                              AND MONTH(mois) = @month
                            ORDER BY id_budget DESC
                            LIMIT 1";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@year", month.Year);
        cmd.Parameters.AddWithValue("@month", month.Month);

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

    public static async Task<int> CreateBudgetAsync(string ownerEmail, DateTime mois, decimal? limite = null, List<int>? categoryIds = null)
    {
        System.Diagnostics.Debug.WriteLine($"CreateBudgetAsync appelé avec limite: {(limite.HasValue ? limite.Value.ToString() : "null")}");
        
        await using var connection = await OpenConnectionAsync();
        
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) throw new Exception("Utilisateur introuvable");
        
        var moisBudget = new DateTime(mois.Year, mois.Month, 1);
        
        // Vérifier si un budget existe déjà pour ce mois (comparaison mois/annee robuste)
        await using var cmdCheck = connection.CreateCommand();
        cmdCheck.CommandText = @"SELECT id_budget
                                 FROM budgetmensuel
                                 WHERE id_utilisateur = @id_utilisateur
                                   AND YEAR(mois) = @year
                                   AND MONTH(mois) = @month
                                 ORDER BY id_budget DESC
                                 LIMIT 1";
        cmdCheck.Parameters.AddWithValue("@year", moisBudget.Year);
        cmdCheck.Parameters.AddWithValue("@month", moisBudget.Month);
        cmdCheck.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var existingId = await cmdCheck.ExecuteScalarAsync();
        int budgetId;
        
        if (existingId != null && existingId != DBNull.Value)
        {
            budgetId = Convert.ToInt32(existingId);
            
            // Si une limite est fournie, mettre à jour le budget existant
            if (limite.HasValue)
            {
                // Convertir decimal en int (la colonne limite est de type int dans la BDD)
                int limiteInt = (int)Math.Round(limite.Value);
                System.Diagnostics.Debug.WriteLine($"Mise à jour du budget {budgetId} avec limite: {limite.Value} (converti en int: {limiteInt})");
                await using var cmdUpdate = connection.CreateCommand();
                cmdUpdate.CommandText = "UPDATE budgetmensuel SET limite = @limite WHERE id_budget = @id_budget";
                var paramLimite = cmdUpdate.Parameters.Add("@limite", MySqlConnector.MySqlDbType.Int32);
                paramLimite.Value = limiteInt;
                cmdUpdate.Parameters.AddWithValue("@id_budget", budgetId);
                var rowsAffected = await cmdUpdate.ExecuteNonQueryAsync();
                System.Diagnostics.Debug.WriteLine($"Mise à jour effectuée: {rowsAffected} ligne(s) affectée(s)");
            }
        }
        else
        {
            // Créer un nouveau budget
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = "INSERT INTO budgetmensuel (mois, limite, id_utilisateur) VALUES (@mois, @limite, @id_utilisateur); SELECT LAST_INSERT_ID();";
            cmd.Parameters.AddWithValue("@mois", moisBudget);
            
            // S'assurer que la limite est correctement passée (convertir decimal en int)
            if (limite.HasValue)
            {
                int limiteInt = (int)Math.Round(limite.Value);
                System.Diagnostics.Debug.WriteLine($"Création d'un budget avec limite: {limite.Value} (converti en int: {limiteInt})");
                var paramLimite = cmd.Parameters.Add("@limite", MySqlConnector.MySqlDbType.Int32);
                paramLimite.Value = limiteInt;
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("Création d'un budget sans limite");
                var paramLimite = cmd.Parameters.Add("@limite", MySqlConnector.MySqlDbType.Int32);
                paramLimite.Value = DBNull.Value;
            }
            
            cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            
            var result = await cmd.ExecuteScalarAsync();
            if (result == null || result == DBNull.Value)
            {
                throw new Exception("Erreur lors de la création du budget");
            }
            
            budgetId = Convert.ToInt32(result);
        }
        
        // Associer les catégories au budget si fournies
        if (categoryIds != null && categoryIds.Count > 0)
        {
            await AssociateCategoriesToBudgetAsync(budgetId, categoryIds);
        }
        
        return budgetId;
    }

    public static async Task<int> CreateCategoryAsync(string ownerEmail, string nomCategorie)
    {
        await using var connection = await OpenConnectionAsync();
        
        var user = await GetUserByEmailAsync(ownerEmail);
        if (user == null) throw new Exception("Utilisateur introuvable");
        
        // Vérifier si la catégorie existe déjà
        await using var cmdCheck = connection.CreateCommand();
        cmdCheck.CommandText = "SELECT id_categorie FROM catégorie WHERE nom_categorie = @nom AND id_utilisateur = @id_utilisateur LIMIT 1";
        cmdCheck.Parameters.AddWithValue("@nom", nomCategorie);
        cmdCheck.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var existingId = await cmdCheck.ExecuteScalarAsync();
        if (existingId != null && existingId != DBNull.Value)
        {
            return Convert.ToInt32(existingId);
        }
        
        // Créer une nouvelle catégorie
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "INSERT INTO catégorie (nom_categorie, id_utilisateur) VALUES (@nom, @id_utilisateur); SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@nom", nomCategorie);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var result = await cmd.ExecuteScalarAsync();
        if (result == null || result == DBNull.Value)
        {
            throw new Exception("Erreur lors de la création de la catégorie");
        }
        
        return Convert.ToInt32(result);
    }

    public static async Task AssociateCategoriesToBudgetAsync(int budgetId, List<int> categoryIds)
    {
        if (categoryIds == null || categoryIds.Count == 0)
            return;
            
        await using var connection = await OpenConnectionAsync();
        
        // Vérifier que le budget existe
        await using var cmdCheckBudget = connection.CreateCommand();
        cmdCheckBudget.CommandText = "SELECT COUNT(*) FROM budgetmensuel WHERE id_budget = @id_budget";
        cmdCheckBudget.Parameters.AddWithValue("@id_budget", budgetId);
        var budgetExists = Convert.ToInt32(await cmdCheckBudget.ExecuteScalarAsync()) > 0;
        if (!budgetExists)
        {
            throw new Exception($"Le budget avec l'ID {budgetId} n'existe pas");
        }
        
        // Vérifier que toutes les catégories existent (optionnel, mais recommandé)
        // On va vérifier lors de l'insertion et gérer les erreurs
        
        // Supprimer les associations existantes pour ce budget
        await using var cmdDelete = connection.CreateCommand();
        cmdDelete.CommandText = "DELETE FROM attribuer WHERE id_budget = @id_budget";
        cmdDelete.Parameters.AddWithValue("@id_budget", budgetId);
        await cmdDelete.ExecuteNonQueryAsync();
        
        // Ajouter les nouvelles associations
        foreach (var categoryId in categoryIds)
        {
            try
            {
                await using var cmdInsert = connection.CreateCommand();
                cmdInsert.CommandText = "INSERT INTO attribuer (id_categorie, id_budget) VALUES (@id_categorie, @id_budget)";
                cmdInsert.Parameters.AddWithValue("@id_categorie", categoryId);
                cmdInsert.Parameters.AddWithValue("@id_budget", budgetId);
                await cmdInsert.ExecuteNonQueryAsync();
            }
            catch (MySqlException ex) when (ex.Number == 1062)
            {
                // Duplicate entry - ignorer
                System.Diagnostics.Debug.WriteLine($"Association déjà existante: catégorie {categoryId} - budget {budgetId}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de l'association catégorie {categoryId} au budget {budgetId}: {ex.Message}");
                throw;
            }
        }
    }

    public static async Task<List<Categorie>> GetCategoriesForBudgetAsync(int budgetId)
    {
        var list = new List<Categorie>();
        
        try
        {
            await using var connection = await OpenConnectionAsync();
            
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"SELECT c.id_categorie, c.nom_categorie, c.id_utilisateur 
                                FROM catégorie c
                                INNER JOIN attribuer a ON c.id_categorie = a.id_categorie
                                WHERE a.id_budget = @id_budget
                                ORDER BY c.nom_categorie";
            cmd.Parameters.AddWithValue("@id_budget", budgetId);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                try
                {
                    list.Add(new Categorie
                    {
                        IdCategorie = reader.GetInt32(0),
                        NomCategorie = reader.GetString(1),
                        IdUtilisateur = reader.GetInt32(2)
                    });
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Erreur lors de la lecture d'une catégorie: {ex.Message}");
                }
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur dans GetCategoriesForBudgetAsync: {ex.Message}");
        }
        
        return list;
    }

    // ==================== GESTION DES BUDGETS ====================

    /// <summary>
    /// Récupère ou crée le budget mensuel pour un utilisateur (retourne l'ID INT)
    /// </summary>
    public static async Task<int> GetOrCreateMonthlyBudgetIdAsync(string email, DateTime month)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) throw new Exception("Utilisateur introuvable");

        var monthStart = new DateTime(month.Year, month.Month, 1);
        
        await using var connection = await OpenConnectionAsync();
        
        // Vérifier si un budget existe déjà pour ce mois
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"SELECT id_budget FROM budgetmensuel 
                                WHERE id_utilisateur = @id_utilisateur 
                                AND mois = @mois 
                                LIMIT 1";
        checkCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        checkCmd.Parameters.AddWithValue("@mois", monthStart);
        
        var existingId = await checkCmd.ExecuteScalarAsync();
        if (existingId != null)
        {
            return Convert.ToInt32(existingId);
        }
        
        // Créer un nouveau budget mensuel
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO budgetmensuel (mois, limite, id_utilisateur) 
                            VALUES (@mois, NULL, @id_utilisateur);
                            SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@mois", monthStart);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Récupère toutes les catégories d'un utilisateur (sans doublons)
    /// </summary>
    public static async Task<List<Categorie>> GetCategoriesAsync(string email)
    {
        var list = new List<Categorie>();
        var user = await GetUserByEmailAsync(email);
        if (user == null) return list;

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        // Utiliser GROUP BY pour éviter les doublons et prendre le premier ID trouvé
        cmd.CommandText = @"SELECT MIN(id_categorie) as id_categorie, nom_categorie, id_utilisateur 
                            FROM catégorie 
                            WHERE id_utilisateur=@id_utilisateur 
                            GROUP BY nom_categorie, id_utilisateur
                            ORDER BY nom_categorie";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);

        await using var reader = await cmd.ExecuteReaderAsync();
        var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        while (await reader.ReadAsync())
        {
            var nomCategorie = reader.GetString(1);
            // Double vérification pour éviter les doublons (au cas où)
            if (seenNames.Contains(nomCategorie)) continue;
            seenNames.Add(nomCategorie);
            
            list.Add(new Categorie
            {
                IdCategorie = reader.GetInt32(0),
                NomCategorie = nomCategorie,
                IdUtilisateur = reader.GetInt32(2)
            });
        }
        return list;
    }

    /// <summary>
    /// Récupère ou crée une catégorie (retourne toujours l'ID, même si elle existe déjà)
    /// </summary>
    public static async Task<int> GetOrCreateCategoryAsync(string email, string nomCategorie)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) throw new Exception("Utilisateur introuvable");

        if (string.IsNullOrWhiteSpace(nomCategorie))
            throw new Exception("Le nom de la catégorie ne peut pas être vide");

        await using var connection = await OpenConnectionAsync();
        
        // Vérifier si la catégorie existe déjà pour cet utilisateur (insensible à la casse)
        // Utiliser MIN pour toujours retourner le même ID en cas de doublons
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"SELECT MIN(id_categorie) FROM catégorie 
                                WHERE LOWER(TRIM(nom_categorie)) = LOWER(TRIM(@nom_categorie)) 
                                AND id_utilisateur = @id_utilisateur";
        checkCmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        checkCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var existingId = await checkCmd.ExecuteScalarAsync();
        if (existingId != null && !Convert.IsDBNull(existingId))
        {
            // La catégorie existe déjà, retourner son ID (le plus petit en cas de doublons)
            return Convert.ToInt32(existingId);
        }

        // Créer la catégorie si elle n'existe pas
        // Utiliser INSERT IGNORE pour éviter les doublons en cas de race condition
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT IGNORE INTO catégorie (nom_categorie, id_utilisateur) 
                            VALUES (@nom_categorie, @id_utilisateur)";
        cmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        await cmd.ExecuteNonQueryAsync();
        
        // Récupérer l'ID (soit celui qui vient d'être créé, soit celui qui existait déjà)
        // Utiliser MIN pour toujours retourner le même ID en cas de doublons
        await using var getIdCmd = connection.CreateCommand();
        getIdCmd.CommandText = @"SELECT MIN(id_categorie) FROM catégorie 
                                WHERE LOWER(TRIM(nom_categorie)) = LOWER(TRIM(@nom_categorie)) 
                                AND id_utilisateur = @id_utilisateur";
        getIdCmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        getIdCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var id = await getIdCmd.ExecuteScalarAsync();
        if (id != null && !Convert.IsDBNull(id))
        {
            return Convert.ToInt32(id);
        }
        
        throw new Exception("Impossible de créer ou récupérer la catégorie");
    }

    /// <summary>
    /// Ajoute une nouvelle catégorie (empêche les doublons, lève une exception si elle existe déjà)
    /// </summary>
    public static async Task<int> AddCategoryAsync(string email, string nomCategorie)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) throw new Exception("Utilisateur introuvable");

        if (string.IsNullOrWhiteSpace(nomCategorie))
            throw new Exception("Le nom de la catégorie ne peut pas être vide");

        await using var connection = await OpenConnectionAsync();
        
        // Vérifier si la catégorie existe déjà pour cet utilisateur (insensible à la casse)
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"SELECT id_categorie FROM catégorie 
                                WHERE LOWER(TRIM(nom_categorie)) = LOWER(TRIM(@nom_categorie)) 
                                AND id_utilisateur = @id_utilisateur 
                                LIMIT 1";
        checkCmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        checkCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var existingId = await checkCmd.ExecuteScalarAsync();
        if (existingId != null)
        {
            // La catégorie existe déjà, lever une exception
            throw new Exception($"La catégorie '{nomCategorie.Trim()}' existe déjà. Impossible de créer un doublon.");
        }

        // Créer la catégorie si elle n'existe pas (utiliser INSERT IGNORE pour éviter les doublons)
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT IGNORE INTO catégorie (nom_categorie, id_utilisateur) 
                            VALUES (@nom_categorie, @id_utilisateur)";
        cmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        await cmd.ExecuteNonQueryAsync();
        
        // Récupérer l'ID (soit celui qui vient d'être créé, soit celui qui existait déjà)
        await using var getIdCmd = connection.CreateCommand();
        getIdCmd.CommandText = @"SELECT id_categorie FROM catégorie 
                                WHERE LOWER(TRIM(nom_categorie)) = LOWER(TRIM(@nom_categorie)) 
                                AND id_utilisateur = @id_utilisateur 
                                LIMIT 1";
        getIdCmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
        getIdCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var id = await getIdCmd.ExecuteScalarAsync();
        if (id != null)
        {
            return Convert.ToInt32(id);
        }
        
        throw new Exception($"La catégorie '{nomCategorie.Trim()}' existe déjà ou n'a pas pu être créée.");
    }

    /// <summary>
    /// Récupère les budgets par catégorie pour un mois donné
    /// Affiche les catégories par défaut + celles des transactions, sans les créer en BD
    /// </summary>
    public static async Task<List<BudgetCategorie>> GetCategoryBudgetsAsync(string email, DateTime month)
    {
        var list = new List<BudgetCategorie>();
        var user = await GetUserByEmailAsync(email);
        if (user == null)
        {
            System.Diagnostics.Debug.WriteLine("GetCategoryBudgetsAsync: Utilisateur introuvable");
            return list;
        }
        
        System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Début pour {email}, mois {month:yyyy-MM}");
        Console.WriteLine($"🔍 GetCategoryBudgetsAsync: Début pour {email}, mois {month:yyyy-MM}");

        var budgetId = await GetOrCreateMonthlyBudgetIdAsync(email, month);
        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        await using var connection = await OpenConnectionAsync();
        
        // Catégories par défaut (affichées mais pas créées en BD)
        var defaultCategories = new[] { "Logement", "Alimentation", "Transport", "Santé", "Loisirs", "Factures" };
        
        // Récupérer toutes les catégories uniques des transactions pour ce mois
        var transactionCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await using var transCmd = connection.CreateCommand();
            transCmd.CommandText = @"SELECT DISTINCT 
                                        SUBSTRING_INDEX(d.description, ' - ', -1) as categorie
                                    FROM depense d
                                    WHERE d.id_utilisateur = @id_utilisateur
                                      AND d.date_depense >= @mois_debut
                                      AND d.date_depense <= @mois_fin
                                      AND d.description LIKE '% - %'
                                      AND LOWER(TRIM(SUBSTRING_INDEX(d.description, ' - ', -1))) != 'salaire'";
            transCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            transCmd.Parameters.AddWithValue("@mois_debut", monthStart);
            transCmd.Parameters.AddWithValue("@mois_fin", monthEnd);
            
            await using var transReader = await transCmd.ExecuteReaderAsync();
            while (await transReader.ReadAsync())
            {
                var cat = transReader.GetString(0).Trim();
                if (!string.IsNullOrWhiteSpace(cat))
                    transactionCategories.Add(cat);
            }
            await transReader.CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des catégories de transactions: {ex.Message}");
        }
        
        // Récupérer aussi les catégories créées en BD (celles qui ont un budget défini)
        var bdCategories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        try
        {
            await using var bdCmd = connection.CreateCommand();
            bdCmd.CommandText = @"SELECT DISTINCT nom_categorie
                                 FROM catégorie
                                 WHERE id_utilisateur = @id_utilisateur
                                   AND LOWER(TRIM(nom_categorie)) != 'salaire'";
            bdCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            
            await using var bdReader = await bdCmd.ExecuteReaderAsync();
            while (await bdReader.ReadAsync())
            {
                var cat = bdReader.GetString(0).Trim();
                if (!string.IsNullOrWhiteSpace(cat))
                    bdCategories.Add(cat);
            }
            await bdReader.CloseAsync();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération des catégories en BD: {ex.Message}");
        }
        
        // Combiner toutes les catégories en utilisant un HashSet pour éviter les doublons
        // Utiliser une clé de normalisation stricte (trim + lowercase)
        var allCategoriesSet = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var categoryMapping = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // normalisé -> nom original
        
        // Fonction helper pour normaliser
        string Normalize(string name) => name?.Trim().ToLowerInvariant() ?? string.Empty;
        
        // Ajouter les catégories par défaut
        foreach (var cat in defaultCategories)
        {
            var normalized = Normalize(cat);
            if (!string.IsNullOrWhiteSpace(normalized) && !allCategoriesSet.Contains(normalized))
            {
                allCategoriesSet.Add(normalized);
                categoryMapping[normalized] = cat.Trim(); // Garder le nom original
            }
        }
        
        // Ajouter les catégories des transactions
        foreach (var cat in transactionCategories)
        {
            var normalized = Normalize(cat);
            if (!string.IsNullOrWhiteSpace(normalized) && !allCategoriesSet.Contains(normalized))
            {
                allCategoriesSet.Add(normalized);
                if (!categoryMapping.ContainsKey(normalized))
                    categoryMapping[normalized] = cat.Trim(); // Garder le nom original
            }
        }
        
        // Ajouter les catégories en BD
        foreach (var cat in bdCategories)
        {
            var normalized = Normalize(cat);
            if (!string.IsNullOrWhiteSpace(normalized) && !allCategoriesSet.Contains(normalized))
            {
                allCategoriesSet.Add(normalized);
                if (!categoryMapping.ContainsKey(normalized))
                    categoryMapping[normalized] = cat.Trim(); // Garder le nom original
            }
        }
        
        // Convertir en liste en préservant l'ordre : catégories par défaut d'abord, puis les autres
        var allCategories = new List<string>();
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        
        // D'abord les catégories par défaut (dans l'ordre)
        foreach (var cat in defaultCategories)
        {
            var normalized = Normalize(cat);
            if (allCategoriesSet.Contains(normalized) && !seen.Contains(normalized))
            {
                allCategories.Add(categoryMapping[normalized]);
                seen.Add(normalized);
            }
        }
        
        // Ensuite les autres catégories (transactions + BD)
        foreach (var normalized in allCategoriesSet)
        {
            if (!seen.Contains(normalized) && categoryMapping.ContainsKey(normalized))
            {
                allCategories.Add(categoryMapping[normalized]);
                seen.Add(normalized);
            }
        }
        
        System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: {allCategories.Count} catégories trouvées (défaut: {defaultCategories.Length}, transactions: {transactionCategories.Count}, BD: {bdCategories.Count})");
        Console.WriteLine($"📊 GetCategoryBudgetsAsync: {allCategories.Count} catégories trouvées (défaut: {defaultCategories.Length}, transactions: {transactionCategories.Count}, BD: {bdCategories.Count})");
        
        // S'assurer qu'on a au moins les catégories par défaut
        if (allCategories.Count == 0)
        {
            System.Diagnostics.Debug.WriteLine("GetCategoryBudgetsAsync: Aucune catégorie trouvée, utilisation des catégories par défaut uniquement");
            allCategories = defaultCategories.ToList();
        }
        
        // Pour chaque catégorie, récupérer les dépenses et le budget (si défini)
        // Utiliser un HashSet pour éviter les doublons dans la liste finale
        var seenInList = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Liste allCategories contient {allCategories.Count} éléments");
        foreach (var categoryName in allCategories)
        {
            // Normaliser le nom (trim + lowercase pour comparaison)
            var normalizedName = categoryName?.Trim() ?? string.Empty;
            var normalizedForComparison = normalizedName.ToLowerInvariant();
            
            System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Traitement de '{categoryName}' (normalisé: '{normalizedForComparison}')");
            
            // Éviter les doublons (au cas où)
            if (string.IsNullOrWhiteSpace(normalizedName))
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Nom vide ignoré");
                continue;
            }
            
            if (seenInList.Contains(normalizedForComparison))
            {
                System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: ⚠️ DOUBLON DÉTECTÉ ET IGNORÉ: '{categoryName}' (normalisé: '{normalizedForComparison}')");
                Console.WriteLine($"⚠️ DOUBLON DÉTECTÉ ET IGNORÉ: '{categoryName}' (normalisé: '{normalizedForComparison}')");
                continue;
            }
            seenInList.Add(normalizedForComparison);
            System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Ajout de '{normalizedName}' à la liste");
            // Récupérer les dépenses pour cette catégorie
            double depense = 0;
            try
            {
                await using var depenseCmd = connection.CreateCommand();
                depenseCmd.CommandText = @"SELECT COALESCE(SUM(ABS(montant)), 0)
                                          FROM depense
                                          WHERE id_utilisateur = @id_utilisateur
                                            AND date_depense >= @mois_debut
                                            AND date_depense <= @mois_fin
                                            AND LOWER(TRIM(SUBSTRING_INDEX(description, ' - ', -1))) = LOWER(TRIM(@categorie))";
                depenseCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
                depenseCmd.Parameters.AddWithValue("@mois_debut", monthStart);
                depenseCmd.Parameters.AddWithValue("@mois_fin", monthEnd);
                depenseCmd.Parameters.AddWithValue("@categorie", normalizedName);
                
                depense = Convert.ToDouble(await depenseCmd.ExecuteScalarAsync() ?? 0);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors du calcul des dépenses pour {categoryName}: {ex.Message}");
            }
            
            // Récupérer le budget si la catégorie existe en BD et a un budget défini
            double limite = 0;
            int idCategorie = 0;
            
            try
            {
                await using var budgetCmd = connection.CreateCommand();
                budgetCmd.CommandText = @"SELECT MIN(c.id_categorie), COALESCE(MAX(a.limite_categorie), 0)
                                         FROM catégorie c
                                         LEFT JOIN attribuer a ON c.id_categorie = a.id_categorie AND a.id_budget = @id_budget
                                         WHERE c.id_utilisateur = @id_utilisateur
                                           AND LOWER(TRIM(c.nom_categorie)) = LOWER(TRIM(@categorie))";
                budgetCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
                budgetCmd.Parameters.AddWithValue("@id_budget", budgetId);
                budgetCmd.Parameters.AddWithValue("@categorie", normalizedName);
                
                await using var budgetReader = await budgetCmd.ExecuteReaderAsync();
                if (await budgetReader.ReadAsync())
                {
                    idCategorie = budgetReader.GetInt32(0);
                    limite = budgetReader.IsDBNull(1) ? 0 : budgetReader.GetDouble(1);
                }
                await budgetReader.CloseAsync();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Erreur lors de la récupération du budget pour {categoryName}: {ex.Message}");
            }
            
            list.Add(new BudgetCategorie
            {
                IdCategorie = idCategorie, // 0 si la catégorie n'existe pas encore en BD
                NomCategorie = normalizedName, // Utiliser le nom normalisé
                IdBudget = budgetId,
                LimiteCategorie = limite,
                Depense = depense,
                Icone = GetCategoryIcon(normalizedName),
                Couleur = GetCategoryColor(normalizedName)
            });
            
            System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: Catégorie ajoutée à la liste: '{normalizedName}' (ID: {idCategorie}, Limite: {limite}, Dépense: {depense})");
        }
        
        // Trier : celles sans budget en haut, celles avec budget en bas
        var result = list.OrderBy(b => b.LimiteCategorie > 0 ? 1 : 0)
                   .ThenBy(b => b.NomCategorie)
                   .ToList();
        
        // Vérifier les doublons dans le résultat final
        var duplicates = result.GroupBy(b => b.NomCategorie.ToLowerInvariant().Trim())
                              .Where(g => g.Count() > 1)
                              .ToList();
        if (duplicates.Any())
        {
            System.Diagnostics.Debug.WriteLine($"⚠️ DOUBLONS DÉTECTÉS DANS LE RÉSULTAT FINAL:");
            Console.WriteLine($"⚠️⚠️⚠️ DOUBLONS DÉTECTÉS DANS LE RÉSULTAT FINAL:");
            foreach (var dup in duplicates)
            {
                System.Diagnostics.Debug.WriteLine($"  - '{dup.Key}': {dup.Count()} occurrences");
                Console.WriteLine($"  - '{dup.Key}': {dup.Count()} occurrences");
                foreach (var item in dup)
                {
                    System.Diagnostics.Debug.WriteLine($"    * ID: {item.IdCategorie}, Limite: {item.LimiteCategorie}, Dépense: {item.Depense}");
                    Console.WriteLine($"    * ID: {item.IdCategorie}, Limite: {item.LimiteCategorie}, Dépense: {item.Depense}");
                }
            }
            
            // Supprimer les doublons en gardant le premier de chaque groupe
            var uniqueResult = new List<BudgetCategorie>();
            var seenNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var item in result)
            {
                var normalized = item.NomCategorie.ToLowerInvariant().Trim();
                if (!seenNames.Contains(normalized))
                {
                    uniqueResult.Add(item);
                    seenNames.Add(normalized);
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine($"  → Suppression du doublon: '{item.NomCategorie}' (ID: {item.IdCategorie})");
                }
            }
            result = uniqueResult.OrderBy(b => b.LimiteCategorie > 0 ? 1 : 0)
                                 .ThenBy(b => b.NomCategorie)
                                 .ToList();
        }
        
        System.Diagnostics.Debug.WriteLine($"GetCategoryBudgetsAsync: {result.Count} catégories retournées (après suppression des doublons)");
        Console.WriteLine($"✅ GetCategoryBudgetsAsync: {result.Count} catégories retournées (après suppression des doublons)");
        return result;
    }

    /// <summary>
    /// Met à jour le budget d'une catégorie
    /// Crée la catégorie en BD si elle n'existe pas encore
    /// </summary>
    public static async Task<bool> UpdateCategoryBudgetAsync(string email, string nomCategorie, double limite, DateTime month)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return false;
        
        var budgetId = await GetOrCreateMonthlyBudgetIdAsync(email, month);

        await using var connection = await OpenConnectionAsync();
        
        // Vérifier si la colonne limite_categorie existe, sinon l'ajouter
        await using var checkCmd = connection.CreateCommand();
        checkCmd.CommandText = @"SELECT COUNT(*) FROM information_schema.COLUMNS 
                                WHERE TABLE_SCHEMA = DATABASE() 
                                AND TABLE_NAME = 'attribuer' 
                                AND COLUMN_NAME = 'limite_categorie'";
        var columnExists = Convert.ToInt32(await checkCmd.ExecuteScalarAsync()) > 0;
        
        if (!columnExists)
        {
            await using var alterCmd = connection.CreateCommand();
            alterCmd.CommandText = @"ALTER TABLE attribuer ADD COLUMN limite_categorie DOUBLE DEFAULT NULL";
            await alterCmd.ExecuteNonQueryAsync();
        }
        
        // Récupérer ou créer la catégorie (en utilisant GetOrCreateCategoryAsync pour éviter les doublons)
        int idCategorie;
        try
        {
            idCategorie = await GetOrCreateCategoryAsync(email, nomCategorie);
        }
        catch
        {
            // Si GetOrCreateCategoryAsync échoue, essayer de récupérer l'ID existant
            await using var catCmd = connection.CreateCommand();
            catCmd.CommandText = @"SELECT MIN(id_categorie) FROM catégorie 
                                  WHERE id_utilisateur = @id_utilisateur 
                                  AND LOWER(TRIM(nom_categorie)) = LOWER(TRIM(@nom_categorie))";
            catCmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            catCmd.Parameters.AddWithValue("@nom_categorie", nomCategorie.Trim());
            
            var existingId = await catCmd.ExecuteScalarAsync();
            if (existingId != null)
            {
                idCategorie = Convert.ToInt32(existingId);
            }
            else
            {
                throw new Exception("Impossible de créer ou récupérer la catégorie");
            }
        }
        
        // Insérer ou mettre à jour le budget
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO attribuer (id_categorie, id_budget, limite_categorie) 
                            VALUES (@id_categorie, @id_budget, @limite) 
                            ON DUPLICATE KEY UPDATE limite_categorie = @limite";
        cmd.Parameters.AddWithValue("@id_categorie", idCategorie);
        cmd.Parameters.AddWithValue("@id_budget", budgetId);
        cmd.Parameters.AddWithValue("@limite", limite);
        
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    // ==================== GESTION DES REVENUS ====================

    /// <summary>
    /// Modèle pour un revenu
    /// </summary>
    public class Revenu
    {
        public int IdRevenu { get; set; }
        public int IdUtilisateur { get; set; }
        public DateTime Mois { get; set; }
        public double Montant { get; set; }
        public string Libelle { get; set; } = string.Empty;
        public DateTime DateCreation { get; set; }
    }

    /// <summary>
    /// Récupère le total des revenus pour un mois donné
    /// </summary>
    public static async Task<double> GetTotalRevenusAsync(string email, DateTime month)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return 0;

        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT COALESCE(SUM(montant), 0) FROM revenu 
                            WHERE id_utilisateur = @id_utilisateur
                            AND mois >= @mois_debut AND mois <= @mois_fin";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@mois_debut", monthStart);
        cmd.Parameters.AddWithValue("@mois_fin", monthEnd);
        
        var result = await cmd.ExecuteScalarAsync();
        return result != null ? Convert.ToDouble(result) : 0;
    }

    /// <summary>
    /// Récupère tous les revenus pour un mois donné
    /// </summary>
    public static async Task<List<Revenu>> GetRevenusAsync(string email, DateTime month)
    {
        var list = new List<Revenu>();
        var user = await GetUserByEmailAsync(email);
        if (user == null) return list;

        var monthStart = new DateTime(month.Year, month.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);
        
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"SELECT id_revenu, id_utilisateur, mois, montant, libelle, date_creation
                            FROM revenu 
                            WHERE id_utilisateur = @id_utilisateur
                            AND mois >= @mois_debut AND mois <= @mois_fin
                            ORDER BY date_creation DESC";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@mois_debut", monthStart);
        cmd.Parameters.AddWithValue("@mois_fin", monthEnd);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Revenu
            {
                IdRevenu = reader.GetInt32(0),
                IdUtilisateur = reader.GetInt32(1),
                Mois = reader.GetDateTime(2),
                Montant = reader.GetDouble(3),
                Libelle = reader.IsDBNull(4) ? "Salaire" : reader.GetString(4),
                DateCreation = reader.GetDateTime(5)
            });
        }
        return list;
    }

    /// <summary>
    /// Ajoute un revenu (salaire ou revenu supplémentaire)
    /// </summary>
    public static async Task<int> AddRevenuAsync(string email, double montant, DateTime month, string libelle = "Salaire")
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) throw new Exception("Utilisateur introuvable");

        var monthStart = new DateTime(month.Year, month.Month, 1);
        
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"INSERT INTO revenu (id_utilisateur, mois, montant, libelle, date_creation) 
                            VALUES (@id_utilisateur, @mois, @montant, @libelle, NOW());
                            SELECT LAST_INSERT_ID();";
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@mois", monthStart);
        cmd.Parameters.AddWithValue("@montant", montant);
        cmd.Parameters.AddWithValue("@libelle", libelle);
        
        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    /// <summary>
    /// Met à jour un revenu existant
    /// </summary>
    public static async Task<bool> UpdateRevenuAsync(string email, int idRevenu, double montant, string libelle)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return false;

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"UPDATE revenu 
                            SET montant = @montant, libelle = @libelle
                            WHERE id_revenu = @id_revenu AND id_utilisateur = @id_utilisateur";
        cmd.Parameters.AddWithValue("@id_revenu", idRevenu);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@montant", montant);
        cmd.Parameters.AddWithValue("@libelle", libelle);
        
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    /// <summary>
    /// Supprime un revenu
    /// </summary>
    public static async Task<bool> DeleteRevenuAsync(string email, int idRevenu)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return false;

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"DELETE FROM revenu 
                            WHERE id_revenu = @id_revenu AND id_utilisateur = @id_utilisateur";
        cmd.Parameters.AddWithValue("@id_revenu", idRevenu);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    /// <summary>
    /// Supprime une catégorie (supprime d'abord les entrées liées dans attribuer)
    /// Supprime toutes les catégories avec le même nom pour cet utilisateur pour éviter les doublons
    /// </summary>
    public static async Task<bool> DeleteCategoryAsync(string email, int idCategorie)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return false;

        await using var connection = await OpenConnectionAsync();
        
        // Récupérer le nom de la catégorie avant de la supprimer
        await using var cmdGetName = connection.CreateCommand();
        cmdGetName.CommandText = @"SELECT nom_categorie FROM catégorie 
                                   WHERE id_categorie = @id_categorie AND id_utilisateur = @id_utilisateur";
        cmdGetName.Parameters.AddWithValue("@id_categorie", idCategorie);
        cmdGetName.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var categoryName = await cmdGetName.ExecuteScalarAsync() as string;
        if (string.IsNullOrEmpty(categoryName)) return false;
        
        // Désactiver temporairement les contraintes
        await using var cmdDisable = connection.CreateCommand();
        cmdDisable.CommandText = "SET FOREIGN_KEY_CHECKS = 0";
        await cmdDisable.ExecuteNonQueryAsync();

        try
        {
            // Récupérer tous les IDs de catégories avec le même nom pour cet utilisateur
            await using var cmdGetIds = connection.CreateCommand();
            cmdGetIds.CommandText = @"SELECT id_categorie FROM catégorie 
                                     WHERE LOWER(nom_categorie) = LOWER(@nom_categorie) 
                                     AND id_utilisateur = @id_utilisateur";
            cmdGetIds.Parameters.AddWithValue("@nom_categorie", categoryName);
            cmdGetIds.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            
            var categoryIds = new List<int>();
            await using var reader = await cmdGetIds.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                categoryIds.Add(reader.GetInt32(0));
            }
            await reader.CloseAsync();
            
            if (categoryIds.Count == 0) return false;
            
            // Supprimer toutes les entrées dans attribuer qui référencent ces catégories
            foreach (var catId in categoryIds)
            {
                await using var cmdAttribuer = connection.CreateCommand();
                cmdAttribuer.CommandText = @"DELETE FROM attribuer WHERE id_categorie = @id_categorie";
                cmdAttribuer.Parameters.AddWithValue("@id_categorie", catId);
                await cmdAttribuer.ExecuteNonQueryAsync();
            }

            // Supprimer toutes les catégories avec le même nom
            await using var cmd = connection.CreateCommand();
            cmd.CommandText = @"DELETE FROM catégorie 
                                WHERE LOWER(nom_categorie) = LOWER(@nom_categorie) 
                                AND id_utilisateur = @id_utilisateur";
            cmd.Parameters.AddWithValue("@nom_categorie", categoryName);
            cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
            
            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }
        finally
        {
            // Réactiver les contraintes
            await using var cmdEnable = connection.CreateCommand();
            cmdEnable.CommandText = "SET FOREIGN_KEY_CHECKS = 1";
            await cmdEnable.ExecuteNonQueryAsync();
        }
    }

    /// <summary>
    /// Met à jour le nom d'une catégorie
    /// </summary>
    public static async Task<bool> UpdateCategoryNameAsync(string email, int idCategorie, string newName)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return false;

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = @"UPDATE catégorie 
                            SET nom_categorie = @nom_categorie 
                            WHERE id_categorie = @id_categorie AND id_utilisateur = @id_utilisateur";
        cmd.Parameters.AddWithValue("@id_categorie", idCategorie);
        cmd.Parameters.AddWithValue("@nom_categorie", newName);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    /// <summary>
    /// Vérifie si une dépense dépasse le budget pour une catégorie
    /// Retourne les informations sur le budget (limite, dépense actuelle, nouveau total)
    /// </summary>
    public static async Task<(bool WillExceed, double BudgetLimit, double CurrentSpent, double NewTotal, string CategoryName)> CheckBudgetExceedanceAsync(
        string email, string categoryName, double amount, DateTime transactionDate, int? existingTransactionId = null)
    {
        var user = await GetUserByEmailAsync(email);
        if (user == null) return (false, 0, 0, 0, categoryName);

        var budgetId = await GetOrCreateMonthlyBudgetIdAsync(email, transactionDate);
        var monthStart = new DateTime(transactionDate.Year, transactionDate.Month, 1);
        var monthEnd = monthStart.AddMonths(1).AddDays(-1);

        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        
        // Récupérer le budget de la catégorie (si elle existe en BD) et les dépenses actuelles
        cmd.CommandText = @"SELECT 
                                COALESCE(a.limite_categorie, 0) as limite,
                                COALESCE((
                                    SELECT SUM(ABS(d2.montant))
                                    FROM depense d2
                                    WHERE d2.id_utilisateur = @id_utilisateur
                                      AND LOWER(TRIM(SUBSTRING_INDEX(d2.description, ' - ', -1))) = LOWER(TRIM(@nom_categorie))
                                      AND d2.date_depense >= @mois_debut
                                      AND d2.date_depense <= @mois_fin
                                      AND LOWER(TRIM(SUBSTRING_INDEX(d2.description, ' - ', -1))) != 'salaire'
                                      " + (existingTransactionId.HasValue ? "AND d2.id_depense != @existing_id" : "") + @"
                                ), 0) as depense_actuelle
                            FROM catégorie c
                            LEFT JOIN attribuer a ON c.id_categorie = a.id_categorie AND a.id_budget = @id_budget
                            WHERE c.id_utilisateur = @id_utilisateur
                              AND LOWER(TRIM(c.nom_categorie)) = LOWER(TRIM(@nom_categorie))
                            LIMIT 1";
        
        cmd.Parameters.AddWithValue("@id_budget", budgetId);
        cmd.Parameters.AddWithValue("@id_utilisateur", user.IdUtilisateur);
        cmd.Parameters.AddWithValue("@nom_categorie", categoryName);
        cmd.Parameters.AddWithValue("@mois_debut", monthStart);
        cmd.Parameters.AddWithValue("@mois_fin", monthEnd);
        if (existingTransactionId.HasValue)
        {
            cmd.Parameters.AddWithValue("@existing_id", existingTransactionId.Value);
        }

        await using var reader = await cmd.ExecuteReaderAsync();
        if (await reader.ReadAsync())
        {
            var limite = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
            var depenseActuelle = reader.GetDouble(1);
            var nouveauTotal = depenseActuelle + Math.Abs(amount);
            var willExceed = limite > 0 && nouveauTotal > limite;

            return (willExceed, limite, depenseActuelle, nouveauTotal, categoryName);
        }

        return (false, 0, 0, Math.Abs(amount), categoryName);
    }

    /// <summary>
    /// Récupère l'icône pour une catégorie
    /// </summary>
    private static string GetCategoryIcon(string category)
    {
        return category.ToLower() switch
        {
            var c when c.Contains("logement") || c.Contains("loyer") => "🏠",
            var c when c.Contains("alimentation") || c.Contains("courses") || c.Contains("nourriture") => "🍽️",
            var c when c.Contains("transport") || c.Contains("voiture") || c.Contains("essence") => "🚗",
            var c when c.Contains("loisir") || c.Contains("divertissement") => "🎮",
            var c when c.Contains("facture") || c.Contains("électricité") || c.Contains("eau") => "💡",
            var c when c.Contains("santé") || c.Contains("médical") => "⚕️",
            var c when c.Contains("salaire") || c.Contains("revenu") => "💰",
            var c when c.Contains("shopping") || c.Contains("vêtement") => "🛍️",
            _ => "📊"
        };
    }

    /// <summary>
    /// Récupère la couleur pour une catégorie
    /// </summary>
    private static string GetCategoryColor(string category)
    {
        return category.ToLower() switch
        {
            var c when c.Contains("logement") => "#E67E22", // Orange
            var c when c.Contains("alimentation") => "#27AE60", // Vert
            var c when c.Contains("transport") => "#F39C12", // Jaune-orange
            var c when c.Contains("loisir") => "#9B59B6", // Violet
            var c when c.Contains("facture") => "#E74C3C", // Rouge
            var c when c.Contains("santé") => "#1ABC9C", // Turquoise
            var c when c.Contains("salaire") || c.Contains("revenu") => "#F1C40F", // Jaune doré
            _ => "#3498DB" // Bleu par défaut
        };
    }

    /// <summary>
    /// Crée les catégories par défaut pour un utilisateur s'il n'en a pas encore
    /// </summary>
}

public readonly record struct RegistrationResult(bool IsSuccess, string? ErrorMessage, bool EmailExists)
{
    public static RegistrationResult Success() => new(true, null, false);
    public static RegistrationResult EmailAlreadyExists() => new(false, "Cette adresse email est déjà utilisée.", true);
    public static RegistrationResult Failed(string? errorMessage) => new(false, errorMessage, false);
}

