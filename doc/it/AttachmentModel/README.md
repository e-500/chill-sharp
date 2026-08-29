# Modulo allegati

Versione originale in inglese: [English](../../AttachmentModel/README.md)


`ChillSharp.Attachment` aggiunge un'entità di allegato incorporata oltre a endpoint di caricamento e download supportati da un archivio di file system.

## Cosa aggiunge

- L'entità `Attachment` è persistita nella tabella `attachment`
- supporto generico Chill CRUD per i metadati degli allegati
- 
- 

Poiché `Attachment` è un vero `ChillEntity`, viene esposto anche tramite il rilevamento dello schema e può essere gestito tramite gli endpoint Chill CRUD standard una volta che l'host `DbContext` implementa `IChillAttachmentDbContext`.

Il modulo espone anche un tipo di query `AttachmentQuery` Chill, che gli helper client possono utilizzare per elencare gli allegati collegati a un'entità di destinazione.

## Registra il modulo

Aggiungi il modello del modulo al tuo contesto ed esponi `DbSet`:

```csharp
using ChillSharp.Attachment;
using ChillSharp.Attachment.Model;

public class AppDbContext : DbContext, IChillContext, IChillAttachmentDbContext
{
    public DbSet<Attachment> Attachments => Set<Attachment>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.AddChillAttachmentModel();
    }

    public string GetChillTypePrefix() => "MyApp.Data";
}
```

Quando il contesto implementa `IChillAttachmentDbContext`, `services.AddChillApi<AppDbContext>()` registra automaticamente gli endpoint dell'allegato.

## Configurazione della radice dell'archivio

Il modulo legge la radice dell'archivio da:

- opzioni di avvio tramite `services.Configure<ChillAttachmentOptions>(...)`
- variabile d'ambiente `CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT`

Esempio:

```csharp
builder.Services.Configure<ChillAttachmentOptions>(options =>
{
    options.ArchiveRoot = "/srv/chill/attachments";
});
```

O attraverso l'ambiente:

```env
CHILLSHARP_ATTACHMENT_ARCHIVE_ROOT=/srv/chill/attachments
```

## Disposizione dell'archivio

I file vengono archiviati nella root dell'archivio configurata utilizzando:

```csharp
public static string BuildAttachmentPath(
    string archiveRoot,
    string attachToChillType,
    Guid id,
    string extension,
    DateTime createdAtUtc)
```

La disposizione risultante è:

```text
{archiveRoot}/{attachToChillType}/{year}/{guid[0..2]}/{guid[2..4]}/{guid}{extension}
```

Esempio:

```text
/srv/chill/attachments/Post/2026/ab/cd/abcd1234....pdf
```

## Carica endpoint



Campi modulo multiparte:

- 
- 
- 
- 
- 
- una o più parti `file`

Per ogni file caricato il modulo:

1. crea un'entità Chill `Attachment` tramite `ChillEngine`
2. memorizza il file fisico nell'archivio
3. restituisce il payload DTO dell'allegato creato

## Scarica l'endpoint



Comportamento:

- carica l'entità `Attachment` dal database
- risolve il percorso del file archiviato
- restituisce il file utilizzando il nome file originale memorizzato e il tipo MIME
- consente il download anonimo quando `Public == true`
- richiede un utente autenticato quando `Public == false`

## Aiutanti del cliente

### `.NET`

`ChillSharp.Client` ora include gli aiutanti per gli allegati:

```csharp
var post = new ChillDtoEntity
{
    Guid = postGuid,
    ChillType = "Model.Post"
};

var uploaded = await client.UploadAttachmentAsync(
    post,
    File.ReadAllBytes("contract.pdf"),
    "contract.pdf",
    "application/pdf",
    title: "Signed contract",
    description: "Customer-facing version",
    isPublic: false);

var attachments = await client.GetAttachmentsAsync(post);
var fileBytes = await client.DownloadAttachmentAsync(uploaded[0]);
```

I sovraccarichi disponibili coprono:

- caricare dal percorso del file
- caricamento da `byte[]`
- caricamento da `Stream`
- scaricabile tramite allegato `Guid`
- scaricabile tramite allegato `ChillDtoEntity`

### TypeScript / Angular / React / Vue / Python

Le librerie client generiche in `extra-libs/` espongono gli helper corrispondenti:

- TypeScript: `uploadAttachment()`, `uploadAttachments()`, `getAttachments()`, `downloadAttachment()`
- Angolare: stessi helper tramite `ChillSharpNgClient` dei wrapper `Observable`
- React e Vue: stessi helper tramite il client raw restituito da `useChillSharpClient()`
- Python: `upload_attachment()`, `upload_attachments()`, `get_attachments()`, `download_attachment()`

##CRUD generico

I metadati degli allegati rimangono disponibili tramite gli endpoint Chill standard:

- 
- 
- 
- 

L'eliminazione di un allegato tramite ChillSharp rimuove anche il file archiviato dal disco.
