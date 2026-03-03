import { NavLink, useNavigate } from 'react-router-dom';
import { useTranslation } from '../../context/LanguageContext';
import './Sidebar.css';

export default function Sidebar({ isOpen, onClose }) {
  const navigate = useNavigate();
  const { t } = useTranslation();

  const goTo = (path) => {
    navigate(path);
    onClose?.();
  };

  return (
    <>
      <aside className={`app-sidebar ${isOpen ? 'sidebar-open' : ''}`} aria-hidden={!isOpen}>
        <div className="sidebar-header">
          <div className="sidebar-logo" onClick={() => goTo('/dashboard')} role="button" tabIndex={0} onKeyDown={(e) => e.key === 'Enter' && goTo('/dashboard')}>
            <span className="sidebar-logo-icon">◆ ◇</span>
            <span className="sidebar-logo-text">ELEMENTUM ANALYTICS</span>
          </div>
          <button type="button" className="sidebar-close" onClick={onClose} aria-label={t('nav.closeMenu')}>
            ✕
          </button>
        </div>
        <nav className="sidebar-nav">
          <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'sidebar-link active' : 'sidebar-link'} onClick={onClose}>
            <span className="sidebar-link-icon" aria-hidden>⌂</span>
            <span>{t('nav.dashboard')}</span>
          </NavLink>
          <NavLink to="/reports" className={({ isActive }) => isActive ? 'sidebar-link active' : 'sidebar-link'} onClick={onClose}>
            <span className="sidebar-link-icon" aria-hidden>▤</span>
            <span>{t('nav.reports')}</span>
          </NavLink>
          <NavLink to="/settings" className={({ isActive }) => isActive ? 'sidebar-link active' : 'sidebar-link'} onClick={onClose}>
            <span className="sidebar-link-icon" aria-hidden>⚙</span>
            <span>{t('nav.settings')}</span>
          </NavLink>
        </nav>
      </aside>
    </>
  );
}
