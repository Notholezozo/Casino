using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using Casino.Application.Abstractions;

namespace Casino.Infrastructure.Network;

/// <summary>
/// Lit les cartes réseau actives. Celles qui ont une passerelle (Wi-Fi, Ethernet) passent avant
/// les cartes virtuelles (Hyper-V, WSL, VPN sans route), qui ne sont pas joignables par un téléphone.
/// </summary>
internal sealed class LanAddressProvider : ILanAddressProvider
{
    public IReadOnlyList<string> GetLanAddresses() =>
        NetworkInterface.GetAllNetworkInterfaces()
            .Where(nic => nic.OperationalStatus == OperationalStatus.Up
                && nic.NetworkInterfaceType is not (NetworkInterfaceType.Loopback or NetworkInterfaceType.Tunnel))
            .Select(nic => nic.GetIPProperties())
            .OrderByDescending(HasIPv4Gateway)
            .SelectMany(properties => properties.UnicastAddresses)
            .Select(unicast => unicast.Address)
            .Where(address => address.AddressFamily == AddressFamily.InterNetwork && !IsLinkLocal(address))
            .Select(address => address.ToString())
            .Distinct()
            .ToList();

    private static bool HasIPv4Gateway(IPInterfaceProperties properties) =>
        properties.GatewayAddresses.Any(gateway =>
            gateway.Address.AddressFamily == AddressFamily.InterNetwork
            && !gateway.Address.Equals(IPAddress.Any));

    /// <summary>169.254.x.x : adresse auto-attribuée quand aucun réseau n'a répondu.</summary>
    private static bool IsLinkLocal(IPAddress address)
    {
        var bytes = address.GetAddressBytes();
        return bytes[0] == 169 && bytes[1] == 254;
    }
}
