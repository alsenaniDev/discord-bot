import type { RoulettePlayer } from '../../types';

interface RouletteWheelProps {
  players: RoulettePlayer[];
  spinning: boolean;
  selectedUserDiscordId?: string | null;
}

const colors = ['#7c3aed', '#2563eb', '#db2777', '#f59e0b', '#06b6d4', '#22c55e', '#ef4444', '#8b5cf6'];

export function RouletteWheel({ players, spinning, selectedUserDiscordId }: RouletteWheelProps) {
  const alive = players.filter(player => player.isAlive);
  const selected = alive.find(player => player.userDiscordId === selectedUserDiscordId);
  const segmentSize = 360 / Math.max(alive.length, 1);
  const gradient = alive.length > 0
    ? `conic-gradient(${alive.map((_, index) => `${colors[index % colors.length]} ${index * segmentSize}deg ${(index + 1) * segmentSize}deg`).join(',')})`
    : 'radial-gradient(circle, rgba(124,58,237,.35), rgba(15,23,42,.9))';

  return (
    <div className={`roulette-wheel-shell ${spinning ? 'is-spinning' : ''} ${selected ? 'has-result' : ''}`}>
      <div className="wheel-pointer">▼</div>
      <div className="roulette-wheel-game" style={{ background: gradient }}>
        <div className="wheel-ring" />
        {alive.map((player, index) => {
          const angle = segmentSize * index + segmentSize / 2;
          const isSelected = selectedUserDiscordId === player.userDiscordId;
          return (
            <span
              className={`wheel-name ${isSelected ? 'selected' : ''}`}
              key={player.userDiscordId}
              style={{ transform: `rotate(${angle}deg) translateY(-45%) translateX(5.8rem) rotate(${-angle}deg)` }}
              title={player.username}
            >
              {player.username.slice(0, 11)}
            </span>
          );
        })}
        <div className="wheel-center">
          <b>🎡</b>
          <small>{spinning ? 'العجلة تدور...' : selected ? 'وقفت على' : 'جاهزة'}</small>
        </div>
      </div>
      {selected && !spinning && <div className="wheel-result">تم اختيار <strong>{selected.username}</strong></div>}
    </div>
  );
}
