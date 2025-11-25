#if canImport(SwiftUI)
import SwiftUI

/// View displaying the content library
public struct ContentLibraryView: View {
    @EnvironmentObject var viewModel: MakerScreenViewModel
    @State private var searchText = ""
    @State private var selectedFilter: ContentType?
    @State private var selectedContent: ContentItem?
    @State private var showingContentDetail = false
    @State private var viewMode: ViewMode = .grid
    
    enum ViewMode {
        case grid
        case list
    }
    
    public init() {}
    
    public var body: some View {
        NavigationView {
            VStack(spacing: 0) {
                // Filter and View Mode
                filterBar
                
                // Content Display
                if filteredContent.isEmpty {
                    emptyStateView
                } else {
                    contentView
                }
            }
            .navigationTitle("Content Library")
            .searchable(text: $searchText, prompt: "Search content")
            .toolbar {
                ToolbarItem(placement: .navigationBarTrailing) {
                    HStack {
                        Button(action: {
                            viewMode = viewMode == .grid ? .list : .grid
                        }) {
                            Image(systemName: viewMode == .grid ? "list.bullet" : "square.grid.2x2")
                        }
                        
                        Button(action: {
                            viewModel.refreshData()
                        }) {
                            Image(systemName: "arrow.clockwise")
                        }
                        .disabled(!viewModel.isConnected)
                    }
                }
            }
            .refreshable {
                viewModel.refreshData()
            }
            .sheet(isPresented: $showingContentDetail) {
                if let content = selectedContent {
                    ContentDetailView(content: content)
                        .environmentObject(viewModel)
                }
            }
        }
    }
    
    // MARK: - Computed Properties
    
    private var filteredContent: [ContentItem] {
        var items = viewModel.contentItems
        
        // Apply type filter
        if let filter = selectedFilter {
            items = items.filter { $0.type == filter }
        }
        
        // Apply search filter
        if !searchText.isEmpty {
            items = items.filter { item in
                item.name.localizedCaseInsensitiveContains(searchText) ||
                item.id.localizedCaseInsensitiveContains(searchText)
            }
        }
        
        return items.sorted { $0.createdAt > $1.createdAt }
    }
    
    // MARK: - Filter Bar
    
    private var filterBar: some View {
        ScrollView(.horizontal, showsIndicators: false) {
            HStack(spacing: 8) {
                FilterChip(title: "All", isSelected: selectedFilter == nil) {
                    selectedFilter = nil
                }
                
                ForEach(ContentType.allCases, id: \.self) { type in
                    FilterChip(
                        title: type.rawValue,
                        count: viewModel.contentItems(ofType: type).count,
                        isSelected: selectedFilter == type
                    ) {
                        selectedFilter = selectedFilter == type ? nil : type
                    }
                }
            }
            .padding(.horizontal)
            .padding(.vertical, 8)
        }
        .background(Color(.systemBackground))
    }
    
    // MARK: - Content View
    
    @ViewBuilder
    private var contentView: some View {
        switch viewMode {
        case .grid:
            gridView
        case .list:
            listView
        }
    }
    
    private var gridView: some View {
        ScrollView {
            LazyVGrid(columns: [
                GridItem(.flexible()),
                GridItem(.flexible())
            ], spacing: 16) {
                ForEach(filteredContent) { item in
                    ContentGridItem(content: item)
                        .onTapGesture {
                            selectedContent = item
                            showingContentDetail = true
                        }
                }
            }
            .padding()
        }
    }
    
    private var listView: some View {
        List {
            ForEach(filteredContent) { item in
                ContentListRow(content: item)
                    .contentShape(Rectangle())
                    .onTapGesture {
                        selectedContent = item
                        showingContentDetail = true
                    }
            }
        }
        .listStyle(.plain)
    }
    
    // MARK: - Empty State
    
