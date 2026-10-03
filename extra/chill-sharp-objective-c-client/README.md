# chill-sharp-objective-c-client

Foundation-based, asynchronous Objective-C client for generic ChillSharp services. It uses dictionaries and arrays for request and response JSON so it works with application-specific models without code generation. The SignalR bridge wraps Microsoft's Swift SignalR client and exposes the realtime API to Objective-C.

The REST-only Objective-C client supports iOS 13 or later. The Swift Package Manager product, including SignalR, requires iOS 14 or later and macOS 11 or later. REST requests use `NSURLSession` and completion blocks.

## Install with Swift Package Manager

Add the ChillSharp repository to your `Package.swift` dependencies and include the client product:

```swift
.package(url: "https://github.com/e-500/chill-sharp.git", branch: "main")
```

Then add `ChillSharpObjectiveCClient` to your target dependencies:

```swift
.product(name: "ChillSharpObjectiveCClient", package: "chill-sharp")
```

Objective-C code imports the REST and SignalR bridge modules separately:

```objective-c
#import <ChillSharpObjectiveCClient/CSChillSharpClient.h>
#import <ChillSharpObjectiveCSignalRBridge/ChillSharpObjectiveCSignalRBridge-Swift.h>
```

## Install with CocoaPods

The CocoaPods spec continues to provide the REST-only Objective-C client. Use Swift Package Manager for SignalR support.

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

## SignalR entity change notifications

Create the bridge with the client's notification URL and current access token. Start it before subscribing. A `nil` GUID subscribes to all entities of the ChillType; pass an `NSUUID` to subscribe to one entity.

```objective-c
CSChillSharpSignalRBridge *notifications = [[CSChillSharpSignalRBridge alloc]
    initWithHubURL:client.notificationHubURL
    accessToken:client.accessToken];

[notifications startWithCompletion:^(NSError *error) {
    if (error) {
        NSLog(@"SignalR start failed: %@", error.localizedDescription);
        return;
    }

    [notifications subscribeToChillType:@"Model.Post"
        guid:nil
        onChanges:^(NSArray *changes) {
            NSLog(@"Entity changes: %@", changes);
        }
        completion:^(CSChillSharpSignalRSubscription *subscription, NSError *subscribeError) {
            if (subscribeError) {
                NSLog(@"Subscription failed: %@", subscribeError.localizedDescription);
                return;
            }

            // Keep the handle while listening, then call unsubscribeWithCompletion:.
        }];
}];
```

The bridge automatically reconnects and restores active registrations. Update `notifications.accessToken` when the application refreshes its token.

Attachment transfer helpers and automatic username/password token refresh are not included yet.
