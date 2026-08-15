const MAXIMUM_VISIBLE_ROWS = 2000;

export class DiceUI {
  constructor(document) {
    this.document = document;
    this.elements = Object.fromEntries(['connection','start','stop','reset','session-state','roll-list','empty-rolls','visible-count','players','rolls','highest','lowest','started','ended','rank-form','rank','rank-result','debug-panel','debug-form','debug-log'].map((id) => [id, document.getElementById(id)]));
  }

  bind(actions) {
    this.elements.start.addEventListener('click', actions.start);
    this.elements.stop.addEventListener('click', actions.stop);
    this.elements.reset.addEventListener('click', actions.reset);
    this.elements['rank-form'].addEventListener('submit', (event) => { event.preventDefault(); actions.rank(Number(this.elements.rank.value), new FormData(event.currentTarget).get('direction')); });
    this.elements['debug-form'].addEventListener('submit', (event) => { event.preventDefault(); actions.debug(this.elements['debug-log'].value); });
  }

  setDebug(enabled) { this.elements['debug-panel'].hidden = !enabled; }
  setConnection(status) { this.elements.connection.classList.toggle('connected', status.connected); this.elements.connection.querySelector('strong').textContent = status.label; }

  reset(session) {
    this.elements['roll-list'].replaceChildren();
    this.elements['rank-result'].textContent = '조회 전';
    this.elements['empty-rolls'].hidden = false;
    this.update(session, { highestRolls: [], lowestRolls: [], totalRollCount: 0, uniquePlayerCount: 0 });
  }

  addRoll(roll) {
    const row = this.document.createElement('tr');
    const cells = [formatClock(roll.timestamp), roll.playerName, String(roll.rollValue)];
    cells.forEach((value, index) => { const cell = this.document.createElement('td'); cell.textContent = value; if (index === 1) cell.className = 'player'; if (index === 2) cell.className = 'roll-value'; row.append(cell); });
    this.elements['roll-list'].append(row);
    while (this.elements['roll-list'].childElementCount > MAXIMUM_VISIBLE_ROWS) this.elements['roll-list'].firstElementChild.remove();
    this.elements['empty-rolls'].hidden = true;
    this.elements['visible-count'].textContent = `${this.elements['roll-list'].childElementCount.toLocaleString()} / 2,000`;
    row.scrollIntoView({ block: 'nearest' });
  }

  update(session, result) {
    this.elements.start.disabled = session.isRunning;
    this.elements.stop.disabled = !session.isRunning;
    this.elements['session-state'].textContent = ({ Idle: '대기 중', Running: '집계 중', Finished: '집계 완료' })[session.state];
    this.elements.players.textContent = String(result.uniquePlayerCount);
    this.elements.rolls.textContent = String(result.totalRollCount);
    this.elements.highest.textContent = formatRolls(result.highestRolls);
    this.elements.lowest.textContent = formatRolls(result.lowestRolls);
    this.elements.started.textContent = formatDate(session.startTime);
    this.elements.ended.textContent = formatDate(session.endTime);
    this.elements['visible-count'].textContent = `${this.elements['roll-list'].childElementCount.toLocaleString()} / 2,000`;
  }

  showRank(result) { this.elements['rank-result'].textContent = result.exists ? `${result.requestedRank}위 / 값 ${result.rollValue}: ${result.entries.map((roll) => roll.playerName).join(', ')}` : '결과 없음'; }
}

function formatRolls(rolls) { return rolls.length ? `${rolls[0].rollValue} · ${rolls.map((roll) => roll.playerName).join(', ')}` : '-'; }
function formatClock(date) { return new Intl.DateTimeFormat('ko-KR', { hour:'2-digit', minute:'2-digit', second:'2-digit', hour12:false }).format(date); }
function formatDate(date) { return date ? new Intl.DateTimeFormat('ko-KR', { year:'numeric', month:'2-digit', day:'2-digit', hour:'2-digit', minute:'2-digit', second:'2-digit', hour12:false }).format(date) : '-'; }
