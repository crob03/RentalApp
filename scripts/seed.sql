-- Enable pgcrypto for bcrypt password hashing.
-- postgis/postgis:16-3.5 ships pgcrypto via postgresql-contrib; app_user is
-- the PostgreSQL superuser in this dev setup so CREATE EXTENSION succeeds.
CREATE EXTENSION IF NOT EXISTS pgcrypto;

-- ── Categories ────────────────────────────────────────────────────────────────

INSERT INTO categories ("Name", "Slug", "CreatedAt", "UpdatedAt")
VALUES
    ('Music',   'music',   NOW(), NOW()),
    ('Camping', 'camping', NOW(), NOW()),
    ('DIY',     'diy',     NOW(), NOW()),
    ('Games',   'games',   NOW(), NOW())
ON CONFLICT ("Slug") DO NOTHING;

-- ── Users ─────────────────────────────────────────────────────────────────────
-- One CTE row produces three distinct salts; each SELECT in the UNION ALL reads
-- a different column, so each user gets a unique salt embedded in their hash.
-- BCrypt.Net.BCrypt.Verify() accepts the resulting $2a$ hashes directly.

WITH salts AS (
    SELECT
        gen_salt('bf', 10) AS s1,
        gen_salt('bf', 10) AS s2,
        gen_salt('bf', 10) AS s3
)
INSERT INTO users ("FirstName", "LastName", "Email", "PasswordHash", "PasswordSalt", "CreatedAt", "UpdatedAt")
SELECT 'Alice', 'Smith', 'alice@example.com', crypt('Password1!', s1), s1, NOW(), NOW() FROM salts
UNION ALL
SELECT 'Bob',   'Jones', 'bob@example.com',   crypt('Password2!', s2), s2, NOW(), NOW() FROM salts
UNION ALL
SELECT 'Carol', 'White', 'carol@example.com', crypt('Password3!', s3), s3, NOW(), NOW() FROM salts
ON CONFLICT ("Email") DO NOTHING;

-- ── Items — Bob (3) ───────────────────────────────────────────────────────────

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Acoustic Guitar',
    'Steel-string acoustic guitar, ideal for campfires and casual jamming.',
    5.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'music'),
    (SELECT "Id" FROM users     WHERE "Email" = 'bob@example.com'),
    ST_SetSRID(ST_MakePoint(-3.1883, 55.9533), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Acoustic Guitar'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Two-Man Tent',
    'Lightweight two-person camping tent with groundsheet.',
    10.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'camping'),
    (SELECT "Id" FROM users     WHERE "Email" = 'bob@example.com'),
    ST_SetSRID(ST_MakePoint(-3.2030, 55.9486), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Two-Man Tent'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Power Drill Set',
    'Cordless power drill with a full set of drill bits and screwdriver attachments.',
    8.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'diy'),
    (SELECT "Id" FROM users     WHERE "Email" = 'bob@example.com'),
    ST_SetSRID(ST_MakePoint(-3.1719, 55.9626), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Power Drill Set'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')
);

-- ── Items — Carol (5) ─────────────────────────────────────────────────────────

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Electric Keyboard',
    '61-key digital keyboard with weighted keys and built-in speakers.',
    15.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'music'),
    (SELECT "Id" FROM users     WHERE "Email" = 'carol@example.com'),
    ST_SetSRID(ST_MakePoint(-3.1878, 55.9419), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Electric Keyboard'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Sleeping Bag',
    '3-season sleeping bag rated to -5°C, includes carry bag.',
    3.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'camping'),
    (SELECT "Id" FROM users     WHERE "Email" = 'carol@example.com'),
    ST_SetSRID(ST_MakePoint(-3.2127, 55.9501), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Sleeping Bag'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Jigsaw Puzzle Bundle',
    'Collection of three 1000-piece jigsaw puzzles.',
    2.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'games'),
    (SELECT "Id" FROM users     WHERE "Email" = 'carol@example.com'),
    ST_SetSRID(ST_MakePoint(-3.1546, 55.9442), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Jigsaw Puzzle Bundle'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Circular Saw',
    '7-1/4 inch circular saw with two blades, ideal for cutting timber and sheet materials.',
    20.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'diy'),
    (SELECT "Id" FROM users     WHERE "Email" = 'carol@example.com'),
    ST_SetSRID(ST_MakePoint(-3.1573, 55.9614), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Circular Saw'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
);

