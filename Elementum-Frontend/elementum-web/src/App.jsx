import { createBrowserRouter, createRoutesFromElements, Route, RouterProvider } from 'react-router-dom';
import Layout from './pages/Layout/Layout';
import Dashboard from './pages/Dashboard/Dashboard';
import ElementDetail from './pages/ElementDetail/ElementDetail';
import Settings from './pages/Settings/Settings';
import TestPage from './pages/TestPage/TestPage';
import './App.css';

const router = createBrowserRouter(
  createRoutesFromElements(
    <Route path="/" element={<Layout />}>
      <Route index element={<Dashboard />} />
      <Route path="dashboard" element={<Dashboard />} />
      <Route path="element/:id" element={<ElementDetail />} />
      <Route path="reports" element={<TestPage />} />
      <Route path="settings" element={<Settings />} />
      <Route path="test" element={<TestPage />} />
    </Route>
  )
);

function App() {
  return <RouterProvider router={router} />;
}

export default App;
