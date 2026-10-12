import { mergeRelationQueryValues } from './crud-configuration.utils';

describe('mergeRelationQueryValues', () => {
  it('uses fixed relation values as query constraints unless explicitly overridden', () => {
    const parentMock = { guid: 'parent-guid' };
    const queryValues = mergeRelationQueryValues({
      Publication: parentMock,
      Shared: 'fixed-value'
    }, {
      QueryOnly: 'query-value',
      Shared: 'query-override'
    });

    expect(queryValues['Publication']).toBe(parentMock);
    expect(queryValues['QueryOnly']).toBe('query-value');
    expect(queryValues['Shared']).toBe('query-override');
  });
});