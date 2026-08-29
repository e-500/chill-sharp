# Guida alla sicurezza e alla conformità

Versione originale in inglese: [English](../../ComplianceGuide/README.md)


Questo documento spiega come ChillSharp può supportare programmi di sicurezza e conformità come NIS2, ISO 27001, SOC 2 o policy interne di sviluppo sicuro.

È intenzionalmente generico: i quadri di conformità differiscono in base alla giurisdizione e al settore, ma molti dei controlli tecnici sottostanti sono gli stessi.

Importante: ChillSharp può aiutarti a implementare e applicare automaticamente diversi controlli tecnici, ma l'utilizzo di ChillSharp non rende di per sé conforme un'applicazione. La conformità dipende ancora dall'ambiente di hosting, dalle procedure operative, dal monitoraggio, dalla risposta agli incidenti, dalla strategia di backup e dall'ambito legale.

## Dove ChillSharp aiuta

ChillSharp è utile quando desideri che il tuo livello API applichi le stesse convenzioni di convalida, autorizzazione, metadati e controllo in modo coerente nell'intero modello invece di reimplementarle controller per controller.

Questa coerenza è importante nel lavoro di conformità perché molti risultati derivano da lacune tra endpoint, controlli dimenticati in un percorso di aggiornamento o comportamento dell'interfaccia utente e dell'API che si allontana nel tempo.

## Aree di controllo supportate da ChillSharp

### 1. Convalida dell'input e integrità dei dati

ChillSharp aiuta a ridurre i dati non validi o non sicuri che entrano nel sistema centralizzando la convalida attorno all'entità e al modello di query.

- Annotazioni dati standard come `[Required]`, `[StringLength]`, `[Range]` e `[EmailAddress]` possono essere applicate ai membri `[ChillProperty]`
- la pipeline di convalida viene eseguita durante i flussi `VALIDATE()` espliciti
- la stessa validazione viene eseguita automaticamente anche quando il client passa direttamente alla creazione o all'aggiornamento
- È possibile aggiungere la convalida aziendale personalizzata tramite `OnValidation()`

Ciò supporta gli obiettivi di controllo generalmente descritti come:

- convalida dell'input
- applicazione della qualità dei dati
- Convalida lato server sicura per impostazione predefinita
- riduzione della convalida incoerente tra gli endpoint

Riferimento:
- [../ValidationModel/README.md](../ValidationModel/README.md)

### 2. Autenticazione e accesso controllato

Con `ChillSharp.Auth`, l'host può esporre flussi di account supportati da identità per:

- registrazione
- login
- gestione del token di aggiornamento
- cambio password
- reimpostazione della password

Ciò aiuta a standardizzare il livello di accesso ed evitare endpoint di autenticazione ad hoc con comportamenti incoerenti.

Riferimento:
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)

### 3. Autorizzazione e privilegio minimo

ChillSharp fornisce un modello di autorizzazione con rifiuto predefinito con:

- autorizzazioni utente
- autorizzazioni di ruolo
- ambiti di modulo, entità e proprietà
- consentire/negare le regole
- regole di precedenza esplicite

Ciò è utile per i programmi di conformità che prevedono l'accesso con privilegi minimi, la separazione dei compiti e una chiara applicazione lato server di chi può visualizzare o modificare i dati.

Poiché le autorizzazioni a livello di proprietà fanno parte del modello, ChillSharp può aiutare a ridurre un rischio comune: gli utenti hanno accesso all'entità giusta ma hanno troppo accesso ai campi sensibili.

Riferimento:
- [../PermissionModel/README.md](../PermissionModel/README.md)

### 4. Campi di controllo sulle modifiche dei dati

`ChillEntity` mantiene automaticamente:

- 
- 
- 
- 

Questi valori vengono aggiornati come parte del percorso di runtime utilizzato da ChillSharp durante gli aggiornamenti, il che aiuta a imporre un audit trail minimo coerente senza dipendere da ogni entità derivata per ricordarsi di farlo manualmente.

Ciò supporta obiettivi di controllo comuni quali:

- tracciabilità delle modifiche
- responsabilità delle azioni dell'utente
- controllo di integrità di base
- evidenza che i documenti sono stati modificati, quando e da chi

Il checksum è particolarmente utile come segnale di integrità leggero per scenari di sincronizzazione, confronti e rilevamento di manomissioni all'interno del modello applicativo.

