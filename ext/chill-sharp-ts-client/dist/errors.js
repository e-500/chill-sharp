export class ChillSharpClientError extends Error {
    statusCode;
    responseText;
    constructor(message, statusCode, responseText, cause) {
        super(message);
        this.name = "ChillSharpClientError";
        this.statusCode = statusCode;
        this.responseText = responseText;
        if (cause !== undefined) {
            this.cause = cause;
        }
    }
}
