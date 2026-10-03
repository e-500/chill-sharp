# Sistema Di Query Automatiche

English version: [English](../../AutomaticQuery/README.md)

Il sistema di query automatiche costruisce expression tree LINQ fortemente tipizzati a partire da definizioni di filtro strutturate. È un'alternativa opzionale per i casi in cui creare un'implementazione personalizzata di `ChillQuery.OnQuery(...)` per ogni combinazione di filtri sarebbe ripetitivo.

L'API di query esistente resta invariata. `ChillEngine.Query(...)` continua ad accettare `IChillQuery<IChillEntity>` e le sottoclassi esistenti di `ChillQuery` continuano a funzionare come prima.

Le definizioni di query automatiche sono disponibili tramite `ChillDtoQuery` e l'endpoint esistente `POST /api/chill/query`. La discovery dello schema non pubblica ancora la compatibilità degli operatori per ciascuna proprietà, quindi attualmente i client devono costruire la definizione in base alla propria conoscenza del modello dell'entità.

## Tipi Principali

Il prototipo fornisce due punti di ingresso correlati:

- `AutomaticQuery` contiene una definizione di filtro strutturata e può applicarla a qualsiasi `IQueryable<T>` compatibile.
- `AutomaticQuery<TEntity>` deriva da `ChillQuery` e permette alla definizione di essere eseguita attraverso la pipeline standard `ChillEngine.Query(...)`.

I tipi di supporto sono:

- `AutomaticQueryGroup`, che combina filtri e gruppi annidati con `And` oppure `Or`.
- `AutomaticQueryFilter`, che identifica un percorso di proprietà, un operatore e un valore.
- `AutomaticQueryOperator`, che definisce i confronti disponibili.

Tutti i tipi si trovano nel namespace `ChillSharp.EF`.

## Uso Dell'Endpoint Di Query Condiviso

Le query normali e automatiche condividono `POST /api/chill/query`. La presenza di `AutomaticQuery` seleziona la modalità di esecuzione:

- Senza `AutomaticQuery`, `ChillType` identifica un tipo di query registrato come `Query.PostQuery`.
- Con `AutomaticQuery`, `ChillType` identifica il tipo di entità di destinazione, per esempio `Model.Post`.

```json
{
  "chillType": "Model.Post",
  "automaticQuery": {
    "filter": {
      "logicalOperator": "And",
      "filters": [
        {
          "propertyName": "Title",
          "operator": "Contains",
          "value": "release",
          "ignoreCase": true
        }
      ],
      "groups": []
    }
  },
  "properties": {
    "FullTextSearch": ""
  },
  "ordering": {
    "propertyName": "Position",
    "direction": "ASC"
  },
  "pagination": {
    "page": 1,
    "pageResults": 25
  },
  "resultProperties": [
    { "propertyName": "Guid" },
    { "propertyName": "Title" }
  ]
}
```

I nomi degli operatori e degli operatori logici vengono serializzati come stringhe. I valori enum numerici esistenti restano leggibili dal convertitore predefinito.

L'endpoint autorizza una query automatica rispetto all'entità indicata da `ChillType`. Le query registrate continuano a essere risolte nella relativa entità prima di verificare lo stesso permesso di query. Le operazioni chunk usano la stessa distinzione.

La risposta usa il normale formato `ChillDtoQuery`, conserva la definizione `AutomaticQuery` e popola normalmente `Results`. I client esistenti che omettono `AutomaticQuery` non richiedono modifiche al payload.

I contratti dei client .NET e TypeScript inclusi espongono lo stesso campo opzionale e gli stessi tipi di filtro. Anche il contratto di query MCP conserva la definizione e verifica le query automatiche rispetto alla visibilità MCP dell'entità di destinazione.

### Disponibilità Nei Client

| Client | Superficie per query automatiche |
| --- | --- |
| C# | DTO `ChillSharp.Client.Dto.AutomaticQuery`, gruppo, filtro ed enum; `ChillDtoQuery.AutomaticQuery` |
| TypeScript | Tipi `AutomaticQuery` nativi esportati e overload tipizzato di `ChillSharpClient.query(...)` |
| Angular | Riesporta tutti i tipi delle query automatiche e fornisce un overload tipizzato `Observable<ChillDtoQuery>` |
| React | Riesporta tutti i tipi delle query automatiche e tipizza `useQueryMutation()` con input/output `ChillDtoQuery` |
| Vue | Riesporta tutti i tipi delle query automatiche e tipizza `useQueryMutation()` con input/output `ChillDtoQuery` |
| Python | Definizioni `TypedDict` esportate per `AutomaticQuery`, gruppo, filtro, operatore e `ChillDtoQuery` |

