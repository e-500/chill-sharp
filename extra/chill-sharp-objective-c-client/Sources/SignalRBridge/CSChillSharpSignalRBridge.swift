import Foundation
import SignalRClient

private struct ChillChangePayload: Decodable {
    let chillType: String
    let guid: UUID
    let action: String
}

private struct RegistrationKey: Hashable {
    let chillType: String
    let guid: UUID?
}

private struct Listener {
    let key: RegistrationKey
    let onChanges: (NSArray) -> Void
}

private final class AccessTokenBox: @unchecked Sendable {
    private let lock = NSLock()
    private var storedToken: String?

    var value: String? {
        get {
            lock.lock()
            defer { lock.unlock() }
            return storedToken
        }
        set {
            lock.lock()
            defer { lock.unlock() }
            storedToken = newValue
        }
    }
}

/// Objective-C-facing subscription handle. Call `unsubscribeWithCompletion:` when finished.
@objc(CSChillSharpSignalRSubscription)
public final class CSChillSharpSignalRSubscription: NSObject {
    private let cancellation: (@escaping (NSError?) -> Void) -> Void
    private let lock = NSLock()
    private var didUnsubscribe = false

    fileprivate init(cancellation: @escaping (@escaping (NSError?) -> Void) -> Void) {
        self.cancellation = cancellation
    }

    @objc(unsubscribeWithCompletion:)
    public func unsubscribe(completion: @escaping (NSError?) -> Void) {
        lock.lock()
        let shouldUnsubscribe = !didUnsubscribe
        didUnsubscribe = true
        lock.unlock()
        guard shouldUnsubscribe else {
            completion(nil)
            return
        }
        cancellation(completion)
    }
}

/// Swift SignalR client exposed as an Objective-C class for applications using the Objective-C client.
@objc(CSChillSharpSignalRBridge)
public final class CSChillSharpSignalRBridge: NSObject {
    private let hubURL: String
    private let tokenBox: AccessTokenBox
    private let listenerLock = NSLock()
    private var listeners: [UUID: Listener] = [:]
    private var connection: HubConnection?

    @objc public var accessToken: String? {
        get { tokenBox.value }
        set { tokenBox.value = newValue }
    }

    @objc(initWithHubURL:accessToken:)
    public init(hubURL: String, accessToken: String?) {
        self.hubURL = hubURL
        self.tokenBox = AccessTokenBox()
        self.tokenBox.value = accessToken
        super.init()
    }

    /// Starts the shared SignalR connection. Register subscriptions after this succeeds.
    @objc(startWithCompletion:)
    public func start(completion: @escaping (NSError?) -> Void) {
        Task {
            do {
                let signalRConnection = await ensureConnection()
                try await signalRConnection.start()
                completion(nil)
            } catch {
                completion(error as NSError)
            }
        }
    }

    @objc(subscribeToChillType:guid:onChanges:completion:)
    public func subscribe(
        toChillType chillType: String,
        guid: NSUUID?,
        onChanges: @escaping (NSArray) -> Void,
        completion: @escaping (CSChillSharpSignalRSubscription?, NSError?) -> Void
    ) {
        guard let connection else {
            completion(nil, NSError(
                domain: "CSChillSharpSignalRBridge",
                code: 1,
                userInfo: [NSLocalizedDescriptionKey: "Start the SignalR connection before subscribing."]
            ))
            return
        }

        let entityGuid = guid.map { $0 as UUID }
        let key = RegistrationKey(chillType: chillType.trimmingCharacters(in: .whitespacesAndNewlines), guid: entityGuid)
        guard !key.chillType.isEmpty else {
            completion(nil, NSError(
                domain: "CSChillSharpSignalRBridge",
                code: 2,
                userInfo: [NSLocalizedDescriptionKey: "chillType is required."]
            ))
            return
        }

        let subscriptionID = UUID()
        listenerLock.lock()
        listeners[subscriptionID] = Listener(key: key, onChanges: onChanges)
        listenerLock.unlock()

        Task {
            guard await connection.state() == .Connected else {
                removeListener(subscriptionID)
                completion(nil, NSError(
                    domain: "CSChillSharpSignalRBridge",
                    code: 1,
                    userInfo: [NSLocalizedDescriptionKey: "Start the SignalR connection before subscribing."]
                ))
                return
            }
            do {
                // Group registration is idempotent, and invoking for each listener avoids
                // races when multiple subscriptions for the same key start concurrently.
                try await invoke("Register", key: key, on: connection)
                let subscription = CSChillSharpSignalRSubscription { [weak self] callback in
                    guard let self else { callback(nil); return }
                    self.unsubscribe(subscriptionID, callback: callback)
                }
                completion(subscription, nil)
            } catch {
                removeListener(subscriptionID)
                completion(nil, error as NSError)
            }
        }
    }

