using Xunit;
using MakerScreen.Core.Interfaces;
using MakerScreen.Core.Models;
using MakerScreen.Services.Monitor;
using Microsoft.Extensions.Logging;
using Moq;
using FluentAssertions;

namespace MakerScreen.Tests;

public class ClientMonitorServiceTests
{
    private readonly Mock<ILogger<ClientMonitorService>> _loggerMock;
    private readonly ClientMonitorService _service;

    public ClientMonitorServiceTests()
    {
        _loggerMock = new Mock<ILogger<ClientMonitorService>>();
        _service = new ClientMonitorService(_loggerMock.Object);
    }

    [Fact]
    public void RegisterClient_ShouldAddClient()
    {
        // Arrange
        var client = new SignageClient
        {
            Id = "client-001",
            Name = "Test Client",
            Status = ClientStatus.Online
        };

        // Act
        _service.RegisterClient(client);

        // Assert
        _service.GetAllClients().Should().ContainSingle();
        _service.GetClient("client-001").Should().NotBeNull();
    }

    [Fact]
    public void UnregisterClient_ShouldRemoveClient()
    {
        // Arrange
        var client = new SignageClient { Id = "client-001", Name = "Test Client" };
        _service.RegisterClient(client);

        // Act
        _service.UnregisterClient("client-001");

        // Assert
        _service.GetAllClients().Should().BeEmpty();
        _service.GetClient("client-001").Should().BeNull();
    }

    [Fact]
    public void UpdateClientStatus_ShouldChangeStatus()
    {
        // Arrange
        var client = new SignageClient { Id = "client-001", Status = ClientStatus.Online };
        _service.RegisterClient(client);

        // Act
        _service.UpdateClientStatus("client-001", ClientStatus.Offline);

        // Assert
        var updatedClient = _service.GetClient("client-001");
        updatedClient.Should().NotBeNull();
        updatedClient!.Status.Should().Be(ClientStatus.Offline);
    }

    [Fact]
    public void RecordHeartbeat_ShouldUpdateLastSeen()
    {
        // Arrange
        var client = new SignageClient 
        { 
            Id = "client-001", 
            LastSeen = DateTime.UtcNow.AddMinutes(-5) 
        };
        _service.RegisterClient(client);

        // Act
        _service.RecordHeartbeat("client-001");

        // Assert
        var updatedClient = _service.GetClient("client-001");
        updatedClient.Should().NotBeNull();
        updatedClient!.LastSeen.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void GetClientsByStatus_ShouldFilterCorrectly()
    {
        // Arrange
        _service.RegisterClient(new SignageClient { Id = "client-001", Status = ClientStatus.Online });
        _service.RegisterClient(new SignageClient { Id = "client-002", Status = ClientStatus.Offline });
        _service.RegisterClient(new SignageClient { Id = "client-003", Status = ClientStatus.Online });

        // Act
        var onlineClients = _service.GetClientsByStatus(ClientStatus.Online);

        // Assert
        onlineClients.Should().HaveCount(2);
    }

    [Fact]
    public void GetStaleClients_ShouldReturnOldClients()
    {
        // Arrange
        _service.RegisterClient(new SignageClient 
        { 
            Id = "client-001", 
            LastSeen = DateTime.UtcNow.AddMinutes(-10) 
        });
        _service.RegisterClient(new SignageClient 
        { 
            Id = "client-002", 
            LastSeen = DateTime.UtcNow 
        });

        // Act
        var staleClients = _service.GetStaleClients(TimeSpan.FromMinutes(5));

        // Assert
        staleClients.Should().HaveCount(1);
        staleClients.First().Id.Should().Be("client-001");
    }

    [Fact]
    public void ClientStatusChanged_ShouldFireEvent()
    {
        // Arrange
        var client = new SignageClient { Id = "client-001", Status = ClientStatus.Online };
        _service.RegisterClient(client);
        
        ClientStatusChangedEventArgs? eventArgs = null;
        _service.ClientStatusChanged += (sender, args) => eventArgs = args;

        // Act
        _service.UpdateClientStatus("client-001", ClientStatus.Offline);

        // Assert
        eventArgs.Should().NotBeNull();
        eventArgs!.ClientId.Should().Be("client-001");
        eventArgs.OldStatus.Should().Be(ClientStatus.Online);
        eventArgs.NewStatus.Should().Be(ClientStatus.Offline);
    }
}