I pacchetti React, Vue e Angular delegano l'esecuzione al client TypeScript, quindi usano lo stesso contratto JSON e lo stesso comportamento dell'endpoint.

## Esecuzione Tramite `ChillEngine.Query`

Usa `AutomaticQuery<TEntity>` quando la destinazione è una `ChillEntity`:

```csharp
using ChillSharp.EF;

var query = new AutomaticQuery<Post>
{
    Definition = new AutomaticQuery
    {
        Filter = new AutomaticQueryGroup
        {
            Filters =
            {
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "release",
                    IgnoreCase = true
                }
            }
        }
    },
    Pagination = new ChillPagination
    {
        Page = 1,
        PageResults = 25
    }
};

var results = new ChillEngine(context).Query(query);
```

Le normali fasi della query continuano a essere eseguite:

1. filtro automatico
2. ricerca full-text
3. ordinamento
4. paginazione
5. `OnSelect(...)` per ogni entità restituita

Le proprietà ereditate `Guid`, `FullTextSearch`, `Ordering`, `Pagination` e `LightweightRequired` restano disponibili.

## Applicazione A Qualsiasi `IQueryable<T>`

`AutomaticQuery.ApplyTo(...)` funziona anche indipendentemente da `ChillEngine` e non richiede che `T` implementi `IChillEntity`:

```csharp
var definition = new AutomaticQuery
{
    Filter = new AutomaticQueryGroup
    {
        Filters =
        {
            new AutomaticQueryFilter
            {
                PropertyName = nameof(ReportRow.Total),
                Operator = AutomaticQueryOperator.GreaterThanOrEqual,
                Value = 100
            }
        }
    }
};

IQueryable<ReportRow> filtered = definition.ApplyTo(reportRows);
```

`ApplyTo(...)` aggiunge un'espressione `Where(...)` e non enumera la query.

## Operatori

| Operatore | Tipo di membro previsto | Valore |
| --- | --- | --- |
| `Equal`, `NotEqual` | scalare, nullable, enum, stringa o riferimento `IChillEntity` | un valore |
| `GreaterThan`, `GreaterThanOrEqual` | valore CLR confrontabile | un valore |
| `LessThan`, `LessThanOrEqual` | valore CLR confrontabile | un valore |
| `Between` | valore CLR confrontabile | `Value` e `SecondValue` inclusivi |
| `Contains` | stringa o collezione | sottostringa o elemento della collezione |
| `StartsWith`, `EndsWith` | stringa | un valore stringa |
| `In` | scalare, enum, stringa o riferimento `IChillEntity` | una collezione di valori accettati |
| `IsNull`, `IsNotNull` | valore nullable o riferimento | nessuno |
| `IsEmpty`, `IsNotEmpty` | stringa o collezione | nessuno |
| `Any`, `All` | collezione | un gruppo `ItemFilter` |

L'uguaglianza tra stringhe, `Contains`, `StartsWith`, `EndsWith` e `In` possono normalizzare le maiuscole e minuscole quando `IgnoreCase` è `true`.

## Valori CLR

I valori dei filtri vengono convertiti nel tipo della proprietà di destinazione prima di creare l'espressione. Il prototipo gestisce:

- stringhe e caratteri
- tipi numerici con e senza segno
- `decimal`, `float` e `double`
- `bool`
- enum per nome o valore numerico
- `Guid`
- `DateTime` e `DateTimeOffset`
- `DateOnly` e `TimeOnly`
- varianti nullable
- valori scalari e array `JsonElement` prodotti dalla deserializzazione JSON

Le conversioni non valide e le combinazioni operatore/tipo non supportate generano una `ChillException` con la descrizione del filtro non valido.

