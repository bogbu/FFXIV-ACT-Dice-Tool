import { createDiceRoll } from '../models/diceRoll.js';

const VALUE = '(?<value>\\d{1,3})(?!\\d)';
const patterns = [
  parseActLocalizedPipe,
  (line) => matchLine(line, new RegExp(`^\\[(?<time>\\d{1,2}:\\d{2}(?::\\d{2})?)\\]\\s*(?<name>.+?)\\s*님이\\s*주사위를\\s*굴려\\s*${VALUE}(?:이|가)\\s*나왔습니다!?$`, 'iu')),
  (line) => matchLine(line, new RegExp(`^\\[(?<time>\\d{4}-\\d{2}-\\d{2}[ T]\\d{2}:\\d{2}:\\d{2})\\].*?(?<name>[\\p{L}\\p{N}_' -]+) rolls? ${VALUE}`, 'iu')),
  (line) => matchLine(line, new RegExp(`^(?<time>\\d{4}-\\d{2}-\\d{2}T\\d{2}:\\d{2}:\\d{2})\\|.*?\\|(?<name>[^|]+?)\\s+(?:rolls?|casts.*?/dice).*?${VALUE}$`, 'iu')),
  (line) => matchLine(line, new RegExp(`^(?<time>\\d{2}:\\d{2}:\\d{2}).*?(?<name>[\\p{L}\\p{N}_' -]+) (?:rolls?|obtains).*?${VALUE}`, 'iu')),
];

export function parseDiceRoll(line) {
  if (typeof line !== 'string' || !line.trim()) return null;
  for (const parse of patterns) {
    const roll = parse(line);
    if (roll) return roll;
  }
  return null;
}

function parseActLocalizedPipe(line) {
  const envelope = /^(?:00|0?0)\|(?<time>[^|]+)\|[^|]*\|\|(?<message>[^|]+)\|/iu.exec(line);
  if (!envelope) return null;
  const message = /^(?<name>.+?)\s*님이\s*주사위를\s*굴려\s*(?<value>\d{1,3})(?:이|가)\s*나왔습니다!?$/iu.exec(envelope.groups.message);
  return createFromMatch(message, line, envelope.groups.time);
}

function matchLine(line, pattern) {
  return createFromMatch(pattern.exec(line), line);
}

function createFromMatch(match, rawLogLine, timestampText) {
  if (!match?.groups) return null;
  const rollValue = Number.parseInt(match.groups.value, 10);
  if (!Number.isInteger(rollValue) || rollValue < 0 || rollValue > 999) return null;
  return createDiceRoll({
    timestamp: parseTimestamp(timestampText ?? match.groups.time),
    playerName: match.groups.name.trim(),
    rollValue,
    rawLogLine,
  });
}

function parseTimestamp(text) {
  if (typeof text === 'string') {
    const time = /^(\d{1,2}):(\d{2})(?::(\d{2}))?$/.exec(text.trim());
    if (time) {
      const date = new Date();
      date.setHours(Number(time[1]), Number(time[2]), Number(time[3] ?? 0), 0);
      return date;
    }
    const parsed = new Date(text);
    if (!Number.isNaN(parsed.getTime())) return parsed;
  }
  return new Date();
}
