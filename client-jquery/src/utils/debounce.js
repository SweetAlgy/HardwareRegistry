/** Calls `callback` once, `delayMs` after the last of a burst of calls. */
export function debounce(callback, delayMs) {
  let timeoutId;

  return () => {
    clearTimeout(timeoutId);
    timeoutId = setTimeout(callback, delayMs);
  };
}