    @objc(stopWithCompletion:)
    public func stop(completion: @escaping (NSError?) -> Void) {
        Task {
            guard let currentConnection = connection else { completion(nil); return }
            do {
                await currentConnection.stop()
                removeAllListeners()
                connection = nil
                completion(nil)
            } catch {
                completion(error as NSError)
            }
        }
    }

    private func ensureConnection() async -> HubConnection {
        if let connection { return connection }

        var options = HttpConnectionOptions()
        let tokenBox = self.tokenBox
        options.accessTokenFactory = { tokenBox.value }
        let newConnection = HubConnectionBuilder()
            .withUrl(url: hubURL, options: options)
            .withAutomaticReconnect()
            .build()

        await newConnection.on("EntitiesChanged") { [weak self] (changes: [ChillChangePayload]) async in
            self?.dispatch(changes)
        }
        await newConnection.onReconnected { [weak self] in
            await self?.restoreRegistrations(on: newConnection)
        }
        connection = newConnection
        return newConnection
    }

    private func dispatch(_ changes: [ChillChangePayload]) {
        listenerLock.lock()
        let currentListeners = Array(listeners.values)
        listenerLock.unlock()

        for listener in currentListeners {
            let matching = changes
                .filter { $0.chillType == listener.key.chillType && (listener.key.guid == nil || $0.guid == listener.key.guid) }
                .map { ["chillType": $0.chillType, "guid": $0.guid.uuidString, "action": $0.action] as NSDictionary }
            if !matching.isEmpty { listener.onChanges(matching as NSArray) }
        }
    }

    private func unsubscribe(_ subscriptionID: UUID, callback: @escaping (NSError?) -> Void) {
        guard let connection else { callback(nil); return }
        listenerLock.lock()
        let removed = listeners.removeValue(forKey: subscriptionID)
        let shouldUnregister = removed.map { removed in !listeners.values.contains { $0.key == removed.key } } ?? false
        listenerLock.unlock()
        guard let removed, shouldUnregister else { callback(nil); return }

        Task {
            do {
                try await invoke("Unregister", key: removed.key, on: connection)
                callback(nil)
            } catch {
                callback(error as NSError)
            }
        }
    }

    private func restoreRegistrations(on connection: HubConnection) async {
        let registrations = registrationSnapshot()
        for registration in registrations {
            do { try await invoke("Register", key: registration, on: connection) }
            catch { /* The connection will report failure and retry reconnection. */ }
        }
    }

    private func invoke(_ method: String, key: RegistrationKey, on connection: HubConnection) async throws {
        if let guid = key.guid {
            try await connection.invoke(method: method, arguments: key.chillType, guid.uuidString)
        } else {
            try await connection.invoke(method: method, arguments: key.chillType, NSNull())
        }
    }

    private func registrationSnapshot() -> Set<RegistrationKey> {
        listenerLock.lock()
        defer { listenerLock.unlock() }
        return Set(listeners.values.map(\.key))
    }

    private func removeListener(_ subscriptionID: UUID) {
        listenerLock.lock()
        listeners.removeValue(forKey: subscriptionID)
        listenerLock.unlock()
    }

    private func removeAllListeners() {
        listenerLock.lock()
        listeners.removeAll()
        listenerLock.unlock()
    }
}
