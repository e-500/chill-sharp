# Serializzazione di data e ora ChillSharp

Versione originale in inglese: [English](../DateTimeSerialization.md)


Questo documento spiega come ChillSharp serializza e analizza i valori `DateTimeOffset`, `DateTime`, `DateOnly` e `TimeOnly` nei payload DTO.

Confronta inoltre il comportamento di ChillSharp con il comportamento predefinito ASP.NET Core `System.Text.Json` in modo da poter vedere rapidamente qual è il comportamento .NET standard e qual è il comportamento specifico di ChillSharp.

## Perché è importante

ChillSharp sposta i dati attraverso i contenitori delle proprietà DTO anziché tramite parametri del controller fortemente tipizzati. Ciò significa che i valori di data e ora vengono convertiti esplicitamente all'interno del mappatore DTO.

Per la maggior parte delle applicazioni le domande importanti sono:

- quale formato di stringa lascia il server
- quale formato di stringa il server accetta in input
- se gli offset e i fusi orari vengono preservati, normalizzati o ignorati

ChillSharp ora segue il comportamento .NET standard per `DateOnly` e `TimeOnly`, accettando comunque stringhe data-ora ISO 8601 complete durante la rilettura in tali tipi CLR.

## Tabella di confronto rapido

| Tipo CLR | ASP.NET Core predefinito/`System.Text.Json` | Uscita ChillSharp |
| --- | --- | --- |
|  | Data-ora ISO 8601 con offset | Data-ora ISO 8601 con offset |
|  | Data-ora ISO 8601, basato su `DateTime.Kind` | Data e ora ISO 8601 convertite nel fuso orario del sistema ChillSharp |
|  |  |  |
|  |  |  |

## Fuso orario del sistema ChillSharp

ChillSharp utilizza un fuso orario di sistema configurabile solo per `DateTime` e alcuni casi di normalizzazione `DateTimeOffset`.

Variabile d'ambiente:

```text
CHILLSHARP_SYSTEM_TIMEZONE
```

Predefinito:

```text
Europe/Rome
```

Valore atteso:

- un ID fuso orario IANA come `Europe/Rome`
- un altro esempio è `America/New_York`

Questa impostazione **non** modifica il formato di output di `DateOnly` o `TimeOnly`.

## Serializzazione in uscita

La serializzazione in uscita avviene quando ChillSharp legge i valori CLR di entità/query e li scrive in DTO `Properties`.

### `DateTimeOffset`

ChillSharp scrive `DateTimeOffset` esattamente come data-ora ISO 8601 con offset.

Esempio di valore CLR:

```csharp
new DateTimeOffset(2026, 4, 11, 14, 30, 0, TimeSpan.FromHours(2))
```

Serializzato da ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

Questo è effettivamente allineato con la normale serializzazione JSON ASP.NET Core.

### `DateTime`

ChillSharp scrive `DateTime` come data/ora ISO 8601 nel fuso orario del sistema ChillSharp configurato.

Se il valore di origine è UTC, ChillSharp lo converte nel fuso orario del sistema configurato prima della scrittura.

