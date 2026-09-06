# Configurazione del menu CRUD

Versione originale in inglese: [English](../../UiCore/CRUD.md)

Crea una voce di menu con `ComponentName` impostato a `CRUD`. Il suo `ComponentConfigurationJson` deve essere un oggetto JSON; le chiavi non distinguono maiuscole e minuscole. `chillType` è obbligatorio. Normalmente è il Chill type dell'entità e `chillQuery` è il Chill type della query che restituisce l'entità.

Quando `chillQuery` è omesso o `null`, il CRUD usa la modalità query automatica. Il dialogo Cerca viene generato da tutti i campi dello schema dell'entità, ogni campo è facoltativo e ciascun campo valorizzato viene inviato come filtro `Equal`. I campi vuoti non vengono inviati. Le selezioni di riferimenti a entità vengono ridotte al relativo GUID per il confronto di uguaglianza. Se `chillQuery` è configurato, continua a essere usato il relativo schema query dedicato con il payload esistente.

La configurazione minima per la query automatica è:

```json
{
  "chillType": "Model.Post"
}
```

```json
{
  "chillType": "Model.Post",
  "chillQuery": "Query.PostQuery",
  "viewCode": "default",
  "disableAdd": false,
  "disableCreate": false,
  "disableEdit": false,
  "disableInlineEdit": false,
  "disableDelete": false,
  "defaultValues": {},
  "fixedValues": {},
  "fixedQueryValues": {},
  "defaultQueryValues": {},
  "relations": []
}
```

## Opzioni

| Chiave | Tipo | Predefinito | Effetto |
| --- | --- | --- | --- |
| `chillType` | stringa | obbligatorio | Chill type dell'entità mostrata e modificata dal task. |
| `chillQuery` | stringa o `null` | `null` | Chill type della query. Se omesso, la UI genera dallo schema dell'entità un form automatico con filtri di uguaglianza. |
| `viewCode` | stringa | `default` | Codice della vista di schema usato dal task. |
| `disableAdd` | booleano | `false` | Nasconde il comando Aggiungi. |
| `disableCreate` | booleano | `false` | Impedisce la creazione di nuovi record. |
| `disableEdit` | booleano | `false` | Impedisce la modifica nel dialogo. |
| `disableInlineEdit` | booleano | `false` | Impedisce la modifica inline nella tabella. |
| `disableDelete` | booleano | `false` | Impedisce l'eliminazione. |
| `defaultValues` | oggetto | `{}` | Valori iniziali del form di creazione, modificabili dall'utente. |
| `fixedValues` | oggetto | `{}` | Valori di creazione resi di sola lettura nel form e nell'editor inline. |
| `fixedQueryValues` | oggetto | `{}` | Filtri query obbligatori che l'utente non può modificare. |
| `defaultQueryValues` | oggetto | `{}` | Valori iniziali della query che l'utente può modificare. |
| `relationLabel` | stringa o oggetto | omesso | Etichetta del CRUD aperto come relazione. La forma oggetto è `{ "labelGuid", "primaryDefaultText", "secondaryDefaultText" }`. |
| `relations` | array | `[]` | Definizioni CRUD figlie disponibili nel menu azioni di ogni riga. |

Le proprietà JSON sconosciute sono mantenute dall'editor del menu ma non sono opzioni CRUD standard.

## Valori e segnaposto delle relazioni

I valori nei quattro oggetti di valori sono valori JSON. In una relazione annidata, la stringa `@{FieldName}` legge quella proprietà dalla riga padre selezionata e `@{mock}` fornisce un oggetto entità leggero per tale riga padre.

### Quale contenitore viene applicato e quando

Esistono due flussi indipendenti:

| Flusso | Valori iniziali modificabili | Valori con precedenza |
| --- | --- | --- |
| Ricerca/query | `defaultQueryValues` | `fixedQueryValues` |
| Creazione entità | `defaultValues` | `fixedValues` |

UI Core unisce ogni flusso in questo ordine. Se la stessa proprietà compare in entrambi i contenitori, prevale il valore nel contenitore `fixed...`. Usa solo il contenitore fixed quando un valore non deve essere modificato; usa solo quello default quando è soltanto un valore iniziale utile. Nella modalità query automatica, i valori query valorizzati vengono convertiti in filtri `Equal` come quelli inseriti nel form generato.

`fixedQueryValues` vincola i record restituiti dal CRUD figlio. `fixedValues` vincola l'entità inviata dal flusso di creazione e rende tali proprietà di sola lettura nel form e nell'editor inline. I contenitori fixed sono configurazione, non sostituiscono autorizzazione o validazione lato server: l'API deve continuare a imporre tenant, proprietà e autorizzazioni.

### Valori statici compatibili con CLR

Il letterale JSON viene passato come valore della proprietà; UI Core non lo valuta né lo converte prima della normale serializzazione di entità/query. Usa la rappresentazione JSON attesa dall'API ChillSharp per la proprietà CLR.

