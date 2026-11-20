# Patrón de Desacoplamiento de Configuraciones de Entidad (IEntityTypeConfiguration)

## Propósito
Este proyecto aplica el patrón de desacoplamiento de configuraciones de entidades usando implementaciones de `IEntityTypeConfiguration<T>` para mantener el `DbContext` limpio, escalable y fácil de mantener.

## Alcance
- **Ubicación:** Las clases de configuración de cada entidad se colocan junto a la entidad correspondiente dentro de su feature (ejemplo: `Features/Posts/Models/Entities/PostConfiguration.cs`).
- **Registro automático:** El `AutoDiscoveryIAMDbContext` registra todas las configuraciones automáticamente mediante `ApplyConfigurationsFromAssembly`.
- **Cobertura:** Este patrón aplica para todos los modelos gestionados por EF Core en el proyecto.

## Ventajas
- Facilita la mantenibilidad y escalabilidad del modelo de datos.
- Permite separar responsabilidades y reducir el tamaño del `DbContext`.
- Favorece la reutilización y pruebas unitarias de las configuraciones.

## Ejemplo rápido
```csharp
// Ejemplo de configuración desacoplada para la entidad Post
public class PostConfiguration : IEntityTypeConfiguration<Post>
{
    public void Configure(EntityTypeBuilder<Post> builder)
    {
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Title).IsRequired().HasMaxLength(200);
        builder.HasQueryFilter(p => !p.IsDeleted);
        // ... otras restricciones
    }
}

// En AutoDiscoveryIAMDbContext
protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    base.OnModelCreating(modelBuilder);
    modelBuilder.ApplyConfigurationsFromAssembly(typeof(AutoDiscoveryIAMDbContext).Assembly);
}
```

## Validación
- Se valida el funcionamiento mediante pruebas unitarias, integración y pruebas de migraciones.
- Se recomienda, tras cualquier cambio estructural, generar y aplicar migraciones para asegurar la consistencia del modelo.

## Referencia
Para detalles y convenciones adicionales, consulta la documentación de arquitectura o el Pull Request correspondiente a la implementación de este patrón.
