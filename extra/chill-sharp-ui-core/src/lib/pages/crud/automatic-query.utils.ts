import type { JsonObject, JsonValue } from '@chill-sharp/ng-client';
import {
  CHILL_PROPERTY_TYPE,
  type AutomaticQueryFilter,
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
    properties: (entitySchema.properties ?? []).map((property) => ({
      ...property,
      isNullable: true,
      metadata: property.metadata ? { ...property.metadata } : undefined
    }))
  };
}

/**
 * Converts values entered in an entity-shaped query form into Equal filters.
 */
export function createAutomaticQueryRequest(query: ChillQuery, schema: ChillSchema): ChillQuery {
  const properties = query.properties ?? {};
  const filters = (schema.properties ?? [])
    .map((property) => createEqualFilter(property, properties[property.name]))
    .filter((filter): filter is AutomaticQueryFilter => filter !== null);
  const fullTextSearch = properties[FULL_TEXT_SEARCH_PROPERTY];

  return {
    ...query,
    properties: hasFilterValue(fullTextSearch)
      ? { [FULL_TEXT_SEARCH_PROPERTY]: fullTextSearch }
      : {},
    automaticQuery: {
      filter: {
        logicalOperator: 'And',
        filters,
        groups: []
      }
    }
  };
}

function createEqualFilter(property: ChillPropertySchema, value: JsonValue | undefined): AutomaticQueryFilter | null {
  const propertyName = property.name?.trim();
  if (!propertyName || !hasFilterValue(value)) {
    return null;
  }

  return {
    propertyName,
    operator: 'Equal',
    value: normalizeFilterValue(property, value)
  };
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
