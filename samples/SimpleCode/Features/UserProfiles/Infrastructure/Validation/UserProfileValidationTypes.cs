namespace TuProyecto.Features.UserProfiles.Infrastructure.Validation;

/// <summary>
/// Centraliza los mensajes de validación de UserProfile en un solo lugar -- si cambias una regla
/// (ej. la longitud máxima de la ocupación), solo se actualiza aquí, no en cada Validator que la
/// use. Mismo patrón que usa la librería en Features/Roles/Infrastructure/Validation/RoleValidationTypes.cs.
/// </summary>
public static class UserProfileValidationTypes
{
  public static class UserPublicIdMessages
  {
    public const string Required = "El usuario dueño del perfil es obligatorio.";
    public const string NotFound = "El usuario indicado no existe.";
  }

  public static class OccupationMessages
  {
    public const string Required = "La ocupación es obligatoria.";
    public const string MaxLength = "La ocupación no puede exceder 200 caracteres.";
  }

  public static class BiographyMessages
  {
    public const string Required = "La biografía es obligatoria.";
  }

  public static class MaritalStatusMessages
  {
    public const string Invalid = "El estado civil no es válido -- ver MaritalStatusCatalog.GetAll() para los valores permitidos.";
  }
}
