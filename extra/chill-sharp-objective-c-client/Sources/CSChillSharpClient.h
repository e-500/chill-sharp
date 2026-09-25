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

#import <Foundation/Foundation.h>

NS_ASSUME_NONNULL_BEGIN

FOUNDATION_EXPORT NSErrorDomain const CSChillSharpClientErrorDomain;
FOUNDATION_EXPORT NSString * const CSChillSharpClientStatusCodeKey;
FOUNDATION_EXPORT NSString * const CSChillSharpClientResponseTextKey;

/// Completion for a ChillSharp request. `result` is an NSDictionary, NSArray,
/// NSString, NSNumber, or nil for an empty response.
typedef void (^CSChillSharpCompletion)(id _Nullable result, NSError * _Nullable error);

/// Asynchronous generic client for the ChillSharp HTTP API.
/// Request and response bodies use Foundation JSON objects instead of generated models.
@interface CSChillSharpClient : NSObject

@property (nonatomic, copy, nullable) NSString *accessToken;
@property (nonatomic, copy, nullable) NSString *cultureName;
@property (nonatomic, readonly, copy) NSString *chillBaseURL;
@property (nonatomic, readonly, copy) NSString *notificationHubURL;

- (instancetype)initWithBaseURL:(NSString *)baseURL;
- (instancetype)initWithBaseURL:(NSString *)baseURL
                    accessToken:(nullable NSString *)accessToken
                    cultureName:(nullable NSString *)cultureName;
- (instancetype)initWithBaseURL:(NSString *)baseURL
                    apiBasePath:(nullable NSString *)apiBasePath
                    accessToken:(nullable NSString *)accessToken
                    cultureName:(nullable NSString *)cultureName
                      URLSession:(NSURLSession *)URLSession NS_DESIGNATED_INITIALIZER;
- (instancetype)init NS_UNAVAILABLE;

/// Sends a request to a relative path below the core `/chill` endpoint.
- (void)requestWithMethod:(NSString *)method
                     path:(NSString *)path
                  payload:(nullable id)payload
                anonymous:(BOOL)anonymous
               completion:(CSChillSharpCompletion)completion;

- (void)query:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)lookup:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)find:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)create:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)update:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)deleteEntity:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)autocomplete:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)validate:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)chunk:(NSArray<NSDictionary *> *)operations completion:(CSChillSharpCompletion)completion;
- (void)testWithCompletion:(CSChillSharpCompletion)completion;

- (void)getSchemaForType:(NSString *)chillType
                viewCode:(NSString *)chillViewCode
             cultureName:(nullable NSString *)cultureName
                  update:(BOOL)update
              completion:(CSChillSharpCompletion)completion;
- (void)setSchema:(NSDictionary *)schema completion:(CSChillSharpCompletion)completion;
- (void)getSchemaListForCulture:(nullable NSString *)cultureName completion:(CSChillSharpCompletion)completion;
- (void)getEntityOptionsForType:(NSString *)chillType completion:(CSChillSharpCompletion)completion;
- (void)setEntityOptions:(NSDictionary *)options completion:(CSChillSharpCompletion)completion;

- (void)getText:(NSDictionary *)request completion:(CSChillSharpCompletion)completion;
- (void)getTexts:(NSArray<NSDictionary *> *)requests completion:(CSChillSharpCompletion)completion;
- (void)setText:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;

- (void)registerAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)loginAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)refreshAuthAccount:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)logoutAuthAccountWithCompletion:(CSChillSharpCompletion)completion;
- (void)changeAuthPassword:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)requestAuthPasswordReset:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)resetAuthPassword:(NSDictionary *)payload completion:(CSChillSharpCompletion)completion;
- (void)getAuthPermissionsWithCompletion:(CSChillSharpCompletion)completion;
- (void)getCurrentUserPreferencesWithCompletion:(CSChillSharpCompletion)completion;
- (void)getAuthUserListWithCompletion:(CSChillSharpCompletion)completion;
- (void)getAuthRoleListWithCompletion:(CSChillSharpCompletion)completion;

@end

NS_ASSUME_NONNULL_END
