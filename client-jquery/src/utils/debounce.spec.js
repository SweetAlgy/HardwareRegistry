import { afterEach, describe, expect, it, vi } from 'vitest';
import { debounce } from './debounce.js';

describe('debounce', () => {
  afterEach(() => vi.useRealTimers());

  it('debounce_burstOfCalls_callsOnceAfterDelay', () => {
    vi.useFakeTimers();
    const callback = vi.fn();
    const debounced = debounce(callback, 300);

    debounced();
    debounced();
    vi.advanceTimersByTime(299);
    debounced();
    vi.advanceTimersByTime(299);

    expect(callback).not.toHaveBeenCalled();

    vi.advanceTimersByTime(1);

    expect(callback).toHaveBeenCalledOnce();
  });

  it('debounce_separatedCalls_callsForEach', () => {
    vi.useFakeTimers();
    const callback = vi.fn();
    const debounced = debounce(callback, 300);

    debounced();
    vi.advanceTimersByTime(300);
    debounced();
    vi.advanceTimersByTime(300);

    expect(callback).toHaveBeenCalledTimes(2);
  });
});
