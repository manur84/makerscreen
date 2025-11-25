#if canImport(SwiftUI)
import SwiftUI

/// Dashboard view showing system overview and statistics
public struct DashboardView: View {
    @EnvironmentObject var viewModel: MakerScreenViewModel
    
    public init() {}
    
    public var body: some View {
        NavigationView {
            ScrollView {
                VStack(spacing: 20) {
                    // Connection Status Card
                    connectionStatusCard
                    
                    // Statistics Grid
                    statisticsGrid
                    
                    // Recent Activity Section
                    recentActivitySection
                    
                    // Quick Actions
                    quickActionsSection
                }
                .padding()
            }
            .navigationTitle("Dashboard")
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button(action: {
                        viewModel.refreshData()
                    }) {
                        Image(systemName: "arrow.clockwise")
                    }
                    .disabled(!viewModel.isConnected)
                }
            }
            .refreshable {
                viewModel.refreshData()
            }
        }
    }
    
    // MARK: - Connection Status Card
    
    private var connectionStatusCard: some View {
        VStack(alignment: .leading, spacing: 12) {
            HStack {
                Circle()
                    .fill(connectionStatusColor)
                    .frame(width: 12, height: 12)
                
                Text(viewModel.connectionStatusText)
                    .font(.headline)
                
                Spacer()
                
                Button(action: {
                    if viewModel.isConnected {
                        viewModel.disconnect()
                    } else {
                        viewModel.connect()
                    }
                }) {
                    Text(viewModel.isConnected ? "Disconnect" : "Connect")
                        .font(.subheadline)
                        .padding(.horizontal, 12)
                        .padding(.vertical, 6)
                        .background(viewModel.isConnected ? Color.red.opacity(0.1) : Color.green.opacity(0.1))
                        .foregroundColor(viewModel.isConnected ? .red : .green)
                        .cornerRadius(8)
                }
            }
            
            if !viewModel.serverURL.isEmpty {
                Text("Server: \(viewModel.serverURL)")
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            
            if let error = viewModel.lastError {
                Text(error)
                    .font(.caption)
                    .foregroundColor(.red)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
    
    private var connectionStatusColor: Color {
        switch viewModel.connectionState {
        case .connected:
            return .green
        case .connecting, .reconnecting:
            return .orange
        case .disconnected:
            return .gray
        case .error:
            return .red
        }
    }
    
    // MARK: - Statistics Grid
    
    private var statisticsGrid: some View {
        LazyVGrid(columns: [
            GridItem(.flexible()),
            GridItem(.flexible())
        ], spacing: 16) {
            StatCard(
                title: "Total Clients",
                value: "\(viewModel.totalClientCount)",
                icon: "display",
                color: .blue
            )
            
            StatCard(
                title: "Online",
                value: "\(viewModel.onlineClientCount)",
                icon: "checkmark.circle.fill",
                color: .green
            )
            
            StatCard(
                title: "Offline",
                value: "\(viewModel.offlineClientCount)",
                icon: "xmark.circle.fill",
                color: .red
            )
            
            StatCard(
                title: "Content Items",
                value: "\(viewModel.contentItems.count)",
                icon: "photo.stack",
                color: .purple
            )
        }
    }
    
    // MARK: - Recent Activity Section
    
    private var recentActivitySection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Recent Clients")
                .font(.headline)
            
            if viewModel.clients.isEmpty {
                Text("No clients connected")
                    .foregroundColor(.secondary)
                    .frame(maxWidth: .infinity, alignment: .center)
                    .padding()
            } else {
                ForEach(viewModel.clients.prefix(5)) { client in
                    ClientRowView(client: client)
                }
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
    
    // MARK: - Quick Actions Section
    
    private var quickActionsSection: some View {
        VStack(alignment: .leading, spacing: 12) {
            Text("Quick Actions")
                .font(.headline)
            
            HStack(spacing: 12) {
                QuickActionButton(
                    title: "Refresh All",
                    icon: "arrow.clockwise",
                    color: .blue
                ) {
                    viewModel.refreshData()
                }
                .disabled(!viewModel.isConnected)
            }
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
}

// MARK: - Supporting Views

struct StatCard: View {
    let title: String
    let value: String
    let icon: String
    let color: Color
    
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            HStack {
                Image(systemName: icon)
                    .foregroundColor(color)
                Spacer()
            }
            
            Text(value)
                .font(.title)
                .fontWeight(.bold)
            
            Text(title)
                .font(.caption)
                .foregroundColor(.secondary)
        }
        .padding()
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
}

struct ClientRowView: View {
    let client: SignageClient
    
    var body: some View {
        HStack {
            Image(systemName: client.status.symbolName)
                .foregroundColor(statusColor)
            
            VStack(alignment: .leading) {
                Text(client.name.isEmpty ? client.id : client.name)
                    .font(.subheadline)
                    .fontWeight(.medium)
                
                Text(client.ipAddress)
                    .font(.caption)
                    .foregroundColor(.secondary)
            }
            
            Spacer()
            
            Text(client.timeSinceLastSeen)
                .font(.caption)
                .foregroundColor(.secondary)
        }
        .padding(.vertical, 4)
    }
    
    private var statusColor: Color {
        switch client.status {
        case .online:
            return .green
        case .offline:
            return .red
        case .installing:
            return .blue
        case .error:
            return .orange
        case .unknown:
            return .gray
        }
    }
}

struct QuickActionButton: View {
    let title: String
    let icon: String
    let color: Color
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            VStack {
                Image(systemName: icon)
                    .font(.title2)
                Text(title)
                    .font(.caption)
            }
            .frame(maxWidth: .infinity)
            .padding()
            .background(color.opacity(0.1))
            .foregroundColor(color)
            .cornerRadius(12)
        }
    }
}

#if DEBUG
struct DashboardView_Previews: PreviewProvider {
    static var previews: some View {
        DashboardView()
            .environmentObject(MakerScreenViewModel())
    }
}
#endif
#endif  // canImport(SwiftUI)
