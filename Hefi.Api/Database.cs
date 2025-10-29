using System.Data;
using System.Data.Common;
using Dapper;

namespace Hefi.Api;

/// <summary>
/// provides a one-time database initialization routine for the Hefi API.
/// creates tables (users, refresh_tokens, meals ..) if they do not already exist.
/// </summary>
public static class Database
{
    // initializes thePostgreSQL database
    public static async Task InitAsync(IDbConnection db)
    {
        if (db.State != ConnectionState.Open)
            await ((DbConnection)db).OpenAsync();

        using var tx = db.BeginTransaction();
        try
        {
            // Users table - stores user credentials and profile information.
            var usersSql = @"
            CREATE TABLE IF NOT EXISTS users (
            id SERIAL PRIMARY KEY,
            name TEXT NOT NULL,
            email TEXT NOT NULL UNIQUE,
            password_hash TEXT NOT NULL
            );";
            await db.ExecuteAsync(usersSql, transaction: tx);

            // refresh tokens table -  maintains issued refresh tokens for long-term auth management.
            var rtSql = @"
            CREATE TABLE IF NOT EXISTS refresh_tokens (
            id SERIAL PRIMARY KEY,
            user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            token_hash TEXT NOT NULL UNIQUE,
            expires_at TIMESTAMPTZ NOT NULL,
            created_at TIMESTAMPTZ NOT NULL DEFAULT now(),
            revoked_at TIMESTAMPTZ NULL,
            replaced_by_token_hash TEXT NULL,
            user_agent TEXT NULL,
            ip_address TEXT NULL
            );
            CREATE INDEX IF NOT EXISTS idx_refresh_tokens_user ON refresh_tokens(user_id);
            ";
            await db.ExecuteAsync(rtSql, transaction: tx);


            // Meals table - represents daily meal summaries per user
            var mealsSql = @"
            CREATE TABLE IF NOT EXISTS meals (
            id SERIAL PRIMARY KEY,
            user_id INT NOT NULL REFERENCES users(id) ON DELETE CASCADE,
            eaten_at TIMESTAMPTZ NOT NULL,
            total_kcal INT NOT NULL,
            total_protein DOUBLE PRECISION NOT NULL,
            total_carbs DOUBLE PRECISION NOT NULL,
            total_fat DOUBLE PRECISION NOT NULL,
            total_sugar DOUBLE PRECISION NOT NULL
            );";
            await db.ExecuteAsync(mealsSql, transaction: tx);

            // Meal items table - contains individual foods linked to each meal 
            var itemsSql = @"
            CREATE TABLE IF NOT EXISTS meal_items (
            id SERIAL PRIMARY KEY,
            meal_id INT NOT NULL REFERENCES meals(id) ON DELETE CASCADE,
            food_label TEXT NOT NULL,
            grams DOUBLE PRECISION NOT NULL,
            kcal INT NOT NULL,
            protein DOUBLE PRECISION NOT NULL,
            carbs DOUBLE PRECISION NOT NULL,
            fat DOUBLE PRECISION NOT NULL,
            sugar DOUBLE PRECISION NOT NULL
            );";
            await db.ExecuteAsync(itemsSql, transaction: tx);

            tx.Commit();
        }
        catch
        {
            tx.Rollback();
            throw;
        }

    }
}
