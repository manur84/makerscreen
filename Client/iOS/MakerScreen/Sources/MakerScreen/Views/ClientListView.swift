#if canImport(SwiftUI)
import SwiftUI

/// View displaying the list of signage clients
public struct ClientListView: View {
    @EnvironmentObject var viewModel: MakerScreenViewModel
    @State private var searchText = ""
    @State private var selectedFilter: ClientStatus?
    @State private var selectedClient: SignageClient?
    @State private var showingClientDetail = false
    
    public init() {}
    
    public var body: some View {
        NavigationView {
            VStack(spacing: 0) {
                // Filter Chips
                filterChipsView
                
                // Client List
                if filteredClients.isEmpty {
                    emptyStateView
                } else {
                    clientListView
                }
            }
            .navigationTitle("Clients")
            .searchable(text: $searchText, prompt: "Search clients")
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
            .sheet(isPresented: $showingClientDetail) {
                if let client = selectedClient {
                    ClientDetailView(client: client)
                        .environmentObject(viewModel)
                }
            }
        }
    }
    
    // MARK: - Computed Properties
    
    private var filteredClients: [SignageClient] {
        var clients = viewModel.clients
        
        // Apply status filter
        if let filter = selectedFilter {
            clients = clients.filter { $0.status == filter }
        }
        
        // Apply search filter
        if !searchText.isEmpty {
            clients = clients.filter { client in
                client.name.localizedCaseInsensitiveContains(searchText) ||
                client.ipAddress.localizedCaseInsensitiveContains(searchText) ||
                client.id.localizedCaseInsensitiveContains(searchText)
            }
        }
        
        return clients.sorted { $0.lastSeen > $1.lastSeen }
    }
    
    // MARK: - Filter Chips
    
    private var filterChipsView: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                FilterChip(title: "All", isSelected: selectedFilter == nil) {
                    selectedFilter = nil
                }
                
                ForEach(ClientStatus.allCases, id: \.self) { status in
                    FilterChip(
                        title: status.rawValue,
                        count: viewModel.clients(withStatus: status).count,
                        isSelected: selectedFilter == status
                    ) {
                        selectedFilter = selectedFilter == status ? nil : status
                    }
                }
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
        }
        .background(Color(.systemBackground))
    }
    
    // MARK: - Client List
    
    private var clientListView: some View {
        List {
            ForEach(filteredClients) { client in
                ClientListRowView(client: client)
                    .contentShape(Rectangle())
                    .onTapGesture {
                        selectedClient = client
                        showingClientDetail = true
                    }
            }
        }
        .listStyle(.plain)
    }
    
    // MARK: - Empty State
    
    private var emptyStateView: some View {
        VStack(spacing: 16) {
            Image(systemName: "display.trianglebadge.exclamationmark")
                .font(.system(size: 60))
                .foregroundColor(.secondary)
            
            Text("No Clients Found")
                .font(.headline)
            
            if !viewModel.isConnected {
                Text("Connect to the server to see clients")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
            } else if !searchText.isEmpty || selectedFilter != nil {
                Text("Try adjusting your search or filter")
                    .font(.subheadline)
                    .foregroundColor(.secondary)
            }
        }
        .frame(maxWidth: .infinity, maxHeight: .infinity)
        .background(Color(.systemGroupedBackground))
    }
}

// MARK: - Filter Chip

struct FilterChip: View {
    let title: String
    var count: Int?
    let isSelected: Bool
    let action: () -> Void
    
    var body: some View {
        Button(action: action) {
            HStack(spacing: 4) {
                Text(title)
                if let count = count, count > 0 {
                    Text("(\(count))")
                        .font(.caption2)
                }
            }
            .font(.subheadline)
            .padding(.horizontal, 12)
            .padding(.vertical, 6)
            .background(isSelected ? Color.accentColor : Color(.systemGray5))
            .foregroundColor(isSelected ? .white : .primary)
            .cornerRadius(16)
        }
    }
}

