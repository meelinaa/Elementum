-- =============================================================================
-- Elementum-Vault: Initial schema (MySQL 8)
--
-- How Docker runs this file:
-- The official MySQL image runs every .sql and .sh inside /docker-entrypoint-initdb.d/
-- automatically on first container start (when the data directory is empty).
-- This folder is mounted from ./init in docker-compose.yml.
-- =============================================================================

-- -----------------------------------------------------------------------------
-- 1. Master data: metals (XAU, XAG, XPT, XPD)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS metals (
    id INT AUTO_INCREMENT PRIMARY KEY,
    symbol VARCHAR(10) UNIQUE NOT NULL,
    name VARCHAR(50) NOT NULL,
    created_at TIMESTAMP DEFAULT CURRENT_TIMESTAMP
) ENGINE=InnoDB;

INSERT IGNORE INTO metals (symbol, name) VALUES
('XAU', 'Gold'),
('XAG', 'Silver'),
('XPT', 'Platinum'),
('XPD', 'Palladium');

-- -----------------------------------------------------------------------------
-- 2. Daily price history (OHLC + close, one row per metal/currency/day)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS price_history (
    id BIGINT AUTO_INCREMENT PRIMARY KEY,
    metal_id INT NOT NULL,
    currency VARCHAR(3) NOT NULL,

    -- Prices (DECIMAL for financial precision)
    price_unze DECIMAL(18, 4) NOT NULL,
    price_gram_24k DECIMAL(18, 4),
    prev_close_price DECIMAL(18, 4),
    open_price DECIMAL(18, 4),
    low_price DECIMAL(18, 4),
    high_price DECIMAL(18, 4),

    -- API metadata
    reference_timestamp BIGINT,
    entry_date DATE NOT NULL,

    FOREIGN KEY (metal_id) REFERENCES metals(id) ON DELETE CASCADE,
    UNIQUE KEY uk_metal_currency_date (metal_id, currency, entry_date),
    INDEX idx_history_lookup (metal_id, currency, entry_date)
) ENGINE=InnoDB;
