import { createCrudSchemaOptions } from './workspace-menu.component';
import type { ChillSchemaListItem } from '../models/chill-schema.models';

describe('createCrudSchemaOptions', () => {
  it('includes every entity and query and keeps queries distinct per entity', () => {
    const schemaList: ChillSchemaListItem[] = [
      { name: 'Person', chillType: 'Model.Person', type: 'entity' },
      { name: 'Publication', chillType: 'Model.Publication', type: 'entity' },
      {
        name: 'PersonSearchQuery',
        chillType: 'Model.PersonSearchQuery',
        type: 'query',
        relatedChillType: 'Model.Person'
      },
      {
        name: 'PersonHistoryQuery',
        chillType: 'Model.PersonHistoryQuery',
        type: 'query',
        relatedChillType: 'Model.Person'
      },
      { name: 'UnlinkedQuery', chillType: 'Model.UnlinkedQuery', type: 'query' }
    ];

    const options = createCrudSchemaOptions(schemaList);

    expect(options).toHaveSize(5);
    expect(options.filter((option) => option.kind === 'Entity').map((option) => option.chillType))
      .toEqual(['Model.Person', 'Model.Publication']);
    expect(options.filter((option) => option.kind === 'Query').map((option) => option.queryChillType))
      .toEqual(['Model.PersonHistoryQuery', 'Model.PersonSearchQuery', 'Model.UnlinkedQuery']);
    expect(new Set(options.map((option) => option.key)).size).toBe(5);
  });
});