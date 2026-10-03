/*
 * ChillSharp is a lightweight .NET library that sits on top of Entity Framework Core 
 * and turns an existing data model into a fully working REST API with almost no setup.
 * Copyright (C) 2025 Andrea Piovesan
 *
 * This program is free software: you can redistribute it and/or modify
 * it under the terms of the GNU Affero General Public License as published by
 * the Free Software Foundation, either version 3 of the License, or
 * (at your option) any later version.
 * 
 * This program is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
 * GNU Affero General Public License for more details.
 * 
 * You should have received a copy of the GNU Affero General Public License
 * along with this program.  If not, see <https://www.gnu.org/licenses/>.
 */

#import "CSChillSharpClient.h"

NSErrorDomain const CSChillSharpClientErrorDomain = @"CSChillSharpClientErrorDomain";
NSString * const CSChillSharpClientStatusCodeKey = @"statusCode";
NSString * const CSChillSharpClientResponseTextKey = @"responseText";

@interface CSChillSharpClient ()
@property (nonatomic, copy, readwrite) NSString *chillBaseURL;
@property (nonatomic, copy, readwrite) NSString *notificationHubURL;
@property (nonatomic, copy) NSString *apiBaseURL;
@property (nonatomic, strong) NSURLSession *URLSession;
@end

@implementation CSChillSharpClient

- (instancetype)initWithBaseURL:(NSString *)baseURL {
    return [self initWithBaseURL:baseURL accessToken:nil cultureName:nil];
}

- (instancetype)initWithBaseURL:(NSString *)baseURL
                    accessToken:(NSString *)accessToken
                    cultureName:(NSString *)cultureName {
    NSURLSessionConfiguration *configuration = NSURLSessionConfiguration.defaultSessionConfiguration;
    configuration.timeoutIntervalForRequest = 30.0;
    return [self initWithBaseURL:baseURL apiBasePath:@"api" accessToken:accessToken
                     cultureName:cultureName URLSession:[NSURLSession sessionWithConfiguration:configuration]];
}

- (instancetype)initWithBaseURL:(NSString *)baseURL
                    apiBasePath:(NSString *)apiBasePath
                    accessToken:(NSString *)accessToken
                    cultureName:(NSString *)cultureName
                      URLSession:(NSURLSession *)URLSession {
    self = [super init];
    if (self) {
        NSString *normalized = [[self normalizedString:baseURL] stringByTrimmingCharactersInSet:[NSCharacterSet characterSetWithCharactersInString:@"/"]];
        if (normalized.length == 0) {
            @throw [NSException exceptionWithName:NSInvalidArgumentException reason:@"baseURL is required." userInfo:nil];
        }
        NSString *lower = normalized.lowercaseString;
        NSArray<NSString *> *knownEndpoints = @[@"/chill", @"/chill-auth", @"/chill-schema", @"/chill-i18n", @"/chill-attachment"];
        BOOL isEndpoint = NO;
        for (NSString *suffix in knownEndpoints) {
            if ([lower hasSuffix:suffix]) { isEndpoint = YES; break; }
        }
        if (isEndpoint) {
            self.chillBaseURL = normalized;
        } else {
            NSString *apiPath = [self normalizedPath:apiBasePath ?: @"api"];
            if (apiPath.length == 0) self.chillBaseURL = [normalized stringByAppendingString:@"/chill"];
            else if ([lower hasSuffix:[@"/" stringByAppendingString:apiPath.lowercaseString]])
                self.chillBaseURL = [normalized stringByAppendingString:@"/chill"];
            else self.chillBaseURL = [NSString stringWithFormat:@"%@/%@/chill", normalized, apiPath];
        }
        NSString *chillLower = self.chillBaseURL.lowercaseString;
        self.apiBaseURL = [chillLower hasSuffix:@"/chill"]
            ? [self.chillBaseURL substringToIndex:self.chillBaseURL.length - @"/chill".length]
            : self.chillBaseURL;
        self.notificationHubURL = [self.apiBaseURL stringByAppendingString:@"/notify"];
        self.accessToken = [self normalizedString:accessToken];
        self.cultureName = [self normalizedString:cultureName];
        self.URLSession = URLSession;
    }
    return self;
}

