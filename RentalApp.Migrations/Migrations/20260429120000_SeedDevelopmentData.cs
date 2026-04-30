using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace RentalApp.Migrations.Migrations
{
    /// <inheritdoc />
    public partial class SeedDevelopmentData : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                INSERT INTO categories ("Name", "Slug", "CreatedAt", "UpdatedAt")
                VALUES
                    ('Music',   'music',   NOW(), NOW()),
                    ('Camping', 'camping', NOW(), NOW()),
                    ('DIY',     'diy',     NOW(), NOW()),
                    ('Games',   'games',   NOW(), NOW())
                ON CONFLICT ("Slug") DO NOTHING;
                """
            );

            var salt1 = BCrypt.Net.BCrypt.GenerateSalt();
            var hash1 = BCrypt.Net.BCrypt.HashPassword("Password1!", salt1);

            var salt2 = BCrypt.Net.BCrypt.GenerateSalt();
            var hash2 = BCrypt.Net.BCrypt.HashPassword("Password2!", salt2);

            var salt3 = BCrypt.Net.BCrypt.GenerateSalt();
            var hash3 = BCrypt.Net.BCrypt.HashPassword("Password3!", salt3);

            migrationBuilder.Sql(
                $"""
                INSERT INTO users ("FirstName", "LastName", "Email", "PasswordHash", "PasswordSalt", "CreatedAt", "UpdatedAt")
                VALUES
                    ('Alice', 'Smith', 'alice@example.com', '{hash1}', '{salt1}', NOW(), NOW()),
                    ('Bob',   'Jones', 'bob@example.com',   '{hash2}', '{salt2}', NOW(), NOW()),
                    ('Carol', 'White', 'carol@example.com', '{hash3}', '{salt3}', NOW(), NOW())
                ON CONFLICT ("Email") DO NOTHING;
                """
            );

            // Bob's 3 items
            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Acoustic Guitar',
                    'Steel-string acoustic guitar, ideal for campfires and casual jamming.',
                    5.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'music'),
                    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.1883, 55.9533), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Acoustic Guitar'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Two-Man Tent',
                    'Lightweight two-person camping tent with groundsheet.',
                    10.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'camping'),
                    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.2030, 55.9486), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Two-Man Tent'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Power Drill Set',
                    'Cordless power drill with a full set of drill bits and screwdriver attachments.',
                    8.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'diy'),
                    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.1719, 55.9626), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Power Drill Set'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
                );
                """
            );

            // Carol's 5 items
            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Electric Keyboard',
                    '61-key digital keyboard with weighted keys and built-in speakers.',
                    15.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'music'),
                    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.1878, 55.9419), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Electric Keyboard'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Sleeping Bag',
                    '3-season sleeping bag rated to -5°C, includes carry bag.',
                    3.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'camping'),
                    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.2127, 55.9501), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Sleeping Bag'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Jigsaw Puzzle Bundle',
                    'Collection of three 1000-piece jigsaw puzzles.',
                    2.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'games'),
                    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.1546, 55.9442), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Jigsaw Puzzle Bundle'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Circular Saw',
                    '7-1/4 inch circular saw with two blades, ideal for cutting timber and sheet materials.',
                    20.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'diy'),
                    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.1573, 55.9614), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Circular Saw'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
                SELECT
                    'Board Game Bundle',
                    'Selection of five classic board games including Catan, Ticket to Ride, and Pandemic.',
                    5.00,
                    (SELECT "Id" FROM categories WHERE "Slug" = 'games'),
                    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
                    ST_SetSRID(ST_MakePoint(-3.2290, 55.9384), 4326)::geography,
                    true, NOW(), NOW()
                WHERE NOT EXISTS (
                    SELECT 1 FROM items
                    WHERE "Title" = 'Board Game Bundle'
                    AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
                );
                """
            );
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                DELETE FROM items
                WHERE "OwnerId" IN (
                    SELECT "Id" FROM users
                    WHERE "Email" IN ('alice@example.com', 'bob@example.com', 'carol@example.com')
                );
                """
            );

            migrationBuilder.Sql(
                """
                DELETE FROM users
                WHERE "Email" IN ('alice@example.com', 'bob@example.com', 'carol@example.com');
                """
            );

            migrationBuilder.Sql(
                """
                DELETE FROM categories
                WHERE "Slug" IN ('music', 'camping', 'diy', 'games');
                """
            );
        }
    }
}
