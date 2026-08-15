import test from 'node:test';
import assert from 'node:assert/strict';
import { parseDiceRoll } from '../src/parser/diceParser.js';
import { DiceSessionManager } from '../src/services/diceSessionManager.js';
import { queryDenseRank } from '../src/services/rankCalculator.js';
import { DuplicateFilter } from '../src/services/duplicateFilter.js';
import { normalizeLogLine, OverlayBridge } from '../src/overlay/overlayBridge.js';

const roll = (playerName, rollValue, rawLogLine = `${playerName}${rollValue}`) => ({ timestamp: new Date('2026-01-01T00:00:00Z'), playerName, rollValue, rawLogLine });

test('한국어 bracket timestamp 로그를 파싱한다', () => {
  const result = parseDiceRoll('[23:33]메이데이 님이 주사위를 굴려 721이 나왔습니다!');
  assert.equal(result.playerName, '메이데이'); assert.equal(result.rollValue, 721);
});

test('한국 클라이언트가 표시하는 주사위 아이콘이 포함된 로그를 파싱한다', () => {
  const result = parseDiceRoll('[2:41]메이데이 님이 주사위를 굴려 🎲 989가 나왔습니다!');
  assert.equal(result.playerName, '메이데이'); assert.equal(result.rollValue, 989);
});

test('ACT localized pipe 로그를 파싱한다', () => {
  const raw = '00|2026-08-15T23:33:14.0000000+09:00|0139||메이데이 님이 주사위를 굴려 721이 나왔습니다!|deadbeef';
  const result = parseDiceRoll(raw);
  assert.equal(result.playerName, '메이데이'); assert.equal(result.rollValue, 721); assert.equal(result.rawLogLine, raw);
});

test('주사위 아이콘이 포함된 ACT localized pipe 로그를 파싱한다', () => {
  const raw = '00|2026-08-16T02:41:41.0000000+09:00|0139||메이데이 님이 주사위를 굴려 🎲 989가 나왔습니다!|deadbeef';
  const result = parseDiceRoll(raw);
  assert.equal(result.playerName, '메이데이'); assert.equal(result.rollValue, 989);
});

test('기존 영문 패턴과 값 범위를 유지한다', () => {
  assert.equal(parseDiceRoll('[2026-01-01 12:30:45] Alice rolls 999').rollValue, 999);
  assert.equal(parseDiceRoll('[2026-01-01 12:30:45] Alice rolls 1000'), null);
  assert.equal(parseDiceRoll('not a dice line'), null);
});

test('세션 최고, 최저, 총 굴림, 고유 참여자를 계산한다', () => {
  const manager = new DiceSessionManager(); manager.startSession();
  manager.addRoll(roll('Alice', 100)); manager.addRoll(roll('Bob', 300)); manager.addRoll(roll('Carol', 200));
  const result = manager.stopSession();
  assert.equal(result.highestRolls[0].playerName, 'Bob'); assert.equal(result.lowestRolls[0].playerName, 'Alice');
  assert.equal(result.totalRollCount, 3); assert.equal(result.uniquePlayerCount, 3); assert.equal(manager.session.state, 'Finished');
});

test('최고/최저 동점을 모두 유지하고 참여자 이름은 대소문자를 무시한다', () => {
  const manager = new DiceSessionManager(); manager.startSession();
  for (const entry of [roll('Alice', 900), roll('alice', 900), roll('Bob', 10), roll('Carol', 10)]) manager.addRoll(entry);
  const result = manager.calculateResult();
  assert.deepEqual(result.highestRolls.map((x) => x.playerName), ['Alice', 'alice']); assert.equal(result.lowestRolls.length, 2); assert.equal(result.uniquePlayerCount, 3);
});

test('높은 순과 낮은 순 DenseRank가 모든 동점을 반환한다', () => {
  const entries = [roll('Alice', 900), roll('Bob', 900), roll('Carol', 700), roll('Dave', 500)];
  assert.deepEqual(queryDenseRank(entries, 1, 'highest').entries.map((x) => x.playerName), ['Alice', 'Bob']);
  assert.equal(queryDenseRank(entries, 2, 'highest').entries[0].playerName, 'Carol');
  assert.equal(queryDenseRank(entries, 2, 'lowest').entries[0].playerName, 'Carol');
  assert.equal(queryDenseRank(entries, 4, 'highest').exists, false);
});

test('중복 필터는 bounded FIFO이며 초기화할 수 있다', () => {
  const filter = new DuplicateFilter(2); const first = roll('Alice', 100, 'one');
  assert.equal(filter.isDuplicate(first), false); assert.equal(filter.isDuplicate(first), true);
  filter.isDuplicate(roll('Bob', 200)); filter.isDuplicate(roll('Carol', 300)); assert.equal(filter.isDuplicate(first), false);
  filter.clear(); assert.equal(filter.isDuplicate(first), false);
});

test('OverlayPlugin payload는 rawLine 우선, line 배열 fallback으로 정규화한다', () => {
  assert.equal(normalizeLogLine({ rawLine:'raw', line:['00','time'] }), 'raw');
  assert.equal(normalizeLogLine({ line:['00','time','','','message','hash'] }), '00|time|||message|hash');
  assert.equal(normalizeLogLine({}), null);
});

test('일반 브라우저에서는 bridge가 오류 없이 preview 상태가 된다', () => {
  let status; const bridge = new OverlayBridge({ onLogLine() {}, onStatus(value) { status = value; } });
  assert.equal(bridge.connect({}), false); assert.equal(status.label, '브라우저 미리보기');
});

test('OverlayPlugin Web API가 있으면 LogLine을 구독하고 연결 상태가 된다', () => {
  let listener; let started = false; let status; let received;
  const scope = {
    addOverlayListener(event, callback) { assert.equal(event, 'LogLine'); listener = callback; },
    startOverlayEvents() { started = true; },
  };
  const bridge = new OverlayBridge({ onLogLine(line) { received = line; }, onStatus(value) { status = value; } });

  assert.equal(bridge.connect(scope), true);
  assert.equal(started, true);
  assert.equal(status.label, 'ACT 연결됨 · 로그 대기 중');

  listener({ rawLine: '[23:33]메이데이 님이 주사위를 굴려 721이 나왔습니다!' });
  assert.equal(received, '[23:33]메이데이 님이 주사위를 굴려 721이 나왔습니다!');
  assert.equal(status.label, 'ACT · 로그 수신 중');
});