- (void)requestWithMethod:(NSString *)method path:(NSString *)path payload:(id)payload
                anonymous:(BOOL)anonymous completion:(CSChillSharpCompletion)completion {
    NSString *urlString = [self.chillBaseURL stringByAppendingFormat:@"/%@", [path stringByTrimmingCharactersInSet:[NSCharacterSet characterSetWithCharactersInString:@"/"]]];
    [self sendMethod:method URLString:urlString payload:payload anonymous:anonymous completion:completion];
}

- (void)query:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"query" payload:payload anonymous:NO completion:completion]; }
- (void)lookup:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"lookup" payload:payload anonymous:NO completion:completion]; }
- (void)find:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"find" payload:payload anonymous:NO completion:completion]; }
- (void)create:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"create" payload:payload anonymous:NO completion:completion]; }
- (void)update:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"update" payload:payload anonymous:NO completion:completion]; }
- (void)deleteEntity:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"delete" payload:payload anonymous:NO completion:completion]; }
- (void)autocomplete:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"autocomplete" payload:payload anonymous:NO completion:completion]; }
- (void)validate:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"validate" payload:payload anonymous:NO completion:completion]; }
- (void)chunk:(NSArray<NSDictionary *> *)operations completion:(CSChillSharpCompletion)completion { [self requestWithMethod:@"POST" path:@"chunk" payload:operations anonymous:NO completion:completion]; }

- (void)testWithCompletion:(CSChillSharpCompletion)completion {
    [self sendMethod:@"GET" URLString:[self.apiBaseURL stringByAppendingString:@"/test"] payload:nil anonymous:YES completion:completion];
}

- (void)getSchemaForType:(NSString *)chillType viewCode:(NSString *)chillViewCode cultureName:(NSString *)cultureName update:(BOOL)update completion:(CSChillSharpCompletion)completion {
    NSURLComponents *components = [NSURLComponents componentsWithString:[[self schemaURL] stringByAppendingString:@"/get-schema"]];
    NSMutableArray<NSURLQueryItem *> *items = [NSMutableArray arrayWithArray:@[
        [NSURLQueryItem queryItemWithName:@"chillType" value:chillType ?: @""],
        [NSURLQueryItem queryItemWithName:@"chillViewCode" value:chillViewCode ?: @""]
    ]];
    NSString *culture = [self normalizedString:cultureName] ?: self.cultureName;
    if (culture.length) [items addObject:[NSURLQueryItem queryItemWithName:@"cultureName" value:culture]];
    if (update) [items addObject:[NSURLQueryItem queryItemWithName:@"update" value:@"true"]];
    components.queryItems = items;
    [self sendMethod:@"GET" URLString:components.URL.absoluteString payload:nil anonymous:YES completion:completion];
}
- (void)setSchema:(NSDictionary *)schema completion:(CSChillSharpCompletion)completion { [self sendMethod:@"POST" URLString:[[self schemaURL] stringByAppendingString:@"/set-schema"] payload:schema anonymous:NO completion:completion]; }
- (void)getSchemaListForCulture:(NSString *)cultureName completion:(CSChillSharpCompletion)completion {
    NSURLComponents *components = [NSURLComponents componentsWithString:[[self schemaURL] stringByAppendingString:@"/get-schema-list"]];
    NSString *culture = [self normalizedString:cultureName] ?: self.cultureName;
    if (culture.length) components.queryItems = @[[NSURLQueryItem queryItemWithName:@"cultureName" value:culture]];
    [self sendMethod:@"GET" URLString:components.URL.absoluteString payload:nil anonymous:YES completion:completion];
}
- (void)getEntityOptionsForType:(NSString *)chillType completion:(CSChillSharpCompletion)completion {
    NSURLComponents *components = [NSURLComponents componentsWithString:[[self schemaURL] stringByAppendingString:@"/get-entity-options"]];
    components.queryItems = @[[NSURLQueryItem queryItemWithName:@"chillType" value:chillType ?: @""]];
    [self sendMethod:@"GET" URLString:components.URL.absoluteString payload:nil anonymous:NO completion:completion];
}
- (void)setEntityOptions:(NSDictionary *)options completion:(CSChillSharpCompletion)completion { [self sendMethod:@"POST" URLString:[[self schemaURL] stringByAppendingString:@"/set-entity-options"] payload:options anonymous:NO completion:completion]; }

