# ChillSharp.Client

Versione originale in inglese: [English](../ChillSharpClient.md)


`ChillSharp.Client` è la libreria client .NET per chiamare un host ChillSharp da app console, lavoratori, test, app desktop o altri servizi .NET.

Usalo quando il consumer è .NET. Per i framework browser o l'automazione Python, utilizzare i client generici in `extra-libs/` o generare un client specifico dell'host da OpenAPI.

## Installa

Fare riferimento al progetto o al pacchetto `ChillSharp.Client` dall'applicazione .NET che utilizza.

```xml
<ProjectReference Include="..\ChillSharp.Client\ChillSharp.Client.csproj" />
```

Quindi importa gli spazi dei nomi client:

```csharp
using ChillSharp.Client;
using ChillSharp.Client.Dto;
```

I metodi dell'account di autenticazione utilizzano contratti di richiesta e risposta da `ChillSharp.Auth.Contracts`:

```csharp
using ChillSharp.Auth.Contracts;
```

I metodi I18n utilizzano contratti da `ChillSharp.I18n.Contracts`:

```csharp
using ChillSharp.I18n.Contracts;
```

## Crea un cliente

L'URL di base normale è l'endpoint principale di ChillSharp:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
```

Puoi anche passare la root dell'host. Il client aggiunge il percorso `api/chill` predefinito:

```csharp
var client = new ChillSharpClient("http://localhost:5000");
```

Per un percorso di base API personalizzato:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000",
    new ChillSharpClientOptions { ApiBasePath = "backend" });
```

Questo risolve l'API principale come:

```text
http://localhost:5000/backend/chill
```

## Cultura

Passa una cultura predefinita durante la lettura di schemi o testo i18n:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    CultureName: "it-IT");

var schema = client.GetSchema("Model.Blog", "default");
```

Puoi comunque sovrascrivere la cultura per chiamata allo schema:

```csharp
var englishSchema = client.GetSchema("Model.Blog", "default", "en-GB");
```

## Autenticazione

Se l'API è protetta, esegui l'autenticazione con uno di questi modelli.

Utilizza un token al portatore esistente:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    AuthToken: accessToken);
```

Utilizza le credenziali e consenti al client di accedere su richiesta:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    UserName: "admin",
    Password: "Pass123$");
```

Registrati o accedi tramite gli endpoint dell'account di autenticazione:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");

var token = client.LoginAuthAccount(new LoginAuthIdentityRequest
{
    UserNameOrEmail = "admin",
    Password = "Pass123$"
});
```

Il client archivia il token di accesso restituito e il token di aggiornamento. Le chiamate autenticate successivamente riutilizzano il token di accesso e lo aggiornano automaticamente quando possibile.

Per forzare l'aggiornamento:

```csharp
client.RefreshAuthAccount();
```

Per revocare la sessione corrente:

```csharp
client.LogoutAuthAccount();
```

## Operazioni dell'entità principale

Le chiamate alle entità ChillSharp utilizzano `ChillDtoEntity`.

Creare:

```csharp
var blog = new ChillDtoEntity
{
    ChillType = "Model.Blog"
};
blog.Properties["Title"] = "My first blog";
blog.Properties["Description"] = "Created through ChillSharp.Client";

var created = client.Create(blog);
```

Trovare:

```csharp
var found = client.Find(new ChillDtoEntity
{
    ChillType = "Model.Blog",
    Guid = created.Guid
});
```

Aggiornamento:

```csharp
created.Properties["Title"] = "Updated blog";
var updated = client.Update(created);
```

Eliminare:

```csharp
client.Delete(updated);
```

Convalida senza salvare:

```csharp
var errors = client.Validate(blog);
```

## Interrogazione e ricerca

Utilizza `Query` quando l'host espone un tipo di query di entità:

