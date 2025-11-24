import SwiftUI

@main
struct MakerScreenApp: App {
    @StateObject private var webSocketService = WebSocketService()
    
    var body: some Scene {
        WindowGroup {
            ContentView()
                .environmentObject(webSocketService)
        }
    }
}