- (void)getText:(NSDictionary *)request completion:(CSChillSharpCompletion)completion { [self sendMethod:@"POST" URLString:[[self i18nURL] stringByAppendingString:@"/get-text"] payload:request anonymous:YES completion:completion]; }
- (void)getTexts:(NSArray<NSDictionary *> *)requests completion:(CSChillSharpCompletion)completion { [self sendMethod:@"POST" URLString:[[self i18nURL] stringByAppendingString:@"/get-multiple-text"] payload:requests anonymous:YES completion:completion]; }
- (void)setText:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self sendMethod:@"PUT" URLString:[[self i18nURL] stringByAppendingString:@"/set-text"] payload:payload anonymous:NO completion:completion]; }

- (void)registerAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"register" payload:payload anonymous:YES completion:completion]; }
- (void)loginAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"login" payload:payload anonymous:YES completion:completion]; }
- (void)refreshAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"refresh" payload:payload anonymous:YES completion:completion]; }
- (void)logoutAuthAccountWithCompletion:(CSChillSharpCompletion)completion {
    [self authMethod:@"POST" path:@"logout" payload:nil anonymous:NO completion:^(id result, NSError *error) {
        if (!error) self.accessToken = nil;
        if (completion) completion(result, error);
    }];
}
- (void)changeAuthPassword:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"change-password" payload:payload anonymous:NO completion:completion]; }
- (void)requestAuthPasswordReset:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"request-password-reset" payload:payload anonymous:YES completion:completion]; }
- (void)resetAuthPassword:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion { [self authMethod:@"POST" path:@"reset-password" payload:payload anonymous:YES completion:completion]; }
- (void)getAuthPermissionsWithCompletion:(CSChillSharpCompletion)completion { [self authMethod:@"GET" path:@"get-permissions" payload:nil anonymous:NO completion:completion]; }
- (void)getCurrentUserPreferencesWithCompletion:(CSChillSharpCompletion)completion { [self authMethod:@"GET" path:@"current-user-preferences" payload:nil anonymous:NO completion:completion]; }
- (void)getAuthUserListWithCompletion:(CSChillSharpCompletion)completion { [self authMethod:@"GET" path:@"get-user-list" payload:nil anonymous:NO completion:completion]; }
- (void)getAuthRoleListWithCompletion:(CSChillSharpCompletion)completion { [self authMethod:@"GET" path:@"get-role-list" payload:nil anonymous:NO completion:completion]; }

- (void)authMethod:(NSString *)method path:(NSString *)path payload:(id)payload anonymous:(BOOL)anonymous completion:(CSChillSharpCompletion)completion {
    NSString *url = [[self authURL] stringByAppendingFormat:@"/%@", path];
    [self sendMethod:method URLString:url payload:payload anonymous:anonymous completion:^(id result, NSError *error) {
        if (!error && [@[@"login", @"register", @"refresh"] containsObject:path] && [result isKindOfClass:NSDictionary.class]) {
            NSString *token = result[@"accessToken"] ?: result[@"AccessToken"];
            if ([token isKindOfClass:NSString.class]) self.accessToken = token;
        }
        if (completion) completion(result, error);
    }];
}