```csharp
var query = new ChillDtoQuery
{
    ChillType = "Query.BlogQuery",
    Pagination = new ChillPagination
    {
        Page = 1,
        PageResults = 20
    }
};

query.Properties["FullTextSearch"] = "release notes";

var result = client.Query(query);
foreach (var item in result.Results)
{
    Console.WriteLine(item.Label);
}
```

Utilizza `Lookup` per la ricerca generica di entità di testo completo:

```csharp
var lookup = client.Lookup(new ChillDtoQuery
{
    ChillType = "Model.Blog",
    Properties =
    {
        ["FullTextSearch"] = "release"
    }
});
```

`FullTextSearch` effettua ricerche contro ChillSharp `FullTextContent`. Il testo senza virgolette senza selettori avanzati è normalizzato, suddiviso in spazi bianchi e abbinato a AND, quindi ogni token deve essere presente. Le parentesi più gli operatori `and`/`or` autonomi al di fuori delle virgolette consentono la ricerca booleana raggruppata. Cerca le parole letterali `and` o `or` racchiudendole tra virgolette corrispondenti. Le virgolette singole o doppie corrispondenti cercano una frase normalizzata con i limiti delle parole:

| Cerca testo | Significato |
| --- | --- |
|  | Record di corrispondenza contenenti sia `release` che `notes`. |
|  | Abbina record contenenti sia `release` che `notes` oppure record contenenti `memo`. |
|  | Cerca la parola chiave letterale `and` anziché l'operatore booleano. |
|  | Abbina la frase esatta come parole intere, ad esempio `bla bla la nazione bla bla`, ma non `bla bla della nazione bla bla`. |
| `"*la nazione"` o `"%la nazione"` | Rilassa il confine sinistro, in modo che `della nazione` possa corrispondere. |
| `"la nazione*"` o `"la nazione%"` | Rilassa il confine destro, in modo che un suffisso possa corrispondere. |
| `"la*nazione"` o `"la%nazione"` | Tratta il carattere jolly centrale come un separatore di token e applica la normale corrispondenza dei token AND. |

## Operazioni batch

Utilizzare `Chunk` per inviare diverse operazioni in una chiamata HTTP.

```csharp
var operations = new List<ChillOperation>
{
    new() { Index = 0, Verb = ChillOperationVerb.TRANSACTION },
    new()
    {
        Index = 1,
        Verb = ChillOperationVerb.CREATE,
        Entity = new ChillDtoEntity
        {
            ChillType = "Model.Blog",
            Properties =
            {
                ["Title"] = "Batch blog"
            }
        }
    },
    new() { Index = 2, Verb = ChillOperationVerb.COMMIT }
};

var processed = client.Chunk(operations);
```

Utilizzare un wrapper di transazione/commit quando è necessario eseguire il commit delle operazioni di scrittura insieme.

## Schema e menù

Leggi i metadati dello schema:

```csharp
var schema = client.GetSchema("Model.Blog", "default");
var schemaList = client.GetSchemaList();
```

Gestisci le opzioni dell'entità:

```csharp
var options = client.GetEntityOptions("Model.Blog");
options.HandleAttachments = true;
client.SetEntityOptions(options);
```

Leggi i nodi del menu:

```csharp
var rootItems = client.GetMenu();
var children = client.GetMenu(rootItems[0].Guid);
```

Crea o aggiorna una voce di menu:

```csharp
var item = client.SetMenu(new ChillDtoMenuItem
{
    PositionNo = 10,
    Title = "Blogs",
    ComponentName = "CRUD",
    MenuHierarchy = "CONTENT.BLOGS"
});
```

Elimina una voce di menu e i suoi discendenti:

```csharp
client.DeleteMenu(item.Guid);
```

Le operazioni di scrittura dello schema richiedono l'accesso alla gestione dello schema sugli host protetti.

## Gestione autenticazione

Gli assistenti per la gestione dell'autenticazione sono disponibili quando l'host registra `ChillSharp.Auth`.

