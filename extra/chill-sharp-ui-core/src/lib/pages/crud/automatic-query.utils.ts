import type { JsonObject, JsonValue } from '@chill-sharp/ng-client';
import {
  CHILL_PROPERTY_TYPE,
  type ChillPropertySchema,
  type ChillQuery,
  type ChillSchema
} from '../../models/chill-schema.models';

const FULL_TEXT_SEARCH_PROPERTY = 'FullTextSearch';
const FORM_LAYOUT_METADATA_KEY = 'chill-form-component';

export function shouldUseAutomaticQuery(chillQuery: string | null | undefined): boolean {
  return !chillQuery?.trim();
}

/**
 * Creates an optional filter form from an entity schema.
 */
export function createAutomaticQuerySchema(entitySchema: ChillSchema): ChillSchema {
  const chillType = entitySchema.chillType?.trim() ?? '';
  const metadata = entitySchema.metadata ? { ...entitySchema.metadata } : {};
  delete metadata[FORM_LAYOUT_METADATA_KEY];
  return {
    ...entitySchema,
    queryRelatedChillType: chillType,
    metadata,
    relations: entitySchema.relations ? [...entitySchema.relations] : undefined,
    properties: (entitySchema.properties ?? [])
      .filter((property) => property.propertyType !== CHILL_PROPERTY_TYPE.ChillEntityCollection)
      .map((property) => ({
        ...property,
        isNullable: true,
        metadata: property.metadata ? { ...property.metadata } : undefined
      }))
  };
}

/**
 * Creates the entity-shaped compatibility query payload. The server converts the
 * populated, exposed properties into Equal filters when no ChillQuery is configured.
 */
export function createAutomaticQueryRequest(query: ChillQuery, schema: ChillSchema): ChillQuery {
  const sourceProperties = query.properties ?? {};
  const properties = Object.fromEntries((schema.properties ?? [])
    .map((property) => createEqualProperty(property, sourceProperties[property.name]))
    .filter((entry): entry is [string, JsonValue] => entry !== null));
  const fullTextSearch = sourceProperties[FULL_TEXT_SEARCH_PROPERTY];

  if (hasFilterValue(fullTextSearch)) {
    properties[FULL_TEXT_SEARCH_PROPERTY] = fullTextSearch;
  }

  return {
    ...query,
    properties,
    automaticQuery: null
  };
}

function createEqualProperty(property: ChillPropertySchema, value: JsonValue | undefined): [string, JsonValue] | null {
  const propertyName = property.name?.trim();
  if (!propertyName || property.propertyType === CHILL_PROPERTY_TYPE.ChillEntityCollection || !hasFilterValue(value)) {
    return null;
  }

  return [propertyName, normalizeFilterValue(property, value)];
}

function normalizeFilterValue(property: ChillPropertySchema, value: JsonValue): JsonValue {
  if (property.propertyType !== CHILL_PROPERTY_TYPE.ChillEntity || !isJsonObject(value)) {
    return value;
  }

  const guid = value['guid'] ?? value['Guid'];
  return typeof guid === 'string' && guid.trim().length > 0
    ? guid.trim()
    : value;
}

function hasFilterValue(value: JsonValue | undefined): value is JsonValue {
  if (value === null || value === undefined) {
    return false;
  }

  if (typeof value === 'string') {
    return value.trim().length > 0;
  }

  if (Array.isArray(value)) {
    return value.length > 0;
  }

  if (isJsonObject(value)) {
    return Object.keys(value).length > 0;
  }

  return true;
}

function isJsonObject(value: JsonValue): value is JsonObject {
  return typeof value === 'object' && value !== null && !Array.isArray(value);
}
