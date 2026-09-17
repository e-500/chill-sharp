# Preparare Un Modello Per ChillSharp

Versione originale in inglese: [English](../ModelPreparation.md)

Questo documento descrive i requisiti lato modello per esporre un dominio EF Core tramite ChillSharp.

## Obiettivi

Dopo la preparazione, il modello puo:

- essere attivato dinamicamente tramite Chill type name
- essere interrogato e modificato tramite Chill DTO
- esporre metadati di schema ai client
- partecipare alla manutenzione dei campi di audit
- usare culture label e informazioni utente specifiche del contesto

## 1. Implementare `IChillContext`

Il tuo `DbContext` deve implementare `IChillContext`.

Comportamento richiesto:

```csharp
public class AppDbContext : DbContext, IChillContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public string GetChillTypePrefix()
    {
        return "MyCompany.MyProduct.Data";
    }

    public string GetPrimaryCultureName()
    {
        return "en-US";
    }

    public string GetSecondaryCultureName()
    {
        return "it-IT";
    }

    public string GetCurrentUserName()
    {
        return Environment.UserName;
    }
}
```

### A cosa serve ogni metodo

- `GetChillTypePrefix()`
  Espande nomi brevi come `Model.Blog` in tipi CLR completamente qualificati.

- `GetPrimaryCultureName()`
  Definisce quale cultura deve usare `PrimaryLanguageLabel`.

- `GetSecondaryCultureName()`
  Definisce quale cultura deve usare `SecondaryLanguageLabel`.

- `GetCurrentUserName()`
  Alimenta il tracciamento audit delle entita.

Ogni istanza di contesto puo restituire valori diversi. Questo e importante in host multi-tenant o multi-modulo dove possono coesistere piu contesti Chill con impostazioni linguistiche o utente differenti.

## 2. Usare `ChillEntity` Per Le Entita Esposte

Il pattern consigliato e ereditare da `ChillSharp.EF.ChillEntity`.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

[ChillEntity(
    UniquePropertyKeyString: "4E16F6C0-6B95-4D67-98BC-9F4D0D63EAF1",
    PrimaryLanguageLabel: "Blog",
    SecondaryLanguageLabel: "Blog")]
