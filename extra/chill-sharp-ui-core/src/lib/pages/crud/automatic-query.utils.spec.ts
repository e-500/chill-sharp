import { CHILL_PROPERTY_TYPE, type ChillQuery, type ChillSchema } from '../../models/chill-schema.models';
import { createAutomaticQueryRequest, createAutomaticQuerySchema, shouldUseAutomaticQuery } from './automatic-query.utils';

describe('automatic query CRUD utilities', () => {
  const entitySchema: ChillSchema = {
    chillType: 'Model.Post',
    displayName: 'Post',
    metadata: {
      'chill-form-component': '{"items":[{"name":"Title","hidden":true}]}',
      preserved: true
    },
    properties: [
      { name: 'Title', propertyType: CHILL_PROPERTY_TYPE.String, isNullable: false },
      { name: 'Score', propertyType: CHILL_PROPERTY_TYPE.Integer, isNullable: false },
      { name: 'Published', propertyType: CHILL_PROPERTY_TYPE.Boolean, isNullable: false },
      { name: 'Blog', propertyType: CHILL_PROPERTY_TYPE.ChillEntity, isNullable: false },
      { name: 'Tags', propertyType: CHILL_PROPERTY_TYPE.ChillEntityCollection, isNullable: false }
    ]
  };

  it('preserves dedicated query mode when a query implementation is configured', () => {
    expect(shouldUseAutomaticQuery('Query.PostQuery')).toBeFalse();
    expect(shouldUseAutomaticQuery('  ')).toBeTrue();
    expect(shouldUseAutomaticQuery(null)).toBeTrue();
  });

  it('uses every equality-filterable entity field as an optional query-form field', () => {
    const schema = createAutomaticQuerySchema(entitySchema);

    expect(schema.chillType).toBe('Model.Post');
    expect(schema.queryRelatedChillType).toBe('Model.Post');
    expect(schema.properties.map((property) => property.name)).toEqual(['Title', 'Score', 'Published', 'Blog']);
    expect(schema.properties.every((property) => property.isNullable)).toBeTrue();
    expect(entitySchema.properties.every((property) => !property.isNullable)).toBeTrue();
    expect(schema.metadata?.['chill-form-component']).toBeUndefined();
    expect(schema.metadata?.['preserved']).toBeTrue();
  });

  it('creates a Properties compatibility request only with populated fields', () => {
    const query: ChillQuery = {
      chillType: 'Model.Post',
      properties: {
        Title: '',
        Score: 0,
        Published: false,
        Blog: { guid: ' 09926e8f-3291-4448-b206-df0ce562ab23 ', chillType: 'Model.Blog' },
        Tags: [{ guid: 'a168b73f-229f-4039-aee5-d0154976dc52' }],
        FullTextSearch: 'release'
      }
    };

    const request = createAutomaticQueryRequest(query, entitySchema);
    const requestProperties = request.properties as Record<string, unknown> | undefined;

    expect(Object.keys(requestProperties ?? {})).toEqual(['Score', 'Published', 'Blog', 'FullTextSearch']);
    expect(requestProperties?.['FullTextSearch']).toBe('release');
    expect(requestProperties?.['Score']).toBe(0);
    expect(requestProperties?.['Published']).toBeFalse();
    expect(requestProperties?.['Blog']).toBe('09926e8f-3291-4448-b206-df0ce562ab23');
    expect(requestProperties?.['Tags']).toBeUndefined();
    expect(request.automaticQuery).toBeNull();
  });
});
