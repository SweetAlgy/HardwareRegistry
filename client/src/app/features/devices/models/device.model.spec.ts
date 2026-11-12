import { DEVICE_CONSTRAINTS } from './device.model';

/** Mirrors the server rules in DeviceConstants; a drift between the two would let invalid cards through. */
describe('DEVICE_CONSTRAINTS', () => {
  it('limits_matchServerRules', () => {
    expect(DEVICE_CONSTRAINTS.maxCount).toBe(2000);
    expect(DEVICE_CONSTRAINTS.inventoryNumberMaxLength).toBe(32);
    expect(DEVICE_CONSTRAINTS.nameMaxLength).toBe(256);
    expect(DEVICE_CONSTRAINTS.roomMin).toBe(1);
    expect(DEVICE_CONSTRAINTS.roomMax).toBe(1000);
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
