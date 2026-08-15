const emptyResult = () => ({ highestRolls: [], lowestRolls: [], totalRollCount: 0, uniquePlayerCount: 0 });

export class DiceSessionManager {
  constructor() { this.resetSession(); }

  startSession() {
    this.session = { state: 'Running', isRunning: true, startTime: new Date(), endTime: null, rolls: [] };
  }

  stopSession() {
    if (this.session.isRunning) {
      this.session.isRunning = false;
      this.session.state = 'Finished';
      this.session.endTime = new Date();
    }
    return this.calculateResult();
  }

  resetSession() {
    this.session = { state: 'Idle', isRunning: false, startTime: null, endTime: null, rolls: [] };
  }

  addRoll(roll) {
    if (!this.session.isRunning) return false;
    this.session.rolls.push(roll);
    return true;
  }

  calculateResult() {
    const rolls = this.session.rolls;
    if (!rolls.length) return emptyResult();
    let highest = rolls[0].rollValue;
    let lowest = rolls[0].rollValue;
    const players = new Set();
    for (const roll of rolls) {
      highest = Math.max(highest, roll.rollValue);
      lowest = Math.min(lowest, roll.rollValue);
      players.add(roll.playerName.toLocaleLowerCase());
    }
    return {
      highestRolls: rolls.filter((roll) => roll.rollValue === highest),
      lowestRolls: rolls.filter((roll) => roll.rollValue === lowest),
      totalRollCount: rolls.length,
      uniquePlayerCount: players.size,
    };
  }
}
