namespace Casino.Application.Abstractions;

/// <summary>
/// Adresses IPv4 de la machine sur le réseau local, la plus probable en premier.
/// Sert à fabriquer un lien d'invitation joignable depuis les téléphones, même quand
/// l'écran table est ouvert en « localhost ». Implémenté par la couche Infrastructure.
/// </summary>
public interface ILanAddressProvider
{
    IReadOnlyList<string> GetLanAddresses();
}