INSERT INTO items ("Title", "Description", "DailyRate", "CategoryId", "OwnerId", "Location", "IsAvailable", "CreatedAt", "UpdatedAt")
SELECT
    'Board Game Bundle',
    'Selection of five classic board games including Catan, Ticket to Ride, and Pandemic.',
    5.00,
    (SELECT "Id" FROM categories WHERE "Slug" = 'games'),
    (SELECT "Id" FROM users     WHERE "Email" = 'carol@example.com'),
    ST_SetSRID(ST_MakePoint(-3.2290, 55.9384), 4326)::geography,
    true, NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM items
    WHERE "Title" = 'Board Game Bundle'
      AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')
);

-- ── Rentals ───────────────────────────────────────────────────────────────────
-- All dates use CURRENT_DATE ± integer days (date + integer = date in PostgreSQL).
-- Status is stored as varchar(50) matching the RentalStatus enum name.
-- Guard: item + borrower + start date uniquely identifies each seed rental.

-- 1. Completed — Alice rented Acoustic Guitar from Bob, ended 7 days ago
INSERT INTO rentals ("ItemId", "OwnerId", "BorrowerId", "StartDate", "EndDate", "Status", "CreatedAt", "UpdatedAt")
SELECT
    (SELECT "Id" FROM items WHERE "Title" = 'Acoustic Guitar'
        AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')),
    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
    (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com'),
    CURRENT_DATE - 10,
    CURRENT_DATE - 7,
    'Completed',
    NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM rentals
    WHERE "ItemId"     = (SELECT "Id" FROM items WHERE "Title" = 'Acoustic Guitar'
                            AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'))
      AND "BorrowerId" = (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com')
      AND "StartDate"  = CURRENT_DATE - 10
);

-- 2. OutForRent — Alice has Two-Man Tent, currently ongoing
INSERT INTO rentals ("ItemId", "OwnerId", "BorrowerId", "StartDate", "EndDate", "Status", "CreatedAt", "UpdatedAt")
SELECT
    (SELECT "Id" FROM items WHERE "Title" = 'Two-Man Tent'
        AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')),
    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
    (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com'),
    CURRENT_DATE - 3,
    CURRENT_DATE + 4,
    'OutForRent',
    NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM rentals
    WHERE "ItemId"     = (SELECT "Id" FROM items WHERE "Title" = 'Two-Man Tent'
                            AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'))
      AND "BorrowerId" = (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com')
      AND "StartDate"  = CURRENT_DATE - 3
);

-- 3. Approved — Alice has Power Drill Set booked, starts in 5 days
INSERT INTO rentals ("ItemId", "OwnerId", "BorrowerId", "StartDate", "EndDate", "Status", "CreatedAt", "UpdatedAt")
SELECT
    (SELECT "Id" FROM items WHERE "Title" = 'Power Drill Set'
        AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com')),
    (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'),
    (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com'),
    CURRENT_DATE + 5,
    CURRENT_DATE + 8,
    'Approved',
    NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM rentals
    WHERE "ItemId"     = (SELECT "Id" FROM items WHERE "Title" = 'Power Drill Set'
                            AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'bob@example.com'))
      AND "BorrowerId" = (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com')
      AND "StartDate"  = CURRENT_DATE + 5
);

-- 4. Requested — Alice requested Electric Keyboard from Carol, awaiting approval
INSERT INTO rentals ("ItemId", "OwnerId", "BorrowerId", "StartDate", "EndDate", "Status", "CreatedAt", "UpdatedAt")
SELECT
    (SELECT "Id" FROM items WHERE "Title" = 'Electric Keyboard'
        AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')),
    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
    (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com'),
    CURRENT_DATE + 3,
    CURRENT_DATE + 6,
    'Requested',
    NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM rentals
    WHERE "ItemId"     = (SELECT "Id" FROM items WHERE "Title" = 'Electric Keyboard'
                            AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'))
      AND "BorrowerId" = (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com')
      AND "StartDate"  = CURRENT_DATE + 3
);

-- 5. Rejected — Alice's request for Sleeping Bag was rejected by Carol
INSERT INTO rentals ("ItemId", "OwnerId", "BorrowerId", "StartDate", "EndDate", "Status", "CreatedAt", "UpdatedAt")
SELECT
    (SELECT "Id" FROM items WHERE "Title" = 'Sleeping Bag'
        AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com')),
    (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'),
    (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com'),
    CURRENT_DATE + 1,
    CURRENT_DATE + 2,
    'Rejected',
    NOW(), NOW()
WHERE NOT EXISTS (
    SELECT 1 FROM rentals
    WHERE "ItemId"     = (SELECT "Id" FROM items WHERE "Title" = 'Sleeping Bag'
                            AND "OwnerId" = (SELECT "Id" FROM users WHERE "Email" = 'carol@example.com'))
      AND "BorrowerId" = (SELECT "Id" FROM users WHERE "Email" = 'alice@example.com')
      AND "StartDate"  = CURRENT_DATE + 1
);
