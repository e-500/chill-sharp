# ChillSharp UI Core

Versione originale in inglese: [English](../../UiCore/README.md)

`@chill-sharp/ui-core` è l'interfaccia Angular condivisa a workspace per le applicazioni ChillSharp. Fornisce il contenitore del workspace, la navigazione autenticata, form e tabelle guidati dallo schema, task CRUD, schermate dei permessi e il registro dei task di menu. I repository applicativi mantengono fuori da questo pacchetto branding del tenant, route, configurazione runtime ed estensioni di proprietà del client.

Il pacchetto legge i metadati di schema e menu dall'API ChillSharp. Il server controlla i metadati delle entità e le voci di menu persistite; UI Core visualizza l'esperienza standard a partire da tali metadati. Usa JSON di configurazione per personalizzare un singolo task di menu senza copiare il codice UI condiviso.

## Guide

- [Configurazione del menu CRUD](./CRUD.md): configurare una voce menu `CRUD` e le relative opzioni JSON.
- [Guida ai menu](../MenuGuide/README.md): voci di menu persistite e visibilità `MenuHierarchy`.
- [Configurazione delle relazioni](../MenuGuide/Relations.md): relazioni di schema che UI Core può trasformare in task CRUD annidati.