Per mantenere limitate le richieste pubbliche, una definizione può contenere al massimo 100 filtri, l'annidamento dei gruppi è limitato a 8 livelli e un percorso di proprietà può contenere al massimo 512 caratteri. Le strutture cicliche o con gruppi null vengono rifiutate prima della generazione dell'espressione.

## Percorsi Di Proprietà Annidati

Usa percorsi separati da punti per valori correlati o annidati:

```csharp
new AutomaticQueryFilter
{
    PropertyName = "Blog.Title",
    Operator = AutomaticQueryOperator.StartsWith,
    Value = "engineering",
    IgnoreCase = true
}
```

La ricerca delle proprietà non distingue tra maiuscole e minuscole. I riferimenti intermedi nullable vengono protetti nell'espressione generata. Per esempio, `Blog.Title IsNull` corrisponde anche a un'entità il cui riferimento `Blog` è `null`.

Un percorso non valido causa un errore prima dell'enumerazione della query invece di ignorare silenziosamente il filtro.

## Riferimenti `ChillEntity`

I filtri di uguaglianza e appartenenza su un riferimento `IChillEntity` confrontano il suo `Guid`. Il valore del filtro può essere l'entità correlata oppure il suo GUID:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Post.Blog),
    Operator = AutomaticQueryOperator.Equal,
    Value = selectedBlog.Guid
}
```

Questo evita di dipendere dall'uguaglianza dei riferimenti CLR o di collegare al contesto un'entità detached soltanto per creare un filtro.

## Collezioni

Usa `Contains` per una collezione di valori scalari:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Article.Tags),
    Operator = AutomaticQueryOperator.Contains,
    Value = "release"
}
```

Per una collezione di `IChillEntity`, `Contains` confronta il GUID dell'elemento.

Usa `Any` o `All` con `ItemFilter` quando gli elementi della collezione richiedono un proprio predicato. I percorsi in `ItemFilter` sono relativi a ciascun elemento:

```csharp
new AutomaticQueryFilter
{
    PropertyName = nameof(Blog.Posts),
    Operator = AutomaticQueryOperator.Any,
    ItemFilter = new AutomaticQueryGroup
    {
        Filters =
        {
            new AutomaticQueryFilter
            {
                PropertyName = nameof(Post.Title),
                Operator = AutomaticQueryOperator.Contains,
                Value = "release",
                IgnoreCase = true
            }
        }
    }
}
```

Usa `IsEmpty` e `IsNotEmpty` quando interessa soltanto la presenza di elementi nella collezione.

## Gruppi Logici

I filtri e i gruppi annidati in un `AutomaticQueryGroup` usano il `LogicalOperator` del gruppo:

```csharp
var root = new AutomaticQueryGroup
{
    LogicalOperator = AutomaticQueryLogicalOperator.And,
    Filters =
    {
        new AutomaticQueryFilter
        {
            PropertyName = nameof(Post.Author),
            Operator = AutomaticQueryOperator.Equal,
            Value = "Andrea"
        }
    },
    Groups =
    {
        new AutomaticQueryGroup
        {
            LogicalOperator = AutomaticQueryLogicalOperator.Or,
            Filters =
            {
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "release"
                },
                new AutomaticQueryFilter
                {
                    PropertyName = nameof(Post.Title),
                    Operator = AutomaticQueryOperator.Contains,
                    Value = "roadmap"
                }
            }
        }
    }
};
```

Questo rappresenta `Author == "Andrea" AND (Title contiene "release" OR Title contiene "roadmap")`.

## Compatibilità Ed Estensione

Le query automatiche sono additive. Continua a usare una `ChillQuery` personalizzata quando il filtro richiede:

- regole di autorizzazione o tenant legate al contesto corrente
- funzioni database specifiche del provider
- join, proiezioni o espressioni calcolate
- comportamento specifico del dominio che non può essere espresso in sicurezza come filtro di proprietà

Il prototipo costruisce expression tree e lascia l'esecuzione al provider della query sorgente. Il supporto può variare tra provider, specialmente per la normalizzazione delle stringhe, i tipi data/ora e le operazioni su collezioni annidate. Verifica le definizioni importanti con il provider relazionale usato in produzione invece di affidarti soltanto al comportamento in memoria.

Il lavoro di integrazione pianificato include metadati di schema per gli operatori supportati, builder di filtri UI e copertura dei provider relazionali.
