import React from 'react';
import { useTheme } from '../../context/ThemeContext';
import { useTranslation } from '../../context/LanguageContext';
import './Settings.css';
import '../../style/Style.css';

export default function Settings() {
  const { theme, setTheme } = useTheme();
  const { t, language, setLanguage } = useTranslation();

  return (
    <div className="settings-page">
      <h1 className="settings-title">{t('settings.title')}</h1>
      <section className="settings-section">
        <h2 className="settings-section-title">{t('settings.appearance')}</h2>
        <div className="settings-option settings-option-row">
          <span className="settings-label">{t('settings.design')}</span>
          <button
            type="button"
            role="switch"
            aria-checked={theme === 'dark'}
            aria-label={theme === 'dark' ? t('settings.darkModeActive') : t('settings.lightModeActive')}
            className={`settings-switch ${theme === 'dark' ? 'settings-switch--dark' : ''}`}
            onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
          >
            <span className="settings-switch-track">
              <span className="settings-switch-knob" />
            </span>
            <span className="settings-switch-labels">
              <span>{t('settings.light')}</span>
              <span>{t('settings.dark')}</span>
            </span>
          </button>
        </div>
        <div className="settings-option settings-option-row">
          <span className="settings-label">{t('settings.language')}</span>
          <select
            value={language}
            onChange={(e) => setLanguage(e.target.value)}
            className="settings-select"
            aria-label={t('settings.language')}
          >
            <option value="de">{t('settings.german')}</option>
            <option value="en">{t('settings.english')}</option>
          </select>
        </div>
      </section>
    </div>
  );
}