// MARK: - Client List Row

struct ClientListRowView: View {
    let client: SignageClient
    
    var body: some View {
        HStack(spacing: 12) {
            // Status Icon
            ZStack {
                Circle()
                    .fill(statusColor.opacity(0.1))
                    .frame(width: 44, height: 44)
                
                Image(systemName: client.status.symbolName)
                    .foregroundColor(statusColor)
            }
            
            // Client Info
            VStack(alignment: .leading, spacing: 4) {
                Text(client.name.isEmpty ? "Unnamed Client" : client.name)
                    .font(.headline)
                
                HStack(spacing: 8) {
                    Text(client.ipAddress)
                        .font(.caption)
                        .foregroundColor(.secondary)
                    
                    if !client.version.isEmpty {
                        Text("v\(client.version)")
                            .font(.caption2)
                            .padding(.horizontal, 6)
                            .padding(.vertical, 2)
                            .background(Color(.systemGray5))
                            .cornerRadius(4)
                    }
                }
            }
            
            Spacer()
            
            // Last Seen
            VStack(alignment: .trailing, spacing: 4) {
                Text(client.status.rawValue)
                    .font(.caption)
                    .fontWeight(.medium)
                    .foregroundColor(statusColor)
                
                Text(client.timeSinceLastSeen)
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
            
            Image(systemName: "chevron.right")
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

// MARK: - Client Detail View

struct ClientDetailView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var viewModel: MakerScreenViewModel
    let client: SignageClient
    
    var body: some View {
        NavigationView {
            List {
                // Status Section
                Section("Status") {
                    HStack {
                        Image(systemName: client.status.symbolName)
                            .foregroundColor(statusColor)
                        Text(client.status.rawValue)
                        Spacer()
                        Text(client.timeSinceLastSeen)
                            .foregroundColor(.secondary)
                    }
                }
                
                // Details Section
                Section("Details") {
                    DetailRow(label: "ID", value: client.id)
                    DetailRow(label: "Name", value: client.name.isEmpty ? "Not Set" : client.name)
                    DetailRow(label: "IP Address", value: client.ipAddress)
                    DetailRow(label: "MAC Address", value: client.macAddress.isEmpty ? "Unknown" : client.macAddress)
                    DetailRow(label: "Version", value: client.version.isEmpty ? "Unknown" : client.version)
                }
                
                // Metadata Section
                if !client.metadata.isEmpty {
                    Section("Metadata") {
                        ForEach(Array(client.metadata.keys.sorted()), id: \.self) { key in
                            DetailRow(label: key, value: client.metadata[key] ?? "")
                        }
                    }
                }
                
                // Actions Section
                Section("Actions") {
                    Button(action: {
                        viewModel.restartClient(client.id)
                    }) {
                        Label("Restart Client", systemImage: "arrow.clockwise")
                    }
                    .disabled(!viewModel.isConnected)
                    
                    Button(action: {
                        viewModel.refreshClientContent(client.id)
                    }) {
                        Label("Refresh Content", systemImage: "arrow.triangle.2.circlepath")
                    }
                    .disabled(!viewModel.isConnected)
                }
            }
            .navigationTitle(client.name.isEmpty ? "Client Details" : client.name)
            .navigationBarTitleDisplayMode(.inline)
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    Button("Done") {
                        dismiss()
                    }
                }
            }
        }
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

struct DetailRow: View {
    let label: String
    let value: String
    
    var body: some View {
        HStack {
            Text(label)
                .foregroundColor(.secondary)
            Spacer()
            Text(value)
                .multilineTextAlignment(.trailing)
        }
    }
}

#if DEBUG
struct ClientListView_Previews: PreviewProvider {
    static var previews: some View {
        ClientListView()
            .environmentObject(MakerScreenViewModel())
    }
}
#endif
#endif  // canImport(SwiftUI)