    private var emptyStateView: some View {
        VStack(spacing: 16) {
            Image(systemName: "photo.on.rectangle.angled")
                .font(.system(size: 60))
                .foregroundColor(.secondary)
            
            Text("No Content Found")
                .font(.headline)
            
            if !viewModel.isConnected {
                Text("Connect to the server to see content")
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

// MARK: - Content Grid Item

struct ContentGridItem: View {
    let content: ContentItem
    
    var body: some View {
        VStack(alignment: .leading, spacing: 8) {
            // Preview Area
            ZStack {
                RoundedRectangle(cornerRadius: 8)
                    .fill(Color(.systemGray5))
                    .aspectRatio(16/9, contentMode: .fit)
                
                Image(systemName: content.type.symbolName)
                    .font(.system(size: 32))
                    .foregroundColor(.secondary)
            }
            
            // Info
            VStack(alignment: .leading, spacing: 4) {
                Text(content.name.isEmpty ? "Unnamed" : content.name)
                    .font(.subheadline)
                    .fontWeight(.medium)
                    .lineLimit(1)
                
                HStack {
                    Text(content.type.rawValue)
                        .font(.caption2)
                        .padding(.horizontal, 6)
                        .padding(.vertical, 2)
                        .background(typeColor.opacity(0.1))
                        .foregroundColor(typeColor)
                        .cornerRadius(4)
                    
                    Spacer()
                    
                    Text(content.formattedDuration)
                        .font(.caption2)
                        .foregroundColor(.secondary)
                }
            }
        }
        .padding(8)
        .background(Color(.systemBackground))
        .cornerRadius(12)
        .shadow(radius: 2)
    }
    
    private var typeColor: Color {
        switch content.type {
        case .image:
            return .blue
        case .video:
            return .purple
        case .html:
            return .orange
        case .url:
            return .green
        }
    }
}

// MARK: - Content List Row

struct ContentListRow: View {
    let content: ContentItem
    
    var body: some View {
        HStack(spacing: 12) {
            // Type Icon
            ZStack {
                RoundedRectangle(cornerRadius: 8)
                    .fill(typeColor.opacity(0.1))
                    .frame(width: 44, height: 44)
                
                Image(systemName: content.type.symbolName)
                    .foregroundColor(typeColor)
            }
            
            // Content Info
            VStack(alignment: .leading, spacing: 4) {
                Text(content.name.isEmpty ? "Unnamed Content" : content.name)
                    .font(.headline)
                
                HStack(spacing: 8) {
                    Text(content.type.rawValue)
                        .font(.caption)
                        .foregroundColor(.secondary)
                    
                    Text("•")
                        .foregroundColor(.secondary)
                    
                    Text(content.formattedDuration)
                        .font(.caption)
                        .foregroundColor(.secondary)
                }
            }
            
            Spacer()
            
            // Date
            VStack(alignment: .trailing, spacing: 4) {
                Text(content.mimeType)
                    .font(.caption2)
                    .foregroundColor(.secondary)
            }
            
            Image(systemName: "chevron.right")
                .font(.caption)
                .foregroundColor(.secondary)
        }
        .padding(.vertical, 4)
    }
    
    private var typeColor: Color {
        switch content.type {
        case .image:
            return .blue
        case .video:
            return .purple
        case .html:
            return .orange
        case .url:
            return .green
        }
    }
}

// MARK: - Content Detail View

struct ContentDetailView: View {
    @Environment(\.dismiss) var dismiss
    @EnvironmentObject var viewModel: MakerScreenViewModel
    let content: ContentItem
    @State private var selectedClientId: String?
    
    var body: some View {
        NavigationView {
            List {
                // Preview Section
                Section {
                    ZStack {
                        RoundedRectangle(cornerRadius: 8)
                            .fill(Color(.systemGray5))
                            .aspectRatio(16/9, contentMode: .fit)
                        
                        Image(systemName: content.type.symbolName)
                            .font(.system(size: 48))
                            .foregroundColor(.secondary)
                    }
                }
                
                // Details Section
                Section("Details") {
                    DetailRow(label: "ID", value: content.id)
                    DetailRow(label: "Name", value: content.name.isEmpty ? "Not Set" : content.name)
                    DetailRow(label: "Type", value: content.type.rawValue)
                    DetailRow(label: "MIME Type", value: content.mimeType.isEmpty ? "Unknown" : content.mimeType)
                    DetailRow(label: "Duration", value: content.formattedDuration)
                    DetailRow(label: "Created", value: content.formattedCreatedAt)
                }
                
                // Metadata Section
                if !content.metadata.isEmpty {
                    Section("Metadata") {
                        ForEach(Array(content.metadata.keys.sorted()), id: \.self) { key in
                            DetailRow(label: key, value: content.metadata[key] ?? "")
                        }
                    }
                }
                
                // Send to Client Section
                Section("Send to Client") {
                    if viewModel.clients.filter({ $0.status == .online }).isEmpty {
                        Text("No online clients available")
                            .foregroundColor(.secondary)
                    } else {
                        ForEach(viewModel.clients.filter { $0.status == .online }) { client in
                            Button(action: {
                                viewModel.displayContent(content.id, on: client.id)
                            }) {
                                HStack {
                                    Image(systemName: "display")
                                    Text(client.name.isEmpty ? client.id : client.name)
                                    Spacer()
                                    Image(systemName: "arrow.right.circle")
                                }
                            }
                            .disabled(!viewModel.isConnected)
                        }
                    }
                }
            }
            .navigationTitle(content.name.isEmpty ? "Content Details" : content.name)
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
}

#if DEBUG
struct ContentLibraryView_Previews: PreviewProvider {
    static var previews: some View {
        ContentLibraryView()
            .environmentObject(MakerScreenViewModel())
    }
}
#endif
#endif  // canImport(SwiftUI)