```csharp
var users = client.GetAuthUsers();
var roles = client.GetAuthRoles();
var permissions = client.GetAuthPermissions();
```

Crea un utente di autenticazione gestita:

```csharp
var user = client.CreateAuthUser(new CreateAuthUserRequest
{
    ExternalId = "external-user-id",
    UserName = "editor",
    DisplayName = "Editor",
    IsActive = true,
    MenuHierarchy = "CONTENT"
});
```

Crea un ruolo e assegnalo:

```csharp
var role = client.CreateAuthRole(new CreateAuthRoleRequest
{
    Name = "Editors",
    IsActive = true,
    MenuHierarchy = "CONTENT"
});

client.AssignAuthRole(user.Guid, role.Guid);
```

Per schermate di amministrazione più ricche, utilizza gli helper aggregati:

```csharp
var managedUser = client.GetAuthManagedUser(user.Guid);
var roleList = client.GetAuthRoleList();
var moduleList = client.GetAuthModuleList();
```

##I18n

Leggere un testo localizzato:

```csharp
var text = client.GetText(new GetTextRequest
{
    LabelGuid = labelGuid,
    CultureName = "it-IT",
    PrimaryDefaultText = "Hello"
});
```

Leggi diversi testi:

```csharp
var texts = client.GetTexts(requests);
```

Crea o aggiorna un testo:

```csharp
client.SetText(new SetTextRequest
{
    LabelGuid = labelGuid,
    CultureName = "it-IT",
    Value = "Ciao"
});
```

## Allegati

Carica un file e allegalo a un'entità:

```csharp
var files = client.UploadAttachment(
    created,
    @"C:\temp\contract.pdf",
    title: "Contract");
```

Elenca gli allegati per un'entità:

```csharp
var attachments = client.GetAttachments(created);
```

Scarica un allegato:

```csharp
var bytes = client.DownloadAttachment(attachments[0].Guid);
```

Scarica direttamente in un file:

```csharp
client.DownloadAttachmentToFile(
    attachments[0].Guid,
    @"C:\temp\downloaded-contract.pdf");
```

Il caricamento degli allegati e il download privato richiedono il modulo degli allegati e la configurazione di autenticazione appropriata.

## HttpClient personalizzato

Utilizza una factory personalizzata quando i test o l'integrazione dell'host richiedono intestazioni, gestori o certificati speciali:

```csharp
var client = new ChillSharpClient(
    "http://localhost:5000/api/chill",
    () =>
    {
        var httpClient = new HttpClient();
        httpClient.DefaultRequestHeaders.Add("X-Test-User", "integration-user");
        return httpClient;
    });
```

La factory viene richiamata per ogni richiesta. Elimina eventuali risorse esterne in base alla strategia `HttpClient` della tua applicazione.

## Errori

Gli errori del server e gli errori di trasporto sono racchiusi in `ChillClientException`.

```csharp
try
{
    client.Create(blog);
}
catch (ChillClientException ex)
{
    Console.WriteLine(ex.Message);
}
```

Per gli errori HTTP, il messaggio di eccezione include il codice di stato e il corpo della risposta, se disponibile.

## Risoluzione dell'endpoint

Da un URL principale che termina con `/chill`, il client risolve automaticamente gli endpoint del modulo:

| Modulo | Endpoint risolto |
| --- | --- |
| Nucleo |  |
| Aut. |  |
| Schema |  |
| I18n |  |
| Allegato |  |

Per esempio:

```csharp
var client = new ChillSharpClient("http://localhost:5000/api/chill");
client.GetMenu();          // calls /api/chill-schema/get-menu
client.LoginAuthAccount(...); // calls /api/chill-auth/login
```

## Documentazione correlata

- [Modello di autenticazione/README.md](./AuthenticationModel/README.md)
- [MenuGuide/README.md](../MenuGuide/README.md)
- [Modelloallegato/README.md](./AttachmentModel/README.md)
- [ClientGeneration/README.md](./ClientGeneration/README.md)
