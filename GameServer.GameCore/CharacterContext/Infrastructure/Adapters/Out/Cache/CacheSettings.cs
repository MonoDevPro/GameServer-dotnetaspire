using System;

namespace GameServer.CharacterService.Infrastructure.Adapters.Out.Cache;

/// <summary>
/// Opções de configuração para o cache
/// </summary>
public class CacheSettings
{
    public const string SectionName = "CacheSettings";
    
    /// <summary>
    /// Tempo de expiração do cache em minutos
    /// </summary>
    public int UserCacheExpirationMinutes { get; set; } = 30;
    
    /// <summary>
    /// Limite máximo de entradas no cache
    /// </summary>
    public int SizeLimit { get; set; } = 1000;
}