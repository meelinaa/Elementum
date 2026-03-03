/**
 * Übersetzungen DE / EN
 * Zugriff: t('nav.dashboard') → "Dashboard" / "Dashboard"
 */
export const translations = {
  nav: {
    dashboard: { de: 'Dashboard', en: 'Dashboard' },
    reports: { de: 'Reports', en: 'Reports' },
    settings: { de: 'Einstellungen', en: 'Settings' },
    openMenu: { de: 'Menü öffnen', en: 'Open menu' },
    closeMenu: { de: 'Menü schließen', en: 'Close menu' },
  },

  dashboard: {
    title: { de: 'ELEMENTE ÜBERSICHT', en: 'ELEMENTS OVERVIEW' },
    last30Days: { de: 'Letzte 30 Tage', en: 'Last 30 Days' },
    last7Days: { de: 'Letzte 7 Tage', en: 'Last 7 Days' },
    last24Hours: { de: 'Letzte 24 Stunden', en: 'Last 24 Hours' },
    refresh: { de: 'Aktualisieren', en: 'Refresh' },
    quickInfo: { de: 'QUICK INFO', en: 'QUICK INFO' },
    commonUses: { de: 'Verwendung:', en: 'Common uses:' },
    majorProducers: { de: 'Hauptproduzenten:', en: 'Major producers:' },
  },

  elements: {
    gold: { de: 'Gold', en: 'Gold' },
    silver: { de: 'Silber', en: 'Silver' },
    platinum: { de: 'Platin', en: 'Platinum' },
    palladium: { de: 'Palladium', en: 'Palladium' },
    goldUses: { de: 'Schmuck, Investition', en: 'Jewelry, Investment' },
    goldProducers: { de: 'China, Australien', en: 'China, Australia' },
    silverUses: { de: 'Elektronik, Industrie', en: 'Electronics, Industrial' },
    silverProducers: { de: 'Mexiko, Peru', en: 'Mexico, Peru' },
    platinumUses: { de: 'Automobil, Schmuck', en: 'Automotive, Jewelry' },
    platinumProducers: { de: 'Südafrika, Russland', en: 'South Africa, Russia' },
    palladiumUses: { de: 'Katalysatoren, Elektronik', en: 'Catalysts, Electronics' },
    palladiumProducers: { de: 'Russland, Kanada', en: 'Russia, Canada' },
  },

  detail: {
    backToOverview: { de: '← Zurück zur Übersicht', en: '← Back to Overview' },
    overviewPerformance: { de: 'ÜBERSICHT & PERFORMANCE', en: 'OVERVIEW & PERFORMANCE' },
    marketSummary: { de: 'Marktübersicht', en: 'Market Summary' },
    performanceMetrics: { de: 'Performance-Kennzahlen', en: 'Performance Metrics' },
    technicalIndicators: { de: 'Technische Indikatoren', en: 'Technical Indicators' },
    latestNews: { de: 'Aktuelle {name}-Nachrichten', en: 'Latest {name} News' },
    marketCap: { de: 'Marktkapitalisierung: $12,3B', en: 'Market Cap: $12.3T' },
    volume24h: { de: '24h-Volumen: $185B', en: '24h Volume: $185B' },
    circulatingSupply: { de: 'Umlaufmenge: 205k Tonnen', en: 'Circulating Supply: 205k tonnes' },
    weekHigh52: { de: '52-Wochen-Hoch: $2.080,00', en: '52 Week High: $2,080.00' },
    weekLow52: { de: '52-Wochen-Tief: $1.610,00', en: '52 Week Low: $1,610.00' },
    ytdReturn: { de: 'Jahresrendite: +8,2%', en: 'Year-to-Date Return: +8.2%' },
    rsi: { de: 'RSI (14): 62 (Neutral)', en: 'RSI (14): 62 (Neutral)' },
    macd: { de: 'MACD: Bullish Crossover', en: 'MACD: Bullish Crossover' },
    movingAverages: { de: 'Gleitende Mittel (56/200): Darüber', en: 'Moving Averages (56/200): Above' },
    news1: { de: '{name} steigt wegen Inflationsängsten', en: '{name} Rallies on Inflation Fears' },
    news2: { de: 'Zentralbanken erhöhen {name}-Reserven', en: 'Central Banks Increase {name} Reserves' },
    news3: { de: 'Neue {name}-Fördertechnik steigert Effizienz', en: 'New {name} Mining Tech Boosts Efficiency' },
    timeAgo2h: { de: 'vor 2 Std.', en: '2h ago' },
    timeAgo1d: { de: 'vor 1 Tag', en: '1d ago' },
    timeAgo2d: { de: 'vor 2 Tagen', en: '2d ago' },
    newsLoading: { de: 'Nachrichten werden geladen…', en: 'Loading news…' },
    newsError: { de: 'Nachrichten konnten nicht geladen werden.', en: 'Could not load news.' },
    noNews: { de: 'Keine Nachrichten gefunden.', en: 'No news found.' },
    readMore: { de: 'Zum Artikel →', en: 'Read more →' },
  },

  footer: {
    apiStatus: { de: 'API-Status: Verbunden', en: 'API Status: Connected' },
    lastUpdate: { de: 'Letzte Aktualisierung:', en: 'last Update:' },
    time: { de: 'Uhrzeit:', en: 'Time:' },
  },

  settings: {
    title: { de: 'Einstellungen', en: 'Settings' },
    appearance: { de: 'Darstellung', en: 'Appearance' },
    design: { de: 'Design', en: 'Design' },
    language: { de: 'Sprache', en: 'Language' },
    german: { de: 'Deutsch', en: 'German' },
    english: { de: 'Englisch', en: 'English' },
    light: { de: 'Hell', en: 'Light' },
    dark: { de: 'Dunkel', en: 'Dark' },
    darkModeActive: { de: 'Dunkelmodus aktiv – klicken für Hell', en: 'Dark mode active – click for Light' },
    lightModeActive: { de: 'Hellmodus aktiv – klicken für Dunkel', en: 'Light mode active – click for Dark' },
  },
};

/**
 * Holt Übersetzung für Key (z. B. 'nav.dashboard') und ersetzt Platzhalter wie {name}.
 * @param {object} dict - translations Objekt
 * @param {string} key - Pfad wie 'nav.dashboard'
 * @param {string} lang - 'de' oder 'en'
 * @param {object} [replace] - z. B. { name: 'Gold' } für {name} im Text
 */
export function getTranslation(dict, key, lang, replace = {}) {
  const keys = key.split('.');
  let value = dict;
  for (const k of keys) {
    value = value?.[k];
  }
  const str = typeof value === 'object' && value !== null ? value[lang] ?? value.en ?? '' : String(value ?? '');
  return Object.entries(replace).reduce((s, [k, v]) => s.replace(new RegExp(`\\{${k}\\}`, 'g'), v), str);
}
