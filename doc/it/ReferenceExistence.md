# Verificare La Presenza Di Un Riferimento EF Core Senza Caricarlo

English version: [English](../ReferenceExistence.md)

`ChillSharp.EF.ChillEntryExtension.Exist()` risponde a una domanda piccola ma utile: questa navigazione di riferimento ha attualmente tutti i valori della chiave esterna configurata?

```csharp
using ChillSharp.EF;

var hasCustomerReference = context.Entry(order)
    .Reference(x => x.Customer)
    .Exist();
```

La chiamata legge i valori correnti delle proprieta FK dal change tracker di EF Core. Con l'argomento predefinito non esegue query sul database e non carica `order.Customer`.

> L'estensione si chiama `Exist`, non `Exists`. La sua firma e `Exist(bool loadIfExist = false)`.

## Quando Usarla

Usa `Exist()` nella logica del modello che deve distinguere una relazione opzionale assente da una relazione assegnata, evitando un caricamento non necessario dell'entita principale. E utile in particolare in `OnUpdate`, `OnSelect` o nella logica di elaborazione dei DTO quando serve soltanto decidere quale ramo eseguire.

```csharp
public override void OnUpdate(IChillContext context)
{
    var db = (AppDbContext)context;

    if (db.Entry(this).Reference(x => x.Customer).Exist())
    {
        // E stato assegnato un valore FK Customer. Customer non e ancora caricato.
        CustomerSummaryRequired = true;
    }
    else
    {
        CustomerSummaryRequired = false;
    }
}
```

Questo e preferibile a controllare `Customer != null` quando la navigazione puo essere semplicemente non caricata. Una navigazione null non distingue tra “nessuna relazione” e “relazione non caricata”.

## Caricamento Opzionale

Passa `true` soltanto quando l'operazione successiva ha davvero bisogno dell'entita correlata:

```csharp
var customerReference = context.Entry(order).Reference(x => x.Customer);

if (customerReference.Exist(loadIfExist: true) && order.Customer is { } customer)
{
    // EF Core ha caricato Customer se non era gia caricato.
    var customerName = customer.Name;
}
```

Il comportamento e:

| Chiamata | Valori FK incompleti o null | Valori FK presenti e navigazione non caricata | Navigazione caricata? |
| --- | --- | --- | --- |
| `Exist()` | Restituisce `false` | Restituisce `true` | Nessun nuovo caricamento |
| `Exist(true)` | Restituisce `false` | Restituisce `true` | Prova a caricare il riferimento |

Se la navigazione era gia caricata, nessuna delle due forme la carica di nuovo. Poiche il risultato descrive comunque i valori FK, `Exist(true)` puo restituire `true` mentre la navigazione caricata e null se un database senza FK contiene un valore orfano.

## Database Senza FK Non Significa Modello Senza Relazione

Questa estensione funziona con un'implementazione legacy o con database senza FK solo se EF Core conosce ancora la relazione e le relative proprieta FK del dipendente. Un vincolo fisico nel database e i metadati di relazione di EF Core sono aspetti separati.

Per esempio, il database puo non applicare un vincolo da `Order.CustomerGuid` a `Customer.Guid`, ma il modello EF deve comunque avere la FK scalare e il mapping della relazione:

```csharp
public sealed class Order : ChillEntity
{
    public Guid? CustomerGuid { get; set; }
    public Customer? Customer { get; set; }
}

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.Entity<Order>()
        .HasOne(x => x.Customer)
        .WithMany()
        .HasForeignKey(x => x.CustomerGuid);
}
```

`Exist()` legge `CustomerGuid` tramite questo mapping. Una relazione non configurata in EF Core non ha metadati FK di `ReferenceEntry` che l'estensione possa ispezionare; in quel caso usa direttamente la chiave scalare oppure configura la relazione.

Le proprieta FK shadow sono supportate finche EF Core ha mappato la navigazione. L'estensione ottiene i metadati della proprieta FK dalla navigazione, senza richiedere una proprieta FK CLR pubblica.

## Significato Del Risultato

`Exist()` e un test locale di presenza dei valori FK. Non esegue una query di esistenza per la riga principale.

Di conseguenza:

- `true` significa che ogni componente FK configurato non e null nell'entry dipendente tracciata.
- `false` significa che almeno un componente FK e null.
- `true` non garantisce che la riga correlata esista, soprattutto quando il database non applica vincoli FK oppure contiene valori orfani legacy.
- `true` non significa che la navigazione sia caricata.

Se la regola di business richiede la prova che una riga principale esista, esegui una query esplicita, per esempio con `AnyAsync`, oppure usa `Exist(true)` e gestisci poi una navigazione caricata null. Preferisci la query esplicita quando serve un controllo di esistenza lato server senza materializzare il principale.

```csharp
var customerRowExists = await context.Set<Customer>()
    .AnyAsync(x => x.Guid == order.CustomerGuid, cancellationToken);
```

## Chiavi Composte E Convenzioni Sui Valori

Per una FK composta, `Exist()` restituisce `true` soltanto quando ogni componente non e null. Una chiave composta parzialmente popolata restituisce `false`.

L'estensione controlla `null`; non valida valori sentinella. Per esempio, `Guid.Empty`, `0` o una stringa vuota non sono null e quindi possono produrre `true` se sono i valori attualmente memorizzati. Usa la validazione appropriata al dominio quando tali valori significano “non assegnato”.

## Prerequisiti E Modalita Di Errore

L'entita deve essere collegata allo stesso `DbContext` EF Core usato per ottenere la sua entry. Chiama l'estensione sulla navigazione di riferimento dal lato dipendente: l'entita nel `ReferenceEntry` deve possedere le proprieta FK riportate da EF Core per quella navigazione. Chiamarla per un membro che non e un riferimento, per una navigazione senza metadati di relazione FK utilizzabili oppure per la navigazione lato principale di una relazione uno-a-uno puo generare `InvalidOperationException`, poiche le proprieta FK non appartengono all'entry ispezionata.

Non usarla per le navigazioni collection. Una collection richiede una domanda diversa—se esiste almeno una riga correlata—che normalmente richiede una query al database.

## Guida Alla Scelta

| Necessita | Usa |
| --- | --- |
| Determinare se un riferimento opzionale ha valori FK assegnati senza caricarlo | `Reference(...).Exist()` |
| Caricare il riferimento soltanto se i valori FK sono assegnati | `Reference(...).Exist(true)` |
| Provare che la riga principale esiste | Una query esplicita `Any`/`AnyAsync` |
| Determinare se una collection ha elementi | Una query sul set dipendente |

Mantieni `Exist()` per la decisione circoscritta di presenza FK. Il suo valore e rendere esplicito questo intento ed evitare il caricamento di un'entita correlata quando non necessario.
