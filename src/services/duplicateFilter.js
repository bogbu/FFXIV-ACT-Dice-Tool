export class DuplicateFilter {
  constructor(maximumSize = 500) {
    if (!Number.isInteger(maximumSize) || maximumSize < 1) throw new RangeError('maximumSize must be a positive integer');
    this.maximumSize = maximumSize;
    this.clear();
  }

  isDuplicate(roll) {
    const key = `${roll.timestamp.toISOString()}|${roll.playerName}|${roll.rollValue}|${roll.rawLogLine}`;
    if (this.keys.has(key)) return true;
    this.keys.add(key);
    this.queue.push(key);
    if (this.queue.length > this.maximumSize) this.keys.delete(this.queue.shift());
    return false;
  }

  clear() {
    this.keys = new Set();
    this.queue = [];
  }
}
