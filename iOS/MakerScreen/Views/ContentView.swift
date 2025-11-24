import SwiftUI

struct ContentView: View {
    @EnvironmentObject var webSocket: WebSocketService
    @State private var selectedTab = 0
    
    var body: some View {
        TabView(selection: $selectedTab) {
            DashboardView()
                .tabItem {
                    Label("Dashboard", systemImage: "square.grid.2x2")
                }
                .tag(0)
            
            ClientsView()
                .tabItem {
                    Label("Clients", systemImage: "display.2")
                }
                .tag(1)
            
            ContentListView()
                .tabItem {
                    Label("Content", systemImage: "photo.stack")
                }
                .tag(2)
            
            SettingsView()
                .tabItem {
                    Label("Settings", systemImage: "gear")
                }
                .tag(3)
        }
    }
}

struct DashboardView: View {
    var body: some View {
        NavigationView {
            ScrollView {
                VStack(spacing: 20) {
                    // Stats Cards
                    HStack(spacing: 15) {
                        StatCard(title: "Online", value: "0", color: .green)
                        StatCard(title: "Total", value: "0", color: .blue)
                        StatCard(title: "Content", value: "0", color: .orange)
                    }
                    .padding()
                    
                    // Recent Activity
                    VStack(alignment: .leading) {
                        Text("Recent Activity")
                            .font(.headline)
                            .padding(.horizontal)
                        
                        Text("No recent activity")
                            .foregroundColor(.gray)
                            .padding()
                    }
                }
            }
            .navigationTitle("Dashboard")
        }
    }
}

struct StatCard: View {
    let title: String
    let value: String
    let color: Color
    
    var body: some View {
        VStack {
            Text(value)
                .font(.system(size: 32, weight: .bold))
                .foregroundColor(color)
            Text(title)
                .font(.caption)
                .foregroundColor(.gray)
        }
        .frame(maxWidth: .infinity)
        .padding()
        .background(Color.gray.opacity(0.1))
        .cornerRadius(10)
    }
}

struct ClientsView: View {
    var body: some View {
        NavigationView {
            List {
                Text("No clients connected")
                    .foregroundColor(.gray)
            }
            .navigationTitle("Clients")
            .toolbar {
                Button(action: {}) {
                    Image(systemName: "arrow.clockwise")
                }
            }
        }
    }
}

struct ContentListView: View {
    var body: some View {
        NavigationView {
            List {
                Text("No content available")
                    .foregroundColor(.gray)
            }
            .navigationTitle("Content")
            .toolbar {
                Button(action: {}) {
                    Image(systemName: "plus")
                }
            }
        }
    }
}

struct SettingsView: View {
    @State private var serverURL = "ws://localhost:8080/ws/"
    
    var body: some View {
        NavigationView {
            Form {
                Section(header: Text("Server")) {
                    TextField("Server URL", text: $serverURL)
                    Button("Connect") {
                        // TODO: Connect to server
                    }
                }
                
                Section(header: Text("About")) {
                    HStack {
                        Text("Version")
                        Spacer()
                        Text("1.0.0")
                            .foregroundColor(.gray)
                    }
                }
            }
            .navigationTitle("Settings")
        }
    }
}

#Preview {
    ContentView()
        .environmentObject(WebSocketService())
}
