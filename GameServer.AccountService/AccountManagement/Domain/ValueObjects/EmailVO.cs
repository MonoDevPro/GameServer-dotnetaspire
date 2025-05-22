using GameServer.AccountService.AccountManagement.Domain.ValueObjects.Base;

namespace GameServer.AccountService.AccountManagement.Domain.ValueObjects;

public sealed class EmailVO : ValueObject<EmailVO>
{
    public string Value { get; private set; }
    
    private EmailVO(){}

    private EmailVO(string value)
    {
        Value = value;
    }

    public static EmailVO Create(string email)
    {
        Validate(email);
        return new EmailVO(email.Trim().ToLowerInvariant());
    }

    public static void Validate(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            throw new ArgumentNullException(nameof(email));

        try
        {
            var addr = new System.Net.Mail.MailAddress(email);
            if (addr.Address != email)
                throw new ArgumentException("Formato de email inválido");

            if (email.EndsWith("@tempmail.com"))
                throw new ArgumentException("Domínio de email não permitido");
        }
        catch (FormatException)
        {
            throw new ArgumentException("Formato de email inválido");
        }
    }

    protected override bool EqualsCore(EmailVO? other) =>
        Value == other?.Value;

    protected override int ComputeHashCode() =>
        HashCode.Combine(Value);

    public override string ToString() => Value;
}