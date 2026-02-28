# Database init scripts

SQL files in this folder are mounted into the container at `/docker-entrypoint-initdb.d/`. The **official MySQL image** runs every `.sql` (and `.sh`) in that directory automatically on **first start only** (when the data volume is empty) — no extra config needed. Files run in alphabetical order.

- **01-schema.sql** — Creates `metals` and `price_history` (including open/low/high prices) and seeds the four metals (XAU, XAG, XPT, XPD).

To re-run init from scratch: remove the volume and start again, e.g. `docker compose down -v` then `docker compose up -d`.
