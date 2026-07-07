import type { CSSProperties } from 'react';
import type { RoulettePlayer } from '../../types';

interface RouletteWheelProps {
  players: RoulettePlayer[];
  spinning: boolean;
  selectedUserDiscordId?: string | null;
  selectedIndex?: number | null;
}

const colors = ['#7c3aed', '#2563eb', '#db2777', '#f59e0b', '#06b6d4', '#22c55e', '#ef4444', '#8b5cf6'];
const initials = (value: string) => value.trim().slice(0, 2) || '؟';

export function RouletteWheel({ players, spinning, selectedUserDiscordId, selectedIndex }: RouletteWheelProps) {
  const wheelPlayers = players.filter(player => player.isAlive);
  const resolvedSelectedIndex = typeof selectedIndex === 'number' && selectedIndex >= 0 && selectedIndex < wheelPlayers.length
    ? selectedIndex
    : wheelPlayers.findIndex(player => player.userDiscordId === selectedUserDiscordId);
  const selected = resolvedSelectedIndex >= 0 ? wheelPlayers[resolvedSelectedIndex] : undefined;
  const segmentSize = 360 / Math.max(wheelPlayers.length, 1);
  const selectedCenter = resolvedSelectedIndex >= 0 ? (resolvedSelectedIndex * segmentSize) + (segmentSize / 2) : 0;
  const wheelRotation = 1440 + 270 - selectedCenter;
  const gradient = wheelPlayers.length > 0
    ? `conic-gradient(${wheelPlayers.map((_, index) => `${colors[index % colors.length]} ${index * segmentSize}deg ${(index + 1) * segmentSize}deg`).join(',')})`
    : 'radial-gradient(circle, rgba(124,58,237,.35), rgba(15,23,42,.9))';

  return (
    <div className={`roulette-wheel-shell ${spinning ? 'is-spinning' : ''} ${selected ? 'has-result' : ''}`}>
      <div className="wheel-pointer">▼</div>
      <div className="roulette-wheel-game" style={{ background: gradient, '--wheel-rotation': `${wheelRotation}deg` } as CSSProperties}>
        <div className="wheel-ring" />
        {wheelPlayers.map((player, index) => {
          const angle = segmentSize * index + segmentSize / 2;
          const isSelected = resolvedSelectedIndex === index;
          return (
            <span
              className={`wheel-name ${isSelected ? 'selected' : ''}`}
              key={player.userDiscordId}
              style={{ transform: `rotate(${angle}deg) translateY(-45%) translateX(5.8rem) rotate(${-angle}deg)` }}
              title={player.displayName || player.username}
            >
              <span className="wheel-face">
                {player.avatarUrl
                  ? <img src={player.avatarUrl} alt="" />
                  : <span>{initials(player.displayName || player.username)}</span>}
              </span>
              <span className="wheel-name-text">{(player.displayName || player.username).slice(0, 10)}</span>
            </span>
          );
        })}
        <div className="wheel-center">
          {selected?.avatarUrl && !spinning ? <img src={selected.avatarUrl} alt="" /> : <b>🎡</b>}
          <small>{spinning ? 'العجلة تدور...' : selected ? 'وقفت على' : 'جاهزة'}</small>
        </div>
      </div>
      {selected && !spinning && <div className="wheel-result">تم اختيار <strong>{selected.displayName || selected.username}</strong></div>}
    </div>
  );
}
