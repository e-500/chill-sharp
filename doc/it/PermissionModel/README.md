# Modello Dei Permessi

Versione originale in inglese: [English](../../PermissionModel/README.md)

Questo documento descrive il modello di autorizzazione implementato da `ChillSharp.Auth`.

## Scopo

Il modello di permessi e progettato per rispondere in modo coerente a due domande:

- l'utente corrente puo eseguire un'operazione a livello entita?
- l'utente corrente puo vedere o modificare una specifica proprieta?

Lo stesso modello supporta sia:

- enforcement lato server
- filtraggio delle capability lato client

## Soggetti

I permessi possono essere assegnati a:

- un utente
- un ruolo

Un utente puo appartenere a piu ruoli.

## Gerarchia Della Risorsa

I permessi vengono valutati su una gerarchia a tre livelli:

```text
Module -> Entity -> Property
```

### Module

Un modulo e un'area logica dell'applicazione, per esempio:

- `Accounting`
- `Accounting.General`
- `Blog`
- `Blog.Admin`

I nomi modulo possono essere gerarchici.

### Entity

Un'entita e un nome Chill entity dentro un modulo, per esempio:

- `Blog`
- `Post`
- `AuthUser`

### Property

Una proprieta e un campo di un'entita, per esempio:

- `Title`
- `Author`
- `CanManagePermissions`

## Azioni

### Azioni entita

- `Query`
- `Create`
- `Update`
- `Delete`

### Azioni proprieta

- `See`
- `Modify`

I permessi proprieta raffinano un'operazione entita gia consentita. Non sostituiscono i permessi entita.

## Effetti

Ogni regola ha un effetto:

- `Allow`
- `Deny`

Allo stesso livello di valutazione, `Deny` vince su `Allow`.

## Precedenza

ChillSharp risolve le regole in questo ordine:

1. regole proprieta utente
2. regole entita utente
3. regole modulo utente
4. regole proprieta ruolo
5. regole entita ruolo
6. regole modulo ruolo
7. deny di default

Questo combina due principi:

- le regole utente sovrascrivono quelle dei ruoli
- le regole piu specifiche sovrascrivono quelle piu ampie

## Come Vengono Valutate Le Operazioni

### Query

Per interrogare un'entita:

1. l'utente deve avere `Query` a livello entita
2. ogni proprieta restituita deve avere anche `See`

Se una proprieta non e consentita, il server puo rimuoverla, impostarla a null o mascherarla a seconda della superficie chiamante e dell'implementazione.

### Create

Per creare un'entita:

1. l'utente deve avere `Create` a livello entita
2. ogni proprieta fornita deve avere `Modify`

### Update

Per aggiornare un'entita:

1. l'utente deve avere `Update` a livello entita
2. ogni proprieta modificata deve avere `Modify`

### Delete

Per eliminare un'entita:

1. l'utente deve avere `Delete` a livello entita

I permessi proprieta non contano nel delete.

## Postura Di Sicurezza Predefinita

Il modello e default-deny.

Se nessuna regola concede accesso, l'accesso viene negato.

Questo e intenzionale. Evita che nuove entita o proprieta diventino visibili solo perche sono state aggiunte al modello.

## Esempi Tipici Di Regole

Consentire la query di tutte le entita blog in un modulo:

```text
Allow Query Module=Blog
```

Consentire l'update dei post:

```text
Allow Update Module=Blog Entity=Post
```

Bloccare l'editing di una proprieta sensibile pur consentendo update piu ampi:

```text
Allow Update Module=Blog Entity=Post
Deny Modify Module=Blog Entity=Post Property=InternalNotes
```

## Perche Questo Conta Per I Client

I client possono usare gli endpoint di valutazione permessi per:

- disabilitare azioni UI
- nascondere campi
- decidere quali editor di proprieta renderizzare

Ma il server resta la fonte di verita. Il filtraggio lato client e comodita, non sicurezza.

## Componenti Runtime Correlati

Il modello di permessi e supportato da:

- `AuthUser`
- `AuthRole`
- `AuthUserRole`
- `AuthPermissionRule`

Gli endpoint di gestione sono esposti tramite `ChillSharp.Auth`.

Per registrazione e flussi account, vedi:

- [AuthenticationModel/README.md](../AuthenticationModel/README.md)
