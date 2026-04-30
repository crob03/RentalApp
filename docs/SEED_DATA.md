# Seed Data Reference

This document describes the development seed data applied by the `SeedDevelopmentData` migration (`20260429120000`). It is intended as a quick reference for developers working locally.

---

## Categories

| ID* | Name    | Slug      |
|-----|---------|-----------|
| —   | Music   | `music`   |
| —   | Camping | `camping` |
| —   | DIY     | `diy`     |
| —   | Games   | `games`   |

\* IDs are auto-assigned by the database identity sequence and may vary between environments.

---

## Users

| Name         | Email                 | Password     | Items |
|--------------|-----------------------|--------------|-------|
| Alice Smith  | alice@example.com     | `Password1!` | 0     |
| Bob Jones    | bob@example.com       | `Password2!` | 3     |
| Carol White  | carol@example.com     | `Password3!` | 5     |

Passwords are hashed with BCrypt at migration run time. The plaintext values above are what you use to log in.

---

## Items

### Bob Jones — 3 items

| Title           | Category | Daily Rate | Location (lat, lon)      |
|-----------------|----------|------------|--------------------------|
| Acoustic Guitar | Music    | £5.00      | 55.9533°N, 3.1883°W      |
| Two-Man Tent    | Camping  | £10.00     | 55.9486°N, 3.2030°W      |
| Power Drill Set | DIY      | £8.00      | 55.9626°N, 3.1719°W      |

### Carol White — 5 items

| Title                | Category | Daily Rate | Location (lat, lon)      |
|----------------------|----------|------------|--------------------------|
| Electric Keyboard    | Music    | £15.00     | 55.9419°N, 3.1878°W      |
| Sleeping Bag         | Camping  | £3.00      | 55.9501°N, 3.2127°W      |
| Jigsaw Puzzle Bundle | Games    | £2.00      | 55.9442°N, 3.1546°W      |
| Circular Saw         | DIY      | £20.00     | 55.9614°N, 3.1573°W      |
| Board Game Bundle    | Games    | £5.00      | 55.9384°N, 3.2290°W      |

All items are seeded with `IsAvailable = true`. Locations are scattered across Edinburgh.

---

## Applying / Reverting

The seed data is applied automatically when running all migrations:

```bash
dotnet ef database update --project RentalApp.Migrations
# or via Docker
docker-compose up
```

To revert the seed data only (leaves schema intact):

```bash
dotnet ef database update 20260428202225_UpdateItemSchema --project RentalApp.Migrations
```

> **Note:** Rolling back this migration will cascade-delete any items belonging to the seeded categories, not just the seeded items. Avoid rolling back if you have added your own test data against these categories.
