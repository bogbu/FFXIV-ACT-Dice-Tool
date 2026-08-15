export class OverlayBridge {
  constructor({ onLogLine, onStatus }) {
    this.onLogLine = onLogLine;
    this.onStatus = onStatus;
    this.receivedLog = false;
  }

  connect(scope = globalThis) {
    if (typeof scope.addOverlayListener !== 'function' || typeof scope.startOverlayEvents !== 'function') {
      this.onStatus({ connected: false, receiving: false, label: '브라우저 미리보기' });
      return false;
    }
    scope.addOverlayListener('LogLine', (event) => {
      if (!this.receivedLog) {
        this.receivedLog = true;
        this.onStatus({ connected: true, receiving: true, label: 'ACT · 로그 수신 중' });
      }
      const line = normalizeLogLine(event);
      if (line) this.onLogLine(line);
    });
    scope.startOverlayEvents();
    this.onStatus({ connected: true, receiving: false, label: 'ACT 연결됨 · 로그 대기 중' });
    return true;
  }
}

// OverlayPlugin LogLine exposes the original text as rawLine and parsed fields as line[].
export function normalizeLogLine(event) {
  if (typeof event?.rawLine === 'string' && event.rawLine) return event.rawLine;
  if (Array.isArray(event?.line)) return event.line.join('|');
  return null;
}
