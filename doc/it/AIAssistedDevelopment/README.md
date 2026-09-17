# Guida allo sviluppo assistito dall'intelligenza artificiale

Versione originale in inglese: [English](../../AIAssistedDevelopment/README.md)


Questo documento spiega come ChillSharp può aiutarti quando crei software con l'assistenza dell'intelligenza artificiale e desideri comunque che la base di codice rimanga strutturata, stabile e rivedibile.

L’idea chiave è semplice: gli strumenti di intelligenza artificiale sono molto più affidabili quando funzionano all’interno di un’architettura vincolata, ripetitiva e ben definita rispetto a quando viene chiesto loro di mantenere manualmente sincronizzati molti controller, DTO, endpoint e percorsi di convalida.

ChillSharp non corregge automaticamente il codice generato dall'intelligenza artificiale. Ciò che fa è ridurre la quantità di superficie che l’intelligenza artificiale deve generare e mantenere.

## Perché è importante

Una modalità di errore comune nello sviluppo backend assistito dall'intelligenza artificiale è che il modello tocca troppe parti mobili contemporaneamente:

- controllori
- Mappature DTO
- contratti di richiesta/risposta
- logica di validazione
- controlli autorizzativi
- comportamenti CRUD duplicati

Maggiore è il numero di file ed endpoint personalizzati di cui disponi, più facile sarà per l'intelligenza artificiale introdurre derive accidentali dell'interfaccia, comportamenti incoerenti o refactoring ampi che non erano mai stati previsti.

ChillSharp riduce tale rischio spostando gran parte della superficie backend in un runtime uniforme basato su modello.

## In che modo ChillSharp aiuta

### 1. La logica aziendale cresce all'interno di un ambiente strutturato

Con ChillSharp, i principali punti di estensione sono espliciti e prevedibili:

- 
- 
- 
- Hook del ciclo di vita come `OnCreate()`, `OnUpdate()`, `OnAfterUpdate()`, `OnDelete()` e `OnSelect()`
- metadati tramite `[ChillProperty]` e relative annotazioni

Ciò offre all’intelligenza artificiale uno spazio più ristretto e strutturato per apportare modifiche.

Invece di chiedere a un modello di intelligenza artificiale di inventare ancora un altro controller, richiedere DTO, risposta DTO, mappatore, validatore e contratto di instradamento, spesso puoi chiedergli di:

- aggiungi una proprietà
- aggiungere la convalida
- aggiungi un filtro di query
- aggiungere la logica del ciclo di vita
- modificare le regole di autorizzazione

Questo di solito produce modifiche più piccole e più sicure.

### 2. Riduzione del rischio di refactoring accidentale degli endpoint

ChillSharp espone una superficie API standard tramite `app.MapChillApi()`, con operazioni stabili come:

- 
- 
- 
- 
- 
- 

Poiché la superficie di trasporto è centralizzata, l’aggiunta o l’evoluzione delle entità aziendali non richiede che l’intelligenza artificiale continui a riscrivere un insieme crescente di controller per entità e definizioni di percorso.

Ciò riduce un rischio specifico dell’IA:

- modifica accidentale dei nomi degli endpoint
- modifica delle forme del carico utile in modo incoerente
- implementare un endpoint in modo diverso dagli altri
- interrompere i client attraverso refactoring API non necessari

L'interfaccia continua ad evolversi insieme al modello, ma il CRUD e i meccanismi di query non devono essere riprodotti ogni volta.

### 3. Gli endpoint crescono in modo uniforme

In un backend tradizionale scritto a mano, ogni nuova entità tende a creare codice API più duplicato. Nel tempo si accumulano piccole differenze:

- Un controller convalida in modo diverso
- un altro controller restituisce payload leggermente diversi
- Un altro endpoint dimentica un controllo di autorizzazione
- Un altro mappatore DTO omette un campo

Gli strumenti di intelligenza artificiale amplificano questo problema perché continuano il modello locale che vedono, anche quando il modello locale è già incoerente.

ChillSharp spinge il sistema nella direzione opposta: entità e query si collegano allo stesso modello di runtime, quindi la crescita è più uniforme per impostazione predefinita.

Questa uniformità aiuta entrambi:

- manutentori umani che esaminano i cambiamenti prodotti dall'intelligenza artificiale
- Strumenti di intelligenza artificiale che ragionano sulla base di codice con meno ambiguità

### 4. Carico utile del programma ridotto per gli strumenti di intelligenza artificiale

