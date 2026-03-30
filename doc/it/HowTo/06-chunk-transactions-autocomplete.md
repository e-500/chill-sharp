# HOW-TO: Usare Chunk, Transazioni e Autocomplete

Versione originale in inglese: [English](../../HowTo/06-chunk-transactions-autocomplete.md)

Questo esempio mostra come inviare piu operazioni ChillSharp in una sola chiamata con `chunk`, come racchiudere le operazioni di scrittura in una singola transazione database e come usare `autocomplete` per DTO di entita e query.

## Obiettivo

Usare in modo efficiente la API core di ChillSharp quando il client deve:

- eseguire piu operazioni in una singola richiesta HTTP
- confermare un gruppo di scritture in modo atomico
- chiedere al server di completare o normalizzare i valori di un DTO prima del salvataggio

## 1. Preparare Il Client

Tutti gli esempi sotto usano il client .NET e assumono che la API core sia mappata su `/api/chill`.

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;

var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

## 2. Inviare Piu Operazioni Con `chunk`

`chunk` invia una lista di `ChillOperation` a `/api/chill/chunk`.
Ogni operazione puo contenere una `Query` o una `Entity`, a seconda del `Verb`.

Imposta `Index` in modo esplicito quando l'ordine di esecuzione e importante.

```csharp
using ChillSharp.Client.Dto;

var firstPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.Parse("11111111-1111-1111-1111-111111111111")
};
firstPost.Properties["Title"] = "First";
firstPost.Properties["Author"] = "Ada";

var secondPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.Parse("22222222-2222-2222-2222-222222222222")
};
secondPost.Properties["Title"] = "Second";
secondPost.Properties["Author"] = "Linus";

var updateFirstPost = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = firstPost.Guid
};
updateFirstPost.Properties["Title"] = "First updated";

var operations = client.Chunk(new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.CREATE, Entity = firstPost },
    new() { Index = 1, Verb = ChillOperationVerb.CREATE, Entity = secondPost },
    new() { Index = 2, Verb = ChillOperationVerb.UPDATE, Entity = updateFirstPost }
});
```

Cosa ti da questo:

- una richiesta HTTP invece di tre
- esecuzione ordinata tramite `Index`
- una risposta combinata contenente le operazioni elaborate

## 3. Racchiudere Un Chunk In Una Transazione

Usa `transaction` e `commit` quando tutte le operazioni di scrittura racchiuse devono avere successo o fallire insieme.

```csharp
var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
blog.Properties["Name"] = "Batch blog";
blog.Properties["Url"] = "https://example.local/batch-blog";

var post = new ChillDtoEntity
{
    ChillType = "Model.Post",
    Guid = Guid.NewGuid()
};
post.Properties["Title"] = "Batch post";
post.Properties["Blog"] = blog.Mock();

var transactionalOperations = client.Chunk(new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.TRANSACTION },
    new() { Index = 1, Verb = ChillOperationVerb.CREATE, Entity = blog },
    new() { Index = 2, Verb = ChillOperationVerb.CREATE, Entity = post },
    new() { Index = 3, Verb = ChillOperationVerb.COMMIT }
});
```

Usa questo pattern solo per le operazioni di scrittura che devono condividere la stessa transazione database.
Se un'operazione fallisce prima di `commit`, la transazione non viene confermata.

## 4. Eseguire Autocomplete Su Un DTO Entita

`autocomplete` usa lo stesso stile DTO di `create`, `update` e `delete`, ma chiama `/api/chill/autocomplete`.

Per le entita, ChillSharp esegue `OnAutocomplete(...)` senza persistere modifiche:

- se l'entita esiste gia, viene caricata dal `DbContext` corrente
- altrimenti viene collegata al contesto in stato `Added`
- la logica di autocomplete gira dentro una transazione temporanea
- la transazione viene annullata alla fine, quindi il database non cambia

Questo e utile per vedere in anteprima valori calcolati come slug, label, testo derivato o combinazioni di campi predefinite.

```csharp
var draftBlog = new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = Guid.NewGuid()
};
draftBlog.Properties["Title"] = "  My first ChillSharp blog  ";

var autocompletedBlog = client.Autocomplete(draftBlog);

Console.WriteLine(autocompletedBlog.GetString("Title"));
Console.WriteLine(autocompletedBlog.GetString("Url"));
```

Logica tipica lato entita:

```csharp
public override void OnAutocomplete(IChillContext context)
{
    base.OnAutocomplete(context);

    Title = Title?.Trim();

    if (string.IsNullOrWhiteSpace(Url) && !string.IsNullOrWhiteSpace(Title))
    {
        Url = "/blogs/" + Title.ToLowerInvariant().Replace(' ', '-');
    }
}
```

## 5. Eseguire Autocomplete Su Un DTO Query

Anche le query usano `/api/chill/autocomplete`, ma non partecipano al flusso transazionale del contesto EF Core.
Il DTO query viene semplicemente passato a `OnAutocomplete(...)` sulla `IChillQuery` risolta.

Questo e utile per normalizzare i filtri prima di `Query(...)`, per esempio:

- rimuovere spazi superflui dagli input testuali
- espandere un campo di ricerca in un campo full-text
- impostare valori predefiniti di paging o ordinamento

```csharp
var blogQuery = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery"
};
blogQuery.Properties["Title"] = "  chillsharp  ";

var autocompletedQuery = client.Autocomplete(blogQuery);

Console.WriteLine(autocompletedQuery.GetString("Title"));
Console.WriteLine(autocompletedQuery.GetString("FullTextSearch"));
```

Logica tipica lato query:

```csharp
public override void OnAutocomplete(IChillContext context)
{
    base.OnAutocomplete(context);

    Title = Title?.Trim();

    if (!string.IsNullOrWhiteSpace(Title))
    {
        FullTextSearch = Title;
    }
}
```

## 6. Scegliere La API Giusta

- usa `create`, `update` e `delete` quando l'operazione deve cambiare subito il database
- usa `chunk` quando piu operazioni devono viaggiare in una sola richiesta
- usa `transaction` piu `commit` dentro `chunk` quando piu scritture devono essere atomiche
- usa `autocomplete` quando il server deve calcolare o normalizzare valori senza salvarli

Prossimo: [Torna all'indice della documentazione](../README.md)
