import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from '../../context/LanguageContext';
import './Dashboard.css';
import '../../style/Style.css';

const ELEMENT_IDS = ['gold', 'silver', 'platinum', 'palladium'];

const ELEMENTS_DATA = {
  gold: { symbol: 'XAU', price: '1,950.20', change: 0.5, changeLabel: '+0.5%', color: '#c9a227', usesKey: 'goldUses', producersKey: 'goldProducers' },
  silver: { symbol: 'XAG', price: '23.15', change: -0.2, changeLabel: '-0.2%', color: '#a0a0a0', usesKey: 'silverUses', producersKey: 'silverProducers' },
  platinum: { symbol: 'XPT', price: '980.00', change: 1.2, changeLabel: '+1.2%', color: '#c0c0c0', usesKey: 'platinumUses', producersKey: 'platinumProducers' },
  palladium: { symbol: 'XPD', price: '1,250.50', change: 0.8, changeLabel: '+0.8%', color: '#6b8eac', usesKey: 'palladiumUses', producersKey: 'palladiumProducers' },
};

function Sparkline({ color }) {
  const points = [0, 20, 15, 40, 25, 60, 45, 80, 55, 100];
  const path = points.map((y, i) => `${(i / (points.length - 1)) * 100},${100 - y}`).join(' ');
  return (
    <svg viewBox="0 0 100 100" preserveAspectRatio="none" className="card-sparkline">
      <polyline fill="none" stroke={color} strokeWidth="2" points={path} />
    </svg>
  );
}

export default function Dashboard() {
  const { t } = useTranslation();
  const [period, setPeriod] = useState('last30Days');

  const periodOptions = [
    { value: 'last30Days', key: 'dashboard.last30Days' },
    { value: 'last7Days', key: 'dashboard.last7Days' },
    { value: 'last24Hours', key: 'dashboard.last24Hours' },
  ];

  return (
    <div className="dashboard-page">
      <div className="dashboard-header">
        <h1 className="dashboard-title">{t('dashboard.title')}</h1>
        <div className="dashboard-controls">
          <select value={period} onChange={(e) => setPeriod(e.target.value)} className="dashboard-select">
            {periodOptions.map((opt) => (
              <option key={opt.value} value={opt.value}>{t(opt.key)}</option>
            ))}
          </select>
          <button type="button" className="dashboard-refresh" aria-label={t('dashboard.refresh')}>↻</button>
        </div>
      </div>

      <div className="elements-grid">
        {ELEMENT_IDS.map((id) => {
          const el = ELEMENTS_DATA[id];
          return (
            <Link key={id} to={`/element/${id}`} className="element-card">
              <div className="element-card-icon" style={{ color: el.color }}>▬ ▬ ▬</div>
              <h2 className="element-card-title">{t(`elements.${id}`)} ({el.symbol})</h2>
              <p className="element-card-price">${el.price}</p>
              <p className={`element-card-change ${el.change >= 0 ? 'positive-change' : 'negative-change'}`}>{el.changeLabel}</p>
              <div className="element-card-chart">
                <Sparkline color={el.color} />
              </div>
              <div className="element-card-quickinfo">
                <p className="quickinfo-title">{t('dashboard.quickInfo')}</p>
                <ul>
                  <li>{t('dashboard.commonUses')} {t(`elements.${el.usesKey}`)}</li>
                  <li>{t('dashboard.majorProducers')} {t(`elements.${el.producersKey}`)}</li>
                </ul>
              </div>
            </Link>
          );
        })}
      </div>
    </div>
  );
}
