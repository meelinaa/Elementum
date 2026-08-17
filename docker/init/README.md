# Database init scripts

SQL files in this folder are mounted into the container at `/docker-entrypoint-initdb.d/`. The **official MySQL image** runs every `.sql` (and `.sh`) in that directory automatically on **first start only** (when the data volume is empty) — no extra config needed. Files run in alphabetical order.

- **01-schema.sql** — Creates `metals` and `price_history` (including open/low/high prices) and seeds the four metals (XAU, XAG, XPT, XPD).
- **02-price-history-earlier-40days.sql** — Optional: price history from 2026-01-19 through 2026-03-19 (60 calendar days × 3 metals) with OHLC and per-gram karat prices (`INSERT IGNORE`). Regenerate with `python generate_price_history_seed.py` in this folder.

To re-run init from scratch: remove the volume and start again, e.g. `docker compose down -v` then `docker compose up -d`.

---

See [../../docs/screenshots/README.md](../../docs/screenshots/README.md) for a short checklist.

## Related documentation

- [Elementum-ServiceApi/API-Endpoints.md](../../Elementum-Services/Elementum-ServiceApi/API-Endpoints.md) — API routes used by the CLI
- [Elementum-Database/README.md](../README.md) — Docker stack (API + DB + worker)
