using MediatR;
using Moq;
using Employee.Application.Common.Behaviors;
using Employee.Application.Common.Interfaces;
using Employee.Application.Common.Security;
using System.Threading;
using System.Threading.Tasks;

namespace Employee.UnitTests.Application.Common.Behaviors
{
  public class AuthorizationBehaviorTests
  {
    // ─── stub commands ────────────────────────────────────────────────────

    /// <summary>No [Authorize] attribute — open to everyone.</summary>
    private class UnprotectedRequest : IRequest<string> { }

    /// <summary>Requires any authenticated user (no specific role).</summary>
    [Authorize]
    private class AuthenticatedOnlyRequest : IRequest<string> { }

    /// <summary>Requires Admin role.</summary>
    [Authorize(Roles = "Admin")]
    private class AdminOnlyRequest : IRequest<string> { }

    /// <summary>Accepts Admin OR HR.</summary>
    [Authorize(Roles = "Admin,HR")]
    private class AdminOrHrRequest : IRequest<string> { }

    // ─── helpers ──────────────────────────────────────────────────────────

    private static AuthorizationBehavior<TRequest, string> BuildBehavior<TRequest>(
        Mock<ICurrentUser> mockUser)
        where TRequest : notnull
        => new AuthorizationBehavior<TRequest, string>(mockUser.Object);

    private static RequestHandlerDelegate<string> Next(string value = "ok")
        => () => Task.FromResult(value);

    private static Mock<ICurrentUser> AnonymousUser()
    {
      var m = new Mock<ICurrentUser>();
      m.Setup(u => u.UserId).Returns(string.Empty);
      m.Setup(u => u.UserName).Returns((string?)null);
      m.Setup(u => u.IsInRole(It.IsAny<string>())).Returns(false);
      return m;
    }

    private static Mock<ICurrentUser> AuthenticatedUser(string userId = "user-1", string? role = null)
    {
      var m = new Mock<ICurrentUser>();
      m.Setup(u => u.UserId).Returns(userId);
      m.Setup(u => u.UserName).Returns("testuser");
      m.Setup(u => u.IsInRole(It.IsAny<string>())).Returns<string>(r => r == role);
      return m;
    }

    // ─── Unprotected route ────────────────────────────────────────────────

    [Fact]
    public async Task Handle_NoAuthorizeAttribute_ShouldAlwaysCallNext()
    {
      var behavior = BuildBehavior<UnprotectedRequest>(AnonymousUser());

      var result = await behavior.Handle(new UnprotectedRequest(), Next("called"), CancellationToken.None);

      Assert.Equal("called", result);
    }

    // ─── Authentication check ─────────────────────────────────────────────

    [Fact]
    public async Task Handle_RequiresAuth_AnonymousUser_ShouldThrowUnauthorized()
    {
      var behavior = BuildBehavior<AuthenticatedOnlyRequest>(AnonymousUser());

      var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => behavior.Handle(new AuthenticatedOnlyRequest(), Next(), CancellationToken.None));

      Assert.Contains("not authenticated", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_RequiresAuth_AuthenticatedUser_ShouldCallNext()
    {
      var behavior = BuildBehavior<AuthenticatedOnlyRequest>(AuthenticatedUser());

      var result = await behavior.Handle(new AuthenticatedOnlyRequest(), Next("ok"), CancellationToken.None);

      Assert.Equal("ok", result);
    }

    // ─── Role check ───────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_RequiresAdminRole_UserHasAdmin_ShouldCallNext()
    {
      var behavior = BuildBehavior<AdminOnlyRequest>(AuthenticatedUser(role: "Admin"));

      var result = await behavior.Handle(new AdminOnlyRequest(), Next("allowed"), CancellationToken.None);

      Assert.Equal("allowed", result);
    }

    [Fact]
    public async Task Handle_RequiresAdminRole_UserHasWrongRole_ShouldThrowUnauthorized()
    {
      var behavior = BuildBehavior<AdminOnlyRequest>(AuthenticatedUser(role: "Employee"));

      var ex = await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => behavior.Handle(new AdminOnlyRequest(), Next(), CancellationToken.None));

      Assert.Contains("required role", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Handle_RequiresAdminRole_AnonymousUser_ShouldThrowUnauthorized()
    {
      var behavior = BuildBehavior<AdminOnlyRequest>(AnonymousUser());

      await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => behavior.Handle(new AdminOnlyRequest(), Next(), CancellationToken.None));
    }

    // ─── Multi-role (OR) check ────────────────────────────────────────────

    [Fact]
    public async Task Handle_RequiresAdminOrHR_UserHasHR_ShouldCallNext()
    {
      var behavior = BuildBehavior<AdminOrHrRequest>(AuthenticatedUser(role: "HR"));

      var result = await behavior.Handle(new AdminOrHrRequest(), Next("ok"), CancellationToken.None);

      Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_RequiresAdminOrHR_UserHasAdmin_ShouldCallNext()
    {
      var behavior = BuildBehavior<AdminOrHrRequest>(AuthenticatedUser(role: "Admin"));

      var result = await behavior.Handle(new AdminOrHrRequest(), Next("ok"), CancellationToken.None);

      Assert.Equal("ok", result);
    }

    [Fact]
    public async Task Handle_RequiresAdminOrHR_UserHasNoMatchingRole_ShouldThrowUnauthorized()
    {
      var behavior = BuildBehavior<AdminOrHrRequest>(AuthenticatedUser(role: "Employee"));

      await Assert.ThrowsAsync<UnauthorizedAccessException>(
          () => behavior.Handle(new AdminOrHrRequest(), Next(), CancellationToken.None));
    }
  }
}
