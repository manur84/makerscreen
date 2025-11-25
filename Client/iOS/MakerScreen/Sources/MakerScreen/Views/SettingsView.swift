#if canImport(SwiftUI)
import SwiftUI

/// Settings view for configuring the app
public struct SettingsView: View {
    @EnvironmentObject var viewModel: MakerScreenViewModel
    @State private var serverURL: String = ""
    @State private var showingResetConfirmation = false
    
    public init() {}
    
    public var body: some View {
        NavigationView {
            List {
                // Server Configuration
                serverConfigSection
                
                // Connection Status
                connectionStatusSection
                
                // About
                aboutSection
                
                // Reset
                resetSection
            }
            .navigationTitle("Settings")
            .onAppear {
                serverURL = viewModel.serverURL
            }
        }
    }
    
    // MARK: - Server Configuration Section
    
    private var serverConfigSection: some View {
        Section {
            VStack(alignment: .leading, spacing: 8) {
                Text("Server URL")
                    .font(.caption)
                    .foregroundColor(.secondary)
                
                TextField("wss://server:8443/ws", text: $serverURL)
                    .textFieldStyle(.roundedBorder)
                    .textInputAutocapitalization(.never)
                    .autocorrectionDisabled()
                    .keyboardType(.URL)
            }
            .padding(.vertical, 4)
            
            Button(action: {
                viewModel.updateServerURL(serverURL)
            }) {
                HStack {
                    Image(systemName: "checkmark.circle")
                    Text("Save & Connect")
                }
            }
            .disabled(serverURL.isEmpty)
        } header: {
            Text("Server Configuration")
        } footer: {
            Text("Enter the WebSocket URL of your MakerScreen server. The URL should start with wss:// for secure connections.")
        }
    }
    
    // MARK: - Connection Status Section
    
    private var connectionStatusSection: some View {
        Section("Connection Status") {
            HStack {
                Circle()
                    .fill(connectionStatusColor)
                    .frame(width: 10, height: 10)
                Text("Status")
                Spacer()
                Text(viewModel.connectionStatusText)
                    .foregroundColor(.secondary)
            }
            
            if viewModel.isConnected {
                HStack {
                    Text("Connected Clients")
                    Spacer()
                    Text("\(viewModel.totalClientCount)")
                        .foregroundColor(.secondary)
                }
                
                HStack {
                    Text("Content Items")
                    Spacer()
                    Text("\(viewModel.contentItems.count)")
                        .foregroundColor(.secondary)
                }
            }
            
            if let error = viewModel.lastError {
                HStack(alignment: .top) {
                    Text("Last Error")
                    Spacer()
                    Text(error)
                        .foregroundColor(.red)
                        .multilineTextAlignment(.trailing)
                        .font(.caption)
                }
            }
            
            // Connection Controls
            HStack(spacing: 16) {
                Button(action: {
                    if viewModel.isConnected {
                        viewModel.disconnect()
                    } else {
                        viewModel.connect()
                    }
                }) {
                    HStack {
                        Image(systemName: viewModel.isConnected ? "xmark.circle" : "play.circle")
                        Text(viewModel.isConnected ? "Disconnect" : "Connect")
                    }
                    .frame(maxWidth: .infinity)
                    .padding(.vertical, 8)
                    .background(viewModel.isConnected ? Color.red.opacity(0.1) : Color.green.opacity(0.1))
                    .foregroundColor(viewModel.isConnected ? .red : .green)
                    .cornerRadius(8)
                }
                .disabled(viewModel.serverURL.isEmpty)
                
                if viewModel.isConnected {
                    Button(action: {
                        viewModel.refreshData()
                    }) {
                        HStack {
                            Image(systemName: "arrow.clockwise")
                            Text("Refresh")
                        }
                        .frame(maxWidth: .infinity)
                        .padding(.vertical, 8)
                        .background(Color.blue.opacity(0.1))
                        .foregroundColor(.blue)
                        .cornerRadius(8)
                    }
                }
            }
            .buttonStyle(.plain)
        }
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
    
    // MARK: - About Section
    
    private var aboutSection: some View {
        Section("About") {
            HStack {
                Text("App Name")
                Spacer()
                Text("MakerScreen")
                    .foregroundColor(.secondary)
            }
            
            HStack {
                Text("Version")
                Spacer()
                Text("1.0.0")
                    .foregroundColor(.secondary)
            }
            
            HStack {
                Text("Platform")
                Spacer()
                Text("iOS 17+")
                    .foregroundColor(.secondary)
            }
            
            Link(destination: URL(string: "https://github.com/makerscreen/makerscreen")!) {
                HStack {
                    Text("GitHub Repository")
                    Spacer()
                    Image(systemName: "arrow.up.right.square")
                        .foregroundColor(.secondary)
                }
            }
        }
    }
    
    // MARK: - Reset Section
    
    private var resetSection: some View {
        Section {
            Button(role: .destructive, action: {
                showingResetConfirmation = true
            }) {
                HStack {
                    Image(systemName: "trash")
                    Text("Reset All Settings")
                }
                .foregroundColor(.red)
            }
            .confirmationDialog(
                "Reset All Settings?",
                isPresented: $showingResetConfirmation,
                titleVisibility: .visible
            ) {
                Button("Reset", role: .destructive) {
                    resetSettings()
                }
                Button("Cancel", role: .cancel) {}
            } message: {
                Text("This will clear all saved settings including the server URL. The app will disconnect from the server.")
            }
        } footer: {
            Text("Clears all saved settings and disconnects from the server.")
        }
    }
    
    // MARK: - Actions
    
    private func resetSettings() {
        viewModel.disconnect()
        serverURL = ""
        viewModel.updateServerURL("")
        UserDefaults.standard.removeObject(forKey: "MakerScreen.ServerURL")
    }
}

#if DEBUG
struct SettingsView_Previews: PreviewProvider {
    static var previews: some View {
        SettingsView()
            .environmentObject(MakerScreenViewModel())
    }
}
#endif
#endif  // canImport(SwiftUI)
