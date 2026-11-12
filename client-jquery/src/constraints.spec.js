import { describe, expect, it } from 'vitest';
import { DEVICE_CONSTRAINTS } from './constraints.js';

/** Mirrors the server rules in DeviceConstants; a drift between the two would let invalid cards through. */
describe('DEVICE_CONSTRAINTS', () => {
  it('limits_matchServerRules', () => {
    expect(DEVICE_CONSTRAINTS).toMatchObject({
      maxCount: 2000,
      inventoryNumberMaxLength: 32,
      nameMaxLength: 256,
      roomMin: 1,
      roomMax: 1000,
    });
  });

  it('constraints_object_isFrozen', () => {
    expect(Object.isFrozen(DEVICE_CONSTRAINTS)).toBe(true);
  });

  it.each(['1', 'PC10', 'abcXYZ09'])('inventoryNumberPattern_%s_matches', (value) => {
    expect(DEVICE_CONSTRAINTS.inventoryNumberPattern.test(value)).toBe(true);
  });

  it.each(['', 'PC 1', 'PC-1', 'ПК1', 'café', 'PC1\n'])(
    'inventoryNumberPattern_%j_doesNotMatch',
    (value) => {
      expect(DEVICE_CONSTRAINTS.inventoryNumberPattern.test(value)).toBe(false);
    },
  );
});
