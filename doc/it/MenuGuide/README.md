# Guida ai menu di ChillSharp

Versione originale in inglese: [English](../../MenuGuide/README.md)

ChillSharp può memorizzare un albero dei menu dell'applicazione nel modulo schema invece di codificarlo nel frontend. La gestione dei menu è esposta sotto `/api/chill-schema`.

## Modello di voce di menu

Ogni voce dispone di `Guid` stabile, `PositionNo`, `Title`, `Description` facoltativa, `Parent` facoltativo, `ComponentName`, `ComponentConfigurationJson` facoltativo e `MenuHierarchy` facoltativo.

- `Parent = null` identifica una voce radice.
- Le voci figlie fanno riferimento al proprio genitore diretto.
- Le voci sullo stesso livello sono ordinate per `PositionNo`, poi `Title`, poi `Guid`.
- `ComponentName` identifica il componente client da aprire, ad esempio `CRUD`.

```json
{
  "Guid": "00000000-0000-0000-0000-000000000000",
  "PositionNo": 10,
  "Title": "Posts",
  "Description": "Open the post management screen",
  "Parent": null,
  "ComponentName": "CRUD",
  "ComponentConfigurationJson": "{\"ChillType\":\"Model.Post\"}",
  "MenuHierarchy": "CONTENT.POSTS"
}
```

## Endpoint

- `GET /api/chill-schema/get-menu` restituisce le voci radice.
- `GET /api/chill-schema/get-menu?parentGuid={guid}` restituisce i figli diretti di una voce.
- `POST /api/chill-schema/set-menu` crea una voce quando `Guid` è vuoto oppure aggiorna la voce corrispondente. Un genitore fornito deve già esistere e una voce non può essere il proprio genitore. L'aggiornamento di una voce esistente con `MenuHierarchy` vuoto conserva il valore memorizzato.
- `DELETE /api/chill-schema/delete-menu?menuItemGuid={guid}` elimina la voce selezionata e l'intero sottoalbero dei suoi discendenti.

Carica l'albero un livello alla volta: prima richiedi le radici, poi i figli quando un ramo viene espanso.

## Visibilità con `MenuHierarchy`

`MenuHierarchy` accetta un codice oppure codici separati da virgola. ChillSharp unisce i valori separati da virgola dell'utente corrente e di tutti i ruoli attivi in un insieme effettivo di prefissi.

- `*` concede l'accesso a ogni voce di menu.
- Senza alcun prefisso effettivo non viene restituita alcuna voce, comprese quelle con gerarchia vuota.
- Con almeno un prefisso effettivo sono visibili le voci con gerarchia vuota.
- Una voce con gerarchia valorizzata è visibile quando inizia con almeno un prefisso effettivo.

Per esempio, `CONTENT` concede `CONTENT`, `CONTENT.POSTS` e `CONTENT.REPORTS.MONTHLY`, ma non `ADMIN`. Una voce può esporre più rami con `CONTENT, REPORTS.MONTHLY`.

Usa codici stabili separati da punti, quali `ADMIN.USERS`, `CONTENT.POSTS` e `REPORTS.MONTHLY`.