public class Blog : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "50B1BB6C-D794-41E4-A85C-D4F9D7A6FA7E",
        PrimaryLanguageLabel: "Blog title",
        SecondaryLanguageLabel: "Titolo del blog")]
    public string Title { get; set; } = string.Empty;

    [ChillProperty(
        UniquePropertyKeyString: "A18E7754-D8F7-45FE-B8A8-EA762A4EC9E6",
        PrimaryLanguageLabel: "Blog url",
        SecondaryLanguageLabel: "Url del blog")]
    public string Url { get; set; } = string.Empty;

    public override string GetLabel(IChillContext context) => Title;
}
```

`ChillEntity` fornisce gia:

- `Guid`
- `Label`
- `ShortLabel`
- `FullTextContent`
- `Checksum`
- `LastUpdateUser`
- `LastUpdate`
- `LastUpdateUtcOffset`

## 3. Annotare Le Proprieta Esposte

ChillSharp considera parte della superficie metadata soltanto le proprieta decorate con `[ChillProperty]`.

Questo influenza:

- mapping DTO
- generazione schema
- calcolo checksum
- metadati label

Se una proprieta non e marcata con `[ChillProperty]`, non fa parte della superficie proprieta standard di Chill.

## 4. Capire Gli Hook Del Ciclo Di Vita

`ChillEngine` guida i metodi del ciclo di vita delle entita.

### Flusso di creazione

Durante la creazione, ChillSharp esegue:

1. `OnCreate(context)`
2. `OnUpdate(context)`
3. save
4. aggiornamento audit interno + `OnAfterUpdate(context)`
5. ricalcolo di `Label`, `ShortLabel`, `FullTextContent`
6. save

### Flusso di aggiornamento

Durante l'update, ChillSharp esegue:

1. `OnUpdate(context)`
2. save
3. aggiornamento audit interno + `OnAfterUpdate(context)`
4. ricalcolo di `Label`, `ShortLabel`, `FullTextContent`
5. save

### Flusso di eliminazione

Durante il delete, ChillSharp esegue:

1. `OnDelete(context)`
2. save delete
3. `OnAfterDelete(context)`
4. save

## 5. Comportamento Dei Campi Di Audit

`ChillEntity` mantiene automaticamente:

- `Checksum`
- `LastUpdateUser`
- `LastUpdate`
- `LastUpdateUtcOffset`

Il checksum viene calcolato da tutti i valori `[ChillProperty]` tranne i campi di audit stessi.

Note:

- i valori scalari vengono serializzati usando cultura invariata
- i riferimenti `IChillEntity` contribuiscono con il loro `Guid`
- le collezioni vengono appiattite in una sequenza deterministica prima di sommare i byte

### Perche ridefinire `OnAfterUpdate()` e sicuro

`ChillEntity` usa un'implementazione esplicita di interfaccia per `IChillEntity.OnAfterUpdate(...)`.

`ChillEngine` chiama `OnAfterUpdate()` tramite l'interfaccia, quindi il flusso runtime e:

1. aggiornare i campi di audit
2. chiamare l'override derivato di `public virtual OnAfterUpdate(...)`

In questo modo le entita derivate hanno una superficie di override pulita, mentre la logica audit di base non puo essere saltata accidentalmente.

## 6. Label E Culture

`PrimaryLanguageLabel` e `SecondaryLanguageLabel` non sono solo commenti. Vengono interpretati usando la UI culture attiva e il `IChillContext` attivo.

Comportamento corrente:

- se la UI culture corrente corrisponde alla cultura secondaria del contesto, ChillSharp preferisce `SecondaryLanguageLabel`
- se corrisponde alla cultura primaria, ChillSharp preferisce `PrimaryLanguageLabel`
- altrimenti fa fallback prima su primaria e poi su secondaria

Questa logica viene usata quando vengono generati i metadati di schema.

## 7. Query

Le query dovrebbero implementare `IChillQuery<IChillEntity>` e possono essere decorate anch'esse con `ChillEntityAttribute` e `ChillPropertyAttribute`.

Questo consente di generare lo schema delle query esattamente come quello delle entita.

## 8. Requisiti Per La Persistenza Dello Schema

Se vuoi metadati di schema persistiti e cache dello schema, il contesto deve anche implementare `IChillSchemaDbContext` e includere:

```csharp
modelBuilder.AddChillSchemaModel();
```

Poi registra:

```csharp
builder.Services.AddChillSchema<AppDbContext>();
```

## 9. Requisiti Per Auth

Se vuoi autenticazione e permessi ChillSharp, il contesto deve implementare `IChillAuthDbContext` e includere:

```csharp
modelBuilder.AddChillAuthModel();
```

Poi registra uno tra:

```csharp
builder.Services.AddChillAuthApi<AppDbContext>();
builder.Services.AddChillAuthIdentityApi<AppDbContext, IdentityUser>();
```

## 10. Requisiti Per I18n

Se vuoi storage e lookup di testi localizzati, il contesto deve implementare `IChillI18nDbContext` e includere:

```csharp
modelBuilder.AddChillI18nModel();
```

Poi registra:

```csharp
builder.Services.AddChillI18nApi<AppDbContext>();
```

## 11. Raccomandazioni

- Preferisci ereditare da `ChillEntity` invece di implementare `IChillEntity` da zero.
- Usa GUID stabili in `UniquePropertyKeyString` e `UniqueEntityKeyString`.
- Marca solo le proprieta che vuoi davvero esporre nella superficie DTO/schema di Chill.
- Mantieni `GetLabel()` e `GetFullTextContent()` abbastanza economici da eseguire nei normali flussi CRUD.
- Restituisci una vera identita di richiesta da `GetCurrentUserName()` negli host API.
- Mantieni le impostazioni cultura specifiche del contesto nel contesto, non in global statici.
