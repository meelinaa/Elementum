import React, { useState, useEffect } from 'react';
import { useParams, Link } from 'react-router-dom';
import { useTranslation } from '../../context/LanguageContext';
import { fetchNewsForQuery } from '../../services/newsApi';
import './ElementDetail.css';
import '../../style/Style.css';

const ELEMENT_DATA = {
  gold: { symbol: 'XAU', price: '1,950.20', change: 0.5, changeLabel: '+0.5%' },
  silver: { symbol: 'XAG', price: '23.15', change: -0.2, changeLabel: '-0.2%' },
  platinum: { symbol: 'XPT', price: '980.00', change: 1.2, changeLabel: '+1.2%' },
  palladium: { symbol: 'XPD', price: '1,250.50', change: 0.8, changeLabel: '+0.8%' },
};

/** Englischer Suchbegriff für die News-API (bessere Treffer) */
const ELEMENT_SEARCH_QUERY = {
  gold: 'Gold price',
  silver: 'Silver price',
  platinum: 'Platinum price',
  palladium: 'Palladium price',
};

function SimpleChart({ color }) {
  const points = [10, 25, 20, 45, 35, 60, 50, 75, 65, 90, 70, 85];
  const w = 100; const h = 60;
  const path = points.map((y, i) => `${(i / (points.length - 1)) * w},${h - (y / 100) * h}`).join(' ');
  return (
    <svg viewBox={`0 0 ${w} ${h}`} preserveAspectRatio="none" className="detail-chart-svg">
      <defs>
        <linearGradient id="chartGrad" x1="0" y1="0" x2="0" y2="1">
          <stop offset="0%" stopColor={color} stopOpacity="0.4" />
          <stop offset="100%" stopColor={color} stopOpacity="0" />
        </linearGradient>
      </defs>
      <polygon fill="url(#chartGrad)" points={`0,${h} ${path} ${w},${h}`} />
      <polyline fill="none" stroke={color} strokeWidth="2" points={path} />
    </svg>
  );
}

export default function ElementDetail() {
  const { id } = useParams();
  const { t, language } = useTranslation();
  const [period, setPeriod] = useState('last30Days');
  const [timeRange, setTimeRange] = useState('1D');
  const [news, setNews] = useState([]);
  const [newsLoading, setNewsLoading] = useState(true);
  const [newsError, setNewsError] = useState(null);

  const data = ELEMENT_DATA[id] || ELEMENT_DATA.gold;
  const elementName = t(`elements.${id}`);
  const chartColor = id === 'gold' ? '#c9a227' : id === 'silver' ? '#a0a0a0' : id === 'platinum' ? '#c0c0c0' : '#6b8eac';
  const searchQuery = ELEMENT_SEARCH_QUERY[id] || ELEMENT_SEARCH_QUERY.gold;

  useEffect(() => {
    let cancelled = false;
    setNewsLoading(true);
    setNewsError(null);

    fetchNewsForQuery(searchQuery, { num: 8, language: language === 'de' ? 'de' : 'en' })
      .then((results) => {
        if (!cancelled) {
          setNews(results);
          setNewsError(null);
        }
      })
      .catch(() => {
        if (!cancelled) {
          setNews([]);
          setNewsError(true);
        }
      })
      .finally(() => {
        if (!cancelled) setNewsLoading(false);
      });

    return () => { cancelled = true; };
  }, [id, searchQuery, language]);

  const periodOptions = [
    { value: 'last30Days', key: 'dashboard.last30Days' },
    { value: 'last7Days', key: 'dashboard.last7Days' },
    { value: 'last24Hours', key: 'dashboard.last24Hours' },
  ];

  return (
    <div className="element-detail-page">
      <p className="detail-back">
        <Link to="/dashboard">{t('detail.backToOverview')}</Link>
      </p>
      <div className="detail-header">
        <h1 className="detail-title">{elementName.toUpperCase()} {t('detail.overviewPerformance')}</h1>
        <div className="dashboard-controls">
          <select value={period} onChange={(e) => setPeriod(e.target.value)} className="dashboard-select">
            {periodOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>{t(opt.key)}</option>
            ))}
          </select>
          <button type="button" className="dashboard-refresh" aria-label={t('dashboard.refresh')}>↻</button>
        </div>
      </div>

      <div className="detail-grid">
        <section className="detail-price-card">
          <div className="detail-price-header">
            <span className="detail-metal-icon" style={{ color: chartColor }}>▬ ▬ ▬</span>
            <span>{elementName} ({data.symbol})</span>
          </div>
          <p className="detail-price">${data.price}</p>
          <p className={`detail-change ${data.change >= 0 ? 'positive-change' : 'negative-change'}`}>{data.changeLabel}</p>
          <div className="detail-chart">
            <SimpleChart color={chartColor} />
          </div>
          <div className="detail-time-tabs">
            {['1H', '1D', '1W', '1M', '1Y'].map((tab) => (
              <button
                key={tab}
                type="button"
                className={`detail-tab ${timeRange === tab ? 'active' : ''}`}
                onClick={() => setTimeRange(tab)}
              >
                {tab}
              </button>
            ))}
          </div>
        </section>

        <section className="detail-cards-row">
          <div className="detail-info-card">
            <h3>{t('detail.marketSummary')}</h3>
            <ul>
              <li>{t('detail.marketCap')}</li>
              <li>{t('detail.volume24h')}</li>
              <li>{t('detail.circulatingSupply')}</li>
            </ul>
          </div>
          <div className="detail-info-card">
            <h3>{t('detail.performanceMetrics')}</h3>
            <ul>
              <li>{t('detail.weekHigh52')}</li>
              <li>{t('detail.weekLow52')}</li>
              <li>{t('detail.ytdReturn')}</li>
            </ul>
          </div>
          <div className="detail-info-card">
            <h3>{t('detail.technicalIndicators')}</h3>
            <ul>
              <li>{t('detail.rsi')}</li>
              <li>{t('detail.macd')}</li>
              <li>{t('detail.movingAverages')}</li>
            </ul>
          </div>
        </section>

        <section className="detail-news-card">
          <h3>{t('detail.latestNews', { name: elementName })}</h3>
          {newsLoading && <p className="news-status">{t('detail.newsLoading')}</p>}
          {newsError && !newsLoading && <p className="news-status news-error">{t('detail.newsError')}</p>}
          {!newsLoading && !newsError && news.length === 0 && <p className="news-status">{t('detail.noNews')}</p>}
          {!newsLoading && news.length > 0 && (
            <ul className="news-list news-list-cards">
              {news.map((item, index) => (
                <li key={index} className="news-item">
                  <a href={item.link} target="_blank" rel="noopener noreferrer" className="news-item-link">
                    {item.thumbnail && (
                      <div className="news-item-image-wrap">
                        <img src={item.thumbnail} alt="" className="news-item-image" loading="lazy" />
                      </div>
                    )}
                    <div className="news-item-body">
                      <h4 className="news-item-title">{item.title}</h4>
                      {item.snippet && <p className="news-item-snippet">{item.snippet}</p>}
                      <span className="news-item-meta">
                        {item.source && <span className="news-item-source">{item.source}</span>}
                        {item.date && <span className="news-item-date">{item.date}</span>}
                      </span>
                      <span className="news-item-cta">{t('detail.readMore')}</span>
                    </div>
                  </a>
                </li>
              ))}
            </ul>
          )}
        </section>
      </div>
    </div>
  );
}
