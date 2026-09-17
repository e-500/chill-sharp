# Politica DateTime di ChillSharp

Versione originale in inglese: [English](../../DateTimePolicy/README.md)


Questo documento definisce la policy DTO di ChillSharp per i valori `DateTime` e `DateTimeOffset`.

La policy si applica quando ChillSharp legge o scrive valori tramite contenitori di proprietà DTO, come `ChillDtoEntity.Properties` e `ChillDtoQuery.Properties`.

## Fuso orario del sistema

ChillSharp utilizza un fuso orario di sistema configurato quando un valore DTO non presenta un offset esplicito.

Variabile d'ambiente:

```text
CHILLSHARP_SYSTEM_TIMEZONE
```

Predefinito:

```text
Europe/Rome
```

Utilizza un ID fuso orario IANA, ad esempio:

```text
Europe/Rome
America/New_York
UTC
```

Questo fuso orario configurato non è la stessa cosa di `DateTimeKind.Local`. `DateTimeKind.Local` indica il fuso orario locale del sistema operativo. ChillSharp utilizza esplicitamente il proprio fuso orario configurato.

## Valori DTO in entrata

I valori in ingresso sono valori ricevuti da un client e applicati alle proprietà CLR.

### DateTimeOffset

`DateTimeOffset` conserva un offset esplicito quando il client ne invia uno.

Esempi:

```text
2026-04-11T14:30:00.0000000+02:00
2026-04-11T12:30:00.0000000Z
```

Politica:

- se il valore in ingresso ha `Z`, conservalo come UTC `DateTimeOffset`
- se il valore in entrata ha un offset esplicito, preserva tale offset
- se il valore in ingresso non ha offset, interpretarlo come ora locale in `CHILLSHARP_SYSTEM_TIMEZONE`
- memorizzare il valore risultante come `DateTimeOffset` con l'offset risolto

Esempio con `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
Incoming: 2026-04-11T14:30:00
Stored:   2026-04-11T14:30:00+02:00
```

### Data e ora

`DateTime` rappresenta un istante ed è normalizzato in UTC quando letto dall'ingresso DTO.

Politica:

- se il valore in ingresso ha `Z`, analizzalo come UTC
- se il valore in entrata ha un offset esplicito, analizzalo come quell'istante
- se il valore in ingresso non ha offset, interpretarlo come ora locale in `CHILLSHARP_SYSTEM_TIMEZONE`
- memorizzare il valore risultante come UTC `DateTime`
- imposta `DateTime.Kind` su `DateTimeKind.Utc`

Esempio con `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
Incoming: 2026-04-11T14:30:00
Stored:   2026-04-11T12:30:00Z
Kind:     Utc
```

Esempio con un offset esplicito:

```text
Incoming: 2026-04-11T14:30:00+02:00
Stored:   2026-04-11T12:30:00Z
Kind:     Utc
```

## Valori DTO in uscita

I valori in uscita sono valori CLR serializzati nei contenitori delle proprietà DTO prima di restituire i dati a un client.

### DateTimeOffset

ChillSharp serializza `DateTimeOffset` come stringa ISO 8601 con il relativo offset.

```text
2026-04-11T14:30:00.0000000+02:00
```

### Data e ora

ChillSharp serializza `DateTime` come stringa ISO 8601 con un offset esplicito.

Politica:

- se il valore di origine è UTC, convertirlo in `CHILLSHARP_SYSTEM_TIMEZONE` per l'output DTO
- se il valore di origine non è specificato, interpretarlo come ora locale in `CHILLSHARP_SYSTEM_TIMEZONE`
- emettere una stringa ISO 8601 con l'offset risolto

Esempio con `CHILLSHARP_SYSTEM_TIMEZONE=Europe/Rome`:

```text
CLR:      2026-04-11T12:30:00Z
DTO:      2026-04-11T14:30:00.0000000+02:00
```

A seconda del serializzatore JSON, il carattere `+` potrebbe apparire come `\u002B` sul cavo:

```json
"2026-04-11T14:30:00.0000000\u002B02:00"
```

Questo è un JSON valido e i client lo rileggono come `+02:00`.

## Guida al database

Questa policy è progettata per funzionare in modo pulito con provider come PostgreSQL/Npgsql.

Mappatura consigliata:

- utilizzare `DateTime` per valori istantanei che devono essere mantenuti come UTC
- utilizzare `DateTimeOffset` quando si preservano le questioni di offset in entrata
- utilizzare `DateOnly` e `TimeOnly` per date di calendario o ore del giorno che non siano istantanee

Per PostgreSQL:

- I valori `DateTime` prodotti dall'analisi DTO sono UTC e sono adatti per `timestamp with time zone`
- i valori dell'orologio locale non devono essere modellati come `DateTime` a meno che non si intenda convertirli in UTC

## Campi di controllo gestiti dal server

I campi di controllo gestiti dal server ChillSharp vengono ignorati quando si applicano i valori DTO dell'entità in entrata:

- 
- 
- 
- 

I client possono ricevere questi valori dall'output DTO, ma il loro invio non sovrascrive lo stato dell'entità gestita dal server.
