import { createRoot } from 'react-dom/client';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import { ActivityProvider } from './context/ActivityProvider';
import './styles.css';
import './styles/game-theme.css';

createRoot(document.getElementById('root')!).render(<BrowserRouter><ActivityProvider><App /></ActivityProvider></BrowserRouter>);
