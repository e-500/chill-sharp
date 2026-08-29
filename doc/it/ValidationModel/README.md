# Modello di convalida

Versione originale in inglese: [English](../../ValidationModel/README.md)


ChillSharp supporta gli attributi di convalida ASP.NET Core/.NET standard sulle proprietà di entità e query, purché tali proprietà siano contrassegnate anche con `[ChillProperty]`.

## Convalida standard sulle proprietà Chill

Decora le tue proprietà `ChillEntity` o `ChillQuery` sia con `[ChillProperty]` che con i consueti attributi DataAnnotations come `[Required]`, `[StringLength]`, `[Range]`, `[EmailAddress]` e così via.

```csharp
using ChillSharp.Annotations;
using ChillSharp.EF;
using System.ComponentModel.DataAnnotations;

public class Customer : ChillEntity
{
    [Key]
    public override Guid Guid { get; set; }

    [ChillProperty(
        UniquePropertyKeyString: "2F262D7E-F676-4857-B41D-D31C766AA38F",
        PrimaryLanguageLabel: "Name",
        SecondaryLanguageLabel: "Nome")]
    [Required(ErrorMessage = "Customer name is required.")]
    [StringLength(80, ErrorMessage = "Customer name must be 80 characters or less.")]
    public string Name { get; set; } = string.Empty;
}
```

In fase di esecuzione ChillSharp convalida solo le proprietà decorate con `[ChillProperty]`.

Se una proprietà ha attributi DataAnnotations ma non è una proprietà Chill, ChillSharp la ignora nella pipeline di convalida Chill.

## Combinazione di DataAnnotations con `OnValidation()`

È comunque possibile aggiungere una convalida ChillSharp personalizzata sovrascrivendo `OnValidation()`.

```csharp
public override IEnumerable<ChillValidationError> OnValidation(IChillContext context)
{
    if (Name == "admin")
    {
        return
        [
            new ChillValidationError
            {
                FieldName = nameof(Name),
                Message = "The name 'admin' is reserved."
            }
        ];
    }

    return [];
}
```

Il comportamento in fase di esecuzione è:

1. ChillSharp esegue la convalida DataAnnotations standard sui membri `[ChillProperty]`.
2. ChillSharp esegue quindi la logica `OnValidation()` personalizzata.
3. Gli errori restituiti vengono esposti come `ChillValidationError` utilizzando le DataAnnotations leggibili dall'uomo `ErrorMessage`.

## Aggiorna il comportamento

La stessa pipeline di convalida viene eseguita automaticamente anche all'inizio dell'hook runtime post-aggiornamento interno utilizzato da ChillSharp.

Ciò significa:

-Se il client chiama la convalida in modo esplicito, riceve voci `ChillValidationError` per errori di convalida personalizzati e basati su annotazioni.
- Se il client salta la convalida esplicita e passa direttamente alla creazione o all'aggiornamento, ChillSharp convalida comunque l'entità durante il ciclo di vita dell'aggiornamento e genera un `ChillValidationException` con gli stessi messaggi leggibili dall'uomo.

## Note

- Utilizza DataAnnotations standard per semplici regole di campo.
- Utilizzare `OnValidation()` per regole che dipendono da più campi, logica aziendale o ricerche nel database.
- Per rendere una proprietà parte della convalida ChillSharp, aggiungere sempre `[ChillProperty]`.

## Appendice: messaggi di convalida basati su GUID

Come hack di flessibilità opzionale, un DataAnnotations `ErrorMessage` può contenere una stringa GUID invece del testo dell'utente finale.

Quindi puoi fornire i testi primari e secondari effettivi sovrascrivendo `GetValidationMessageDefinitions()`.

```csharp
public class Customer : ChillEntity
{
    [ChillProperty(
        UniquePropertyKeyString: "B2AB35A8-6A89-4D39-8F1D-183F686811A9",
        PrimaryLanguageLabel: "Code",
        SecondaryLanguageLabel: "Codice")]
    [Required(ErrorMessage = "4F880CC1-5C7A-4E23-982A-5F0C490B44DE")]
    public string Code { get; set; } = string.Empty;

    public override IEnumerable<ChillValidationMessageDefinition> GetValidationMessageDefinitions(IChillContext context)
    {
        return
        [
            new ChillValidationMessageDefinition
            {
                MessageGuid = Guid.Parse("4F880CC1-5C7A-4E23-982A-5F0C490B44DE"),
                PrimaryLanguageMessage = "Code is required.",
                SecondaryLanguageMessage = "Il codice e obbligatorio."
            }
        ];
    }
}
```

Quando ChillSharp vede il GUID in `ErrorMessage`, risolve il messaggio finale attraverso la stessa convenzione di lingua primaria/secondaria utilizzata altrove nei metadati di ChillSharp.

Note:

- Questo è facoltativo. I semplici valori `ErrorMessage` leggibili dall'uomo funzionano ancora normalmente.
- Utilizzalo solo quando desideri un identificatore stabile per un testo di convalida.
- Se il GUID non viene trovato in `GetValidationMessageDefinitions()`, ChillSharp torna alla stringa grezza `ErrorMessage`.