- (void)sendMethod:(NSString *)method URLString:(NSString *)URLString payload:(id)payload anonymous:(BOOL)anonymous completion:(CSChillSharpCompletion)completion {
    NSURL *URL = [NSURL URLWithString:URLString];
    if (!URL) {
        if (completion) completion(nil, [self errorWithDescription:@"Invalid request URL." status:0 responseText:nil]);
        return;
    }
    NSMutableURLRequest *request = [NSMutableURLRequest requestWithURL:URL];
    request.HTTPMethod = method;
    request.timeoutInterval = 30.0;
    [request setValue:@"application/json" forHTTPHeaderField:@"Accept"];
    if (!anonymous && self.accessToken.length) [request setValue:[@"Bearer " stringByAppendingString:self.accessToken] forHTTPHeaderField:@"Authorization"];
    if (payload) {
        NSError *serializationError = nil;
        NSData *body = [NSJSONSerialization dataWithJSONObject:payload options:0 error:&serializationError];
        if (!body) {
            if (completion) completion(nil, serializationError ?: [self errorWithDescription:@"Could not encode JSON request." status:0 responseText:nil]);
            return;
        }
        request.HTTPBody = body;
        [request setValue:@"application/json; charset=utf-8" forHTTPHeaderField:@"Content-Type"];
    }
    NSURLSessionDataTask *task = [self.URLSession dataTaskWithRequest:request completionHandler:^(NSData *data, NSURLResponse *response, NSError *networkError) {
        if (networkError) { if (completion) completion(nil, networkError); return; }
        NSString *responseText = data.length ? [[NSString alloc] initWithData:data encoding:NSUTF8StringEncoding] : @"";
        NSInteger status = [response isKindOfClass:NSHTTPURLResponse.class] ? ((NSHTTPURLResponse *)response).statusCode : 0;
        if (status < 200 || status >= 300) {
            if (completion) completion(nil, [self errorWithDescription:[NSString stringWithFormat:@"HTTP %ld calling %@ %@", (long)status, method, URLString] status:status responseText:responseText]);
            return;
        }
        if (data.length == 0) { if (completion) completion(nil, nil); return; }
        NSError *parseError = nil;
        id result = [NSJSONSerialization JSONObjectWithData:data options:NSJSONReadingFragmentsAllowed error:&parseError];
        if (!result && parseError) result = responseText ?: @"";
        if (completion) completion(result, nil);
    }];
    [task resume];
}

- (NSError *)errorWithDescription:(NSString *)description status:(NSInteger)status responseText:(NSString *)responseText {
    NSMutableDictionary *info = [NSMutableDictionary dictionaryWithObject:description forKey:NSLocalizedDescriptionKey];
    if (status) info[CSChillSharpClientStatusCodeKey] = @(status);
    if (responseText) info[CSChillSharpClientResponseTextKey] = responseText;
    return [NSError errorWithDomain:CSChillSharpClientErrorDomain code:status userInfo:info];
}
- (NSString *)schemaURL { return [self siblingURLForEndpoint:@"chill-schema"]; }
- (NSString *)authURL { return [self siblingURLForEndpoint:@"chill-auth"]; }
- (NSString *)i18nURL { return [self siblingURLForEndpoint:@"chill-i18n"]; }
- (NSString *)siblingURLForEndpoint:(NSString *)endpoint {
    if ([self.chillBaseURL.lowercaseString hasSuffix:@"/chill"])
        return [[self.chillBaseURL substringToIndex:self.chillBaseURL.length - @"/chill".length] stringByAppendingFormat:@"/%@", endpoint];
    NSString *suffix = [endpoint substringFromIndex:@"chill-".length];
    return [self.chillBaseURL stringByAppendingFormat:@"-%@", suffix];
}
- (NSString *)normalizedString:(NSString *)value { return [value isKindOfClass:NSString.class] && value.length ? [value stringByTrimmingCharactersInSet:NSCharacterSet.whitespaceAndNewlineCharacterSet] : nil; }
- (NSString *)normalizedPath:(NSString *)value { return [[self normalizedString:value] stringByTrimmingCharactersInSet:[NSCharacterSet characterSetWithCharactersInString:@"/"]] ?: @""; }

@end
