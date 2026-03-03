import { NavLink, useNavigate } from 'react-router-dom';
import './Header.css';

export default function Header() {
  const navigate = useNavigate();

  return (
    <header>
      <div className="header-logo" onClick={() => navigate('/dashboard')} role="button" tabIndex={0} onKeyDown={(e) => e.key === 'Enter' && navigate('/dashboard')}>
        <span className="header-logo-text" style={{ color: 'black' }}>Elementum</span>
      </div>
      <nav className="nav-menu">
        <NavLink to="/dashboard" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
          Dashboard
        </NavLink>
        <NavLink to="/test" className={({ isActive }) => isActive ? 'nav-link active' : 'nav-link'}>
          Test
        </NavLink>
      </nav>
    </header>
  );
}
