using MakerScreen.Core.Models;

namespace MakerScreen.Core.Interfaces;

/// <summary>
/// Service for deploying and installing clients automatically
/// </summary>
public interface IClientDeploymentService
{
    Task<DeploymentPackage> CreateDeploymentPackageAsync(Dictionary<string, string> configuration, CancellationToken cancellationToken = default);
    Task<bool> DeployToClientAsync(string clientIp, DeploymentPackage package, CancellationToken cancellationToken = default);
    Task<bool> AutoDiscoverAndDeployAsync(CancellationToken cancellationToken = default);
    Task<string> GenerateRaspberryPiImageAsync(DeploymentPackage package, CancellationToken cancellationToken = default);
}
