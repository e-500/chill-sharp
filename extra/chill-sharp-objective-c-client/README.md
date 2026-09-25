# chill-sharp-objective-c-client

Foundation-based, asynchronous Objective-C client for generic ChillSharp services. It uses dictionaries and arrays for request and response JSON so it works with application-specific models without code generation.

The client targets iOS 13 or later and supports the core Chill API, schema, auth, and i18n endpoints. Requests use `NSURLSession` and completion blocks.

## Install with CocoaPods

Add the local pod to your `Podfile`:

```ruby
pod 'ChillSharpObjectiveCClient', :path => '../chill-sharp-objective-c-client'
```

Then run `pod install` and open the generated `.xcworkspace`.

## Quick start

```objective-c
#import <ChillSharpObjectiveCClient/CSChillSharpClient.h>

CSChillSharpClient *client = [[CSChillSharpClient alloc]
    initWithBaseURL:@"http://localhost:5000"
    accessToken:nil
    cultureName:@"it-IT"];

NSDictionary *post = @{
    @"ChillType": @"Model.Post",
    @"Guid": @"00000000-0000-0000-0000-000000000001",
    @"Properties": @{
        @"Title": @"Hello",
        @"Author": @"Ada Lovelace"
    }
};

[client create:post completion:^(id result, NSError *error) {
    if (error) {
        NSLog(@"Request failed: %@", error.localizedDescription);
        return;
    }
    NSLog(@"Created: %@", result);
}];
```

Pass a server root, an existing `/api/chill` URL, or another recognized ChillSharp endpoint. A server root defaults to `/api/chill`. Supply a different API base path or custom `NSURLSession` with the designated initializer.

## Available methods

Core methods: `query:completion:`, `lookup:completion:`, `find:completion:`, `create:completion:`, `update:completion:`, `deleteEntity:completion:`, `autocomplete:completion:`, `validate:completion:`, and `chunk:completion:`.

Schema methods cover reading and writing schemas and entity options. I18n methods cover reading one or many texts and setting text. Auth helpers cover registration, login, refresh, logout, password reset, permissions, current user preferences, and user and role lists.

The `accessToken` property is applied as a bearer token to authenticated requests. Login, registration, and refresh update it from the returned `accessToken` field. HTTP failures use `CSChillSharpClientErrorDomain`; inspect `CSChillSharpClientStatusCodeKey` and `CSChillSharpClientResponseTextKey` in `NSError.userInfo` for the server status and response body.

Attachment transfer helpers and automatic username/password token refresh are not included yet.
