# Marketing TODO
- Update website chillsharp.dev e pubblicazione video introduttivo
- Creazione di podcast per specifica funzionalità (e pubblicazione su spotify?)

## Site
- 

## Aspects
- Cost for AI rereprocess all endpoint to add afunctionlity or horizontal new featrue introduction (the same cost in the human way) 
- Hardening the security
- Time to market
- ACL ad NIS2

# Features TODO

## Core
- Test Claim CanManagePermission and CanManageSchema !!!!! different?""?

+ ENV variabile configuration (Priority: ENV_VARS, appsettings.json)
- Enable/Disable checksum on web entity based con default schema settings (header options stored in db to be available server-side)
- Store on default schema also the string to compose the Lookup string
- Store on default schema also the list of the fields to be used as full-text-search
- Store on default schema the number of the entity versions es. 10 store (somewhere to decide) the old copies of the entity in a log

## Query capabilities
- Include advanced full text search based on FullTextContent

## Auth
+ Create and test reset password email capability
+ Create ENV variables for reset password.

## Schema

- Creation of ChillEntityConfiguration (and ChillQueryConfiguration ?!) to store standard behaviour options for example:
  - What fields and in wich order put in FullTextContent
  - Label and ShortLabel calculation string format
  - Enable/Disable advanced logging capabilities for example field changes
- 

