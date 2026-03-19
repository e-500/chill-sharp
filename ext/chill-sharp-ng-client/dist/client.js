var __decorate = (this && this.__decorate) || function (decorators, target, key, desc) {
    var c = arguments.length, r = c < 3 ? target : desc === null ? desc = Object.getOwnPropertyDescriptor(target, key) : desc, d;
    if (typeof Reflect === "object" && typeof Reflect.decorate === "function") r = Reflect.decorate(decorators, target, key, desc);
    else for (var i = decorators.length - 1; i >= 0; i--) if (d = decorators[i]) r = (c < 3 ? d(r) : c > 3 ? d(target, key, r) : d(target, key)) || r;
    return c > 3 && r && Object.defineProperty(target, key, r), r;
};
var __param = (this && this.__param) || function (paramIndex, decorator) {
    return function (target, key) { decorator(target, key, paramIndex); }
};
import { Inject, Injectable } from "@angular/core";
import { from } from "rxjs";
import { CHILL_SHARP_CLIENT } from "./tokens.js";
import { CHILL_SHARP_NG_CLIENT_VERSION } from "./version.js";
let ChillSharpNgClient = class ChillSharpNgClient {
    client;
    constructor(client) {
        this.client = client;
    }
    query(dtoQuery) {
        return from(this.client.query(dtoQuery));
    }
    find(dtoEntity) {
        return from(this.client.find(dtoEntity));
    }
    create(dtoEntity) {
        return from(this.client.create(dtoEntity));
    }
    update(dtoEntity) {
        return from(this.client.update(dtoEntity));
    }
    delete(dtoEntity) {
        return from(this.client.delete(dtoEntity));
    }
    chunk(operations) {
        return from(this.client.chunk(operations));
    }
    version() {
        return CHILL_SHARP_NG_CLIENT_VERSION;
    }
    test() {
        return from(this.client.test());
    }
    getSchema(chillType, chillViewCode, cultureName) {
        return from(this.client.getSchema(chillType, chillViewCode, cultureName));
    }
    getSchemaList(cultureName) {
        return from(this.client.getSchemaList(cultureName));
    }
    setSchema(schema) {
        return from(this.client.setSchema(schema));
    }
    getText(request) {
        return from(this.client.getText(request));
    }
    getTexts(requests) {
        return from(this.client.getTexts(requests));
    }
    setText(payload) {
        return from(this.client.setText(payload));
    }
    registerAuthAccount(payload) {
        return from(this.client.registerAuthAccount(payload));
    }
    loginAuthAccount(payload) {
        return from(this.client.loginAuthAccount(payload));
    }
    refreshAuthAccount() {
        return from(this.client.refreshAuthAccount());
    }
    changeAuthPassword(payload) {
        return from(this.client.changeAuthPassword(payload));
    }
    requestAuthPasswordReset(payload) {
        return from(this.client.requestAuthPasswordReset(payload));
    }
    resetAuthPassword(payload) {
        return from(this.client.resetAuthPassword(payload));
    }
    getRawClient() {
        return this.client;
    }
};
ChillSharpNgClient = __decorate([
    Injectable({
        providedIn: "root"
    }),
    __param(0, Inject(CHILL_SHARP_CLIENT))
], ChillSharpNgClient);
export { ChillSharpNgClient };
