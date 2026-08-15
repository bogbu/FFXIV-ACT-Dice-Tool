export function queryDenseRank(entries, requestedRank, direction = 'highest') {
  const result = { requestedRank, direction, mode: 'DenseRank', exists: false, rollValue: null, entries: [] };
  if (!Number.isInteger(requestedRank) || requestedRank <= 0 || !entries.length) return result;
  const byValue = new Map();
  for (const entry of entries) {
    if (!byValue.has(entry.rollValue)) byValue.set(entry.rollValue, []);
    byValue.get(entry.rollValue).push(entry);
  }
  const values = [...byValue.keys()].sort((a, b) => direction === 'lowest' ? a - b : b - a);
  const rollValue = values[requestedRank - 1];
  if (rollValue === undefined) return result;
  return { ...result, exists: true, rollValue, entries: byValue.get(rollValue) };
}
