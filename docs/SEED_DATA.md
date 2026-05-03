# Seed Data Reference

This document describes the development seed data applied by `make seed`.

---

## How to seed

```bash
make seed    # Insert development data (categories, users, items, rentals)
make clear   # Truncate all seeded tables and reset sequences
```

`make seed` is idempotent — safe to re-run. `make clear` is destructive.

### First-time setup

```bash
docker-compose up -d db   # Start the database
make migrate              # Apply schema migrations
make seed                 # Populate seed data
```

### Recreating an existing dev database

If you have a stale local database (e.g. after migration history changes):

```bash
docker-compose down -v    # Wipe DB volume
docker-compose up         # Recreate and migrate
make seed                 # Reseed
```

---

## Categories

| Name    | Slug      |
|---------|-----------|
| Music   | `music`   |
| Camping | `camping` |
| DIY     | `diy`     |
| Games   | `games`   |

---

## Users

| Name         | Email                 | Password     |
|--------------|-----------------------|--------------|
| Alice Smith  | alice@example.com     | `Password1!` |
| Bob Jones    | bob@example.com       | `Password2!` |
| Carol White  | carol@example.com     | `Password3!` |

Passwords are hashed with BCrypt via PostgreSQL's `pgcrypto` extension at seed time.

---

## Items

### Bob Jones — 3 items

| Title           | Category | Daily Rate | Location (lat, lon)   |
|-----------------|----------|------------|-----------------------|
| Acoustic Guitar | Music    | £5.00      | 55.9533°N, 3.1883°W   |
| Two-Man Tent    | Camping  | £10.00     | 55.9486°N, 3.2030°W   |
| Power Drill Set | DIY      | £8.00      | 55.9626°N, 3.1719°W   |

### Carol White — 5 items

| Title                | Category | Daily Rate | Location (lat, lon)   |
|----------------------|----------|------------|-----------------------|
| Electric Keyboard    | Music    | £15.00     | 55.9419°N, 3.1878°W   |
| Sleeping Bag         | Camping  | £3.00      | 55.9501°N, 3.2127°W   |
| Jigsaw Puzzle Bundle | Games    | £2.00      | 55.9442°N, 3.1546°W   |
| Circular Saw         | DIY      | £20.00     | 55.9614°N, 3.1573°W   |
| Board Game Bundle    | Games    | £5.00      | 55.9384°N, 3.2290°W   |

All items seeded with `IsAvailable = true`. Locations are scattered across Edinburgh.

---

## Rentals

Alice is the borrower in all seed rentals. All dates are relative to `CURRENT_DATE` at seed time.

| # | Item                | Owner | Start      | End        | Status       |
|---|---------------------|-------|------------|------------|--------------|
| 1 | Acoustic Guitar     | Bob   | Today − 10 | Today − 7  | `Completed`  |
| 2 | Two-Man Tent        | Bob   | Today − 3  | Today + 4  | `OutForRent` |
| 3 | Power Drill Set     | Bob   | Today + 5  | Today + 8  | `Approved`   |
| 4 | Electric Keyboard   | Carol | Today + 3  | Today + 6  | `Requested`  |
| 5 | Sleeping Bag        | Carol | Today + 1  | Today + 2  | `Rejected`   |

Rental dates are always anchored to when `make seed` was last run, so they never go stale.
