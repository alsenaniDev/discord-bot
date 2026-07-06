import { Navigate, Route, Routes } from 'react-router-dom';
import { Layout } from './components/Layout';
import { GamesHubPage } from './pages/GamesHubPage';
import { QuizPage } from './pages/QuizPage';
import { LeaderboardPage } from './pages/LeaderboardPage';
import { RoulettePage } from './pages/RoulettePage';
import { RouletteRoomPage } from './pages/RouletteRoomPage';
import { StorePage } from './pages/StorePage';

export default function App() { return <Routes><Route element={<Layout />}><Route path="/" element={<Navigate to="/games" replace />} /><Route path="/games" element={<GamesHubPage />} /><Route path="/games/quiz" element={<QuizPage />} /><Route path="/games/roulette" element={<RoulettePage />} /><Route path="/games/roulette/room/:roomId" element={<RouletteRoomPage />} /><Route path="/store" element={<StorePage />} /><Route path="/leaderboard" element={<LeaderboardPage />} /><Route path="/error" element={<main className="center-state"><h1>تعذر فتح مركز الألعاب.</h1></main>} /><Route path="*" element={<Navigate to="/games" replace />} /></Route></Routes>; }
