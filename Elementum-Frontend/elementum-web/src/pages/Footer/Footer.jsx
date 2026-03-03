import React, { useState, useEffect } from 'react';
import { useTranslation } from '../../context/LanguageContext';
import './Footer.css';

function formatDateTime(date, locale) {
  return date.toLocaleString(locale === 'de' ? 'de-DE' : 'en-GB', {
    day: '2-digit',
    month: '2-digit',
    year: 'numeric',
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });
}

function formatTime(date, locale) {
  return date.toLocaleTimeString(locale === 'de' ? 'de-DE' : 'en-GB', {
    hour: '2-digit',
    minute: '2-digit',
    second: '2-digit',
    hour12: false,
  });
}

const fixedLastUpdateDate = new Date();

export default function Footer() {
  const { t, language } = useTranslation();
  const [time, setTime] = useState(() => formatTime(new Date(), language));
  const fixedLastUpdate = formatDateTime(fixedLastUpdateDate, language);

  useEffect(() => {
    setTime(formatTime(new Date(), language));
    const id = setInterval(() => setTime(formatTime(new Date(), language)), 1000);
    return () => clearInterval(id);
  }, [language]);

  return (
    <footer className="app-footer">
      <span className="app-footer-left">
        <span className="app-footer-status">{t('footer.apiStatus')}</span>
        <span className="app-footer-last-update"> {t('footer.lastUpdate')} {fixedLastUpdate}</span>
      </span>
      <span className="app-footer-time">{t('footer.time')} {time}</span>
    </footer>
  );
}
