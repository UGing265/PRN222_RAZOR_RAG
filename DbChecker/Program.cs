using System;
using Npgsql;

class Program
{
    static void Main()
    {
        string connStr = "Host=localhost;Port=5432;Database=prn222_db;Username=postgres;Password=postgres";
        using var conn = new NpgsqlConnection(connStr);
        conn.Open();

        using var cmd = new NpgsqlCommand(@"
            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS email_verified boolean DEFAULT false NOT NULL;
            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS username character varying(255);
            ALTER TABLE public.users ADD COLUMN IF NOT EXISTS ""displayUsername"" character varying(255);
        ", conn);
        cmd.ExecuteNonQuery();
        Console.WriteLine("Added missing columns to users table successfully.");
    }
}