Riferimento:
- [../README.md](../README.md#audit-fields)

### 5. Metadati dello schema coerenti e generazione di client più sicura

ChillSharp può esporre metadati dello schema e generare client dalla descrizione dell'API.

Ciò non sostituisce di per sé un controllo di sicurezza, ma può ridurre la deriva dell'implementazione tra:

- Convalida backend e moduli frontend
- Autorizzazione backend e funzionalità frontend
- Contratti API effettivi e client scritti a mano

Ridurre i problemi di deriva negli audit perché client incoerenti e colla API duplicata spesso creano eccezioni nascoste al modello di controllo previsto.

Riferimento:
- [../ClientGeneration/README.md](../ClientGeneration/README.md)

## Perché questo è importante per NIS2 e framework simili

Framework come NIS2 di solito non certificano una libreria. Si aspettano che le organizzazioni implementino misure tecniche e organizzative basate sul rischio.

In tale contesto, ChillSharp è meglio inteso come un componente di applicazione del controllo che può aiutare con:

- controllo dell'identità e degli accessi
- privilegio minimo
- tracciabilità degli aggiornamenti
- validazione coerente dei dati in ingresso
- riduzione della sicurezza manuale idraulica

Ciò può ridurre la probabilità di difetti di implementazione comuni e rendere l'applicazione più facile da rivedere durante gli audit interni o le valutazioni esterne.

## Cosa ChillSharp non risolve da solo

È ancora necessario progettare e gestire il sistema di sicurezza più ampio attorno alla biblioteca. In particolare, ChillSharp non fornisce di per sé:

- una strategia SIEM completa o di registrazione di sicurezza centralizzata
- procedure di rilevamento e risposta agli incidenti
- gestione delle vulnerabilità e governance delle patch
- rafforzamento delle infrastrutture
- segmentazione della rete
- configurazione della sicurezza del trasporto
- gestione delle chiavi di crittografia
- gestione dei segreti
- processi di backup e disaster recovery
- Politica del MAE e governance dell'identità aziendale
- gestione del rischio fornitori
- interpretazione giuridica di NIS2 o di qualsiasi altro regolamento

Questi controlli appartengono in parte alla tua applicazione, ma soprattutto alla tua piattaforma e ai processi organizzativi.

## Posizionamento consigliato nella documentazione di revisione

Quando si documenta ChillSharp in una revisione della sicurezza, descriverlo come:

- un framework che centralizza la convalida delle API
- un framework che impone l'autorizzazione basata su ruoli e proprietà
- un framework che mantiene i metadati di controllo di base sugli aggiornamenti dell'entità
- un framework che riduce il codice CRUD personalizzato incoerente

Evita affermazioni più forti come:

- "l'applicazione è compatibile con NIS2 perché utilizza ChillSharp"
- "ChillSharp garantisce la conformità normativa"

L’affermazione più forte e più difendibile è:

"ChillSharp aiuta a implementare e automatizzare diversi controlli tecnici comunemente richiesti dai framework di sicurezza e conformità, mentre la conformità finale dipende dalla progettazione dell'intero sistema e dal modello operativo."

## Lista di controllo pratica

Se desideri utilizzare ChillSharp come parte di un'architettura orientata alla conformità, la linea di base è:

1. utilizzare `ChillEntity` e annotare le proprietà esposte con `[ChillProperty]`
2. aggiungere DataAnnotations e regole `OnValidation()` personalizzate per i vincoli aziendali
3. abilitare `ChillSharp.Auth` per i sistemi autenticati
4. configurare ruoli e regole di autorizzazione con atteggiamento di rifiuto predefinito
5. verifica che `GetCurrentUserName()` sia correttamente implementato nel tuo `IChillContext`
6. preservare e monitorare `LastUpdate`, `LastUpdateUtcOffset`, `LastUpdateUser` e `Checksum`
7. proteggere l'host con HTTPS, registrazione, backup, patch e controlli operativi esterni a ChillSharp

## Documenti correlati

- [../ValidationModel/README.md](../ValidationModel/README.md)
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)
- [../PermissionModel/README.md](../PermissionModel/README.md)
- [../ClientGeneration/README.md](../ClientGeneration/README.md)
- [../RegisterContext.md](../RegisterContext.md)
