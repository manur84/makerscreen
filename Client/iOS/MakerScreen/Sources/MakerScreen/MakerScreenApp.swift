#if canImport(SwiftUI)
import SwiftUI

/// Main entry point for the MakerScreen iOS Management App
@main
struct MakerScreenApp: App {
    @StateObject private var viewModel = MakerScreenViewModel()
    
    var body: some Scene {
        WindowGroup {
            TabView {
                DashboardView()
                    .tabItem {
                        Label("Dashboard", systemImage: "rectangle.3.group")
                    }
                
                ClientListView()
                    .tabItem {
                        Label("Clients", systemImage: "display")
                    }
                
                ContentLibraryView()
                    .tabItem {
                        Label("Content", systemImage: "photo.stack")
                    }
                
                SettingsView()
                    .tabItem {
                        Label("Settings", systemImage: "gear")
                    }
            }
            .environmentObject(viewModel)
        }
    }
}
#endif