```json
{
  "defaultQueryValues": {
    "IsPublished": true,
    "MinimumScore": 50,
    "Category": "News",
    "From": "2026-01-01T00:00:00+01:00"
  },
  "fixedValues": {
    "TenantCode": "acme",
    "Priority": 10,
    "IsInternal": false,
    "ArchivedAt": null
  }
}
```

Usa stringhe JSON per `string`, `Guid`, `DateTime`, `DateTimeOffset`, `DateOnly` CLR e per gli enum che il server espone come stringhe. Usa numeri JSON per valori CLR numerici e booleani JSON per `bool`. Date e GUID devono essere stringhe JSON, non espressioni JavaScript: `"2026-01-01"` e `"8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b"`, non `new Date(...)` o `Guid.NewGuid()`.

### Valori statici di riferimento a entità

Sì: un riferimento a entità può essere statico poiché un oggetto JSON è un valore ammesso. Fornisci la stessa forma di riferimento accettata dalla proprietà destinazione, normalmente almeno identificatore e tipo dell'entità riferita:

```json
{
  "fixedQueryValues": {
    "Customer": {
      "guid": "8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b",
      "chillType": "Model.Customer"
    }
  },
  "fixedValues": {
    "Status": {
      "guid": "7d8af0dd-d20d-4bb7-9a4d-f3b9d2f9c2b4",
      "chillType": "Model.OrderStatus",
      "label": "Approved"
    }
  }
}
```

`label` è metadato UI facoltativo. Non usarlo come identità: l'entità è identificata da `guid`. Includi `chillType` quando il riferimento può essere polimorfico o quando server/client richiedono informazioni di tipo esplicite. Il nome della proprietà e la forma dell'oggetto devono corrispondere allo schema query o entità destinazione; una proprietà di chiave esterna scalare richiede invece la stringa GUID scalare, ad esempio `"CustomerGuid": "8d0946dc-fc2b-4d95-b5ca-6f12d9618a5b"`.

### Valori dinamici: `@{FieldName}` e `@{mock}`

I segnaposto vengono risolti solo aprendo una relazione dalla riga padre selezionata. Non sono espressioni e non vengono valutati in un CRUD menu radice, perché non esiste un'entità padre. In quel caso la stringa del segnaposto rimane invariata.

- `@{Guid}` copia il valore `Guid`/`guid` della riga selezionata.
- `@{CustomerCode}` copia un campo dall'oggetto `properties` della riga selezionata o dalle proprietà dirette dell'oggetto. Un token camel-case usa anche come fallback il nome della proprietà Pascal-case.
- Un campo mancante viene risolto a `null`.
- `@{mock}` crea una copia leggera dell'entità padre selezionata. Contiene `guid`, `chillType`, `label` e una copia di `properties`; non recupera nuovamente l'entità.

Usa `@{mock}` per una proprietà di riferimento figlia come `Order` e `@{Guid}` per una chiave esterna scalare come `OrderGuid`:

```json
{
  "relations": [
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "fixedQueryValues": { "Order": "@{mock}" },
      "defaultQueryValues": { "Order": "@{mock}" },
      "fixedValues": { "Order": "@{mock}" },
      "defaultValues": { "Order": "@{mock}" }
    },
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "fixedQueryValues": { "OrderGuid": "@{Guid}" },
      "fixedValues": { "OrderGuid": "@{Guid}" }
    }
  ]
}
```

Usa la prima forma solo quando lo schema query/entità espone una proprietà riferimento-entità `Order`. Usa la seconda solo quando espone una proprietà scalare `OrderGuid`. Non inviare `@{mock}` a una proprietà GUID scalare.

```json
{
  "chillType": "Model.Order",
  "chillQuery": "Query.OrderQuery",
  "relations": [
    {
      "chillType": "Model.OrderRow",
      "chillQuery": "Query.OrderRowQuery",
      "relationLabel": {
        "labelGuid": "ORDER-ROWS-LABEL",
        "primaryDefaultText": "Rows",
        "secondaryDefaultText": "Righe"
      },
      "fixedQueryValues": { "Order": "@{mock}" },
      "defaultQueryValues": { "Order": "@{mock}" },
      "defaultValues": { "Order": "@{mock}" },
      "fixedValues": { "Order": "@{mock}" }
    }
  ]
}
```

Usa `fixedQueryValues` per un filtro padre non modificabile e `defaultQueryValues` per un filtro iniziale modificabile. Usa `fixedValues` per un valore di creazione di sola lettura e `defaultValues` per un valore iniziale di creazione modificabile. Le relazioni possono contenere ulteriori array `relations`.

## Persistenza della voce di menu

L'API menu memorizza il JSON come stringa. Per esempio, usa `ComponentName: "CRUD"` e serializza l'oggetto in `ComponentConfigurationJson` quando chiami `set-menu`. JSON non valido, oppure un array JSON al posto di un oggetto, non può configurare il task.