Quando un progetto si basa su molti controller CRUD personalizzati, classi DTO, livelli di mappatura e definizioni di endpoint ripetitive, l'intelligenza artificiale ha bisogno di più contesto di repository per apportare una modifica sicura.

Ciò aumenta:

- utilizzo dei gettoni
- latenza
- costo
- la possibilità che il modello manchi uno dei livelli duplicati

ChillSharp riduce questo onere poiché gran parte della logica di trasporto ripetitiva è già gestita dal runtime del framework.

In pratica ciò significa che un compito dell’intelligenza artificiale può spesso essere risolto leggendo e modificando:

- un'entità
- una domanda
- una regola di convalida
- una definizione di autorizzazione

invece di una lunga catena di file correlati.

### 5. Abbassare la pressione per il refactoring continuo su larga scala

Senza una struttura basata su modelli, i team spesso chiedono all'IA di continuare a eseguire il refactoring di un elenco crescente di:

- endpoint
- controllori
- DTO
- validatori
- mappatori
- controlli dei permessi

Questo è costoso e fragile. Incoraggia inoltre ampie riscritture automatizzate che potrebbero non fornire valore aziendale.

ChillSharp riduce la necessità di questo stile di manutenzione perché la superficie CRUD/query generica è già centralizzata.

Ciò ha vantaggi pratici:

- minor consumo di token AI
- Meno refactoring ampi su file ripetitivi
- minore sforzo di revisione per il codice generato
- minore utilizzo del calcolo per la stessa funzionalità

Se ti interessano sia l'efficienza ingegneristica che l'efficienza energetica, questo è uno degli argomenti più forti per l'utilizzo di un runtime uniforme invece di una grande quantità di standard endpoint ripetuti.

## In cosa è bravo ChillSharp nei flussi di lavoro AI

ChillSharp è una buona soluzione quando vuoi che l'intelligenza artificiale ti aiuti con:

- estensione delle entità di dominio
- aggiunta di regole di convalida
- aggiunta di funzionalità di query
- esposizione delle modifiche al modello attraverso una superficie API generica esistente
- mantenere autorizzazioni e metadati più vicini al modello

Questo di solito è una soluzione migliore rispetto a chiedere all’IA di generare ripetutamente grandi set di codice dell’infrastruttura CRUD.

## Cosa ChillSharp non risolve

ChillSharp non elimina la necessità di revisione tecnica. In particolare è necessario ancora verificare:

- Le regole aziendali sono corrette
- le regole di autorizzazione sono corrette
- le proprietà esposte sono intenzionali
- I cambiamenti di modello non distruggono i consumatori
- La logica del ciclo di vita generata dall'intelligenza artificiale è effettivamente sicura

ChillSharp riduce la duplicazione e la deriva. Non elimina la necessità del giudizio.

## Posizionamento consigliato

Se desideri un modo breve e difendibile per descriverlo nella documentazione o nelle note sull'architettura, usa qualcosa come:

"ChillSharp aiuta lo sviluppo assistito dall'intelligenza artificiale centralizzando i meccanismi API ripetitivi in ​​un runtime basato su modello. Ciò riduce la deviazione accidentale dell'interfaccia, mantiene il comportamento degli endpoint più uniforme e riduce la quantità di codice e contesto del repository che gli strumenti di intelligenza artificiale devono generare e mantenere."

## Lista di controllo pratica

Se desideri utilizzare ChillSharp come architettura backend compatibile con l'intelligenza artificiale, la linea di base è:

1. mantenere le entità aziendali e le query come il luogo principale in cui viene definito il comportamento delle funzionalità
2. utilizzare `[ChillProperty]` in modo coerente in modo che il DTO e la superficie di convalida rimangano intenzionali
3. preferire `OnValidation()` e gli hook del ciclo di vita rispetto alla logica del controller ad hoc
4. evitare di reintrodurre endpoint CRUD personalizzati ripetitivi a meno che non ce ne sia una reale necessità
5. esaminare attentamente le modifiche al modello poiché una superficie basata su modello può influire su più operazioni client contemporaneamente
6. mantenere le autorizzazioni e l'autenticazione allineate con lo stesso approccio basato sul modello

## Documenti correlati

- [../README.md](../README.md)
- [../RegisterContext.md](../RegisterContext.md)
- [../ValidationModel/README.md](../ValidationModel/README.md)
- [../PermissionModel/README.md](../PermissionModel/README.md)
- [../AuthenticationModel/README.md](../AuthenticationModel/README.md)
- [../ClientGeneration/README.md](../ClientGeneration/README.md)
