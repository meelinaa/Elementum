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
    exchange VARCHAR(50),
    symbol VARCHAR(100),

    -- Timestamps (API)
    reference_timestamp BIGINT,
    open_time BIGINT,
    entry_date DATE NOT NULL,

    -- Main price & OHLC (DECIMAL for financial precision)
    price DECIMAL(18, 4) NOT NULL,
    prev_close_price DECIMAL(18, 4),
    open_price DECIMAL(18, 4),
    low_price DECIMAL(18, 4),
    high_price DECIMAL(18, 4),

    -- Change
    ch DECIMAL(18, 4),
    chp DECIMAL(18, 4),

    -- Ask/Bid
    ask DECIMAL(18, 4),
    bid DECIMAL(18, 4),

    -- Price per gram by purity (24k, 22k, 21k, 20k, 18k, 16k, 14k, 10k)
    price_gram_24k DECIMAL(18, 4),
    price_gram_22k DECIMAL(18, 4),
    price_gram_21k DECIMAL(18, 4),
    price_gram_20k DECIMAL(18, 4),
    price_gram_18k DECIMAL(18, 4),
    price_gram_16k DECIMAL(18, 4),
    price_gram_14k DECIMAL(18, 4),
    price_gram_10k DECIMAL(18, 4),

    FOREIGN KEY (metal_id) REFERENCES metals(id) ON DELETE CASCADE,
    UNIQUE KEY uk_metal_currency_date (metal_id, currency, entry_date),
    INDEX idx_history_lookup (metal_id, currency, entry_date)
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 3. Distributed locking (for multi-instance concurrency coordination)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS distributed_locks (
    resource VARCHAR(128) PRIMARY KEY,
    acquired_by VARCHAR(128) NOT NULL,
    acquired_at_utc DATETIME NOT NULL,
    expires_at_utc DATETIME NOT NULL
) ENGINE=InnoDB;

-- -----------------------------------------------------------------------------
-- 4. Daily price summaries / candlesticks (Min/Max/Open/Close at 22:00)
-- -----------------------------------------------------------------------------
CREATE TABLE IF NOT EXISTS daily_price_summaries (
    id INT AUTO_INCREMENT PRIMARY KEY,
    metal_id INT NOT NULL,
    currency VARCHAR(3) NOT NULL,
    entry_date DATE NOT NULL,
    open_price DECIMAL(18, 4) NOT NULL,
    high_price DECIMAL(18, 4) NOT NULL,
    low_price DECIMAL(18, 4) NOT NULL,
    close_price DECIMAL(18, 4) NOT NULL,
    exchange_rate_usd_eur DECIMAL(18, 8),
    created_at_utc DATETIME NOT NULL,
    updated_at_utc DATETIME NOT NULL,
    FOREIGN KEY (metal_id) REFERENCES metals(id) ON DELETE CASCADE,
    UNIQUE KEY uk_candle_metal_currency_date (metal_id, currency, entry_date),
    INDEX idx_candles_lookup (metal_id, currency, entry_date)
) ENGINE=InnoDB;

