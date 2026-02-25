using Employee.Application.Common.Exceptions;
using Employee.Application.Common.Interfaces;
using MediatR;

namespace Employee.Application.Features.Auth.Commands.CreateRole
{
    public class CreateRoleCommandHandler : IRequestHandler<CreateRoleCommand, string>
    {
    private readonly IIdentityService _identityService;

    public CreateRoleCommandHandler(IIdentityService identityService)
        {
      _identityService = identityService;
        }

        public async Task<string> Handle(CreateRoleCommand request, CancellationToken cancellationToken)
        {
      var result = await _identityService.CreateRoleAsync(request.RoleName);

      if (!result.Succeeded)
      {
        throw new ValidationException(string.Join(", ", result.Errors));
      }

      return request.RoleName; // Or return something else if needed, but the original returned Id.
                               // Since I don't return Id from CreateRoleAsync in IIdentityService yet, I'll just return RoleName for now or update IIdentityService.
    }
    }
}
