using GameServer.AccountService.AccountManagement.Adapters.Out.Identity.Entities;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence;
using GameServer.AccountService.AccountManagement.Adapters.Out.Persistence.UnityOfWork;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Interfaces;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Models;
using GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Results;
using GameServer.AccountService.AccountManagement.Domain.Entities;
using GameServer.AccountService.AccountManagement.Domain.Events.Base;
using GameServer.AccountService.AccountManagement.Domain.ValueObjects;
using GameServer.AccountService.AccountManagement.Infrastructure.UnityOfWork;
using GameServer.AccountService.AccountManagement.Ports.In;
using GameServer.AccountService.AccountManagement.Ports.Out.Identity;
using GameServer.AccountService.AccountManagement.Ports.Out.Messaging;
using GameServer.AccountService.AccountManagement.Ports.Out.Persistence;
using GameServer.AccountService.AccountManagement.Ports.Out.Security;
using Microsoft.AspNetCore.Identity;
using OpenIddict.Abstractions;
using OpenIddict.Server.AspNetCore;

namespace GameServer.AccountService.AccountManagement.Application.CQRS.Commands.Handlers.Authentication;

public class RegisterCommandHandler 
    : ICommandHandler<RegisterCommand, ResultCommand>
{
    //private readonly UserManager<ApplicationUser> _userManager;
    //private readonly SignInManager<ApplicationUser> _signInManager;
    //private readonly IOpenIddictTokenManager _tokenManager; // ou via HttpContext
    
    private readonly IAccountIdentitySyncService _accountIdentitySyncService;
    private readonly IAccountRepository _accountRepository;
    private readonly IPasswordHashService _passwordHashService;
    private readonly IEventBus _eventBus;

    public RegisterCommandHandler(
        //UserManager<ApplicationUser> userManager,
        //SignInManager<ApplicationUser> signInManager,
        //IOpenIddictTokenManager tokenManager,
        IAccountIdentitySyncService accountIdentitySyncService,
        IAccountRepository accountRepository,
        IPasswordHashService passwordHashService,
        IEventBus eventBus
    )
    {
        //_userManager = userManager;
        //_signInManager = signInManager;
        //_tokenManager = tokenManager;
        _accountIdentitySyncService = accountIdentitySyncService;
        _accountRepository = accountRepository;
        _passwordHashService = passwordHashService;
        _eventBus = eventBus;
    }

    public async Task<ResultCommand> HandleAsync(
        RegisterCommand request, CancellationToken cancellationToken)
    {
        // 0. Verificar se o usuário já existe
        var usernameVO = UsernameVO.Create(request.Username);
        var existingUser = await _accountRepository.ExistsByUsernameAsync(usernameVO);
        if (existingUser)
            return ResultCommand.Failure("Username already exists");
        
        // 0.1 Verificar se o e-mail já existe
        var emailVO = EmailVO.Create(request.Email);
        existingUser = await _accountRepository.ExistsByEmailAsync(emailVO);
        if (existingUser)
            return ResultCommand.Failure("Email already exists");
        
        // 0.2 Verificar se a senha é válida
        var passwordVO = PasswordVO.Create(request.Password, _passwordHashService);
        
        // 1. Criar usuário de domínio
        var account = new Account(
            usernameVO, 
            emailVO, 
            passwordVO
        );
        
        // 2. (Opcional) adicionar roles, claims, e-mail confirmation...
        // await _userManager.AddToRoleAsync(user, "User");
        
        // Salvar no repositório
        var result = await _accountRepository.SaveAsync(account);
        if (!result)
            return ResultCommand.Failure("Failed to create account");
        
        var user = await _accountIdentitySyncService.SyncToIdentityAsync(account);
        
        // publicar eventos de domínio
        await PublishEventAsync(account);

        return ResultCommand.Success();
    }
    
    private async Task PublishEventAsync<T>(Entity<T> domainEntity) 
        where T : notnull
    {
        foreach (var domainEvent in domainEntity.GetDomainEvents())
        {
            await _eventBus.PublishAsync(domainEvent);
        }
        domainEntity.ClearDomainEvents();
    }
}