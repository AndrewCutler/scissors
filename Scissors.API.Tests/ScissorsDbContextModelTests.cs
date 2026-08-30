using Microsoft.EntityFrameworkCore;
using Scissors.API.Data;
using Scissors.API.Models.Entities;
using Scissors.API.Tests.Infrastructure;
using Xunit;

namespace Scissors.API.Tests;

public class ScissorsDbContextModelTests
{
    [Fact]
    public void ClippingTextHasTheExpectedLengthAndRequiredSettings()
    {
        using var db = ApiTestHelpers.CreateDbContext();

        var entityType = db.Model.FindEntityType(typeof(Clipping));
        var textProperty = entityType!.FindProperty(nameof(Clipping.Text));
        var capturedAtProperty = entityType.FindProperty(nameof(Clipping.CapturedAt));
        var createdAtProperty = entityType.FindProperty(nameof(Clipping.CreatedAt));

        Assert.Equal(2000, textProperty!.GetMaxLength());
        Assert.False(textProperty.IsNullable);
        Assert.False(capturedAtProperty!.IsNullable);
        Assert.Equal("CURRENT_TIMESTAMP", createdAtProperty!.GetDefaultValueSql());
    }

    [Fact]
    public void ExternalIdentityHasAUniqueProviderSubjectIndex()
    {
        using var db = ApiTestHelpers.CreateDbContext();

        var entityType = db.Model.FindEntityType(typeof(ExternalIdentity));
        var index = entityType!.GetIndexes().Single();

        Assert.True(index.IsUnique);
        Assert.Equal(new[] { nameof(ExternalIdentity.Provider), nameof(ExternalIdentity.Subject) }, index.Properties.Select(property => property.Name));
    }

    [Fact]
    public void RefreshTokenRequiresANonOptionalUserRelationship()
    {
        using var db = ApiTestHelpers.CreateDbContext();

        var entityType = db.Model.FindEntityType(typeof(RefreshToken));
        var foreignKey = entityType!.GetForeignKeys().Single();

        Assert.True(foreignKey.IsRequired);
        Assert.Equal(typeof(User), foreignKey.PrincipalEntityType.ClrType);
    }
}
