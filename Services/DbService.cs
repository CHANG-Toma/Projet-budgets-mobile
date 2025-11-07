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

        try
        {
            await using var connection = await OpenConnectionAsync();
            await using var command = connection.CreateCommand();
            command.CommandText = "INSERT INTO users (email, password_hash, fullname) VALUES (@email, @password_hash, @fullname)";
            command.Parameters.AddWithValue("@email", email);
            command.Parameters.AddWithValue("@password_hash", passwordHash);
            command.Parameters.AddWithValue("@fullname", string.IsNullOrWhiteSpace(fullName) ? email : fullName);

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
            command.CommandText = "SELECT id, email, fullname, password_hash, created_at, updated_at FROM users WHERE email = @email LIMIT 1";
            command.Parameters.AddWithValue("@email", email);

            await using var reader = await command.ExecuteReaderAsync();
            if (await reader.ReadAsync())
            {
                return new UserAccount
                {
                    Id = Convert.ToUInt32(reader.GetValue(0)),
                    Email = reader.GetString(1),
                    FullName = reader.IsDBNull(2) ? null : reader.GetString(2),
                    PasswordHash = reader.GetString(3),
                    CreatedAt = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4),
                    UpdatedAt = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5)
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
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE users SET fullname=@fullname WHERE email=@email";
        cmd.Parameters.AddWithValue("@fullname", newFullName);
        cmd.Parameters.AddWithValue("@email", email);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public static async Task<int> AddOrUpdateTransactionAsync(Transaction tx)
    {
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        if (tx.Id == 0)
        {
            cmd.CommandText = "INSERT INTO transactions (title, amount, date, category, owner_email) VALUES (@title, @amount, @date, @category, @owner_email); SELECT LAST_INSERT_ID();";
        }
        else
        {
            cmd.CommandText = "UPDATE transactions SET title=@title, amount=@amount, date=@date, category=@category WHERE id=@id AND owner_email=@owner_email; SELECT @id;";
            cmd.Parameters.AddWithValue("@id", tx.Id);
        }
        cmd.Parameters.AddWithValue("@title", tx.Title);
        cmd.Parameters.AddWithValue("@amount", tx.Amount);
        cmd.Parameters.AddWithValue("@date", tx.Date);
        cmd.Parameters.AddWithValue("@category", tx.Category);
        cmd.Parameters.AddWithValue("@owner_email", tx.OwnerEmail);

        var result = await cmd.ExecuteScalarAsync();
        return Convert.ToInt32(result);
    }

    public static async Task<bool> DeleteTransactionAsync(int id, string ownerEmail)
    {
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        cmd.CommandText = "DELETE FROM transactions WHERE id=@id AND owner_email=@owner_email";
        cmd.Parameters.AddWithValue("@id", id);
        cmd.Parameters.AddWithValue("@owner_email", ownerEmail);
        var rows = await cmd.ExecuteNonQueryAsync();
        return rows > 0;
    }

    public static async Task<List<Transaction>> GetTransactionsAsync(string ownerEmail, string? search = null)
    {
        var list = new List<Transaction>();
        await using var connection = await OpenConnectionAsync();
        await using var cmd = connection.CreateCommand();
        if (string.IsNullOrWhiteSpace(search))
        {
            cmd.CommandText = "SELECT id, title, amount, date, category, owner_email FROM transactions WHERE owner_email=@owner_email ORDER BY date DESC, id DESC";
        }
        else
        {
            cmd.CommandText = "SELECT id, title, amount, date, category, owner_email FROM transactions WHERE owner_email=@owner_email AND (title LIKE @q OR category LIKE @q) ORDER BY date DESC, id DESC";
            cmd.Parameters.AddWithValue("@q", $"%{search}%");
        }
        cmd.Parameters.AddWithValue("@owner_email", ownerEmail);

        await using var reader = await cmd.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            list.Add(new Transaction
            {
                Id = reader.GetInt32(0),
                Title = reader.GetString(1),
                Amount = reader.GetDouble(2),
                Date = reader.GetDateTime(3),
                Category = reader.GetString(4),
                OwnerEmail = reader.GetString(5)
            });
        }
        return list;
    }
}

public readonly record struct RegistrationResult(bool IsSuccess, string? ErrorMessage, bool EmailExists)
{
    public static RegistrationResult Success() => new(true, null, false);
    public static RegistrationResult EmailAlreadyExists() => new(false, "Cette adresse email est déjà utilisée.", true);
    public static RegistrationResult Failed(string? errorMessage) => new(false, errorMessage, false);
}

