import { Inject, Injectable } from "@angular/core";
import { from, Observable } from "rxjs";
import { ChillSharpClient } from "chill-sharp-ts-client";
import type {
  AuthRoleDetailsResponse,
  AuthRoleListItem,
  AuthUserDetailsResponse,
  AuthUserListItem,
  ChillDtoSchema,
  ChillDtoSchemaListItem,
  ChillEntityChangeNotification,
  ChillEntityChangeSubscription,
  GetAuthPermissionsResponse,
  GetTextRequest,
  GetTextResponse,
  JsonObject,
  SetAuthRoleRequest,
  SetAuthUserRequest
} from "chill-sharp-ts-client";
import { CHILL_SHARP_CLIENT } from "./tokens.js";
import { CHILL_SHARP_NG_CLIENT_VERSION } from "./version.js";

@Injectable({
  providedIn: "root"
})
export class ChillSharpNgClient {
  constructor(@Inject(CHILL_SHARP_CLIENT) private readonly client: ChillSharpClient) {}

  query(dtoQuery: JsonObject): Observable<JsonObject> {
    return from(this.client.query(dtoQuery));
  }

  find(dtoEntity: JsonObject): Observable<JsonObject | null> {
    return from(this.client.find(dtoEntity));
  }

  create(dtoEntity: JsonObject): Observable<JsonObject> {
    return from(this.client.create(dtoEntity));
  }

  update(dtoEntity: JsonObject): Observable<JsonObject> {
    return from(this.client.update(dtoEntity));
  }

  delete(dtoEntity: JsonObject): Observable<void> {
    return from(this.client.delete(dtoEntity));
  }

  chunk(operations: JsonObject[]): Observable<JsonObject[]> {
    return from(this.client.chunk(operations));
  }

  version(): string {
    return CHILL_SHARP_NG_CLIENT_VERSION;
  }

  test(): Observable<string> {
    return from(this.client.test());
  }

  getSchema(chillType: string, chillViewCode: string, cultureName?: string): Observable<ChillDtoSchema | null> {
    return from(this.client.getSchema(chillType, chillViewCode, cultureName));
  }

  getSchemaList(cultureName?: string): Observable<ChillDtoSchemaListItem[]> {
    return from(this.client.getSchemaList(cultureName));
  }

  setSchema(schema: ChillDtoSchema): Observable<ChillDtoSchema | null> {
    return from(this.client.setSchema(schema));
  }

  getText(request: GetTextRequest): Observable<GetTextResponse | null> {
    return from(this.client.getText(request));
  }

  getTexts(requests: GetTextRequest[]): Observable<Array<GetTextResponse | null>> {
    return from(this.client.getTexts(requests));
  }

  setText(payload: JsonObject): Observable<GetTextResponse> {
    return from(this.client.setText(payload));
  }

  watchEntityChanges(chillType: string, guid?: string | null): Observable<ChillEntityChangeNotification[]> {
    return new Observable<ChillEntityChangeNotification[]>((subscriber) => {
      let remoteSubscription: ChillEntityChangeSubscription | null = null;
      let isClosed = false;

      void this.client
        .subscribeToEntityChanges(
          chillType,
          async (changes) => {
            subscriber.next(changes);
          },
          guid
        )
        .then(async (subscription) => {
          remoteSubscription = subscription;
          if (isClosed) {
            await subscription.unsubscribe();
          }
        })
        .catch((error) => {
          subscriber.error(error);
        });

      return () => {
        isClosed = true;
        if (remoteSubscription) {
          void remoteSubscription.unsubscribe();
        }
      };
    });
  }

  disconnectEntityChanges(): Observable<void> {
    return from(this.client.disconnectEntityChanges());
  }

  registerAuthAccount(payload: JsonObject): Observable<JsonObject> {
    return from(this.client.registerAuthAccount(payload));
  }

  loginAuthAccount(payload: JsonObject): Observable<JsonObject> {
    return from(this.client.loginAuthAccount(payload));
  }

  refreshAuthAccount(): Observable<JsonObject> {
    return from(this.client.refreshAuthAccount());
  }

  changeAuthPassword(payload: JsonObject): Observable<JsonObject> {
    return from(this.client.changeAuthPassword(payload));
  }

  requestAuthPasswordReset(payload: JsonObject): Observable<JsonObject> {
    return from(this.client.requestAuthPasswordReset(payload));
  }

  resetAuthPassword(payload: JsonObject): Observable<JsonObject> {
    return from(this.client.resetAuthPassword(payload));
  }

  getAuthPermissions(): Observable<GetAuthPermissionsResponse> {
    return from(this.client.getAuthPermissions());
  }

  getAuthUserList(): Observable<AuthUserListItem[]> {
    return from(this.client.getAuthUserList());
  }

  getAuthUser(userGuid: string): Observable<AuthUserDetailsResponse> {
    return from(this.client.getAuthUser(userGuid));
  }

  setAuthUser(payload: SetAuthUserRequest): Observable<AuthUserDetailsResponse> {
    return from(this.client.setAuthUser(payload));
  }

  getAuthRoleList(): Observable<AuthRoleListItem[]> {
    return from(this.client.getAuthRoleList());
  }

  getAuthRole(roleGuid: string): Observable<AuthRoleDetailsResponse> {
    return from(this.client.getAuthRole(roleGuid));
  }

  setAuthRole(payload: SetAuthRoleRequest): Observable<AuthRoleDetailsResponse> {
    return from(this.client.setAuthRole(payload));
  }

  getRawClient(): ChillSharpClient {
    return this.client;
  }
}