Esempio con `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```csharp
new DateTime(2026, 4, 11, 12, 30, 0, DateTimeKind.Utc)
```

Serializzato da ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

Un altro esempio con un `DateTime` non specificato:

```csharp
new DateTime(2026, 4, 11, 14, 30, 0, DateTimeKind.Unspecified)
```

Serializzato da ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

La differenza rispetto al semplice ASP.NET Core è che ChillSharp applica in modo coerente un fuso orario di sistema configurato durante la scrittura di `DateTime`.

### `DateOnly`

ChillSharp ora mantiene il comportamento .NET standard per `DateOnly`.

Esempio di valore CLR:

```csharp
new DateOnly(2026, 4, 11)
```

Serializzato da ChillSharp:

```json
"2026-04-11"
```

Questo è intenzionalmente semplice. Non sono presenti offset, componenti temporali e conversioni del fuso orario sull'output.

### `TimeOnly`

ChillSharp ora mantiene il comportamento .NET standard per `TimeOnly`.

Esempio di valore CLR:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Serializzato da ChillSharp:

```json
"14:30:15.1230000"
```

Non è presente alcuna conversione di data e fuso orario sull'output.

## Analisi in entrata

L'analisi in entrata avviene quando ChillSharp legge DTO `Properties` e li applica a oggetti CLR entità/query.

Questo è il lato più permissivo del mappatore.

### `DateTimeOffset` regole di input

Se il JSON in entrata contiene:

- 
- un offset UTC esplicito

e il target CLR è `DateTimeOffset`, ChillSharp si comporta in questo modo:

- se il valore è UTC (`Z` o `+00:00`), converte il valore nel fuso orario del sistema ChillSharp configurato
- Se il valore ha un altro offset esplicito, mantiene l'offset così com'è

Ingresso di esempio:

```json
"2026-04-11T12:30:00Z"
```

Memorizzato in una proprietà `DateTimeOffset` con fuso orario del sistema `Europe/Rome`:

```csharp
2026-04-11 14:30:00 +02:00
```

Ingresso di esempio:

```json
"2026-04-11T12:30:00+01:00"
```

Memorizzato in una proprietà `DateTimeOffset`:

```csharp
2026-04-11 12:30:00 +01:00
```

### `DateTime` regole di input

Se la destinazione CLR è `DateTime`:

- L'input UTC viene convertito nel fuso orario del sistema ChillSharp configurato
- Anche l'input con un offset esplicito viene convertito nel fuso orario del sistema ChillSharp configurato
- l'input senza offset viene analizzato come un normale valore data-ora

Ingresso di esempio:

```json
"2026-04-11T12:30:00Z"
```

Memorizzato in una proprietà `DateTime` con fuso orario del sistema `Europe/Rome`:

```csharp
2026-04-11 14:30:00
```

Ingresso di esempio:

```json
"2026-04-11T12:30:00+01:00"
```

Memorizzato in una proprietà `DateTime` con fuso orario del sistema `Europe/Rome`:

```csharp
2026-04-11 13:30:00
```

### `DateOnly` regole di input

Se la destinazione CLR è `DateOnly`, ChillSharp estrae solo l'anno, il mese e il giorno.

Ciò significa che accetta entrambi:

- una semplice stringa di data
- una stringa data-ora ISO 8601 completa

e ignora le informazioni su ora, offset e fuso orario.

Ingresso di esempio:

```json
"2026-04-11"
```

Memorizzato come:

```csharp
new DateOnly(2026, 4, 11)
```

Ingresso di esempio:

```json
"2026-04-11T23:59:58.321-05:00"
```

Memorizzato come:

```csharp
new DateOnly(2026, 4, 11)
```

Questa regola è intenzionale. `DateOnly` rappresenta solo una data di calendario, quindi ChillSharp scarta i dettagli di ora e zona durante l'assegnazione.

### `TimeOnly` regole di input

Se la destinazione CLR è `TimeOnly`, ChillSharp estrae solo la parte temporale.

Ciò significa che accetta entrambi:

- una semplice stringa temporale
- una stringa data-ora ISO 8601 completa

e ignora le informazioni su data, offset e fuso orario.

Ingresso di esempio:

```json
"14:30:15.1230000"
```

Memorizzato come:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Ingresso di esempio:

```json
"2026-04-11T23:59:58.321-05:00"
```

Memorizzato come:

```csharp
new TimeOnly(23, 59, 58, 321)
```

Ciò è utile quando i client inviano un timestamp completo ma il campo di destinazione rappresenta concettualmente solo l'ora dell'orologio locale.

## Esempi affiancati

Assumere:

```text
CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome
```

### Esempio 1: `DateTimeOffset`

Valore CLR:

```csharp
new DateTimeOffset(2026, 4, 11, 14, 30, 0, TimeSpan.FromHours(2))
```

Output ASP.NET Core predefinito:

```json
"2026-04-11T14:30:00+02:00"
```

Uscita ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

### Esempio 2: `DateTime` in UTC

Valore CLR:

```csharp
new DateTime(2026, 4, 11, 12, 30, 0, DateTimeKind.Utc)
```

Output ASP.NET Core predefinito:

```json
"2026-04-11T12:30:00Z"
```

Uscita ChillSharp:

```json
"2026-04-11T14:30:00.0000000+02:00"
```

### Esempio 3: `DateOnly`

Valore CLR:

```csharp
new DateOnly(2026, 4, 11)
```

Output ASP.NET Core predefinito:

```json
"2026-04-11"
```

Uscita ChillSharp:

```json
"2026-04-11"
```

### Esempio 4: `TimeOnly`

Valore CLR:

```csharp
new TimeOnly(14, 30, 15, 123)
```

Output ASP.NET Core predefinito:

```json
"14:30:15.1230000"
```

Uscita ChillSharp:

```json
"14:30:15.1230000"
```

### Esempio 5: timestamp completo inviato a `DateOnly`

JSON in entrata:

```json
"2026-04-11T23:59:58.321-05:00"
```

Archiviato da ChillSharp in una proprietà `DateOnly`:

```csharp
new DateOnly(2026, 4, 11)
```

### Esempio 6: timestamp completo inviato a `TimeOnly`

JSON in entrata:

```json
"2026-04-11T23:59:58.321-05:00"
```

Archiviato da ChillSharp in una proprietà `TimeOnly`:

```csharp
new TimeOnly(23, 59, 58, 321)
```

## Guida pratica

- Utilizzare `DateTimeOffset` quando l'offset stesso è importante e deve sopravvivere ai viaggi di andata e ritorno.
- Utilizzare `DateTime` quando l'applicazione considera un valore come l'ora locale nel fuso orario del sistema ChillSharp configurato.
- Utilizza `DateOnly` per compleanni, date contabili, date lavorative, scadenze per giorno di calendario e concetti simili.
- Utilizzare `TimeOnly` per orari di apertura, orari degli appuntamenti e altri valori che non sono intenzionalmente timestamp completi.

## Configurazione correlata

Per il riferimento alle variabili d'ambiente, vedere:

- [Configurazione/README.md](./Configuration/README.md)

Per esempi di Docker e ambiente runtime, vedere:

- [HowTo/05-docker-env-variables.md](./HowTo/05-docker-env-variables.md)
