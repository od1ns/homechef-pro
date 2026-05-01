using FluentValidation;
using HomeChefPro.Application.Abstractions;
using HomeChefPro.Application.Auth.Abstractions;
using HomeChefPro.Application.Common.Exceptions;
using MediatR;

namespace HomeChefPro.Application.Auth.Commands.ChangePassword;

public sealed record ChangePasswordCommand(string CurrentPassword, string NewPassword) : IRequest;

public sealed class ChangePasswordValidator : AbstractValidator<ChangePasswordCommand>
{
    public ChangePasswordValidator()
    {
        RuleFor(x => x.CurrentPassword).NotEmpty();
        RuleFor(x => x.NewPassword).NotEmpty().MinimumLength(8).MaximumLength(128)
            .NotEqual(x => x.CurrentPassword)
            .WithMessage("New password must differ from current password.");
    }
}

public sealed class ChangePasswordHandler(
    IIdentityService identity,
    ICurrentUser currentUser)
    : IRequestHandler<ChangePasswordCommand>
{
    public async Task Handle(ChangePasswordCommand request, CancellationToken ct)
    {
        var userId = currentUser.RequireUserId();
        var op = await identity.SetPasswordAsync(userId, request.CurrentPassword, request.NewPassword, ct)
            .ConfigureAwait(false);
        if (!op.Succeeded)
            throw new Common.Exceptions.ValidationException(
                op.Errors.Select(e => new FluentValidation.Results.ValidationFailure("Password", e)));
    }
}
