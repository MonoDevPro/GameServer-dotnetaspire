﻿namespace GameServer.AuthService.Service.Domain.Entities.Base;

/// <summary>
/// Identifier common interface
/// </summary>
public interface IHaveId
{
    /// <summary>
    /// Identifier
    /// </summary>
    Guid Id { get; set; }
}