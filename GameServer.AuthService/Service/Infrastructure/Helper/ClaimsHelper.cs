using System.ComponentModel;
using System.Reflection;
using System.Security.Claims;

namespace GameServer.AuthService.Service.Infrastructure.Helper;

public class ClaimsHelper
{
  public static IEnumerable<Claim> CreateClaims<T>(
    T entity,
    IEnumerable<Claim> additionalClaims = null,
    ClaimsHelperOptions options = null)
    where T : class
  {
    ClaimsHelperOptions claimsOptions = options ?? new ClaimsHelperOptions();
    if ((object) entity == null)
      throw new ArgumentNullException(nameof (entity));
    List<Claim> claims = new List<Claim>();
    if (additionalClaims != null)
      claims.AddRange(additionalClaims);
    IEnumerable<Claim> collection = ((IEnumerable<PropertyInfo>) typeof (T).GetProperties()).Where<PropertyInfo>((Func<PropertyInfo, bool>) (t => t.PropertyType.IsPrimitive || t.PropertyType.IsValueType || t.PropertyType == typeof (string))).Select(property => new
    {
      property = property,
      value = property.GetValue((object) (T) entity)
    }).Where(_param1 => _param1.value != null && !string.IsNullOrEmpty(_param1.value.ToString())).Select(_param1 => new Claim(claimsOptions.LowerCase ? _param1.property.Name.ToLower() : _param1.property.Name, _param1.value?.ToString()));
    claims.AddRange(collection);
    return (IEnumerable<Claim>) claims;
  }

  public static T GetValue<T>(ClaimsIdentity identity, string claimName)
  {
    Claim first = identity.FindFirst((Predicate<Claim>) (x => x.Type == claimName));
    if (first == null)
      return default (T);
    if (string.IsNullOrWhiteSpace(first.Value))
      return default (T);
    try
    {
      return (T) TypeDescriptor.GetConverter(typeof (T)).ConvertFromInvariantString(first.Value);
    }
    catch (Exception ex)
    {
      throw new InvalidCastException($"{first.Value} from {first.Value} to {typeof (T)}", ex);
    }
  }

  public static List<T> GetValues<T>(ClaimsIdentity items, string claimName)
  {
    List<T> result = new List<T>();
    List<Claim> list = items.FindAll((Predicate<Claim>) (x => x.Type == claimName)).ToList<Claim>();
    if (!list.Any<Claim>())
      return result;
    list.ToList<Claim>().ForEach((Action<Claim>) (x =>
    {
      if (string.IsNullOrWhiteSpace(x.Value))
        return;
      try
      {
        result.Add((T) TypeDescriptor.GetConverter(typeof (T)).ConvertFromInvariantString(x.Value));
      }
      catch (Exception ex)
      {
        throw new InvalidCastException($"{x.Value} from {x.Value} to {typeof (T)}", ex);
      }
    }));
    return result;
  }

  private static Claim FindFirstOrEmpty(IEnumerable<Claim> claims, string claimType)
  {
    return claims.FirstOrDefault<Claim>((Func<Claim, bool>) (x => x.Value == claimType));
  }
}