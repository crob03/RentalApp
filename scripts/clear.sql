-- Removes all seed data and resets identity sequences.
-- Order: rentals first (FK to items + users), then items (FK to categories + users),
-- then categories and users. CASCADE handles any residual FK dependencies.
TRUNCATE TABLE rentals, items, categories, users RESTART IDENTITY CASCADE;
