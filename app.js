import { parseDiceRoll } from './src/parser/diceParser.js';
import { DuplicateFilter } from './src/services/duplicateFilter.js';
import { DiceSessionManager } from './src/services/diceSessionManager.js';
import { queryDenseRank } from './src/services/rankCalculator.js';
import { OverlayBridge } from './src/overlay/overlayBridge.js';
import { DiceUI } from './src/ui/diceUI.js';

const sessionManager = new DiceSessionManager();
const duplicates = new DuplicateFilter(500);
const ui = new DiceUI(document);

function processLogLine(line) {
  const roll = parseDiceRoll(line);
  if (!roll || !sessionManager.session.isRunning || duplicates.isDuplicate(roll)) return;
  sessionManager.addRoll(roll);
  ui.addRoll(roll);
  ui.update(sessionManager.session, sessionManager.calculateResult());
}

ui.bind({
  start: () => { sessionManager.startSession(); duplicates.clear(); ui.reset(sessionManager.session); },
  stop: () => { const result = sessionManager.stopSession(); ui.update(sessionManager.session, result); },
  reset: () => { sessionManager.resetSession(); duplicates.clear(); ui.reset(sessionManager.session); },
  rank: (rank, direction) => ui.showRank(queryDenseRank(sessionManager.session.rolls, rank, direction)),
  debug: processLogLine,
});
ui.reset(sessionManager.session);
ui.setDebug(new URLSearchParams(location.search).get('debug') === '1');
new OverlayBridge({ onLogLine: processLogLine, onStatus: (status) => ui.setConnection(status) }).connect();
