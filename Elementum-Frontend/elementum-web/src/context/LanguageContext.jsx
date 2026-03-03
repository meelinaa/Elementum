import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { translations, getTranslation } from '../translations';

const LANG_KEY = 'elementum-language';

const LanguageContext = createContext(null);

export function LanguageProvider({ children }) {
  const [language, setLanguageState] = useState(() => {
    return localStorage.getItem(LANG_KEY) || 'de';
  });

  useEffect(() => {
    localStorage.setItem(LANG_KEY, language);
    document.documentElement.lang = language === 'de' ? 'de' : 'en';
  }, [language]);

  const setLanguage = useCallback((lang) => {
    setLanguageState(lang === 'en' ? 'en' : 'de');
  }, []);

  const t = useCallback(
    (key, replace = {}) => getTranslation(translations, key, language, replace),
    [language]
  );

  return (
    <LanguageContext.Provider value={{ language, setLanguage, t }}>
      {children}
    </LanguageContext.Provider>
  );
}

export function useTranslation() {
  const ctx = useContext(LanguageContext);
  if (!ctx) throw new Error('useTranslation must be used within LanguageProvider');
  return ctx;
}
