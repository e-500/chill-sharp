# Generare Librerie Client

Versione originale in inglese: [English](../../ClientGeneration/README.md)

Questa sezione spiega come generare librerie client non-.NET per un host ChillSharp.

Target coperti qui:

- TypeScript
- Python

Per client generici gia pronti inclusi in questo repository, vedi:

- [../../../ext/chill-sharp-ts-client/README.md](../../../ext/chill-sharp-ts-client/README.md)
- [../../../ext/chill-sharp-react-client/README.md](../../../ext/chill-sharp-react-client/README.md)
- [../../../ext/chill-sharp-vue-client/README.md](../../../ext/chill-sharp-vue-client/README.md)
- [../../../ext/chill-sharp-ng-client/README.md](../../../ext/chill-sharp-ng-client/README.md)
- [../../../ext/chill-sharp-py-client/README.md](../../../ext/chill-sharp-py-client/README.md)

Questi pacchetti sono wrapper generici della HTTP API standard di ChillSharp. Il resto di questo documento copre la generazione di client specifici per host a partire da OpenAPI.

## Vincolo Importante

ChillSharp non pubblica automaticamente un documento OpenAPI da solo.

La generazione client dipende quindi dal fatto che l'applicazione host ne esponga uno tramite il normale tooling Swagger/OpenAPI di ASP.NET Core.

## 1. Esporre OpenAPI Nell'Host

Aggiungi la generazione Swagger all'applicazione host:

```csharp
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();
app.MapChillApi();
```

Con questa configurazione, un documento Swagger JSON standard e di solito disponibile a:

```text
/swagger/v1/swagger.json
```

Esempio:

```text
http://localhost:5000/swagger/v1/swagger.json
```

## 2. Decidere Cosa Deve Coprire Il Client Generato

Un host ChillSharp puo esporre varie superfici:

- core Chill API
- endpoint auth/account
- endpoint di gestione auth
- endpoint i18n

Se tutti i moduli sono registrati nello stesso host e Swagger e abilitato globalmente, il documento OpenAPI generato puo includerli tutti.

## 3. Generare Un Client TypeScript

Un'opzione pratica e `openapi-generator-cli`.

Installalo o usalo tramite il package manager che preferisci, poi esegui:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g typescript-fetch \
  -o generated/ts-client
```

Altri generatori TypeScript utili includono:

- `typescript-axios`
- `typescript-angular`

Esempio:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g typescript-axios \
  -o generated/ts-client
```

## 4. Generare Un Client Python

Usando lo stesso documento OpenAPI:

```bash
openapi-generator-cli generate \
  -i http://localhost:5000/swagger/v1/swagger.json \
  -g python \
  -o generated/python-client
```

Questo produce un pacchetto Python con model request e wrapper API basati sulla descrizione OpenAPI pubblicata.

## 5. Note Specifiche Dell'Host

I client generati sono accurati solo quanto il documento OpenAPI dell'host.

Questo significa che:

- se l'host non espone Swagger, non c'e nulla da cui generare
- se l'host esclude alcuni controller, quegli endpoint non compariranno nel client generato
- se auth e abilitata, il client generato dovra comunque essere configurato per gestire i bearer token

## 6. Workflow Consigliato

Per TypeScript e Python, il workflow consigliato e:

1. build dell'host ChillSharp
2. aggiungere Swagger/OpenAPI all'host
3. avviare l'host localmente o in CI
4. esportare `/swagger/v1/swagger.json`
5. generare la libreria client
6. pubblicare o committare il client generato secondo il workflow del progetto

## 7. Quando Preferire `ChillSharp.Client`

Se il consumer e .NET, preferisci `ChillSharp.Client`.

Usa client generati TypeScript o Python quando:

- il frontend e browser-based e non .NET
- ti servono automazioni o integrazioni Python
- vuoi client fortemente tipizzati in ambienti non-.NET

Se non ti servono tipi generati specifici dell'host, puoi anche usare i client generici presenti in `ext/`:

- `ext/chill-sharp-ts-client`
- `ext/chill-sharp-react-client`
- `ext/chill-sharp-vue-client`
- `ext/chill-sharp-ng-client`
- `ext/chill-sharp-py-client`

## 8. Indicazioni Di Stabilita

Se prevedi di generare client regolarmente:

- mantieni stabili le route pubbliche dell'host
- versiona l'API
- rigenera i client come parte del workflow di release
- tratta i cambi di shape OpenAPI come cambiamenti di contratto pubblico
