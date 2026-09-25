// swift-tools-version: 5.10
import PackageDescription

let package = Package(
    name: "ChillSharpObjectiveCClient",
    platforms: [
        .iOS(.v14),
        .macOS(.v11)
    ],
    products: [
        .library(
            name: "ChillSharpObjectiveCClient",
            targets: ["ChillSharpObjectiveCClient", "ChillSharpObjectiveCSignalRBridge"]
        )
    ],
    dependencies: [
        .package(url: "https://github.com/dotnet/signalr-client-swift", .upToNextMinor(from: "1.0.0"))
    ],
    targets: [
        .target(
            name: "ChillSharpObjectiveCClient",
            path: "Sources",
            exclude: ["SignalRBridge"],
            publicHeadersPath: "."
        ),
        .target(
            name: "ChillSharpObjectiveCSignalRBridge",
            dependencies: [
                "ChillSharpObjectiveCClient",
                .product(name: "SignalRClient", package: "signalr-client-swift")
            ],
            path: "Sources/SignalRBridge"
        )
    ],
    swiftLanguageVersions: [.v5]
)
