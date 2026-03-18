import { type Observable } from "rxjs";
import { ChillSharpClient } from "chill-sharp-ts-client";
import type { GetTextRequest, GetTextResponse, JsonObject } from "chill-sharp-ts-client";
export declare class ChillSharpNgClient {
    private readonly client;
    constructor(client: ChillSharpClient);
    query(dtoQuery: JsonObject): Observable<JsonObject>;
    find(dtoEntity: JsonObject): Observable<JsonObject | null>;
    create(dtoEntity: JsonObject): Observable<JsonObject>;
    update(dtoEntity: JsonObject): Observable<JsonObject>;
    delete(dtoEntity: JsonObject): Observable<void>;
    chunk(operations: JsonObject[]): Observable<JsonObject[]>;
    version(): string;
    test(): Observable<string>;
    getSchema(chillType: string, chillViewCode: string, cultureName?: string): Observable<JsonObject | null>;
    setSchema(schema: JsonObject): Observable<JsonObject | null>;
    getText(request: GetTextRequest): Observable<GetTextResponse | null>;
    getTexts(requests: GetTextRequest[]): Observable<Array<GetTextResponse | null>>;
    setText(payload: JsonObject): Observable<GetTextResponse>;
    registerAuthAccount(payload: JsonObject): Observable<JsonObject>;
    loginAuthAccount(payload: JsonObject): Observable<JsonObject>;
    refreshAuthAccount(): Observable<JsonObject>;
    changeAuthPassword(payload: JsonObject): Observable<JsonObject>;
    requestAuthPasswordReset(payload: JsonObject): Observable<JsonObject>;
    resetAuthPassword(payload: JsonObject): Observable<JsonObject>;
    getRawClient(): ChillSharpClient;
}
