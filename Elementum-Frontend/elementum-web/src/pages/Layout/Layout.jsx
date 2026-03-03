import { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { useTranslation } from '../../context/LanguageContext';
import Sidebar from '../Sidebar/Sidebar';
import Footer from '../Footer/Footer';
import '../Sidebar/Sidebar.css';
import '../Footer/Footer.css';
import '../../style/Style.css';
import './Layout.css';

export default function Layout() {
  const [menuOpen, setMenuOpen] = useState(false);
  const { t } = useTranslation();

  return (
    <div className="app-layout">
      {!menuOpen && (
        <button
          type="button"
          className="burger-btn"
          onClick={() => setMenuOpen(true)}
          aria-label={t('nav.openMenu')}
          aria-expanded={false}
        >
          <span className="burger-line" />
          <span className="burger-line" />
          <span className="burger-line" />
        </button>
      )}

      <div className={`sidebar-container ${menuOpen ? 'sidebar-container--open' : ''}`}>
        <Sidebar isOpen={menuOpen} onClose={() => setMenuOpen(false)} />
      </div>

      <div className={`app-content-wrap ${menuOpen ? 'app-content-wrap--menu-open' : ''}`}>
        <div className="app-content">
          <main className="main-body">
            <Outlet />
          </main>
        </div>
        <Footer />
      </div>
    </div>
  );
}
