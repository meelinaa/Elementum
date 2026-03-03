/**
 * Bing News API via SearchAPI.io
 * @see https://www.searchapi.io/docs/bing-news
 */

const API_BASE = 'https://www.searchapi.io/api/v1/search';

/**
 * Sucht News zu einem Suchbegriff (z. B. Metallname).
 * @param {string} query - Suchbegriff (z. B. "Gold", "Silver")
 * @param {object} [options] - num (Anzahl, max 50), language (z. B. "de", "en")
 * @returns {Promise<{ title: string, link: string, source: string, date: string, snippet: string, thumbnail?: string }[]>}
 */
export async function fetchNewsForQuery(query, options = {}) {
  const apiKey = import.meta.env.VITE_SEARCHAPI_API_KEY;
  if (!apiKey) {
    return [];
  }

  const params = new URLSearchParams({
    engine: 'bing_news',
    q: query,
    api_key: apiKey,
    num: String(options.num ?? 10),
    sort_by: 'most_recent',
  });

  if (options.language) {
    params.set('language', options.language);
  }

  const url = `${API_BASE}?${params.toString()}`;
  const res = await fetch(url);

  if (!res.ok) {
    throw new Error(`News API error: ${res.status}`);
  }

  const data = await res.json();
  const results = data.organic_results ?? [];

  return results.map((item) => ({
    title: item.title ?? '',
    link: item.link ?? '',
    source: item.source ?? '',
    date: item.date ?? '',
    snippet: item.snippet ?? '',
    thumbnail: item.thumbnail || null,
  }));
}
