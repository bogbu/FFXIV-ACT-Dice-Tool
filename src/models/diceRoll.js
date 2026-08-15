export function createDiceRoll({ timestamp, playerName, rollValue, rawLogLine }) {
  return Object.freeze({ timestamp, playerName, rollValue, rawLogLine });
}
